namespace TopekaIT.Core.Domain.Entities;

public class Division
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public string? PrinterPasswordCode { get; set; }
    public string? PrinterPasswordZipCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
