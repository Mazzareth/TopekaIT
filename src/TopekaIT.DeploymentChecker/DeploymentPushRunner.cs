using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace TopekaIT.DeploymentChecker;

public sealed class DeploymentPushRunner
{
    public async Task<DeploymentPushResult> PushAsync(
        DeploymentSettings settings,
        IProgress<string> log,
        CancellationToken ct = default)
    {
        var started = DateTimeOffset.Now;
        var stamp = started.ToString("yyyyMMdd-HHmmss");
        var root = FindRepositoryRoot();
        var publishDirectory = Path.Combine(root, "publish-temp");
        var archivePath = Path.Combine(root, $"publish-temp-{stamp}.zip");
        var remoteArchive = $@"C:\temp_deploy-{stamp}.zip";
        var webProject = Path.Combine(root, "src", "TopekaIT.Web", "TopekaIT.Web.csproj");

        try
        {
            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                return Fail(stamp, archivePath, "Enter the remote administrator password before pushing updates.");
            }

            log.Report($"deploy stamp {stamp}");
            log.Report($"repo root {root}");
            log.Report("0. cleaning previous publish output and stale deployment archives");
            DeleteDirectoryIfExists(publishDirectory);
            DeleteFileIfExists(Path.Combine(root, "publish-temp.zip"));
            foreach (var file in Directory.EnumerateFiles(root, "publish-temp-*.zip"))
            {
                DeleteFileIfExists(file);
            }

            log.Report("1. publishing TopekaIT.Web release build only");
            var publishExit = await RunProcessAsync(
                "dotnet",
                [
                    "publish",
                    webProject,
                    "-c",
                    "Release",
                    "-r",
                    "win-x64",
                    "--self-contained",
                    "true",
                    "-o",
                    publishDirectory,
                ],
                root,
                log,
                ct);

            if (publishExit != 0)
            {
                return Fail(stamp, archivePath, $"dotnet publish failed with exit code {publishExit}.", publishExit);
            }

            log.Report("2. zipping published files");
            ZipPublishDirectory(publishDirectory, archivePath);
            ValidateWebArchive(archivePath);
            log.Report($"archive ready {archivePath}");
            log.Report("archive guard passed: deployment console is not included");

            log.Report("3. opening remote session and stopping service");
            log.Report("4. transferring zip file to remote server");
            log.Report("5. extracting files on server");
            log.Report("6. starting remote service");
            var deployExit = await RunRemoteDeployAsync(
                settings,
                archivePath,
                remoteArchive,
                stamp,
                started,
                log,
                ct);

            if (deployExit != 0)
            {
                return Fail(stamp, archivePath, $"remote deployment failed with exit code {deployExit}.", deployExit);
            }

            log.Report("7. cleaning local deployment artifacts");
            DeleteDirectoryIfExists(publishDirectory);
            DeleteFileIfExists(archivePath);
            log.Report("deployment complete");

            return new DeploymentPushResult
            {
                Succeeded = true,
                DeploymentStamp = stamp,
                ArchivePath = archivePath,
                Reason = "Deployment completed.",
                Duration = DateTimeOffset.Now - started,
            };
        }
        catch (Exception ex)
        {
            return Fail(stamp, archivePath, ex.Message);
        }
    }

    static DeploymentPushResult Fail(string stamp, string archivePath, string reason, int? exitCode = null) => new()
    {
        Succeeded = false,
        DeploymentStamp = stamp,
        ArchivePath = archivePath,
        Reason = reason,
        ExitCode = exitCode,
    };

    static async Task<int> RunRemoteDeployAsync(
        DeploymentSettings settings,
        string archivePath,
        string remoteArchive,
        string stamp,
        DateTimeOffset started,
        IProgress<string> log,
        CancellationToken ct)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"topeka-deploy-{Guid.NewGuid():N}.ps1");

        try
        {
            await File.WriteAllTextAsync(scriptPath, RemoteDeployScript, Encoding.UTF8, ct);

            var start = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            start.ArgumentList.Add("-ExecutionPolicy");
            start.ArgumentList.Add("Bypass");
            start.ArgumentList.Add("-File");
            start.ArgumentList.Add(scriptPath);
            start.ArgumentList.Add("-RemoteServer");
            start.ArgumentList.Add(settings.RemoteServer);
            start.ArgumentList.Add("-RemotePath");
            start.ArgumentList.Add(settings.RemotePath);
            start.ArgumentList.Add("-ServiceName");
            start.ArgumentList.Add(settings.ServiceName);
            start.ArgumentList.Add("-Username");
            start.ArgumentList.Add(settings.Username);
            start.ArgumentList.Add("-LocalArchive");
            start.ArgumentList.Add(archivePath);
            start.ArgumentList.Add("-RemoteArchive");
            start.ArgumentList.Add(remoteArchive);
            start.ArgumentList.Add("-DeploymentStamp");
            start.ArgumentList.Add(stamp);
            start.ArgumentList.Add("-StartedLocal");
            start.ArgumentList.Add(started.ToString("yyyy-MM-dd HH:mm:ss"));
            start.ArgumentList.Add("-StartedUtc");
            start.ArgumentList.Add(started.ToUniversalTime().ToString("o"));
            start.ArgumentList.Add("-SourceMachine");
            start.ArgumentList.Add(Environment.MachineName);
            start.ArgumentList.Add("-SourceUser");
            start.ArgumentList.Add(Environment.UserName);
            start.Environment["TOPEKA_CHECKER_PASSWORD"] = settings.Password;

            return await RunProcessAsync(start, log, ct);
        }
        finally
        {
            DeleteFileIfExists(scriptPath);
        }
    }

    static async Task<int> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        IProgress<string> log,
        CancellationToken ct)
    {
        var start = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        return await RunProcessAsync(start, log, ct);
    }

    static async Task<int> RunProcessAsync(ProcessStartInfo start, IProgress<string> log, CancellationToken ct)
    {
        using var process = new Process { StartInfo = start };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                log.Report(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                log.Report($"ERR {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }

    static void ZipPublishDirectory(string publishDirectory, string archivePath)
    {
        DeleteFileIfExists(archivePath);
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

        foreach (var file in Directory.EnumerateFiles(publishDirectory, "*", SearchOption.AllDirectories))
        {
            var entryName = Path.GetRelativePath(publishDirectory, file).Replace('\\', '/');
            archive.CreateEntryFromFile(file, entryName, CompressionLevel.Fastest);
        }
    }

    static void ValidateWebArchive(string archivePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var hasWebHost = archive.Entries.Any(entry =>
            string.Equals(entry.FullName, "TopekaIT.Web.dll", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.FullName, "TopekaIT.Web.exe", StringComparison.OrdinalIgnoreCase));
        if (!hasWebHost)
        {
            throw new InvalidOperationException("Publish archive does not contain the TopekaIT.Web host.");
        }

        var blockedEntry = archive.Entries.FirstOrDefault(entry =>
            entry.FullName.Contains("TopekaIT.DeploymentChecker", StringComparison.OrdinalIgnoreCase));
        if (blockedEntry is not null)
        {
            throw new InvalidOperationException($"Publish archive contains the deployment console: {blockedEntry.FullName}");
        }
    }

    static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "6IA-IT-Portal.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    static void DeleteFileIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Cleanup is best-effort; later steps surface real deployment failures.
        }
    }

    static void DeleteDirectoryIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Cleanup is best-effort; later steps surface real deployment failures.
        }
    }

    const string RemoteDeployScript = """
param(
    [Parameter(Mandatory = $true)][string]$RemoteServer,
    [Parameter(Mandatory = $true)][string]$RemotePath,
    [Parameter(Mandatory = $true)][string]$ServiceName,
    [Parameter(Mandatory = $true)][string]$Username,
    [Parameter(Mandatory = $true)][string]$LocalArchive,
    [Parameter(Mandatory = $true)][string]$RemoteArchive,
    [Parameter(Mandatory = $true)][string]$DeploymentStamp,
    [Parameter(Mandatory = $true)][string]$StartedLocal,
    [Parameter(Mandatory = $true)][string]$StartedUtc,
    [Parameter(Mandatory = $true)][string]$SourceMachine,
    [Parameter(Mandatory = $true)][string]$SourceUser
)

$ErrorActionPreference = "Stop"

function New-CheckerSecureString {
    param([Parameter(Mandatory = $true)][string]$Value)

    $secure = [System.Security.SecureString]::new()
    foreach ($character in $Value.ToCharArray()) {
        $secure.AppendChar($character)
    }
    $secure.MakeReadOnly()
    return $secure
}

$password = $env:TOPEKA_CHECKER_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    Write-Error "Remote administrator password is required."
    exit 2
}

$session = $null

try {
    Write-Output "creating remote session $RemoteServer"
    $securePassword = New-CheckerSecureString -Value $password
    $credential = New-Object System.Management.Automation.PSCredential ($Username, $securePassword)
    $session = New-PSSession -ComputerName $RemoteServer -Credential $credential -ErrorAction Stop

    Write-Output "stopping service $ServiceName"
    Invoke-Command -Session $session -ArgumentList $ServiceName -ScriptBlock {
        param($ServiceName)

        $service = Get-Service -Name $ServiceName -ErrorAction Stop
        if ($service.Status -ne "Stopped") {
            Stop-Service -Name $ServiceName -ErrorAction Stop
            (Get-Service -Name $ServiceName).WaitForStatus("Stopped", "00:00:45")
        }
    }

    Write-Output "copying archive to $RemoteArchive"
    Copy-Item -Path $LocalArchive -Destination $RemoteArchive -ToSession $session -Force -ErrorAction Stop

    Write-Output "extracting archive into $RemotePath"
    Invoke-Command -Session $session -ArgumentList $RemoteArchive, $RemotePath, $DeploymentStamp, $StartedLocal, $StartedUtc, $SourceMachine, $SourceUser -ScriptBlock {
        param($RemoteArchive, $RemotePath, $DeploymentStamp, $StartedLocal, $StartedUtc, $SourceMachine, $SourceUser)

        if (-not (Test-Path $RemotePath)) {
            New-Item -ItemType Directory -Path $RemotePath -Force | Out-Null
        }

        Expand-Archive -Path $RemoteArchive -DestinationPath $RemotePath -Force

        @(
            "DeploymentStamp=$DeploymentStamp",
            "StartedLocal=$StartedLocal",
            "StartedUtc=$StartedUtc",
            "SourceMachine=$SourceMachine",
            "SourceUser=$SourceUser"
        ) | Set-Content -Path (Join-Path $RemotePath "deployment-info.txt") -Encoding UTF8

        Remove-Item -Path $RemoteArchive -Force -ErrorAction SilentlyContinue
    }

    Write-Output "starting service $ServiceName"
    Invoke-Command -Session $session -ArgumentList $ServiceName -ScriptBlock {
        param($ServiceName)

        Start-Service -Name $ServiceName -ErrorAction Stop
        (Get-Service -Name $ServiceName).WaitForStatus("Running", "00:00:45")
    }

    Write-Output "remote deployment complete"
    exit 0
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
finally {
    if ($session) {
        Remove-PSSession -Session $session
    }
}
""";
}
