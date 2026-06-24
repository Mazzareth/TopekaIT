using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TopekaIT.DeploymentChecker;

/// <summary>
/// Starts, stops, and checks the local portal process used for quick verification.
/// </summary>
public sealed class LocalDevRunner
{
    public const string LocalUrl = "http://localhost:5117";
    public const string LocalBindUrl = "http://0.0.0.0:5117";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    readonly List<Process> _startedProcesses = new();

    public async Task<LocalDevActionResult> KillLocalAppProcessesAsync(
        IProgress<string> log,
        CancellationToken ct = default)
    {
        var started = DateTimeOffset.Now;
        var root = FindRepositoryRoot();
        var webProject = GetWebProjectPath(root);

        log.Report($"repo root {root}");
        log.Report("searching for local TopekaIT.Web processes");

        var scriptPath = Path.Combine(Path.GetTempPath(), $"topeka-local-kill-{Guid.NewGuid():N}.ps1");

        try
        {
            await File.WriteAllTextAsync(scriptPath, KillLocalProcessesScript, Encoding.UTF8, ct);

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
            start.ArgumentList.Add("-RepositoryRoot");
            start.ArgumentList.Add(root);
            start.ArgumentList.Add("-WebProjectPath");
            start.ArgumentList.Add(webProject);

            var output = new StringBuilder();
            var error = new StringBuilder();
            var exitCode = await RunProcessAsync(start, line => output.AppendLine(line), line => error.AppendLine(line), ct);
            var result = ParseKillResult(output.ToString());

            foreach (var process in result.Processes)
            {
                var state = process.Stopped ? "stopped" : "failed";
                log.Report($"{state} local process {process.ProcessName} ({process.ProcessId})");
                if (!string.IsNullOrWhiteSpace(process.Error))
                {
                    log.Report($"ERR {process.Error}");
                }
            }

            result.Action = "Kill local app processes";
            result.ExitCode = exitCode;
            result.Duration = DateTimeOffset.Now - started;

            if (exitCode != 0)
            {
                result.Succeeded = false;
                result.Reason = string.IsNullOrWhiteSpace(error.ToString())
                    ? $"Local kill script failed with exit code {exitCode}."
                    : error.ToString().Trim();
                return result;
            }

            var failedCount = result.Processes.Count(process => !process.Stopped);
            if (failedCount > 0)
            {
                result.Succeeded = false;
                result.Reason = $"Matched {result.Processes.Count} local app process(es), but {failedCount} could not be stopped.";
                return result;
            }

            result.Succeeded = true;
            result.Reason = result.Processes.Count == 0
                ? "No matching local app processes were running."
                : $"Stopped {result.Processes.Count} local app process(es).";
            return result;
        }
        catch (Exception ex)
        {
            return new LocalDevActionResult
            {
                Action = "Kill local app processes",
                Succeeded = false,
                Reason = ex.Message,
                Duration = DateTimeOffset.Now - started,
            };
        }
        finally
        {
            DeleteFileIfExists(scriptPath);
        }
    }

    public async Task<LocalDevActionResult> BuildAndRunAsync(
        IProgress<string> log,
        CancellationToken ct = default)
    {
        var started = DateTimeOffset.Now;
        var stamp = started.ToString("yyyyMMdd-HHmmss");
        var root = FindRepositoryRoot();
        var webProject = GetWebProjectPath(root);
        var runLogPath = Path.Combine(SettingsStore.AppDirectory, $"dev-run-{stamp}.log");

        try
        {
            Directory.CreateDirectory(SettingsStore.AppDirectory);

            log.Report($"repo root {root}");
            log.Report("stopping existing local app process before build");
            var killResult = await KillLocalAppProcessesAsync(log, ct);
            if (!killResult.Succeeded)
            {
                killResult.Action = "Build and run local app";
                killResult.Duration = DateTimeOffset.Now - started;
                return killResult;
            }

            log.Report("building TopekaIT.Web debug build");
            var buildExit = await RunProcessAsync(
                "dotnet",
                ["build", webProject, "-c", "Debug"],
                root,
                line => log.Report(line),
                line => log.Report($"ERR {line}"),
                ct);

            if (buildExit != 0)
            {
                return new LocalDevActionResult
                {
                    Action = "Build and run local app",
                    Succeeded = false,
                    Reason = $"dotnet build failed with exit code {buildExit}.",
                    ExitCode = buildExit,
                    LogPath = runLogPath,
                    Duration = DateTimeOffset.Now - started,
                    Processes = killResult.Processes,
                };
            }

            log.Report($"starting local app at {LocalUrl}");
            log.Report($"run log {runLogPath}");
            var process = StartLocalWebProcess(root, webProject, runLogPath);

            return new LocalDevActionResult
            {
                Action = "Build and run local app",
                Succeeded = true,
                Reason = $"Local app started at {LocalUrl}.",
                StartedProcessId = process.Id,
                LogPath = runLogPath,
                Duration = DateTimeOffset.Now - started,
                Processes = killResult.Processes,
            };
        }
        catch (Exception ex)
        {
            return new LocalDevActionResult
            {
                Action = "Build and run local app",
                Succeeded = false,
                Reason = ex.Message,
                LogPath = runLogPath,
                Duration = DateTimeOffset.Now - started,
            };
        }
    }

