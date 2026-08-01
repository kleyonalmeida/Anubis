namespace Domain.ValueObjects;

public enum TechSignatureType
{
    Html,
    Cookie,
    Header,
    HeaderServer,
    HeaderXPoweredBy
}

public readonly record struct TechSignature(TechSignatureType Type, string Pattern);
