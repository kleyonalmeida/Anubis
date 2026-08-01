using System.Threading;
using System.Threading.Tasks;
using Anubis.Domain.ValueObjects;

namespace Anubis.Application.Interfaces;

public interface ITcpConnector
{
    Task<bool> IsPortOpenAsync(IpAddressValue ip, PortNumber port, CancellationToken cancellationToken = default);
}
