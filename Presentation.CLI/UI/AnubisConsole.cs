using System;

namespace Presentation.CLI.UI;

public static class AnubisConsole
{
    private const string AnubisBanner = @"
    ___              __   _      
   /   |  ____  __ __/ /_ (_)____
  / /| | / __ \/ / / / __ \/ / __/
 / ___ |/ / / / /_/ / /_/ / /\__ \ 
/_/  |_/_/ /_/\__,_/_.___/_/____/ 
                                  
+-------------------------------------------+
| [!] ANUBIS: OSINT & SECURITY ANALYSIS     |
+-------------------------------------------+
| Native AOT Engine - Target Lock Engaged.  |
+-------------------------------------------+
";

    // ANSI Colors
    private const string Reset = "\x1b[0m";
    private const string Red = "\x1b[31m";
    private const string Green = "\x1b[32m";
    private const string Yellow = "\x1b[33m";
    private const string Blue = "\x1b[34m";
    private const string Magenta = "\x1b[35m";
    private const string Cyan = "\x1b[36m";

    public static void PrintBanner()
    {
        Console.WriteLine($"{Cyan}{AnubisBanner}{Reset}");
    }

    public static void LogInfo(string message)
    {
        Console.WriteLine($"{Blue}[*] {message}{Reset}");
    }

    public static void LogSuccess(string message)
    {
        Console.WriteLine($"{Green}[+] {message}{Reset}");
    }

    public static void LogWarning(string message)
    {
        Console.WriteLine($"{Yellow}[!] {message}{Reset}");
    }

    public static void LogError(string message)
    {
        Console.WriteLine($"{Red}[-] {message}{Reset}");
    }

    public static void PrintVulnerability(string description, Domain.Enums.VulnerabilitySeverity severity)
    {
        string color = severity switch
        {
            Domain.Enums.VulnerabilitySeverity.Critical => Red,
            Domain.Enums.VulnerabilitySeverity.High => Red,
            Domain.Enums.VulnerabilitySeverity.Medium => Yellow,
            Domain.Enums.VulnerabilitySeverity.Low => Cyan,
            _ => Blue
        };

        Console.WriteLine($"{color}[VULN - {severity.ToString().ToUpper()}] {description}{Reset}");
    }
}
