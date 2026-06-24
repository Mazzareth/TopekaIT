namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// Known printer model names and tiny model-specific facts. If more models matter later, this is the first boring place to look.
/// </summary>
public static class PrinterModels
{
    public const string T8000 = "T8000";

    public static bool SupportsLogging(string? model) =>
        string.Equals(model?.Trim(), T8000, StringComparison.OrdinalIgnoreCase);
}
