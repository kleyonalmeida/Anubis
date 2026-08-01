using System.Collections.Generic;
using System.Threading;
using Anubis.Domain.Entities;
using Anubis.Domain.ValueObjects;

namespace Anubis.Application.Interfaces;

public interface IVulnScanService
{
    IAsyncEnumerable<VulnerabilityFinding> ScanAsync(DomainName target, CancellationToken cancellationToken = default);
}
