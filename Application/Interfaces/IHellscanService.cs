using System.Collections.Generic;
using System.Threading;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface IHellscanService
{
    IAsyncEnumerable<PortScanResult> ScanPortsAsync(DomainName target, CancellationToken cancellationToken = default);
}
