using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Anubis.Application.Interfaces;
using Anubis.Domain.Entities;
using Anubis.Domain.ValueObjects;
using Anubis.Infrastructure.Network.Configuration;
using Anubis.Infrastructure.Network.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Anubis.Infrastructure.Network;

public class SubdomainFinderService : ISubdomainFinderService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SubdomainFinderService> _logger;
    private readonly SubdomainFinderOptions _options;

    public SubdomainFinderService(
        IHttpClientFactory httpClientFactory,
        IOptions<SubdomainFinderOptions> options,
        ILogger<SubdomainFinderService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<SubdomainResult> FindSubdomainsAsync(
        DomainName targetDomain, 
        IEnumerable<string> wordlist, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<SubdomainResult>(new BoundedChannelOptions(_options.MaxDegreeOfParallelism * 2)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        var producerTask = Task.Run(async () =>
        {
            try
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                };

                await Parallel.ForEachAsync(wordlist, parallelOptions, async (word, ct) =>
                {
                    var host = $"{word}.{targetDomain.Value}";
                    var result = await CheckSubdomainAsync(host, ct);
                    if (result != null)
                    {
                        await channel.Writer.WriteAsync(result, ct);
                    }
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error producing subdomains");
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }

        await producerTask;
    }

    private async Task<SubdomainResult?> CheckSubdomainAsync(string host, CancellationToken ct)
    {
        try
        {
            var dnsClient = _httpClientFactory.CreateClient("DnsClient");
            dnsClient.BaseAddress = new Uri(_options.DnsBaseUrl);
            
            var dnsUrl = $"/resolve?name={host}&type=A";
            var dnsResponse = await dnsClient.GetFromJsonAsync(dnsUrl, NetworkJsonContext.Default.DnsResponse, ct);

            if (dnsResponse == null || dnsResponse.Status != 0 || dnsResponse.Answer == null || dnsResponse.Answer.Length == 0)
                return null;

            var ip = dnsResponse.Answer[0].Data;
            
            int statusCode = 0;
            bool httpsOk = false;
            string title = "-";
            string tech = "-";

            var probeClient = _httpClientFactory.CreateClient("ProbeClient");
            var schemes = new[] { "https", "http" };
            
            foreach (var scheme in schemes)
            {
                try
                {
                    var response = await probeClient.GetAsync($"{scheme}://{host}/", HttpCompletionOption.ResponseHeadersRead, ct);
                    statusCode = (int)response.StatusCode;
                    httpsOk = scheme == "https";

                    tech = ExtractTechHeaders(response);
                    title = await ExtractTitleZeroAllocationAsync(response, ct);
                    break;
                }
                catch
                {
                    // Ignore and try the next scheme
                }
            }

            string asn = "-";
            try
            {
                var whoIsClient = _httpClientFactory.CreateClient("IpWhoIsClient");
                whoIsClient.BaseAddress = new Uri(_options.IpWhoIsBaseUrl);
                var geo = await whoIsClient.GetFromJsonAsync($"/{ip}", NetworkJsonContext.Default.IpWhoIsResponse, ct);
                
                if (geo?.Connection != null)
                {
                    var rawAsn = geo.Connection.Asn.ToString();
                    var isp = geo.Connection.Isp;
                    asn = string.IsNullOrEmpty(isp) ? rawAsn : $"{rawAsn} / {isp}";
                }
            }
            catch
            {
                // Ignore whois errors
            }

            return new SubdomainResult(
                DomainName.Create(host),
                IpAddressValue.Create(ip),
                statusCode,
                httpsOk,
                title,
                tech,
                asn
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG ERROR {host}: {ex}");
            _logger.LogError(ex, $"Error checking subdomain {host}");
            return null;
        }
    }

    private string ExtractTechHeaders(HttpResponseMessage response)
    {
        var srv = response.Headers.TryGetValues("Server", out var srvVals) ? string.Join(" ", srvVals) : "";
        var xpb = response.Headers.TryGetValues("X-Powered-By", out var xpbVals) ? string.Join(" ", xpbVals) : "";
        
        var tech = $"{srv} {xpb}".Trim();
        return string.IsNullOrEmpty(tech) ? "-" : tech;
    }

    private async Task<string> ExtractTitleZeroAllocationAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrEmpty(content)) return "-";

        var span = content.AsSpan();
        var startIdx = span.IndexOf("<title", StringComparison.OrdinalIgnoreCase);
        if (startIdx < 0) return "-";

        var sliceFromTitle = span.Slice(startIdx);
        var closeBracket = sliceFromTitle.IndexOf('>');
        if (closeBracket < 0) return "-";

        var titleStart = startIdx + closeBracket + 1;
        var sliceFromTitleStart = span.Slice(titleStart);
        var endIdx = sliceFromTitleStart.IndexOf("</title", StringComparison.OrdinalIgnoreCase);
        
        if (endIdx < 0) return "-";

        var titleSpan = span.Slice(titleStart, endIdx).Trim();
        if (titleSpan.Length > 40)
            titleSpan = titleSpan.Slice(0, 40);

        return titleSpan.ToString();
    }
}
