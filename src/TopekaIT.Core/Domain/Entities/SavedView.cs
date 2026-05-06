namespace TopekaIT.Core.Domain.Entities;

public class SavedView
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";       // UserId
    public string Name { get; set; } = "";
    public string FilterJson { get; set; } = "{}";  // serialized filter/sort state
    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
