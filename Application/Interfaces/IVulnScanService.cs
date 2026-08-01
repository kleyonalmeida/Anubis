using System.Collections.Generic;
using System.Threading;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface IVulnScanService
{
    IAsyncEnumerable<VulnerabilityFinding> ScanAsync(DomainName target, CancellationToken cancellationToken = default);
}
