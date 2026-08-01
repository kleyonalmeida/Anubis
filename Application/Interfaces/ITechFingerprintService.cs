using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface ITechFingerprintService
{
    Task<TechFingerprintResult?> ScanAsync(DomainName target, CancellationToken cancellationToken = default);
}
