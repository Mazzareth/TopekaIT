using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace TopekaIT.DeploymentChecker;

/// <summary>
/// Main checker window. It is a thin UI over local checks, remote status, and deployment push actions.
/// </summary>
public partial class MainWindow : Window
{
    readonly PowerShellStatusChecker _checker = new();
    readonly DeploymentPushRunner _pusher = new();
    readonly LocalDevRunner _devRunner = new();
    bool _isEditing;

    public MainWindow()
    {
        InitializeComponent();
    }

    async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var settings = await SettingsStore.LoadSettingsAsync();
        RemoteServerBox.Text = settings.RemoteServer;
        RemotePathBox.Text = settings.RemotePath;
        ServiceNameBox.Text = settings.ServiceName;
        UsernameBox.Text = settings.Username;
        LocalUrlBox.Text = LocalDevRunner.LocalUrl;
        LocalProjectBox.Text = LocalDevRunner.GetWebProjectPath();
        LogPathText.Text = $"check log: {SettingsStore.HistoryPath}\ndeploy log: {SettingsStore.DeploymentLogPath}\ndev logs: {SettingsStore.AppDirectory}\\dev-run-*.log";
        SetEditing(false);
        AppendTerminal("console ready");
        await RefreshHistoryAsync();
    }

    async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isEditing)
        {
            await SettingsStore.SaveSettingsAsync(ReadSettings());
            SetEditing(false);
            AppendTerminal("settings saved locally without password");
            return;
        }

        SetEditing(true);
        AppendTerminal("settings unlocked");
    }

    async void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = ReadSettings();
        await SettingsStore.SaveSettingsAsync(settings);

        SetBusy(true);
        StatusText.Text = "CHECKING";
        StatusText.Foreground = (Brush)FindResource("WarnBrush");
        ReasonText.Text = "Opening a remote PowerShell session...";
        DetailBox.Text = "";
        AppendTerminal("$ check remote status");

        try
        {
            var result = await _checker.CheckAsync(settings);
            await SettingsStore.AppendHistoryAsync(result);
            RenderResult(result);
            AppendTerminal(result.IsOnline ? "remote process online" : $"remote process offline: {result.Reason}");
            await RefreshHistoryAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void KillLocalButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        StatusText.Text = "KILLING LOCAL";
        StatusText.Foreground = (Brush)FindResource("WarnBrush");
        ReasonText.Text = "Stopping local TopekaIT.Web processes...";
        DetailBox.Text = "";
        AppendTerminal("$ kill local app processes");

        try
        {
            var result = await _devRunner.KillLocalAppProcessesAsync(new Progress<string>(AppendTerminal));
            RenderDevResult(result);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void BuildRunButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        StatusText.Text = "BUILDING";
        StatusText.Foreground = (Brush)FindResource("WarnBrush");
        ReasonText.Text = "Building and starting the local app...";
        DetailBox.Text = "";
        AppendTerminal("$ build and run local app");

        try
        {
            var result = await _devRunner.BuildAndRunAsync(new Progress<string>(AppendTerminal));
            RenderDevResult(result);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void PushButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = ReadSettings();
        await SettingsStore.SaveSettingsAsync(settings);

        SetBusy(true);
        StatusText.Text = "DEPLOYING";
        StatusText.Foreground = (Brush)FindResource("WarnBrush");
        ReasonText.Text = "Publishing, packing, and shipping the release build...";
        DetailBox.Text = "";
        AppendTerminal("$ push updates");

        try
        {
            var deployResult = await _pusher.PushAsync(settings, new Progress<string>(AppendTerminal));
            DetailBox.Text = FormatDeployResult(deployResult);

            if (!deployResult.Succeeded)
            {
                StatusText.Text = "DIAGNOSING";
                ReasonText.Text = "Deployment failed. Checking the remote service for details...";
                AppendTerminal("$ check remote status after failed push");

                var failureStatusResult = await _checker.CheckAsync(settings);
                await SettingsStore.AppendHistoryAsync(failureStatusResult);

                StatusText.Text = "DEPLOY FAILED";
                StatusText.Foreground = (Brush)FindResource("DownBrush");
                ReasonText.Text = failureStatusResult.Reason;
                DetailBox.Text = FormatDeployResult(deployResult) + Environment.NewLine + FormatResult(failureStatusResult);
                AppendTerminal($"deploy failed: {deployResult.Reason}");
                AppendTerminal($"post-failure status: {failureStatusResult.Reason}");
                AppendTerminalBlock(FormatResult(failureStatusResult));
                await RefreshHistoryAsync();
                return;
            }

            StatusText.Text = "VERIFYING";
            StatusText.Foreground = (Brush)FindResource("WarnBrush");
            ReasonText.Text = "Deployment completed. Checking the remote service...";
            AppendTerminal("$ verify remote status");

            var statusResult = await _checker.CheckAsync(settings);
            await SettingsStore.AppendHistoryAsync(statusResult);
            RenderResult(statusResult);
            DetailBox.Text = FormatDeployResult(deployResult) + Environment.NewLine + FormatResult(statusResult);
            AppendTerminal(statusResult.IsOnline ? "verify online" : $"verify failed: {statusResult.Reason}");
            await RefreshHistoryAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    DeploymentSettings ReadSettings() => new()
    {
        RemoteServer = RemoteServerBox.Text.Trim(),
        RemotePath = RemotePathBox.Text.Trim(),
        ServiceName = ServiceNameBox.Text.Trim(),
        Username = UsernameBox.Text.Trim(),
        Password = PasswordBox.Password,
    };

    void SetEditing(bool isEditing)
    {
        _isEditing = isEditing;
        RemoteServerBox.IsReadOnly = !isEditing;
        RemotePathBox.IsReadOnly = !isEditing;
        ServiceNameBox.IsReadOnly = !isEditing;
        UsernameBox.IsReadOnly = !isEditing;
        PasswordBox.IsEnabled = isEditing;
        EditButton.Content = isEditing ? "SAVE" : "EDIT";
    }

    void SetBusy(bool isBusy)
    {
        CheckButton.IsEnabled = !isBusy;
        PushButton.IsEnabled = !isBusy;
        EditButton.IsEnabled = !isBusy;
        KillLocalButton.IsEnabled = !isBusy;
        BuildRunButton.IsEnabled = !isBusy;
        PasswordBox.IsEnabled = !isBusy && _isEditing;
    }

    void RenderResult(StatusCheckResult result)
    {
        StatusText.Text = result.Status.ToUpperInvariant();
        StatusText.Foreground = result.IsOnline
            ? (Brush)FindResource("OkBrush")
            : (Brush)FindResource("DownBrush");
        ReasonText.Text = result.Reason;
        DetailBox.Text = FormatResult(result);
    }

    void RenderDevResult(LocalDevActionResult result)
    {
        StatusText.Text = result.Succeeded ? "LOCAL READY" : "LOCAL FAILED";
        StatusText.Foreground = result.Succeeded
            ? (Brush)FindResource("OkBrush")
            : (Brush)FindResource("DownBrush");
        ReasonText.Text = result.Reason;
        DetailBox.Text = FormatDevResult(result);
        AppendTerminal(result.Succeeded ? result.Reason : $"local action failed: {result.Reason}");
    }

    void AppendTerminal(string line)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {line}";
        TerminalBox.AppendText(entry + Environment.NewLine);
        TerminalBox.ScrollToEnd();

        try
        {
            Directory.CreateDirectory(SettingsStore.AppDirectory);
            File.AppendAllText(SettingsStore.DeploymentLogPath, entry + Environment.NewLine);
        }
        catch
        {
            // The visible console remains the primary log if local disk logging fails.
        }
    }

    void AppendTerminalBlock(string text)
    {
        foreach (var line in text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            AppendTerminal(line);
        }
    }

    async Task RefreshHistoryAsync()
    {
        var history = await SettingsStore.LoadRecentHistoryAsync();
        if (history.Count == 0)
        {
            HistoryBox.Text = "No checks have been logged yet.";
            return;
        }

        var builder = new StringBuilder();
        foreach (var item in history)
        {
            builder.AppendLine($"{item.CheckedAtUtc.LocalDateTime:yyyy-MM-dd HH:mm:ss}  {item.Status.ToUpperInvariant()}  {item.RemoteServer}/{item.ServiceName}");
            builder.AppendLine($"  {item.Reason}");
            builder.AppendLine();
        }

        HistoryBox.Text = builder.ToString();
    }

    static string FormatDeployResult(DeploymentPushResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("DEPLOY_RESULT");
        builder.AppendLine($"Stamp:     {result.DeploymentStamp}");
        builder.AppendLine($"Status:    {(result.Succeeded ? "SUCCESS" : "FAILED")}");
        builder.AppendLine($"Reason:    {result.Reason}");
        builder.AppendLine($"Archive:   {result.ArchivePath}");
        builder.AppendLine($"Exit code: {(result.ExitCode.HasValue ? result.ExitCode.Value.ToString() : "n/a")}");
        builder.AppendLine($"Duration:  {result.Duration:mm\\:ss}");
        return builder.ToString();
    }

    static string FormatDevResult(LocalDevActionResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("LOCAL_DEV_RESULT");
        builder.AppendLine($"Action:    {result.Action}");
        builder.AppendLine($"Status:    {(result.Succeeded ? "SUCCESS" : "FAILED")}");
        builder.AppendLine($"Reason:    {result.Reason}");
        builder.AppendLine($"PID:       {(result.StartedProcessId.HasValue ? result.StartedProcessId.Value.ToString() : "n/a")}");
        builder.AppendLine($"Log:       {result.LogPath ?? "n/a"}");
        builder.AppendLine($"Exit code: {(result.ExitCode.HasValue ? result.ExitCode.Value.ToString() : "n/a")}");
        builder.AppendLine($"Duration:  {result.Duration:mm\\:ss}");

        if (result.Processes.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Matched local processes:");
            foreach (var process in result.Processes)
            {
                builder.AppendLine($"- {(process.Stopped ? "stopped" : "failed")} {process.ProcessName} ({process.ProcessId})");
                if (!string.IsNullOrWhiteSpace(process.CommandLine))
                {
                    builder.AppendLine($"  {TrimForDisplay(process.CommandLine)}");
                }

                if (!string.IsNullOrWhiteSpace(process.Error))
                {
                    builder.AppendLine($"  ERR {process.Error}");
                }
            }
        }

        return builder.ToString();
    }

    static string FormatResult(StatusCheckResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("STATUS_RESULT");
        builder.AppendLine($"Checked UTC: {result.CheckedAtUtc:O}");
        builder.AppendLine($"Remote:      {result.RemoteServer}");
        builder.AppendLine($"Service:     {result.ServiceName}");
        builder.AppendLine($"Path:        {result.RemotePath}");
        builder.AppendLine($"Status:      {result.Status}");
        builder.AppendLine($"Reason:      {result.Reason}");
        builder.AppendLine($"ServiceState:{result.ServiceStatus ?? "n/a"}");
        builder.AppendLine($"Log on as:   {result.ServiceStartName ?? "n/a"}");
        builder.AppendLine($"Path name:   {result.ServicePathName ?? "n/a"}");
        builder.AppendLine($"Svc exit:    {(result.ServiceExitCode.HasValue ? result.ServiceExitCode.Value.ToString() : "n/a")}");
        builder.AppendLine($"Svc-specific:{(result.ServiceSpecificExitCode.HasValue ? result.ServiceSpecificExitCode.Value.ToString() : "n/a")}");
        builder.AppendLine($"Process:     {(result.ProcessId.HasValue ? $"{result.ProcessName} ({result.ProcessId})" : "n/a")}");
        builder.AppendLine($"Started UTC: {result.ProcessStartTime ?? "n/a"}");
        builder.AppendLine($"Exit code:   {result.ToolExitCode}");

        if (!string.IsNullOrWhiteSpace(result.ToolError))
        {
            builder.AppendLine();
            builder.AppendLine("PowerShell stderr:");
            builder.AppendLine(result.ToolError);
        }

        if (!string.IsNullOrWhiteSpace(result.DeploymentInfo))
        {
            builder.AppendLine();
            builder.AppendLine("Deployment info:");
            builder.AppendLine(result.DeploymentInfo.Trim());
        }

        if (result.ConfigurationHints.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Configuration hints:");
            foreach (var hint in result.ConfigurationHints)
            {
                builder.AppendLine($"- {hint}");
            }
        }

        if (result.Events.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Recent Windows events:");
            foreach (var item in result.Events)
            {
                var source = string.Join("/", new[] { item.LogName, item.ProviderName }.Where(value => !string.IsNullOrWhiteSpace(value)));
                builder.AppendLine($"- {item.TimeCreated}  {source}  Event {item.Id}  {item.LevelDisplayName}");
                builder.AppendLine($"  {item.Message}");
            }
        }

        return builder.ToString();
    }

    static string TrimForDisplay(string value)
    {
        const int maxLength = 220;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
