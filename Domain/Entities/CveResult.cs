using Domain.Enums;

namespace Domain.Entities;

public readonly record struct CveResult(
    string CveId,
    string Description,
    VulnerabilitySeverity Severity,
    string BaseScore,
    string VectorString
);
