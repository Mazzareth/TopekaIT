namespace TopekaIT.Core.Domain.Enums;

/// <summary>
/// Asset state flags. One primary location/workflow flag should lead, with modifiers like DayLoan layered on top.
/// </summary>
[Flags]
public enum StatusFlags
{
    None               = 0,
    InLocker           = 1 << 0,
    InCC               = 1 << 1,
    WithHolder         = 1 << 2,
    OnLoan             = 1 << 3,
    InRepair           = 1 << 4,
    InRMA              = 1 << 5,
    Missing            = 1 << 6,
    OnHold             = 1 << 7,
    Spare              = 1 << 8,
    DayLoan            = 1 << 9,
    UnderInvestigation = 1 << 10,
}
