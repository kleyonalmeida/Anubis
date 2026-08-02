using System;
using System.CommandLine;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Network;
using Presentation.CLI.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Presentation.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 1. AOT-Safe Dependency Injection Setup (No Microsoft.Extensions.Hosting overhead)
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder => 
        {
            builder.SetMinimumLevel(LogLevel.Error); // Only show critical DI/HTTP errors to console standard out, hide trace
            builder.AddConsole();
        });

        // Application Services
        services.AddSingleton<ISubdomainFinderService, SubdomainFinderService>();
        services.AddSingleton<IHellscanService, HellscanService>();
        services.AddSingleton<ITechFingerprintService, TechFingerprintService>();
        services.AddSingleton<IVulnScanService, VulnScanService>();
        services.AddSingleton<ICveLookupService, CveLookupService>();

        // Infrastructure Services
        services.AddSingleton<IDnsResolver, DnsResolver>();
        services.AddSingleton<ITcpConnector, TcpConnector>();
        services.AddSingleton<INvdApiClient, NvdApiClient>();
        services.AddSingleton<IExploitSender, ExploitSender>();

        // HTTP Clients (Connection Pooling / No socket exhaustion)
        services.AddHttpClient("TechClient").ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            AllowAutoRedirect = true,
            EnableMultipleHttp2Connections = true,
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            }
        });

        services.AddHttpClient("VulnScanClient").ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            AllowAutoRedirect = false, // VulnScan needs manual redirect handling for OpenRedirect checks
            AutomaticDecompression = DecompressionMethods.All,
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            }
        });

        services.AddHttpClient("NvdClient").ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            AutomaticDecompression = DecompressionMethods.All
        });

        // Build the Service Provider
        await using var serviceProvider = services.BuildServiceProvider();

        // 2. Root Command Setup
        var rootCommand = new RootCommand("Anubis: OSINT, Recon, and Offensive Security Engine");

        // 3. Register Subcommands using the explicit AOT-Safe DI mapping
        rootCommand.AddCommand(SubdomainCommand.Create(serviceProvider));
        rootCommand.AddCommand(PortScanCommand.Create(serviceProvider));
        rootCommand.AddCommand(TechFingerprintCommand.Create(serviceProvider));
        rootCommand.AddCommand(VulnScanCommand.Create(serviceProvider));
        rootCommand.AddCommand(CveCommand.Create(serviceProvider));

        // 4. Run CLI
        return await rootCommand.InvokeAsync(args);
    }
}
