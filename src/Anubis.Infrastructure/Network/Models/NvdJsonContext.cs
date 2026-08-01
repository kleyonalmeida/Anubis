using System.Text.Json.Serialization;

namespace Anubis.Infrastructure.Network.Models;

[JsonSerializable(typeof(NvdResponse))]
[JsonSerializable(typeof(NvdVulnerabilityItem))]
[JsonSerializable(typeof(NvdCve))]
[JsonSerializable(typeof(NvdDescription))]
[JsonSerializable(typeof(NvdMetrics))]
[JsonSerializable(typeof(NvdCvssMetric))]
[JsonSerializable(typeof(NvdCvssData))]
public partial class NvdJsonContext : JsonSerializerContext
{
}
