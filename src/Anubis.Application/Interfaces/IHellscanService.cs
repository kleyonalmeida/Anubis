using System.Collections.Generic;
using System.Threading;
using Anubis.Domain.Entities;
using Anubis.Domain.ValueObjects;

namespace Anubis.Application.Interfaces;

public interface IHellscanService
{
    IAsyncEnumerable<PortScanResult> ScanPortsAsync(DomainName target, CancellationToken cancellationToken = default);
}
