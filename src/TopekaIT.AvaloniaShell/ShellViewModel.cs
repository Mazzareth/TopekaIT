using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TopekaIT.AvaloniaShell;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private HubShellViewModel? _hub;

    public ShellViewModel(PortalSettings settings, ShellApiClient api)
    {
        Login = new LoginShellViewModel(settings, api, OpenHub);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoginShellViewModel Login { get; }

    public HubShellViewModel? Hub
    {
        get => _hub;
        private set
        {
            if (!ReferenceEquals(_hub, value))
            {
                _hub = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLoginVisible));
                OnPropertyChanged(nameof(IsHubVisible));
            }
        }
    }

    public bool IsLoginVisible => Hub == null;

    public bool IsHubVisible => Hub != null;

    private void OpenHub(ShellSession session)
    {
        Hub = new HubShellViewModel(session, SignOut);
    }

    private void SignOut()
    {
        Hub = null;
        Login.ResetAfterSignOut();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
