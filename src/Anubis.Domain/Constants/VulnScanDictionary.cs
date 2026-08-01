using System.Collections.Generic;

namespace Anubis.Domain.Constants;

public static class VulnScanDictionary
{
    public static readonly string[] SecurityHeaders = 
    {
        "X-Frame-Options", "X-Content-Type-Options", "Strict-Transport-Security",
        "Content-Security-Policy", "X-XSS-Protection", "Referrer-Policy"
    };

    public static readonly string[] SqliPayloads = { "'", "\"", "' OR 1=1--" };
    public static readonly string[] SqliErrors = { "sql syntax", "mysql_fetch", "ORA-", "syntax error" };

    public static readonly string[] OpenRedirectParams = { "redirect", "url", "next", "return", "goto" };

    public static readonly string[] AdminPaths = 
    {
        "/admin", "/admin/login", "/wp-admin", "/phpmyadmin",
        "/dashboard", "/panel", "/cpanel", "/login", "/api/admin"
    };

    public static readonly string[] XssPayloads = 
    {
        "<script>alert(1)</script>", "<img src=x onerror=alert(1)>", "'><svg onload=alert(1)>"
    };

    public static readonly string[] LfiPayloads = { "../../etc/passwd", "../../../../etc/passwd" };
    public static readonly string[] LfiSignatures = { "root:x:", "daemon:" };
    public static readonly string[] DirectoryPaths = { "/images/", "/uploads/", "/files/", "/backup/", "/static/" };
    public static readonly string[] DirectorySignatures = { "index of", "parent directory" };

    public static readonly string[] SensitivePaths = 
    {
        "/.env", "/.env.backup", "/.env.local", "/.git/config", "/.git/HEAD",
        "/wp-config.php", "/wp-config.php.bak", "/config.php", "/config.yml",
        "/config.yaml", "/database.yml", "/settings.py", "/local_settings.py",
        "/.htpasswd", "/.htaccess", "/composer.json", "/package.json",
        "/Dockerfile", "/docker-compose.yml", "/id_rsa", "/id_rsa.pub",
        "/server.key", "/backup.sql", "/dump.sql", "/db.sql"
    };

    public static readonly string[] SensitiveIndicators = 
    {
        "DB_", "SECRET", "PASSWORD", "API_KEY", "TOKEN",
        "mysql", "postgres", "redis", "[core]", "<?php",
        "private", "BEGIN RSA", "PRIVATE KEY"
    };
}
