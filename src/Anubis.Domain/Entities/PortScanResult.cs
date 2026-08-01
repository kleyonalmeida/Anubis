using Anubis.Domain.ValueObjects;

namespace Anubis.Domain.Entities;

public readonly record struct PortScanResult(
    PortNumber Port,
    string ServiceName,
    bool IsOpen
);
