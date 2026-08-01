using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Domain.ValueObjects;
using Infrastructure.Network;
using Infrastructure.Network.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Tests.Infrastructure.Network;

public class SubdomainFinderServiceTests : IDisposable
{
    private readonly WireMockServer _wireMockServer;
    
    public SubdomainFinderServiceTests()
    {
        _wireMockServer = WireMockServer.Start();
    }
    
    public void Dispose()
    {
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
    }

    [Fact]
    public async Task FindSubdomainsAsync_ShouldReturnActiveSubdomains_WhenDnsAndHttpResolve()
    {
        // Arrange
        var targetDomain = DomainName.Create("example.com");
        var wordlist = new[] { "www", "dev", "api" };
        
        // Mock Google DNS over HTTPS response for 'api.example.com'
        _wireMockServer
            .Given(Request.Create().WithPath("/resolve").WithParam("name", "api.example.com").WithParam("type", "A"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"Status\": 0, \"Answer\": [{\"name\": \"api.example.com\", \"type\": 1, \"TTL\": 300, \"data\": \"10.0.0.1\"}]}"));

        // Mock Google DNS for others to return no answers
        _wireMockServer
            .Given(Request.Create().WithPath("/resolve").WithParam("name", "www.example.com").WithParam("type", "A"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{\"Status\": 3}")); // NXDOMAIN

        _wireMockServer
            .Given(Request.Create().WithPath("/resolve").WithParam("name", "dev.example.com").WithParam("type", "A"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{\"Status\": 3}")); 

        // Mock HTTP probe for api.example.com
        _wireMockServer
            .Given(Request.Create().WithPath("/").WithHeader("Host", "api.example.com"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Server", "nginx/1.24.0")
                .WithHeader("X-Powered-By", "ASP.NET")
                .WithBody("<html><title>API Dashboard</title></html>"));

        // Mock IPWhoIs
        _wireMockServer
            .Given(Request.Create().WithPath("/10.0.0.1"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"connection\": {\"asn\": 12345, \"isp\": \"Test ISP\"}}"));

        // Setup HttpClientFactory to return a NEW instance each time (to simulate real behavior and allow BaseAddress setting)
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        
        mockHttpClientFactory.Setup(f => f.CreateClient("DnsClient")).Returns(() => new HttpClient());
        
        mockHttpClientFactory.Setup(f => f.CreateClient("ProbeClient")).Returns(() => 
        {
            var probeHandler = new RedirectToWireMockHandler(_wireMockServer.Urls[0], new HttpClientHandler());
            return new HttpClient(probeHandler);
        });
        
        mockHttpClientFactory.Setup(f => f.CreateClient("IpWhoIsClient")).Returns(() => new HttpClient());

        var options = Options.Create(new SubdomainFinderOptions 
        { 
            DnsBaseUrl = _wireMockServer.Urls[0],
            IpWhoIsBaseUrl = _wireMockServer.Urls[0],
            MaxDegreeOfParallelism = 2
        });

        var logger = new Mock<ILogger<SubdomainFinderService>>();

        var service = new SubdomainFinderService(mockHttpClientFactory.Object, options, logger.Object);

        // Act
        var results = new List<Domain.Entities.SubdomainResult>();
        await foreach (var result in service.FindSubdomainsAsync(targetDomain, wordlist, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1);
        var found = results.First();
        
        found.Host.Value.Should().Be("api.example.com");
        found.Ip.Value.Should().Be("10.0.0.1");
        found.StatusCode.Should().Be(200);
        
        // wiremock handles the probe directly
        found.Title.Should().Be("API Dashboard");
        found.Tech.Should().Be("nginx/1.24.0 ASP.NET");
        found.Asn.Should().Be("12345 / Test ISP");
    }
}

public class RedirectToWireMockHandler : DelegatingHandler
{
    private readonly Uri _wireMockUri;

    public RedirectToWireMockHandler(string wireMockUrl, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _wireMockUri = new Uri(wireMockUrl);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Host = request.RequestUri.Host;
        var builder = new UriBuilder(request.RequestUri);
        builder.Host = _wireMockUri.Host;
        builder.Port = _wireMockUri.Port;
        builder.Scheme = _wireMockUri.Scheme;
        request.RequestUri = builder.Uri;
        return base.SendAsync(request, cancellationToken);
    }
}
