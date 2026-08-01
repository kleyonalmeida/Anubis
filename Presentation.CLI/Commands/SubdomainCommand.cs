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
        command.AddOption(targetOption);

        command.SetHandler(async (string targetUrl) =>
        {
            var subdomainService = serviceProvider.GetRequiredService<ISubdomainFinderService>();
            
            AnubisConsole.PrintBanner();
            AnubisConsole.LogInfo($"Initializing Subdomain Recon against: {targetUrl} ...");

            var domain = DomainName.Create(targetUrl);
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var defaultWordlist = new[] { "www", "mail", "dev", "staging", "api", "test" };
            int findingsCount = 0;
            try
            {
                await foreach (var subdomain in subdomainService.FindSubdomainsAsync(domain, defaultWordlist, cts.Token))
                {
                    AnubisConsole.LogSuccess($"[Subdomain] {subdomain}");
                    findingsCount++;
                }

                AnubisConsole.LogInfo($"Subdomain Recon completed. Found {findingsCount} subdomains.");
            }
            catch (Exception ex)
            {
                AnubisConsole.LogError($"Subdomain Recon failed: {ex.Message}");
            }
        }, targetOption);

        return command;
    }
}
