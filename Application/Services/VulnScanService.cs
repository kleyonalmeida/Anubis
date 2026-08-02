using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class VulnScanService : IVulnScanService
{
    private readonly IExploitSender _exploitSender;
    private readonly ILogger<VulnScanService> _logger;

    public VulnScanService(IExploitSender exploitSender, ILogger<VulnScanService> logger)
    {
        _exploitSender = exploitSender;
        _logger = logger;
    }

    public async IAsyncEnumerable<VulnerabilityFinding> ScanAsync(DomainName target, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var targetUrl = $"https://{target.Value}";
        var channel = Channel.CreateUnbounded<VulnerabilityFinding>();

        var checks = new List<Task>
        {
            CheckSecurityHeadersAsync(targetUrl, channel.Writer, cancellationToken),
            CheckSqliAsync(targetUrl, channel.Writer, cancellationToken),
            CheckOpenRedirectAsync(targetUrl, channel.Writer, cancellationToken),
            CheckExposedAdminPathsAsync(targetUrl, channel.Writer, cancellationToken),
            CheckReflectedXssAsync(targetUrl, channel.Writer, cancellationToken),
            CheckLfiAsync(targetUrl, channel.Writer, cancellationToken),
            CheckDirectoryListingAsync(targetUrl, channel.Writer, cancellationToken),
            CheckSensitiveFilesAsync(targetUrl, channel.Writer, cancellationToken)
        };

        var runnerTask = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(checks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during VulnScan parallel execution");
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        await foreach (var finding in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return finding;
        }

        await runnerTask;
    }

    private async Task CheckSecurityHeadersAsync(string baseurl, ChannelWriter<VulnerabilityFinding> writer, CancellationToken ct)
    {
        using var response = await _exploitSender.SendExploitAsync(baseurl, ct);
        if (response.StatusCode == 0) return;

        foreach (var header in VulnScanDictionary.SecurityHeaders)
        {
            if (!response.HeadersKeys.Contains(header, StringComparer.OrdinalIgnoreCase))
            {
                await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.Medium, $"Missing header: {header}", baseurl), ct);
            }
        }
    }

    private async Task CheckSqliAsync(string baseurl, ChannelWriter<VulnerabilityFinding> writer, CancellationToken ct)
    {
        var testUrl = baseurl.TrimEnd('/') + "/?id=";
        
        foreach (var payload in VulnScanDictionary.SqliPayloads)
        {
            var url = testUrl + Uri.EscapeDataString(payload);
            using var response = await _exploitSender.SendExploitAsync(url, ct);
            
            if (response.BytesRead > 0)
            {
                var span = new ReadOnlySpan<char>(response.Buffer, 0, response.BytesRead);
                
                foreach (var err in VulnScanDictionary.SqliErrors)
                {
                    if (span.Contains(err.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.High, $"SQLi: {payload}", url), ct);
                        break;
                    }
                }
            }
        }
    }

    private async Task CheckOpenRedirectAsync(string baseurl, ChannelWriter<VulnerabilityFinding> writer, CancellationToken ct)
    {
        var testUrl = baseurl.TrimEnd('/') + "/?";
        
        foreach (var param in VulnScanDictionary.OpenRedirectParams)
        {
            var url = testUrl + param + "=https://evil.com";
            using var response = await _exploitSender.SendExploitNoRedirectAsync(url, ct);
            
            if (!string.IsNullOrEmpty(response.LocationHeader) && response.LocationHeader.Contains("evil.com", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.High, $"Open redirect: ?{param}", url), ct);
            }
        }
    }

    private async Task CheckExposedAdminPathsAsync(string baseurl, ChannelWriter<VulnerabilityFinding> writer, CancellationToken ct)
    {
        foreach (var path in VulnScanDictionary.AdminPaths)
        {
            var url = baseurl.TrimEnd('/') + path;
            using var response = await _exploitSender.SendExploitAsync(url, ct);
            
            if (response.StatusCode == 200 || response.StatusCode == 401 || response.StatusCode == 403)
            {
                await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.Medium, $"Exposed Admin/Sensitive Path: {path} (HTTP {response.StatusCode})", url), ct);
            }
        }
    }

    private async Task CheckReflectedXssAsync(string baseurl, ChannelWriter<VulnerabilityFinding> writer, CancellationToken ct)
    {
        var testUrl = baseurl.TrimEnd('/') + "/?q=";
        
        foreach (var payload in VulnScanDictionary.XssPayloads)
        {
            var url = testUrl + Uri.EscapeDataString(payload); 
            using var response = await _exploitSender.SendExploitAsync(url, ct);
            
            if (response.BytesRead > 0)
            {
                var span = new ReadOnlySpan<char>(response.Buffer, 0, response.BytesRead);
                
                if (span.Contains(payload.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.High, $"XSS: {payload}", url), ct);
                    break;
                }
            }
        }
    }

    private async Task CheckLfiAsync(string baseurl, ChannelWriter<VulnerabilityFinding> writer, CancellationToken ct)
    {
        var testUrl = baseurl.TrimEnd('/') + "/?file=";
        
        foreach (var payload in VulnScanDictionary.LfiPayloads)
        {
            var url = testUrl + Uri.EscapeDataString(payload);
            using var response = await _exploitSender.SendExploitAsync(url, ct);
            
            if (response.BytesRead > 0)
            {
                var span = new ReadOnlySpan<char>(response.Buffer, 0, response.BytesRead);
                
                foreach (var sig in VulnScanDictionary.LfiSignatures)
                {
                    if (span.Contains(sig.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.High, $"LFI: {payload}", url), ct);
                        break;
                    }
                }
            }
        }
    }

    private async Task CheckDirectoryListingAsync(string baseurl, ChannelWriter<VulnerabilityFinding> writer, CancellationToken ct)
    {
        foreach (var path in VulnScanDictionary.DirectoryPaths)
        {
            var url = baseurl.TrimEnd('/') + path;
            using var response = await _exploitSender.SendExploitAsync(url, ct);
            
            if (response.StatusCode == 200 && response.BytesRead > 0)
            {
                var span = new ReadOnlySpan<char>(response.Buffer, 0, response.BytesRead);
                foreach (var sig in VulnScanDictionary.DirectorySignatures)
                {
                    if (span.Contains(sig.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.Medium, $"Directory listing: {path}", url), ct);
                        break;
                    }
                }
            }
        }
    }

    private async Task CheckSensitiveFilesAsync(string baseurl, ChannelWriter<VulnerabilityFinding> writer, CancellationToken ct)
    {
        // For sensitive files, run checks in parallel bounded by MaxDegreeOfParallelism
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct };
        
        await Parallel.ForEachAsync(VulnScanDictionary.SensitivePaths, parallelOptions, async (path, cancellation) =>
        {
            var url = baseurl.TrimEnd('/') + path;
            using var response = await _exploitSender.SendExploitAsync(url, cancellation);
            
            if (response.StatusCode == 200 && response.BytesRead > 10)
            {
                var span = new ReadOnlySpan<char>(response.Buffer, 0, response.BytesRead);
                bool isCritical = false;
                
                foreach (var ind in VulnScanDictionary.SensitiveIndicators)
                {
                    if (span.Contains(ind.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        isCritical = true;
                        break;
                    }
                }

                if (isCritical)
                {
                    await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.Critical, $"Sensitive file exposed: {path}", url), cancellation);
                }
                else
                {
                    await writer.WriteAsync(new VulnerabilityFinding(VulnerabilitySeverity.Medium, $"File accessible: {path}", url), cancellation);
                }
            }
        });
    }
}