    public static string GetWebProjectPath() => GetWebProjectPath(FindRepositoryRoot());

    static string GetWebProjectPath(string root) => Path.Combine(root, "src", "TopekaIT.Web", "TopekaIT.Web.csproj");

    Process StartLocalWebProcess(string root, string webProject, string runLogPath)
    {
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--project");
        start.ArgumentList.Add(webProject);
        start.ArgumentList.Add("--no-build");
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        start.Environment["ASPNETCORE_URLS"] = LocalBindUrl;

        var process = new Process
        {
            StartInfo = start,
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += (_, e) => AppendRunLog(runLogPath, e.Data);
        process.ErrorDataReceived += (_, e) => AppendRunLog(runLogPath, e.Data is null ? null : $"ERR {e.Data}");
        process.Exited += (_, _) =>
        {
            AppendRunLog(runLogPath, $"process exited with code {process.ExitCode}");
            lock (_startedProcesses)
            {
                _startedProcesses.Remove(process);
            }

            process.Dispose();
        };

        process.Start();
        lock (_startedProcesses)
        {
            _startedProcesses.Add(process);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    static LocalDevActionResult ParseKillResult(string output)
    {
        var jsonStart = output.IndexOf('{', StringComparison.Ordinal);
        var jsonEnd = output.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            return new LocalDevActionResult
            {
                Reason = string.IsNullOrWhiteSpace(output)
                    ? "Local kill script returned no process result."
                    : output.Trim(),
            };
        }

        var json = output.Substring(jsonStart, jsonEnd - jsonStart + 1);
        return JsonSerializer.Deserialize<LocalDevActionResult>(json, JsonOptions) ?? new LocalDevActionResult
        {
            Reason = "Local kill script returned an invalid process result.",
        };
    }

    static async Task<int> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        Action<string> output,
        Action<string> error,
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

        return await RunProcessAsync(start, output, error, ct);
    }

    static async Task<int> RunProcessAsync(
        ProcessStartInfo start,
        Action<string> output,
        Action<string> error,
        CancellationToken ct)
    {
        using var process = new Process { StartInfo = start };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                output(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                error(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
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

    static void AppendRunLog(string path, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? SettingsStore.AppDirectory);
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
        }
        catch
        {
            // The process should keep running even if local log writes fail.
        }
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
            // Temp script cleanup should not hide the real dev action result.
        }
    }

    const string KillLocalProcessesScript = """
param(
    [Parameter(Mandatory = $true)][string]$RepositoryRoot,
    [Parameter(Mandatory = $true)][string]$WebProjectPath
)

$ErrorActionPreference = "Stop"

function Test-ContainsText {
    param(
        [AllowNull()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Needle
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return $Value.IndexOf($Needle, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

$root = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\')
$webProject = [System.IO.Path]::GetFullPath($WebProjectPath)
$relativeBackslash = "src\TopekaIT.Web\TopekaIT.Web.csproj"
$relativeSlash = "src/TopekaIT.Web/TopekaIT.Web.csproj"

# Match only this repo's local web host, not every dotnet or shell process on the machine.
$matches = Get-CimInstance Win32_Process | Where-Object {
    $processId = [int]$_.ProcessId
    if ($processId -eq $PID) {
        return $false
    }

    $name = [string]$_.Name
    $path = [string]$_.ExecutablePath
    $command = [string]$_.CommandLine
    $mentionsProject = (Test-ContainsText $command $webProject) -or
        ((Test-ContainsText $command "TopekaIT.Web.csproj") -and
            ((Test-ContainsText $command $root) -or
             (Test-ContainsText $command $relativeBackslash) -or
             (Test-ContainsText $command $relativeSlash)))

    $isPublishedWeb = $name.Equals("TopekaIT.Web.exe", [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-ContainsText $path $root)
    $isDotnetRun = $name.Equals("dotnet.exe", [System.StringComparison]::OrdinalIgnoreCase) -and $mentionsProject
    $isShellWrapper = @("powershell.exe", "pwsh.exe", "cmd.exe").Contains($name.ToLowerInvariant()) -and $mentionsProject

    return $isPublishedWeb -or $isDotnetRun -or $isShellWrapper
}

$results = @()

foreach ($process in @($matches)) {
    $item = [ordered]@{
        ProcessId = [int]$process.ProcessId
        ProcessName = [string]$process.Name
        ExecutablePath = [string]$process.ExecutablePath
        CommandLine = [string]$process.CommandLine
        Stopped = $false
        Error = $null
    }

    try {
        Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
        $item.Stopped = $true
    }
    catch {
        $item.Error = $_.Exception.Message
    }

    $results += [pscustomobject]$item
}

[pscustomobject]@{
    Processes = @($results)
} | ConvertTo-Json -Depth 6
""";
}
