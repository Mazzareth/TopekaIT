namespace TopekaIT.Core.Domain.Entities;

public class AssetIssueTag
{
    public string Id { get; set; } = "";
    public string AssetId { get; set; } = "";
    public string DefinitionCode { get; set; } = "";
    public string? Notes { get; set; }
    public DateTimeOffset TaggedAt { get; set; }
    public string TaggedBy { get; set; } = "";
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }

    public Asset Asset { get; set; } = null!;
    public IssueTagDefinition Definition { get; set; } = null!;
}
