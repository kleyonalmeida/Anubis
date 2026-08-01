using Anubis.Domain.ValueObjects;

namespace Anubis.Domain.Entities;

public record SubdomainResult(
    DomainName Host,
    IpAddressValue Ip,
    int StatusCode,
    bool HttpsOk,
    string Title,
    string Tech,
    string Asn
);
