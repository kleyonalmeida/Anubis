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
        var client = _httpClientFactory.CreateClient("TechClient");

        HttpResponseMessage? response = null;
        string targetUrl = $"https://{target.Value}";
        string lastError = string.Empty;

        try
        {
            // Tentativa 1: HTTPS
            try
            {
                using var ctsHttps = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                ctsHttps.CancelAfter(TimeSpan.FromSeconds(15));

                using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctsHttps.Token);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = ex is OperationCanceledException or TaskCanceledException
                    ? "Connection timed out (15s elapsed on HTTPS)."
                    : ex.Message;
                _logger.LogWarning("HTTPS request failed for {Target}, trying HTTP fallback. Error: {Message}", target.Value, lastError);
            }

            if (response == null)
            {
                var httpUrl = $"http://{target.Value}";
                try
                {
                    using var ctsHttp = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    ctsHttp.CancelAfter(TimeSpan.FromSeconds(15));

                    using var request = new HttpRequestMessage(HttpMethod.Get, httpUrl);
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctsHttp.Token);
                    if (response != null)
                    {
                        targetUrl = httpUrl;
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastError = ex is OperationCanceledException or TaskCanceledException
                        ? "Connection timed out (15s elapsed on HTTP)."
                        : ex.Message;
                    _logger.LogWarning("HTTP request failed for {Target}. Error: {Message}", target.Value, lastError);
                }
            }

            if (response == null)
            {
                return new TechFingerprintResult(
                    TargetUrl: targetUrl,
                    TechnologiesDetected: new List<string>(),
                    ErrorMessage: string.IsNullOrWhiteSpace(lastError) ? "Failed to connect via HTTPS and HTTP." : lastError
                );
            }

            using (response)
            {
                string html = string.Empty;
                try
                {
                    using var ctsRead = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    ctsRead.CancelAfter(TimeSpan.FromSeconds(10));
                    html = await response.Content.ReadAsStringAsync(ctsRead.Token);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Failed to read HTML body content for {Target}: {Message}", targetUrl, ex.Message);
                }

                var htmlSpan = html.AsSpan();
                var detected = new List<string>();

                var headers = response.Headers;
                var contentHeaders = response.Content.Headers;

                string serverHeader = headers.TryGetValues("Server", out var sv) ? string.Join(" ", sv) : string.Empty;
                string xPoweredByHeader = headers.TryGetValues("X-Powered-By", out var xp) ? string.Join(" ", xp) : string.Empty;
                string contentTypeHeader = contentHeaders.ContentType?.ToString() ?? string.Empty;
                var setCookieHeaders = headers.TryGetValues("Set-Cookie", out var cookies) ? cookies.ToList() : new List<string>();

                if (!string.IsNullOrEmpty(serverHeader))
                {
                    detected.Add($"Server: {serverHeader}");
                }
                if (!string.IsNullOrEmpty(xPoweredByHeader))
                {
                    detected.Add($"PoweredBy: {xPoweredByHeader}");
                }
                if (!string.IsNullOrEmpty(contentTypeHeader))
                {
                    detected.Add($"ContentType: {contentTypeHeader}");
                }

                foreach (var kvp in TechFingerprintDictionary.Signatures)
                {
                    var techName = kvp.Key;
                    var signatures = kvp.Value;

                    // Pular servidores ou poweredby já incluídos pelos cabeçalhos para evitar duplicatas
                    if (techName is "Apache" or "Nginx" or "IIS" or "PHP" or "ASP.NET" or "Node.js/Express")
                    {
                        continue;
                    }

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
                            break;
                        }
                    }
                }

                string finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? targetUrl;

                return new TechFingerprintResult(
                    TargetUrl: finalUrl,
                    TechnologiesDetected: detected,
                    StatusCode: (int)response.StatusCode,
                    ServerHeader: serverHeader,
                    HtmlLength: html.Length
                );
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error fingerprinting {Url}", targetUrl);
            return new TechFingerprintResult(
                TargetUrl: targetUrl,
                TechnologiesDetected: new List<string>(),
                ErrorMessage: ex.Message
            );
        }
    }
}
