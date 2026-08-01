using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anubis.Domain.Entities;

namespace Anubis.Application.Interfaces;

public interface INvdApiClient
{
    Task<IReadOnlyList<CveResult>> GetVulnerabilitiesByKeywordAsync(string keyword, CancellationToken cancellationToken = default);
    Task<CveResult?> GetVulnerabilityByIdAsync(string cveId, CancellationToken cancellationToken = default);
}
