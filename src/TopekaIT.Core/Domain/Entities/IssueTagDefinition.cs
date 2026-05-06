using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

public class IssueTagDefinition
{
    public string Code { get; set; } = "";          // e.g. "RightPortBroken" — PK
    public string Label { get; set; } = "";
    public string? Description { get; set; }
    public IssueSeverity Severity { get; set; }
    /// <summary>Comma-separated AssetCategory values this tag applies to. Null = all categories.</summary>
    public string? ApplicableCategories { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<AssetIssueTag> IssueTags { get; set; } = new List<AssetIssueTag>();
}
