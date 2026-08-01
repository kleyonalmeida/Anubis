```
                    .-@W=                                             
                    #WWWW-                                            
                    *WWWWW*-.                              -++-       
            :       -WWWWWWW%.                           .#WW%-       
            **:     .WWWWWWWW%.                          +WWW=        
            :WW#-    *WWWWWWWW*                          :%WW:        
           .-#WWW#=.  %WWWWWW=#.                          .*@*-..     
          -@WWWWWWWW#:=WWWWWW: .                            .-++**+.  
       .-#WWWWWWWWWWWW%WWWWWW%--..                 .....         :*@: 
     .+WWWWWWWWWWWWWWWWWWWWWWWWWWW#*=-:::....-=+*#%@WW%#*+-.       @* 
      .=#%###++*%WWWWWWWWWWWWWWWWWWWWWWWW@@@WWWWWWWWWW@*==*#*-:::=##. 
                .-*WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW*..-++*++-   
 ..    .-+==-+#%+. =WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW%:          
 =W@@@%WWWWWWWWW%%%@WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW@:         
 .+WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW%@WWWWWWWWWWWWWW@:        
   .:..+WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW%::%WWWWW@+%WWWWWWW+:      
     .:@WWWW@#*#%@@WWWWWWWWWWWWWWWWWWWWWWW%.   =%WWWW+ .-+*%@WWW%.    
    .*W%+:...     ..::=#WWWWWWWWWWWWWWWWWW#     .=%WWW*     .=@WW*    
     .:                *WWW##=#%%@@@#+@WWW:       .@WW*       -WWW:   
                     .#WW%- .  .....  @WW*         %WW:        +WW*   
                    :%WW=            -WW@.         #WW.        .@W@.  
                 ..+WW%=             #WW*       .:-@W#         =WW@.  
               +@@WWW+            .-*WWW-       *@W@#.       :%WWW*   
               :=+=-.            .#@WWW*.       ....         .:-::    
                                  ..:-.                               

    ___              __   _      
   /   |  ____  __ __/ /_ (_)____
  / /| | / __ \/ / / / __ \/ / __/
 / ___ |/ / / / /_/ / /_/ / /\__ \ 
/_/  |_/_/ /_/\__,_/_.___/_/____/ 
                                  
        OSINT & SECURITY ANALYSIS — v2.0.0 (High-Performance .NET Native AOT)
```

> Use Anubis only on systems you own or have explicit permission to test. Unauthorized scanning is illegal.

---

## ⚡ What is Anubis?

**Anubis** is a next-generation OSINT, Reconnaissance, and Offensive Security Engine engineered in C# (.NET 10) for Red Teams, SDETs, and Security Engineers. Inspired by the Egyptian deity who weighs the hearts of the dead, Anubis acts as an unyielding audit gatekeeper: it scans, probes, and analyzes target infrastructure and applications with extreme precision, delivering clear risk diagnostics before an attacker can exploit them.

Unlike legacy Python security scripts constrained by the GIL (Global Interpreter Lock) or bloated Go scanners that consume hundreds of megabytes of RAM under load, Anubis is built from the ground up for **extreme concurrency, zero-allocation memory streaming, and Native AOT binary generation**.

---

## 🔥 Why Anubis? (The Pentester's Ultimate Weapon)

In modern Offensive Security and DevSecOps pipelines, speed and stealth are everything. Anubis streamlines your recon workflow into a single, blazing-fast CLI tool:

- **⚡ Sub-Millisecond Cold Starts (Native AOT)**: Compiles down to an enclaved 9.5MB native binary without requiring the .NET Runtime, Python, or Go installed. Launches in under 5ms.
- **🚀 Zero Memory Bloat (No LOH / No GC Pauses)**: Network responses stream directly into unmanaged memory via `ArrayPool<byte>` and `ArrayPool<char>` buffers with `ReadOnlySpan<char>` parsing. Scan thousands of targets without memory spikes.
- **🎯 Multi-Vector Attack Surface Coverage**:
  - **OSINT & Subdomain Mapping**: Uncovers hidden assets across target networks.
  - **Hellscan TCP Engine**: Ultra-fast async TCP connector (`Socket.ConnectAsync`) that punches through stealth firewalls.
  - **Tech Stack Fingerprinting**: Instant detection of server headers, frameworks, and CMS vulnerabilities without bloated regex engines.
  - **VulnScan Security Analyzer**: Parallel payload injection for SQLi, Reflected XSS, LFI (Path Traversal), Open Redirect, and exposed sensitive panels.
  - **Real-Time NVD CVE Query**: Source-generated JSON streaming against NIST's National Vulnerability Database.
