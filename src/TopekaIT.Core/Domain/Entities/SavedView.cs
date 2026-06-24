namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A saved filter layout for a user. The filter stays JSON so the UI can evolve without a migration every Tuesday.
/// </summary>
public class SavedView
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string FilterJson { get; set; } = "{}";
    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
