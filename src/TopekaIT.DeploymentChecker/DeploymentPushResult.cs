namespace TopekaIT.DeploymentChecker;

public sealed class DeploymentPushResult
{
    public bool Succeeded { get; init; }
    public string DeploymentStamp { get; init; } = "";
    public string ArchivePath { get; init; } = "";
    public string Reason { get; init; } = "";
    public int? ExitCode { get; init; }
    public TimeSpan Duration { get; init; }
}