- **🛠️ Grep-Friendly Tactical Output**: ANSI escape codes deliver vibrant visual feedback in interactive terminals while producing clean text output when piped (`|`) or saved (`>`) to files.

---

## 🛠️ Quick Command Usage (`./anubis`)

You don't need long `dotnet` commands. Simply run `./anubis` directly:

```bash
# VulnScan (SQLi, XSS, LFI, Admin Panels)
./anubis vulnscan --target example.com

# Subdomain Enumeration
./anubis subdomain --target example.com

# Hellscan TCP Port Scanner
./anubis portscan --target example.com

# Tech Stack Fingerprinting
./anubis tech --target example.com

# NVD CVE Real-Time Query
./anubis cve --id CVE-2021-44228
./anubis cve --product "apache 2.4.49"
```

---

## 🎯 Command Outputs & Examples

### 1. Offensive Security VulnScan (`vulnscan`)
Injects multiple payload vectors concurrently using `Task.WhenAll`:

```bash
./anubis vulnscan --target example.com
```

**Terminal Output:**
```text
[VULN - CRITICAL] SQL Injection detected on parameter ?id= via payload ' OR '1'='1
[VULN - HIGH] Local File Inclusion (LFI) vulnerability: exposed /etc/passwd signature
[VULN - MEDIUM] Missing Content-Security-Policy (CSP) header
[*] VulnScan completed. Found 3 vulnerabilities.
```

---

### 2. Hellscan TCP Port Scanner (`portscan`)
Varredura TCP ultrarápida sem travamento de threads:

```bash
./anubis portscan --target example.com
```

**Terminal Output:**
```text
[+] [Port] 80 (Open) - HTTP
[+] [Port] 443 (Open) - HTTPS
[+] [Port] 22 (Open) - SSH
[*] Hellscan completed. Found 3 open ports.
```

---

### 3. OSINT Tech Stack Fingerprint (`tech`)
Identificação instantânea de componentes e infraestrutura:

```bash
./anubis tech --target example.com
```

**Terminal Output:**
```text
[*] Target: https://example.com
[+] [Tech] Server: nginx/1.24.0
[+] [Tech] PoweredBy: PHP/8.2
[+] [Tech] ContentType: text/html; charset=UTF-8
[*] Tech Fingerprint completed.
```

---

### 4. NVD CVE Lookup (`cve`)
Busca em tempo real com parse JSON em código assembly nativo (Source Generators):

```bash
./anubis cve --id CVE-2021-44228
```

**Terminal Output:**
```text
[*] Querying NVD for CVE-2021-44228 ...
[VULN - CRITICAL] [CVE-2021-44228] Base Score: 10.0 | CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H
Log4j RCE Vulnerability in Apache Log4j2 2.0-beta9 through 2.15.0
```

---

## 🏗️ Architecture & Clean Design

Anubis follows strict **Clean Architecture** for maximum maintainability:

```text
├── Domain/              # Business Entities, Value Objects, Payloads, Enums
├── Application/         # Orchestration Services (Hellscan, VulnScan, CVE) & Contracts
├── Infrastructure/      # Socket Connectors, HTTP Streaming Engines & AOT Source Generators
├── Presentation.CLI/    # AOT-Safe System.CommandLine Entrypoint & ANSI Terminal Renderer
└── tests/Tests/         # Offline TDD Test Suite (Moq & WireMock integration)
```

---

## 🚀 Building & Publishing

### Compile Native AOT Binary
Generates a 9.5MB standalone binary optimized for speed:

```bash
dotnet publish Presentation.CLI -c Release -r linux-x64
```

### Run Test Suite
```bash
dotnet test Anubis.sln
```

---

## ⚖️ Legal & Compliance

This tool is designed strictly for educational purposes and authorized security testing. Unauthorized scanning or exploit testing against target systems without prior explicit consent is strictly prohibited and illegal.