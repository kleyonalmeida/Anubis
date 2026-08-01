using System.Collections.Generic;

namespace Domain.Entities;

public record TechFingerprintResult(
    string TargetUrl,
    IReadOnlyList<string> TechnologiesDetected
);
