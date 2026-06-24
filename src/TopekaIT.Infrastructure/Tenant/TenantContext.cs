using TopekaIT.Core.Ports;

namespace TopekaIT.Infrastructure.Tenant;

/// <summary>
/// The request-scoped memory of which division we are inside. If this is blank, tenant repositories should refuse to guess.
/// </summary>
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
