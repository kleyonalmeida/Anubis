using System.Threading;
using System.Threading.Tasks;
using Anubis.Domain.Entities;
using Anubis.Domain.ValueObjects;

namespace Anubis.Application.Interfaces;

public interface ITechFingerprintService
{
    Task<TechFingerprintResult?> ScanAsync(DomainName target, CancellationToken cancellationToken = default);
}
