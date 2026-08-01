using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anubis.Domain.Entities;

namespace Anubis.Application.Interfaces;

public interface ICveLookupService
{
    Task<IReadOnlyList<CveResult>> SearchByProductAsync(string productKeyword, CancellationToken cancellationToken = default);
    Task<CveResult?> SearchByCveIdAsync(string cveId, CancellationToken cancellationToken = default);
}
