using System.Text.Json.Serialization;

namespace Anubis.Infrastructure.Network.Models;

public class DnsAnswer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public class DnsResponse
{
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    [JsonPropertyName("Answer")]
    public DnsAnswer[]? Answer { get; set; }
}

public class IpWhoIsConnection
{
    [JsonPropertyName("asn")]
    public int Asn { get; set; }

    [JsonPropertyName("isp")]
    public string Isp { get; set; } = string.Empty;
}

public class IpWhoIsResponse
{
    [JsonPropertyName("connection")]
    public IpWhoIsConnection? Connection { get; set; }
}

[JsonSerializable(typeof(DnsResponse))]
[JsonSerializable(typeof(IpWhoIsResponse))]
public partial class NetworkJsonContext : JsonSerializerContext
{
}
