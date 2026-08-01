using System.Threading;
using System.Threading.Tasks;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface IDnsResolver
{
    Task<IpAddressValue?> ResolveAsync(DomainName domain, CancellationToken cancellationToken = default);
}
