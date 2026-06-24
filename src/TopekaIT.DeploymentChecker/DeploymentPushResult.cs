namespace TopekaIT.DeploymentChecker;

/// <summary>
/// Result of pushing the portal to the remote host. Keeps the status, summary, and log text together.
/// </summary>
public sealed class DeploymentPushResult
{
    public bool Succeeded { get; init; }
    public string DeploymentStamp { get; init; } = "";
    public string ArchivePath { get; init; } = "";
    public string Reason { get; init; } = "";
    public int? ExitCode { get; init; }
    public TimeSpan Duration { get; init; }
}
