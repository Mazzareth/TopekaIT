namespace TopekaIT.Web.Services;

/// <summary>
/// Shared UI state for the Blazor shell: command palettes, theme, selected printer, and toast messages.
/// </summary>
public class AppState
{
    public string Theme { get; private set; } = "light";
    public bool CommandPaletteOpen { get; private set; }
    public bool PrinterCommandPaletteOpen { get; private set; }

    public List<Toast> Toasts { get; } = new();

    public event Action? OnChange;

    public void SetTheme(string theme)
    {
        if (Theme == theme) return;
        Theme = theme;
        NotifyChanged();
    }

    public void OpenCommandPalette()
    {
        if (CommandPaletteOpen) return;
        PrinterCommandPaletteOpen = false;
        CommandPaletteOpen = true;
        NotifyChanged();
    }

    public void CloseCommandPalette()
    {
        if (!CommandPaletteOpen) return;
        CommandPaletteOpen = false;
        NotifyChanged();
    }

    public void OpenPrinterCommandPalette()
    {
        if (PrinterCommandPaletteOpen) return;
        CommandPaletteOpen = false;
        PrinterCommandPaletteOpen = true;
        NotifyChanged();
    }

    public void ClosePrinterCommandPalette()
    {
        if (!PrinterCommandPaletteOpen) return;
        PrinterCommandPaletteOpen = false;
        NotifyChanged();
    }

    public void PushToast(string message, string tone = "")
    {
        var toast = new Toast(Guid.NewGuid().ToString("N")[..8], message, tone);
        Toasts.Add(toast);
        NotifyChanged();
        _ = RemoveAfterDelay(toast);
    }

    private async Task RemoveAfterDelay(Toast toast)
    {
        await Task.Delay(3000);
        Toasts.Remove(toast);
        NotifyChanged();
    }

    private void NotifyChanged() => OnChange?.Invoke();
}

/// <summary>
/// A short-lived message for the toast stack.
/// </summary>
public record Toast(string Id, string Message, string Tone);
