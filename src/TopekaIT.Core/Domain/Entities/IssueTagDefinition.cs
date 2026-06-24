using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// The dictionary entry for issue tags. Change these and you change what people can pick when marking asset problems.
/// </summary>
public class IssueTagDefinition
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Description { get; set; }
    public IssueSeverity Severity { get; set; }
    // Null means the tag can be used for every asset category.
    public string? ApplicableCategories { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<AssetIssueTag> IssueTags { get; set; } = new List<AssetIssueTag>();
}
