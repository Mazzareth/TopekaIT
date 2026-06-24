namespace TopekaIT.Core.Domain.Enums;

/// <summary>
/// Scanner form factor/type. Mostly here so inventory can be specific without inventing new categories.
/// </summary>
public enum ScannerKind
{
    TwoD,
    OneD,
    Ring,
    Other
}
