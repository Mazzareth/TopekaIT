namespace TopekaIT.DeploymentChecker;

/// <summary>
/// Result of a local dev action like start, stop, or health check.
/// </summary>
public sealed class LocalDevActionResult
{
    public string Action { get; set; } = "";
    public bool Succeeded { get; set; }
    public string Reason { get; set; } = "";
    public int? ExitCode { get; set; }
    public int? StartedProcessId { get; set; }
    public string? LogPath { get; set; }
    public TimeSpan Duration { get; set; }
    public List<LocalProcessAction> Processes { get; set; } = new();
}

/// <summary>
/// One process action the local runner attempted, useful when the UI needs to show what it stopped or started.
/// </summary>
public sealed class LocalProcessAction
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public string? ExecutablePath { get; set; }
    public string? CommandLine { get; set; }
    public bool Stopped { get; set; }
    public string? Error { get; set; }
}
