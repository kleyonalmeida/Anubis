using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Anubis.Application.Interfaces;
using Anubis.Domain.ValueObjects;
using Anubis.Presentation.CLI.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Anubis.Presentation.CLI.Commands;

public static class VulnScanCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("vulnscan", "Run Offensive Security VulnScan (SQLi, XSS, LFI) against a target");
        var targetOption = new Option<string>("--target", "Target domain (e.g., example.com)") { IsRequired = true };
        command.AddOption(targetOption);

        // AOT-Safe Handler: We extract dependencies via IServiceProvider instead of relying on System.CommandLine's generic injection
        command.SetHandler(async (string targetUrl) =>
        {
            var vulnScanService = serviceProvider.GetRequiredService<IVulnScanService>();
            
            AnubisConsole.PrintBanner();
            AnubisConsole.LogInfo($"Initializing VulnScan against: {targetUrl} ...");

            var domain = DomainName.Create(targetUrl);
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            int findingsCount = 0;
            try
            {
                await foreach (var finding in vulnScanService.ScanAsync(domain, cts.Token))
                {
                    AnubisConsole.PrintVulnerability(finding.Description, finding.Severity);
                    findingsCount++;
                }

                AnubisConsole.LogInfo($"VulnScan completed. Found {findingsCount} vulnerabilities.");
            }
            catch (Exception ex)
            {
                AnubisConsole.LogError($"VulnScan failed: {ex.Message}");
            }
        }, targetOption);

        return command;
    }
}
