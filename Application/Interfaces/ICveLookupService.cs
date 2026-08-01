using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces;

public interface ICveLookupService
{
    Task<IReadOnlyList<CveResult>> SearchByProductAsync(string productKeyword, CancellationToken cancellationToken = default);
    Task<CveResult?> SearchByCveIdAsync(string cveId, CancellationToken cancellationToken = default);
}
