namespace TopekaIT.Core.Ports;

/// <summary>
/// The current division for this request or background operation. If this is not resolved, tenant data should not be touched.
/// </summary>
public interface ITenantContext
{
    string? DivisionId { get; }
    string? ConnectionString { get; }
    bool IsResolved { get; }
    void SetDivision(string divisionId, string connectionString);
}
