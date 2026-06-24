namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A tagged problem on an asset. Good for quick "what is wrong with this thing?" labels without losing the notes.
/// </summary>
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
