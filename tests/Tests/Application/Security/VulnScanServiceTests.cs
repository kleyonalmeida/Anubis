using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Services;
using Domain.ValueObjects;
using Infrastructure.Network;
using Tests.Infrastructure.Network;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Tests.Application.Security;

public class VulnScanServiceTests : IDisposable
{
    private readonly WireMockServer _wireMockServer;
    private readonly Mock<ILogger<VulnScanService>> _mockLogger;

    public VulnScanServiceTests()
    {
        _wireMockServer = WireMockServer.Start();
        _mockLogger = new Mock<ILogger<VulnScanService>>();
    }

    public void Dispose()
    {
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
    }

    [Fact]
    public async Task ScanAsync_ShouldDetectVulnerabilities_WhenServerIsVulnerable()
    {
        // Arrange
        var targetDomain = DomainName.Create("vuln.example.com");

        // 1. Missing Security Headers (Nao enviamos Headers de seguranca no Response.Create)
        _wireMockServer
            .Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("Welcome"));

        // 2. Reflected XSS
        _wireMockServer
            .Given(Request.Create().WithPath("/").WithParam("q", new WireMock.Matchers.WildcardMatcher("<script>*")).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("<html>You searched for: <script>alert(1)</script></html>"));

        // 3. SQLi
        _wireMockServer
            .Given(Request.Create().WithPath("/").WithParam("id", "' OR 1=1--").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("Warning: mysql_fetch_array() expects parameter 1"));

        // 4. LFI
        _wireMockServer
            .Given(Request.Create().WithPath("/").WithParam("file", "../../../../etc/passwd").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("root:x:0:0:root:/root:/bin/bash\ndaemon:x:1:1:daemon"));

        // 5. Exposed Admin Panel
        _wireMockServer
            .Given(Request.Create().WithPath("/admin").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("Admin Login"));

        // 6. Sensitive Files Exposed
        _wireMockServer
            .Given(Request.Create().WithPath("/.git/config").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("[core]\n\trepositoryformatversion = 0"));

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient("VulnScanClient")).Returns(() => 
        {
            var handler = new RedirectToWireMockHandler(_wireMockServer.Urls[0], new HttpClientHandler
            {
                AllowAutoRedirect = false // Pra testar Open Redirect se precisarmos
            });
            return new HttpClient(handler);
        });

        // Neste TDD, usaremos o ExploitSender real mas com o HttpClient mockado apontando para o WireMock
        var exploitSenderLogger = new Mock<ILogger<ExploitSender>>();
        var exploitSender = new ExploitSender(mockHttpClientFactory.Object, exploitSenderLogger.Object);
        var service = new VulnScanService(exploitSender, _mockLogger.Object);

        // Act
        var findings = new List<Domain.Entities.VulnerabilityFinding>();
        await foreach (var finding in service.ScanAsync(targetDomain, CancellationToken.None))
        {
            findings.Add(finding);
        }

        // Assert
        findings.Should().NotBeEmpty();

        var xssFinding = findings.FirstOrDefault(f => f.Description.StartsWith("XSS:"));
        xssFinding.Should().NotBeNull();
        xssFinding.Severity.Should().Be(Domain.Enums.VulnerabilitySeverity.High);

        var sqliFinding = findings.FirstOrDefault(f => f.Description.Contains("SQLi"));
        sqliFinding.Should().NotBeNull();
        sqliFinding.Severity.Should().Be(Domain.Enums.VulnerabilitySeverity.High);

        var lfiFinding = findings.FirstOrDefault(f => f.Description.Contains("LFI"));
        lfiFinding.Should().NotBeNull();
        lfiFinding.Severity.Should().Be(Domain.Enums.VulnerabilitySeverity.High);

        var headerFinding = findings.FirstOrDefault(f => f.Description.Contains("Missing header"));
        headerFinding.Should().NotBeNull();
        headerFinding.Severity.Should().Be(Domain.Enums.VulnerabilitySeverity.Medium);

        var adminFinding = findings.FirstOrDefault(f => f.Description.Contains("Exposed Admin/Sensitive Path: /admin"));
        adminFinding.Should().NotBeNull();
        adminFinding.Severity.Should().Be(Domain.Enums.VulnerabilitySeverity.Medium);

        var gitFinding = findings.FirstOrDefault(f => f.Description.Contains("Sensitive file exposed: /.git/config"));
        gitFinding.Should().NotBeNull();
        gitFinding.Severity.Should().Be(Domain.Enums.VulnerabilitySeverity.Critical);
    }
}
