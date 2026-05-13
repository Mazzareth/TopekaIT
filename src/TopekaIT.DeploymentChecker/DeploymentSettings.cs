namespace TopekaIT.DeploymentChecker;

public sealed class DeploymentSettings
{
    public string RemoteServer { get; set; } = "10.36.155.64";
    public string RemotePath { get; set; } = @"C:\Topeka Portal";
    public string ServiceName { get; set; } = "ItPortal";
    public string Username { get; set; } = "C5L9999";
    public string Password { get; set; } = "";

    public DeploymentSettings WithoutPassword() => new()
    {
        RemoteServer = RemoteServer,
        RemotePath = RemotePath,
        ServiceName = ServiceName,
        Username = Username,
    };
}
