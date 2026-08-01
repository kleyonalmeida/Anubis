using System.Threading;
using System.Threading.Tasks;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface ITcpConnector
{
    Task<bool> IsPortOpenAsync(IpAddressValue ip, PortNumber port, CancellationToken cancellationToken = default);
}
