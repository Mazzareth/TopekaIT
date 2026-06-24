namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A printer model the setup and admin screens understand.
/// </summary>
public class PrinterModel
{
    public string Name { get; set; } = "";
    public bool SupportsLogging { get; set; }
}
