using TopekaIT.Core.Ports;

namespace TopekaIT.Infrastructure.Tenant;

public class TenantContext : ITenantContext
{
    public string? DivisionId { get; private set; }
    public string? ConnectionString { get; private set; }
    public bool IsResolved => !string.IsNullOrWhiteSpace(DivisionId)
        && !string.IsNullOrWhiteSpace(ConnectionString);

    public void SetDivision(string divisionId, string connectionString)
    {
        DivisionId = divisionId;
        ConnectionString = connectionString;
    }
}
