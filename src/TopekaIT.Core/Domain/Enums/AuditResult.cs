namespace TopekaIT.Core.Domain.Enums;

/// <summary>
/// How an audit scan compared to what the portal thought should be true.
/// </summary>
public enum AuditResult
{
    Expected,
    Discrepancy,
    Missing,
    Unexpected
}
