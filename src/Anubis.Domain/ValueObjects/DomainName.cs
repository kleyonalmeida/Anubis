using System;

namespace Anubis.Domain.ValueObjects;

public readonly record struct DomainName
{
    public string Value { get; }

    private DomainName(string value)
    {
        Value = value;
    }

    public static DomainName Create(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be null or empty", nameof(domain));
            
        return new DomainName(domain.Trim().ToLowerInvariant());
    }

    public override string ToString() => Value;
}
