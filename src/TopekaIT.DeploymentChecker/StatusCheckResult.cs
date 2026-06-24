namespace TopekaIT.DeploymentChecker;

/// <summary>
/// Remote status result shown by the checker window.
/// </summary>
public sealed class StatusCheckResult
{
    public DateTimeOffset CheckedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string RemoteServer { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string RemotePath { get; set; } = "";
    public bool IsOnline { get; set; }
    public string Status { get; set; } = "Unknown";
    public string Reason { get; set; } = "";
    public string? ServiceStatus { get; set; }
    public string? ServiceStartName { get; set; }
    public string? ServicePathName { get; set; }
    public int? ServiceExitCode { get; set; }
    public int? ServiceSpecificExitCode { get; set; }
    public int? ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public string? ProcessStartTime { get; set; }
    public string? DeploymentInfo { get; set; }
    public List<string> ConfigurationHints { get; set; } = new();
    public List<RemoteEventEntry> Events { get; set; } = new();

    public string? ToolError { get; set; }

    public int ToolExitCode { get; set; }
}

/// <summary>
/// One remote event line from the status script.
/// </summary>
public sealed class RemoteEventEntry
{
    public string? TimeCreated { get; set; }
    public string? LogName { get; set; }
    public string? ProviderName { get; set; }
    public int Id { get; set; }
    public string? LevelDisplayName { get; set; }
    public string? Message { get; set; }
}
