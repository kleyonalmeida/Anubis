using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces;

public interface INvdApiClient
{
    Task<IReadOnlyList<CveResult>> GetVulnerabilitiesByKeywordAsync(string keyword, CancellationToken cancellationToken = default);
    Task<CveResult?> GetVulnerabilityByIdAsync(string cveId, CancellationToken cancellationToken = default);
}
