using System;
using System.Net;

namespace Domain.ValueObjects;

public readonly record struct IpAddressValue
{
    public string Value { get; }

    private IpAddressValue(string value)
    {
        Value = value;
    }

    public static IpAddressValue Create(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
            throw new ArgumentException("Invalid IP Address", nameof(ipAddress));
            
        return new IpAddressValue(ipAddress);
    }

    public override string ToString() => Value;
}
