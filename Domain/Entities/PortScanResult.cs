using Domain.ValueObjects;

namespace Domain.Entities;

public readonly record struct PortScanResult(
    PortNumber Port,
    string ServiceName,
    bool IsOpen
);
