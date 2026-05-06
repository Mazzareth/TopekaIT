namespace TopekaIT.Core.Ports;

public interface ITenantContext
{
    string? DivisionId { get; }
    string? ConnectionString { get; }
    bool IsResolved { get; }
    void SetDivision(string divisionId, string connectionString);
}
