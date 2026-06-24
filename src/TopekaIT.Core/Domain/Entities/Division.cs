namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A site/division the portal can enter. It owns the tenant database connection and a few local setup defaults.
/// </summary>
public class Division
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public string? PrinterPasswordCode { get; set; }
    public string? PrinterPasswordZipCode { get; set; }
    public int EquipmentCheckInIntervalDays { get; set; } = 30;
    public DateTimeOffset CreatedAt { get; set; }
}
