using System.Collections.Generic;

namespace Domain.Entities;

public record TechFingerprintResult(
    string TargetUrl,
    IReadOnlyList<string> TechnologiesDetected,
    int StatusCode = 0,
    string ServerHeader = "",
    int HtmlLength = 0,
    string ErrorMessage = ""
);
