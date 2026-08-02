using System;

namespace Domain.ValueObjects;

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
            
        var cleaned = domain.Trim().ToLowerInvariant();
        if (cleaned.StartsWith("https://")) cleaned = cleaned.Substring(8);
        if (cleaned.StartsWith("http://")) cleaned = cleaned.Substring(7);
        
        var slashIndex = cleaned.IndexOf('/');
        if (slashIndex >= 0) cleaned = cleaned.Substring(0, slashIndex);

        if (cleaned.StartsWith("www.")) cleaned = cleaned.Substring(4);

        return new DomainName(cleaned);
    }

    public override string ToString() => Value;
}
