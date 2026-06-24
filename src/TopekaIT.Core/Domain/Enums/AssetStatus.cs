namespace TopekaIT.Core.Domain.Enums;

/// <summary>
/// The older status field for assets. Flags carry the richer current picture, but this still keeps legacy screens readable.
/// </summary>
public enum AssetStatus
{
    In,
    Out,
    Repair,
    InUse,
    InLocker,
    InCC,
    MIA,
    InRMA,
    Investigating,
    Holding,
    Spare,
    Loaned
}
