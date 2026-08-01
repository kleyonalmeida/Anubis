using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Application.Services;

public class HellscanServiceTests
{
    private readonly Mock<IDnsResolver> _mockDnsResolver;
    private readonly Mock<ITcpConnector> _mockTcpConnector;
    private readonly Mock<ILogger<HellscanService>> _mockLogger;
    
    public HellscanServiceTests()
    {
        _mockDnsResolver = new Mock<IDnsResolver>();
        _mockTcpConnector = new Mock<ITcpConnector>();
        _mockLogger = new Mock<ILogger<HellscanService>>();
    }

    [Fact]
    public async Task ScanPortsAsync_ShouldReturnOnlyOpenPorts_WhenDnsResolvesSuccessfully()
    {
        // Arrange
        var targetDomain = DomainName.Create("scan.example.com");
        var resolvedIp = IpAddressValue.Create("10.0.0.1");

        _mockDnsResolver
            .Setup(r => r.ResolveAsync(targetDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedIp);

        // Simulando que as portas 80 e 443 estao abertas, e todas as outras fechadas (time-out ou refused)
        _mockTcpConnector
            .Setup(c => c.IsPortOpenAsync(resolvedIp, It.IsAny<PortNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpAddressValue ip, PortNumber port, CancellationToken ct) => 
                port.Value == 80 || port.Value == 443);

        var service = new HellscanService(_mockDnsResolver.Object, _mockTcpConnector.Object, _mockLogger.Object);

        // Act
        var openPorts = new List<PortScanResult>();
        await foreach (var result in service.ScanPortsAsync(targetDomain, CancellationToken.None))
        {
            openPorts.Add(result);
        }

        // Assert
        openPorts.Should().HaveCount(2);
        
        var port80 = openPorts.Single(p => p.Port.Value == 80);
        port80.ServiceName.Should().Be("HTTP");
        port80.IsOpen.Should().BeTrue();

        var port443 = openPorts.Single(p => p.Port.Value == 443);
        port443.ServiceName.Should().Be("HTTPS");
        port443.IsOpen.Should().BeTrue();

        // Verifica se o DnsResolver foi chamado exatamente 1 vez
        _mockDnsResolver.Verify(r => r.ResolveAsync(targetDomain, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verifica se o TcpConnector tentou se conectar a mais de 2 portas (todas do dicionario legadas)
        _mockTcpConnector.Verify(c => c.IsPortOpenAsync(resolvedIp, It.IsAny<PortNumber>(), It.IsAny<CancellationToken>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task ScanPortsAsync_ShouldYieldEmpty_WhenDnsFailsToResolve()
    {
        // Arrange
        var targetDomain = DomainName.Create("unknown.example.com");

        _mockDnsResolver
            .Setup(r => r.ResolveAsync(targetDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpAddressValue?)null);

        var service = new HellscanService(_mockDnsResolver.Object, _mockTcpConnector.Object, _mockLogger.Object);

        // Act
        var openPorts = new List<PortScanResult>();
        await foreach (var result in service.ScanPortsAsync(targetDomain, CancellationToken.None))
        {
            openPorts.Add(result);
        }

        // Assert
        openPorts.Should().BeEmpty();
        _mockTcpConnector.Verify(c => c.IsPortOpenAsync(It.IsAny<IpAddressValue>(), It.IsAny<PortNumber>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
