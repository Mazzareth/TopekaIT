using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace TopekaIT.AvaloniaShell;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = PortalSettings.Load();
            var api = new ShellApiClient(settings);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new ShellViewModel(settings, api)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
