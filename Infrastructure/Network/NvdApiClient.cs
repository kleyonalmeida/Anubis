using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Network.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Network;

public class NvdApiClient : INvdApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NvdApiClient> _logger;
    private const string BaseUrl = "https://services.nvd.nist.gov/rest/json/cves/2.0";

    public NvdApiClient(IHttpClientFactory httpClientFactory, ILogger<NvdApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CveResult>> GetVulnerabilitiesByKeywordAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}?keywordSearch={Uri.EscapeDataString(keyword)}&resultsPerPage=10";
        var results = await SendAndParseAsync(url, cancellationToken);

        if (results.Count > 0) return results;

        // Se a busca com string completa (ex: "nginx 1.24.0") não retornar resultados,
        // extrair a palavra base/nome do produto (ex: "nginx") e tentar novamente na API NVD.
        var spaceIndex = keyword.Trim().IndexOf(' ');
        if (spaceIndex > 0)
        {
            var baseProduct = keyword.Substring(0, spaceIndex).Trim();
            if (!string.IsNullOrWhiteSpace(baseProduct) && baseProduct.Length > 2)
            {
                _logger.LogInformation("No direct results for '{Keyword}'. Trying fallback search for product: '{BaseProduct}'", keyword, baseProduct);
                var fallbackUrl = $"{BaseUrl}?keywordSearch={Uri.EscapeDataString(baseProduct)}&resultsPerPage=10";
                return await SendAndParseAsync(fallbackUrl, cancellationToken);
            }
        }

        return results;
    }

    public async Task<CveResult?> GetVulnerabilityByIdAsync(string cveId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}?cveId={Uri.EscapeDataString(cveId)}";
        var results = await SendAndParseAsync(url, cancellationToken);
        return results.Count > 0 ? results[0] : null;
    }

    private async Task<IReadOnlyList<CveResult>> SendAndParseAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("NvdClient");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Anubis-Sec/1.0");

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NVD API returned non-success status code: {StatusCode}", response.StatusCode);
                return Array.Empty<CveResult>();
            }

            using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token);
            
            // Native AOT Deserialization using Source Generators
            var nvdResponse = await JsonSerializer.DeserializeAsync(
                contentStream, 
                NvdJsonContext.Default.NvdResponse, 
                cts.Token
            );

            if (nvdResponse?.Vulnerabilities == null || nvdResponse.Vulnerabilities.Count == 0)
                return Array.Empty<CveResult>();

            var results = new List<CveResult>(nvdResponse.Vulnerabilities.Count);

            foreach (var item in nvdResponse.Vulnerabilities)
            {
                var cve = item.Cve;
                if (cve == null) continue;

                var id = cve.Id ?? "UNKNOWN";
                var desc = cve.Descriptions?.FirstOrDefault()?.Value ?? "N/A";
                
                string baseScore = "N/A";
                string severityStr = "N/A";
                string vectorStr = "N/A";

                var metrics = cve.Metrics;
                NvdCvssData? bestData = null;

                if (metrics?.CvssMetricV31?.Count > 0)
                {
                    bestData = metrics.CvssMetricV31[0].CvssData;
                }
                else if (metrics?.CvssMetricV30?.Count > 0)
                {
                    bestData = metrics.CvssMetricV30[0].CvssData;
                }
                else if (metrics?.CvssMetricV2?.Count > 0)
                {
                    bestData = metrics.CvssMetricV2[0].CvssData;
                }

                if (bestData != null)
                {
                    baseScore = bestData.BaseScore.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    severityStr = bestData.BaseSeverity ?? "N/A";
                    vectorStr = bestData.VectorString ?? "N/A";
                }

                var severity = ParseSeverity(severityStr);
                
                results.Add(new CveResult(id, desc, severity, baseScore, vectorStr));
            }

            return results;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("NVD API request failed: {Message}", ex.Message);
            return Array.Empty<CveResult>();
        }
    }

    private static VulnerabilitySeverity ParseSeverity(string severityStr)
    {
        return severityStr.ToUpperInvariant() switch
        {
            "CRITICAL" => VulnerabilitySeverity.Critical,
            "HIGH" => VulnerabilitySeverity.High,
            "MEDIUM" => VulnerabilitySeverity.Medium,
            _ => VulnerabilitySeverity.Low
        };
    }
}
