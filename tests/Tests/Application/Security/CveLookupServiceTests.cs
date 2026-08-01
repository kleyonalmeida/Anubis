using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Network;
using Domain.Entities;
using Domain.Enums;
using Tests.Infrastructure.Network;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using System.Net.Http;

namespace Tests.Application.Security;

public class CveLookupServiceTests : IDisposable
{
    private readonly WireMockServer _wireMockServer;
    private readonly Mock<ILogger<CveLookupService>> _mockLogger;

    public CveLookupServiceTests()
    {
        _wireMockServer = WireMockServer.Start();
        _mockLogger = new Mock<ILogger<CveLookupService>>();
    }

    public void Dispose()
    {
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
    }

    [Fact]
    public async Task SearchByCveIdAsync_ShouldExtractCriticalSeverityAndScore_WithAotJson()
    {
        // Arrange
        var mockJsonPayload = @"
        {
            ""resultsPerPage"": 1,
            ""vulnerabilities"": [
                {
                    ""cve"": {
                        ""id"": ""CVE-2021-44228"",
                        ""descriptions"": [
                            { ""lang"": ""en"", ""value"": ""Log4j RCE Vulnerability"" }
                        ],
                        ""metrics"": {
                            ""cvssMetricV31"": [
                                {
                                    ""cvssData"": {
                                        ""baseScore"": 10.0,
                                        ""baseSeverity"": ""CRITICAL"",
                                        ""vectorString"": ""CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H""
                                    }
                                }
                            ]
                        }
                    }
                }
            ]
        }";

        _wireMockServer
            .Given(Request.Create().WithPath("/rest/json/cves/2.0").WithParam("cveId", "CVE-2021-44228").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(mockJsonPayload).WithHeader("Content-Type", "application/json"));

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient("NvdClient")).Returns(() => 
        {
            var handler = new RedirectToWireMockHandler(_wireMockServer.Urls[0], new HttpClientHandler());
            return new HttpClient(handler);
        });

        var nvdApiClientLogger = new Mock<ILogger<NvdApiClient>>();
        var nvdApiClient = new NvdApiClient(mockHttpClientFactory.Object, nvdApiClientLogger.Object);
        var service = new CveLookupService(nvdApiClient, _mockLogger.Object);

        // Act
        var result = await service.SearchByCveIdAsync("CVE-2021-44228", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Value.CveId.Should().Be("CVE-2021-44228");
        result.Value.Severity.Should().Be(VulnerabilitySeverity.Critical);
        result.Value.BaseScore.Should().Be("10.0");
        result.Value.Description.Should().Be("Log4j RCE Vulnerability");
        result.Value.VectorString.Should().Be("CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H");
    }
}
