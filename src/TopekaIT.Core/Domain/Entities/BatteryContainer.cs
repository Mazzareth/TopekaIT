namespace TopekaIT.Core.Domain.Entities;

public class BatteryContainer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";          // e.g. "Tray A", "Cart 3 Slots 1-6"
    public string? Location { get; set; }           // free-text location description
    public int Capacity { get; set; }
    public int CurrentCount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
