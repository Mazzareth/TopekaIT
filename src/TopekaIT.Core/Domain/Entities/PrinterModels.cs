namespace TopekaIT.Core.Domain.Entities;

public static class PrinterModels
{
    public const string T8000 = "T8000";

    public static bool SupportsLogging(string? model) =>
        string.Equals(model?.Trim(), T8000, StringComparison.OrdinalIgnoreCase);
}
