namespace TopekaIT.Core.Domain.Enums;

/// <summary>
/// How long a spare is expected to be out. The TBD values are honest placeholders for repair and missing-device cases.
/// </summary>
public enum LoanDuration 
{ 
    DayLoan, 
    LessThanWeek, 
    MoreThanWeek, 
    TbdRma, 
    TbdMia 
}
