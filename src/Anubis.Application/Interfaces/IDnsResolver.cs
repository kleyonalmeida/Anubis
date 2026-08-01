using System.Threading;
using System.Threading.Tasks;
using Anubis.Domain.ValueObjects;

namespace Anubis.Application.Interfaces;

public interface IDnsResolver
{
    Task<IpAddressValue?> ResolveAsync(DomainName domain, CancellationToken cancellationToken = default);
}
