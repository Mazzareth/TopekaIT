using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;

namespace TopekaIT.AvaloniaShell;

public sealed class LoginShellViewModel : INotifyPropertyChanged
{
    private readonly PortalSettings _settings;
    private readonly ShellApiClient _api;
    private readonly Action<ShellSession> _onLoginSucceeded;
    private bool _isDarkTheme = true;
    private bool _isSigningIn;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _stationPin = string.Empty;
    private string _deviceScan = string.Empty;
    private string _statusMessage;

    public LoginShellViewModel(
        PortalSettings settings,
        ShellApiClient api,
        Action<ShellSession> onLoginSucceeded)
    {
        _settings = settings;
        _api = api;
        _onLoginSucceeded = onLoginSucceeded;
        _statusMessage = "Ready when you are.";
        SignInCommand = new AsyncRelayCommand(SignInAsync, () => !IsSigningIn);
        OpenQuickAccessCommand = new RelayCommand(() => OpenPortalPath("/station/equipment", "device check-in station"));
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string PortalBaseUrl => _settings.BaseUrl;

    public string ThemeButtonText => _isDarkTheme ? "Light Mode" : "Dark Mode";

    public string SignInButtonText => IsSigningIn ? "Signing In..." : "Sign In";

    public bool IsSigningIn
    {
        get => _isSigningIn;
        private set
        {
            if (SetField(ref _isSigningIn, value))
            {
                OnPropertyChanged(nameof(SignInButtonText));
                SignInCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string StationPin
    {
        get => _stationPin;
        set => SetField(ref _stationPin, value);
    }

    public string DeviceScan
    {
        get => _deviceScan;
        set => SetField(ref _deviceScan, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public AsyncRelayCommand SignInCommand { get; }

    public ICommand OpenQuickAccessCommand { get; }

    public ICommand ToggleThemeCommand { get; }

    public void ResetAfterSignOut()
    {
        Password = string.Empty;
        StatusMessage = "Signed out. Ready when you are.";
    }

    private async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Enter your username and password.";
            return;
        }

        IsSigningIn = true;
        StatusMessage = "Checking access.";

        try
        {
            var result = await _api.LoginAsync(Username, Password);
            if (!result.Succeeded || result.Response == null)
            {
                StatusMessage = result.ErrorMessage ?? "Sign-in failed.";
                return;
            }

            if (result.Response.RequiresPasswordChange)
            {
                StatusMessage = "Password change required. Use the current web portal to update it before opening the hub.";
                return;
            }

            Password = string.Empty;
            _onLoginSucceeded(ShellSession.FromLoginResponse(result.Response));
        }
        finally
        {
            IsSigningIn = false;
        }
    }

    private void OpenPortalPath(string path, string destinationName)
    {
        var url = _settings.BuildUri(path);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url.ToString(),
                UseShellExecute = true
            });

            StatusMessage = $"Opening {destinationName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open {destinationName}. {ex.Message}";
        }
    }

    private void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = _isDarkTheme
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }

        OnPropertyChanged(nameof(ThemeButtonText));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
