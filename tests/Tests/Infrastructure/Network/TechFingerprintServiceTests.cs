using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Network;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Tests.Infrastructure.Network;

public class TechFingerprintServiceTests : IDisposable
{
    private readonly WireMockServer _wireMockServer;
    
    public TechFingerprintServiceTests()
    {
        _wireMockServer = WireMockServer.Start();
    }
    
    public void Dispose()
    {
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
    }

    [Fact]
    public async Task ScanAsync_ShouldDetectTechnologiesFromHeadersAndHtml_WhenTargetResponds()
    {
        // Arrange
        var targetDomain = DomainName.Create("tech.example.com");
        
        // Mock a web server that looks like WordPress on Nginx + PHP
        _wireMockServer
            .Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Server", "nginx/1.22.1")
                .WithHeader("X-Powered-By", "PHP/8.1")
                .WithHeader("Set-Cookie", "wordpress_test_cookie=WP Cookie check; path=/")
                .WithBody("<html><head><link rel='stylesheet' href='wp-content/themes/style.css'/></head><body><h1>Welcome</h1></body></html>"));

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        
        // Setup HttpClient with a RedirectHandler (so "https://tech.example.com/" actually hits WireMock localhost)
        mockHttpClientFactory.Setup(f => f.CreateClient("TechClient")).Returns(() => 
        {
            var probeHandler = new RedirectToWireMockHandler(_wireMockServer.Urls[0], new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer() // Para forçar captura dos cookies na engine 
            });
            return new HttpClient(probeHandler);
        });

        var logger = new Mock<ILogger<TechFingerprintService>>();
        var service = new TechFingerprintService(mockHttpClientFactory.Object, logger.Object);

        // Act
        var result = await service.ScanAsync(targetDomain, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.TargetUrl.Should().StartWith("http");
        result.TechnologiesDetected.Should().Contain(t => t.Contains("Server: nginx/1.22.1"), "Header Server match");
        result.TechnologiesDetected.Should().Contain(t => t.Contains("PoweredBy: PHP/8.1"), "Header X-Powered-By match");
        result.TechnologiesDetected.Should().Contain("WordPress", "HTML content match (wp-content)");
    }
}
