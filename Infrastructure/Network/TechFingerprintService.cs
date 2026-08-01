using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Network;

public class TechFingerprintService : ITechFingerprintService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TechFingerprintService> _logger;

    public TechFingerprintService(IHttpClientFactory httpClientFactory, ILogger<TechFingerprintService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TechFingerprintResult?> ScanAsync(DomainName target, CancellationToken cancellationToken = default)
    {
        var targetUrl = $"https://{target.Value}";
        
        try
        {
            var client = _httpClientFactory.CreateClient("TechFingerprintClient");
            client.Timeout = TimeSpan.FromSeconds(8); // Match Python timeout=8

            using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            // Simulate headers from get_headers() in python
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var htmlSpan = html.AsSpan();

            var detected = new List<string>();

            // Parse headers safely avoiding extra string allocations where possible
            var headers = response.Headers;
            
            // Extract common header strings once to avoid doing it for every signature
            string serverHeader = headers.TryGetValues("Server", out var sv) ? string.Join(" ", sv) : string.Empty;
            string xPoweredByHeader = headers.TryGetValues("X-Powered-By", out var xp) ? string.Join(" ", xp) : string.Empty;
            
            // To capture Set-Cookie (sometimes response.Headers contains them, but ideally CookieContainer handles them. 
            // The Python script reads response.cookies. For us, Set-Cookie is in response.Headers or content headers.
            var setCookieHeaders = headers.TryGetValues("Set-Cookie", out var cookies) ? cookies.ToList() : new List<string>();

            foreach (var kvp in TechFingerprintDictionary.Signatures)
            {
                var techName = kvp.Key;
                var signatures = kvp.Value;

                foreach (var sig in signatures)
                {
                    bool matched = sig.Type switch
                    {
                        TechSignatureType.Html => htmlSpan.Contains(sig.Pattern.AsSpan(), StringComparison.OrdinalIgnoreCase),
                        
                        TechSignatureType.HeaderServer => serverHeader.Contains(sig.Pattern, StringComparison.OrdinalIgnoreCase),
                        
                        TechSignatureType.HeaderXPoweredBy => xPoweredByHeader.Contains(sig.Pattern, StringComparison.OrdinalIgnoreCase),
                        
                        TechSignatureType.Header => headers.Contains(sig.Pattern) || headers.TryGetValues(sig.Pattern, out _),
                        
                        TechSignatureType.Cookie => setCookieHeaders.Any(c => c.Contains(sig.Pattern, StringComparison.OrdinalIgnoreCase)),
                        
                        _ => false
                    };

                    if (matched)
                    {
                        detected.Add(techName);
                        break; // Move to next technology
                    }
                }
            }

            return new TechFingerprintResult(targetUrl, detected);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error fingerprinting {Url}", targetUrl);
            return null;
        }
    }
}
