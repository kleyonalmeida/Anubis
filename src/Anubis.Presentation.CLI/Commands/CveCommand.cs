using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Anubis.Application.Interfaces;
using Anubis.Presentation.CLI.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Anubis.Presentation.CLI.Commands;

public static class CveCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("cve", "Query the National Vulnerability Database (NVD) via REST API");
        
        var idOption = new Option<string>("--id", "Search for a specific CVE ID (e.g., CVE-2021-44228)");
        var productOption = new Option<string>("--product", "Search CVEs by product keyword (e.g., apache 2.4.49)");
        
        command.AddOption(idOption);
        command.AddOption(productOption);

        command.SetHandler(async (string? id, string? product) =>
        {
            var cveService = serviceProvider.GetRequiredService<ICveLookupService>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            AnubisConsole.PrintBanner();

            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    AnubisConsole.LogInfo($"Querying NVD for {id} ...");
                    var result = await cveService.SearchByCveIdAsync(id, cts.Token);
                    if (result.HasValue)
                    {
                        AnubisConsole.PrintVulnerability($"[{result.Value.CveId}] Base Score: {result.Value.BaseScore} | {result.Value.VectorString}\n{result.Value.Description}", result.Value.Severity);
                    }
                    else
                    {
                        AnubisConsole.LogWarning($"CVE {id} not found.");
                    }
                }
                else if (!string.IsNullOrEmpty(product))
                {
                    AnubisConsole.LogInfo($"Querying NVD for keyword: '{product}' ...");
                    var results = await cveService.SearchByProductAsync(product, cts.Token);
                    
                    if (results.Count == 0)
                    {
                        AnubisConsole.LogWarning("No CVEs found for product.");
                    }
                    else
                    {
                        foreach (var cve in results)
                        {
                            AnubisConsole.PrintVulnerability($"[{cve.CveId}] Base Score: {cve.BaseScore} | {cve.VectorString}\n{cve.Description}\n", cve.Severity);
                        }
                    }
                }
                else
                {
                    AnubisConsole.LogError("You must specify either --id or --product.");
                }
            }
            catch (Exception ex)
            {
                AnubisConsole.LogError($"CVE lookup failed: {ex.Message}");
            }
        }, idOption, productOption);

        return command;
    }
}
