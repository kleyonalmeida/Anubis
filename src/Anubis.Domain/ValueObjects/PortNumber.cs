using System;

namespace Anubis.Domain.ValueObjects;

public readonly record struct PortNumber
{
    public int Value { get; }

    private PortNumber(int value)
    {
        Value = value;
    }

    public static PortNumber Create(int value)
    {
        if (value < 1 || value > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Port must be between 1 and 65535.");
        }
        return new PortNumber(value);
    }
}
