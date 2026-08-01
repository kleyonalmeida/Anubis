using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Anubis.Application.Interfaces;
using Anubis.Domain.ValueObjects;
using Anubis.Presentation.CLI.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Anubis.Presentation.CLI.Commands;

public static class PortScanCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("portscan", "Run Hellscan TCP Port Scanner against a target");
        var targetOption = new Option<string>("--target", "Target domain or IP (e.g., example.com)") { IsRequired = true };
        command.AddOption(targetOption);

        command.SetHandler(async (string targetUrl) =>
        {
            var hellscanService = serviceProvider.GetRequiredService<IHellscanService>();
            
            AnubisConsole.PrintBanner();
            AnubisConsole.LogInfo($"Initializing Hellscan against: {targetUrl} ...");

            var domain = DomainName.Create(targetUrl);
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            int openPorts = 0;
            try
            {
                await foreach (var result in hellscanService.ScanPortsAsync(domain, cts.Token))
                {
                    if (result.IsOpen)
                    {
                        AnubisConsole.LogSuccess($"[Port] {result.Port.Value} (Open) - {result.ServiceName}");
                        openPorts++;
                    }
                }

                AnubisConsole.LogInfo($"Hellscan completed. Found {openPorts} open ports.");
            }
            catch (Exception ex)
            {
                AnubisConsole.LogError($"Hellscan failed: {ex.Message}");
            }
        }, targetOption);

        return command;
    }
}
