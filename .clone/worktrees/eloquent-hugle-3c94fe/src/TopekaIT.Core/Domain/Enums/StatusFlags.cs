namespace TopekaIT.Core.Domain.Enums;

[Flags]
public enum StatusFlags
{
    None               = 0,
    InLocker           = 1 << 0,   // physically in an assigned locker
    InCC               = 1 << 1,   // in the command center / IT area
    WithHolder         = 1 << 2,   // checked out to a primary holder
    OnLoan             = 1 << 3,   // loaned to a non-primary borrower
    InRepair           = 1 << 4,   // in IT repair queue
    InRMA              = 1 << 5,   // shipped to manufacturer for RMA
    Missing            = 1 << 6,   // cannot locate
    OnHold             = 1 << 7,   // held — waiting on parts, decision, etc.
    Spare              = 1 << 8,   // in the spare pool, available to lend
    DayLoan            = 1 << 9,   // modifier: same-day loan (stacks with OnLoan)
    UnderInvestigation = 1 << 10,  // modifier: under investigation (stacks with any)
}
