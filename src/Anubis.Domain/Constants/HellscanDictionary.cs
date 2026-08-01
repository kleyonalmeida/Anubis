using System.Collections.Generic;

namespace Anubis.Domain.Constants;

public static class HellscanDictionary
{
    public static readonly IReadOnlyDictionary<int, string> Ports = new Dictionary<int, string>
    {
        { 21, "FTP" },
        { 22, "SSH" },
        { 23, "Telnet" },
        { 25, "SMTP" },
        { 53, "DNS" },
        { 80, "HTTP" },
        { 110, "POP3" },
        { 143, "IMAP" },
        { 443, "HTTPS" },
        { 445, "SMB" },
        { 3306, "MySQL" },
        { 3389, "RDP" },
        { 5432, "PostgreSQL" },
        { 8080, "HTTP-Alt" },
        { 8443, "HTTPS-Alt" }
    };
}
