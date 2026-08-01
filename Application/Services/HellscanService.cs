using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class HellscanService : IHellscanService
{
    private readonly IDnsResolver _dnsResolver;
    private readonly ITcpConnector _tcpConnector;
    private readonly ILogger<HellscanService> _logger;

    public HellscanService(IDnsResolver dnsResolver, ITcpConnector tcpConnector, ILogger<HellscanService> logger)
    {
        _dnsResolver = dnsResolver;
        _tcpConnector = tcpConnector;
        _logger = logger;
    }

    public async IAsyncEnumerable<PortScanResult> ScanPortsAsync(
        DomainName target, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var resolvedIp = await _dnsResolver.ResolveAsync(target, cancellationToken);
        
        if (resolvedIp == null)
        {
            _logger.LogWarning("Host {Host} could not be resolved.", target.Value);
            yield break;
        }

        var channel = Channel.CreateBounded<PortScanResult>(new BoundedChannelOptions(100)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        var producerTask = Task.Run(async () =>
        {
            try
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 50,
                    CancellationToken = cancellationToken
                };

                await Parallel.ForEachAsync(HellscanDictionary.Ports, parallelOptions, async (kvp, ct) =>
                {
                    var portNumber = PortNumber.Create(kvp.Key);
                    var isOpen = await _tcpConnector.IsPortOpenAsync(resolvedIp.Value, portNumber, ct);
                    
                    if (isOpen)
                    {
                        var result = new PortScanResult(portNumber, kvp.Value, true);
                        await channel.Writer.WriteAsync(result, ct);
                    }
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error producing scan results for {Host}", target.Value);
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }

        await producerTask;
    }
}
