namespace TopekaIT.DeploymentChecker;

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

public sealed class LocalProcessAction
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public string? ExecutablePath { get; set; }
    public string? CommandLine { get; set; }
    public bool Stopped { get; set; }
    public string? Error { get; set; }
}
