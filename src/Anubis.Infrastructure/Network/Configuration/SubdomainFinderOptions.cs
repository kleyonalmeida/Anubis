namespace Anubis.Infrastructure.Network.Configuration;

public class SubdomainFinderOptions
{
    public string DnsBaseUrl { get; set; } = "https://dns.google";
    public string IpWhoIsBaseUrl { get; set; } = "https://ipwho.is";
    public int MaxDegreeOfParallelism { get; set; } = 50;
    public int RequestTimeoutSeconds { get; set; } = 4;
}
