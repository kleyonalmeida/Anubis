using System.Collections.Generic;

namespace Anubis.Domain.Entities;

public record TechFingerprintResult(
    string TargetUrl,
    IReadOnlyList<string> TechnologiesDetected
);
