namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// The approved model name list for assets. Simple table, but it keeps the UI from becoming free-text soup.
/// </summary>
public class AssetModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}
