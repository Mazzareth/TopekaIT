namespace TopekaIT.Core.Domain.Enums;

/// <summary>
/// The kind of asset we are tracking. Category decides which fields and workflows make sense.
/// </summary>
public enum AssetCategory
{
    SaeDevice,
    PodTc77,
    Battery,
    Scanner
}
