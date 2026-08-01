using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Anubis.Application.Interfaces;
using Anubis.Domain.ValueObjects;

namespace Anubis.Infrastructure.Network;

public class DnsResolver : IDnsResolver
{
    public async Task<IpAddressValue?> ResolveAsync(DomainName domain, CancellationToken cancellationToken = default)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain.Value, cancellationToken);
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            
            if (ipv4 != null)
            {
                return IpAddressValue.Create(ipv4.ToString());
            }
            
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
