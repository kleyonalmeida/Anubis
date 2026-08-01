using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Infrastructure.Network.Models;

public class NvdResponse
{
    [JsonPropertyName("resultsPerPage")]
    public int ResultsPerPage { get; set; }

    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; }

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("vulnerabilities")]
    public List<NvdVulnerabilityItem>? Vulnerabilities { get; set; }
}

public class NvdVulnerabilityItem
{
    [JsonPropertyName("cve")]
    public NvdCve? Cve { get; set; }
}

public class NvdCve
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("descriptions")]
    public List<NvdDescription>? Descriptions { get; set; }

    [JsonPropertyName("metrics")]
    public NvdMetrics? Metrics { get; set; }
}

public class NvdDescription
{
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class NvdMetrics
{
    [JsonPropertyName("cvssMetricV31")]
    public List<NvdCvssMetric>? CvssMetricV31 { get; set; }

    [JsonPropertyName("cvssMetricV30")]
    public List<NvdCvssMetric>? CvssMetricV30 { get; set; }

    [JsonPropertyName("cvssMetricV2")]
    public List<NvdCvssMetric>? CvssMetricV2 { get; set; }
}

public class NvdCvssMetric
{
    [JsonPropertyName("cvssData")]
    public NvdCvssData? CvssData { get; set; }
}

public class NvdCvssData
{
    [JsonPropertyName("baseScore")]
    public decimal BaseScore { get; set; }

    [JsonPropertyName("baseSeverity")]
    public string? BaseSeverity { get; set; }

    [JsonPropertyName("vectorString")]
    public string? VectorString { get; set; }
}
