using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TopekaIT.AvaloniaShell;

public sealed partial class MainWindow : Window
{
    private Grid? _panelGrid;
    private Border? _signInPanel;
    private Border? _quickAccessPanel;
    private bool? _isCompactLayout;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _panelGrid = this.FindControl<Grid>("PanelGrid");
        _signInPanel = this.FindControl<Border>("SignInPanel");
        _quickAccessPanel = this.FindControl<Border>("QuickAccessPanel");

        Opened += (_, _) => UpdateAdaptiveLayout();
        SizeChanged += (_, _) => UpdateAdaptiveLayout();
    }

    private void UpdateAdaptiveLayout()
    {
        if (_panelGrid is null || _signInPanel is null || _quickAccessPanel is null)
        {
            return;
        }

        var compactLayout = Bounds.Width < 920;
        if (_isCompactLayout == compactLayout)
        {
            return;
        }

        _isCompactLayout = compactLayout;

        if (compactLayout)
        {
            _panelGrid.ColumnDefinitions = new ColumnDefinitions("1*");
            _panelGrid.RowDefinitions = new RowDefinitions("Auto,16,Auto");

            Grid.SetColumn(_signInPanel, 0);
            Grid.SetRow(_signInPanel, 0);
            Grid.SetColumn(_quickAccessPanel, 0);
            Grid.SetRow(_quickAccessPanel, 2);
            return;
        }

        _panelGrid.ColumnDefinitions = new ColumnDefinitions("1.05*,24,0.95*");
        _panelGrid.RowDefinitions = new RowDefinitions("Auto");

        Grid.SetColumn(_signInPanel, 0);
        Grid.SetRow(_signInPanel, 0);
        Grid.SetColumn(_quickAccessPanel, 2);
        Grid.SetRow(_quickAccessPanel, 0);
    }
}
