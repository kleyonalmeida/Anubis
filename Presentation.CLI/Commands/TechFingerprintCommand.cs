using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.ValueObjects;
using Presentation.CLI.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.CLI.Commands;

public static class TechFingerprintCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("tech", "Run OSINT Tech Fingerprinting against a target");
        var targetOption = new Option<string>("--target", "Target domain (e.g., example.com)") { IsRequired = true };
        command.AddOption(targetOption);

        command.SetHandler(async (string targetUrl) =>
        {
            var techService = serviceProvider.GetRequiredService<ITechFingerprintService>();
            
            AnubisConsole.PrintBanner();
            AnubisConsole.LogInfo($"Initializing Tech Fingerprint against: {targetUrl} ...");

            var domain = DomainName.Create(targetUrl);
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            try
            {
                var techResult = await techService.ScanAsync(domain, cts.Token);
                
                if (techResult != null)
                {
                    AnubisConsole.LogInfo($"Target: {techResult.TargetUrl}");

                    if (techResult.TechnologiesDetected.Count > 0)
                    {
                        foreach (var tech in techResult.TechnologiesDetected)
                        {
                            AnubisConsole.LogSuccess($"[Tech] {tech}");
                        }
                        AnubisConsole.LogInfo("Tech Fingerprint completed.");
                    }
                    else if (!string.IsNullOrEmpty(techResult.ErrorMessage))
                    {
                        AnubisConsole.LogError($"Tech Fingerprint connection failed: {techResult.ErrorMessage}");
                    }
                    else
                    {
                        AnubisConsole.LogWarning("Tech Fingerprint yielded no matching signatures.");
                    }
                }
                else
                {
                    AnubisConsole.LogWarning("Tech Fingerprint yielded no response.");
                }
            }
            catch (Exception ex)
            {
                AnubisConsole.LogError($"Tech Fingerprint failed: {ex.Message}");
            }
        }, targetOption);

        return command;
    }
}
