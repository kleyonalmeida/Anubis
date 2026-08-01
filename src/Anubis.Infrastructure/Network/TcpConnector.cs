using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Anubis.Application.Interfaces;
using Anubis.Domain.ValueObjects;

namespace Anubis.Infrastructure.Network;

public class TcpConnector : ITcpConnector
{
    private readonly TimeSpan _timeout;

    public TcpConnector(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(1); // 1 second timeout exactly like legacy Python
    }

    public async Task<bool> IsPortOpenAsync(IpAddressValue ip, PortNumber port, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        try
        {
            await socket.ConnectAsync(ip.Value, port.Value, cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            // Timeout or externally cancelled
            return false;
        }
        catch (SocketException)
        {
            // Connection refused or unreachable
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
