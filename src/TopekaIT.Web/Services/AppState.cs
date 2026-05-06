namespace TopekaIT.Web.Services;

public class AppState
{
    public string Theme { get; private set; } = "light";
    public bool NavCollapsed { get; private set; }

    public List<Toast> Toasts { get; } = new();

    public event Action? OnChange;

    public void SetTheme(string theme)
    {
        if (Theme == theme) return;
        Theme = theme;
        NotifyChanged();
    }

    public void ToggleNav()
    {
        NavCollapsed = !NavCollapsed;
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

public record Toast(string Id, string Message, string Tone);
