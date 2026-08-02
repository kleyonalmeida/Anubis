using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.ValueObjects;
using Presentation.CLI.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.CLI.Commands;

public static class SubdomainCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("subdomain", "Run OSINT Subdomain Finder against a target");
        var targetOption = new Option<string>("--target", "Target domain (e.g., example.com)") { IsRequired = true };
        var wordlistOption = new Option<string?>("--wordlist", "Path to custom wordlist file (optional)");

        command.AddOption(targetOption);
        command.AddOption(wordlistOption);

        command.SetHandler(async (string targetUrl, string? wordlistPath) =>
        {
            var subdomainService = serviceProvider.GetRequiredService<ISubdomainFinderService>();
            
            var domain = DomainName.Create(targetUrl);
            
            AnubisConsole.PrintBanner();
            AnubisConsole.LogInfo($"Initializing Subdomain Recon against: {domain.Value} ...");

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            IEnumerable<string> wordlist;
            if (!string.IsNullOrWhiteSpace(wordlistPath) && System.IO.File.Exists(wordlistPath))
            {
                wordlist = await System.IO.File.ReadAllLinesAsync(wordlistPath, cts.Token);
            }
            else
            {
                wordlist = new[] { 
                    "www", "mail", "dev", "staging", "api", "test", "admin", "dashboard", 
                    "app", "portal", "cloud", "auth", "vpn", "blog", "shop", "status", "core", "corp" 
                };
            }

            var foundSubdomains = new System.Collections.Generic.List<Domain.Entities.SubdomainResult>();
            try
            {
                await foreach (var subdomain in subdomainService.FindSubdomainsAsync(domain, wordlist, cts.Token))
                {
                    foundSubdomains.Add(subdomain);
                    var statusStr = subdomain.StatusCode > 0 ? $"HTTP {subdomain.StatusCode}" : "No Response";
                    AnubisConsole.LogSuccess($"[Subdomain] {subdomain.Host.Value} ({subdomain.Ip.Value}) [{statusStr}] - Title: {subdomain.Title}");
                }

                AnubisConsole.LogInfo($"\nSubdomain Recon completed. Found {foundSubdomains.Count} subdomains.");

                if (foundSubdomains.Count > 0)
                {
                    Console.WriteLine("\n========================================================");
                    Console.WriteLine("          DISCOVERED SUBDOMAINS SUMMARY                 ");
                    Console.WriteLine("========================================================");
                    foreach (var item in foundSubdomains)
                    {
                        var httpInfo = item.StatusCode > 0 ? $"[HTTP {item.StatusCode}]" : "[Offline/No HTTP]";
                        Console.WriteLine($"  • {item.Host.Value,-35} | {item.Ip.Value,-15} | {httpInfo}");
                    }
                    Console.WriteLine("========================================================\n");
                }
            }
            catch (Exception ex)
            {
                AnubisConsole.LogError($"Subdomain Recon failed: {ex.Message}");
            }
        }, targetOption, wordlistOption);

        return command;
    }
}
