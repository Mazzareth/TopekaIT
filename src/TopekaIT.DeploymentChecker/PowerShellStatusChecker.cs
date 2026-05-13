using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TopekaIT.DeploymentChecker;

public sealed class PowerShellStatusChecker
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<StatusCheckResult> CheckAsync(DeploymentSettings settings, CancellationToken ct = default)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"topeka-checker-{Guid.NewGuid():N}.ps1");

        try
        {
            await File.WriteAllTextAsync(scriptPath, RemoteCheckScript, Encoding.UTF8, ct);

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
            start.Environment["TOPEKA_CHECKER_PASSWORD"] = settings.Password;

            using var process = new Process { StartInfo = start };
            var output = new StringBuilder();
            var error = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null) output.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null) error.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(ct);

            var result = ParseResult(output.ToString(), settings);
            result.ToolExitCode = process.ExitCode;
            result.ToolError = error.ToString().Trim();
            return result;
        }
        catch (Exception ex)
        {
            return new StatusCheckResult
            {
                CheckedAtUtc = DateTimeOffset.UtcNow,
                RemoteServer = settings.RemoteServer,
                ServiceName = settings.ServiceName,
                RemotePath = settings.RemotePath,
                IsOnline = false,
                Status = "Checker failed",
                Reason = ex.Message,
            };
        }
        finally
        {
            try
            {
                if (File.Exists(scriptPath))
                {
                    File.Delete(scriptPath);
                }
            }
            catch
            {
                // Temp script cleanup failure should not hide the actual check result.
            }
        }
    }

    static StatusCheckResult ParseResult(string output, DeploymentSettings settings)
    {
        var jsonStart = output.IndexOf('{', StringComparison.Ordinal);
        var jsonEnd = output.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            return new StatusCheckResult
            {
                CheckedAtUtc = DateTimeOffset.UtcNow,
                RemoteServer = settings.RemoteServer,
                ServiceName = settings.ServiceName,
                RemotePath = settings.RemotePath,
                IsOnline = false,
                Status = "No checker result",
                Reason = string.IsNullOrWhiteSpace(output)
                    ? "PowerShell did not return a status payload."
                    : output.Trim(),
            };
        }

        var json = output.Substring(jsonStart, jsonEnd - jsonStart + 1);
        return JsonSerializer.Deserialize<StatusCheckResult>(json, JsonOptions) ?? new StatusCheckResult
        {
            CheckedAtUtc = DateTimeOffset.UtcNow,
            RemoteServer = settings.RemoteServer,
            ServiceName = settings.ServiceName,
            RemotePath = settings.RemotePath,
            IsOnline = false,
            Status = "Invalid checker result",
            Reason = "PowerShell returned a status payload that could not be parsed.",
        };
    }

    const string RemoteCheckScript = """
param(
    [Parameter(Mandatory = $true)][string]$RemoteServer,
    [Parameter(Mandatory = $true)][string]$RemotePath,
    [Parameter(Mandatory = $true)][string]$ServiceName,
    [Parameter(Mandatory = $true)][string]$Username
)

$ErrorActionPreference = "Stop"

function New-BaseResult {
    [ordered]@{
        CheckedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        RemoteServer = $RemoteServer
        ServiceName = $ServiceName
        RemotePath = $RemotePath
        IsOnline = $false
        Status = "Offline"
        Reason = ""
        ServiceStatus = $null
        ProcessId = $null
        ProcessName = $null
        ProcessStartTime = $null
        DeploymentInfo = $null
        Events = @()
    }
}

function Write-ResultAndExit {
    param(
        [Parameter(Mandatory = $true)]$Result,
        [Parameter(Mandatory = $true)][int]$ExitCode
    )

    $Result | ConvertTo-Json -Depth 8
    exit $ExitCode
}

function New-CheckerSecureString {
    param(
        [Parameter(Mandatory = $true)][string]$Value
    )

    $secure = [System.Security.SecureString]::new()
    foreach ($character in $Value.ToCharArray()) {
        $secure.AppendChar($character)
    }
    $secure.MakeReadOnly()
    return $secure
}

$result = New-BaseResult
$password = $env:TOPEKA_CHECKER_PASSWORD

if ([string]::IsNullOrWhiteSpace($password)) {
    $result.Status = "Missing password"
    $result.Reason = "Enter the remote administrator password before checking."
    Write-ResultAndExit -Result $result -ExitCode 2
}

$session = $null

try {
    $securePassword = New-CheckerSecureString -Value $password
    $credential = New-Object System.Management.Automation.PSCredential ($Username, $securePassword)
    $session = New-PSSession -ComputerName $RemoteServer -Credential $credential -ErrorAction Stop
}
catch {
    $result.Status = "Remote session failed"
    $result.Reason = "Could not open a remote PowerShell session: $($_.Exception.Message)"
    Write-ResultAndExit -Result $result -ExitCode 3
}

try {
    $remoteResult = Invoke-Command -Session $session -ArgumentList $ServiceName, $RemotePath -ScriptBlock {
        param($ServiceName, $RemotePath)

        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        $serviceDetails = Get-CimInstance -ClassName Win32_Service | Where-Object { $_.Name -eq $ServiceName } | Select-Object -First 1
        $deploymentInfo = $null
        $deploymentInfoPath = Join-Path $RemotePath "deployment-info.txt"

        if (Test-Path $deploymentInfoPath) {
            $deploymentInfo = Get-Content -Path $deploymentInfoPath -Raw -ErrorAction SilentlyContinue
        }

        $events = @()

        if ($null -eq $service) {
            try {
                $events = Get-WinEvent -FilterHashtable @{
                    LogName = "System"
                    ProviderName = "Service Control Manager"
                    StartTime = (Get-Date).AddDays(-2)
                } -MaxEvents 100 -ErrorAction Stop |
                    Where-Object { $_.Message -like "*$ServiceName*" } |
                    Select-Object -First 8 @{
                        Name = "TimeCreated"
                        Expression = { $_.TimeCreated.ToString("o") }
                    }, Id, LevelDisplayName, @{
                        Name = "Message"
                        Expression = { $_.Message -replace "`r?`n", " " }
                    }
            }
            catch {
                $events = @()
            }

            return [pscustomobject]@{
                IsOnline = $false
                Status = "Service missing"
                Reason = "Service '$ServiceName' was not found on the remote server."
                ServiceStatus = $null
                ProcessId = $null
                ProcessName = $null
                ProcessStartTime = $null
                DeploymentInfo = $deploymentInfo
                Events = @($events)
            }
        }

        $process = $null
        if ($serviceDetails -and $serviceDetails.ProcessId -gt 0) {
            $process = Get-Process -Id $serviceDetails.ProcessId -ErrorAction SilentlyContinue
        }

        $isOnline = $service.Status -eq "Running" -and $null -ne $process
        $reason = "Service is running."

        if ($service.Status -ne "Running") {
            $reason = "Service status is $($service.Status)."
        }
        elseif ($null -eq $process) {
            $reason = "Service reports Running, but no backing process was found."
        }

        if (-not $isOnline) {
            try {
                $events = Get-WinEvent -FilterHashtable @{
                    LogName = "System"
                    ProviderName = "Service Control Manager"
                    StartTime = (Get-Date).AddDays(-2)
                } -MaxEvents 100 -ErrorAction Stop |
                    Where-Object { $_.Message -like "*$ServiceName*" } |
                    Select-Object -First 8 @{
                        Name = "TimeCreated"
                        Expression = { $_.TimeCreated.ToString("o") }
                    }, Id, LevelDisplayName, @{
                        Name = "Message"
                        Expression = { $_.Message -replace "`r?`n", " " }
                    }
            }
            catch {
                $events = @()
            }
        }

        [pscustomobject]@{
            IsOnline = $isOnline
            Status = if ($isOnline) { "Online" } else { "Offline" }
            Reason = $reason
            ServiceStatus = $service.Status.ToString()
            ProcessId = if ($process) { $process.Id } else { $null }
            ProcessName = if ($process) { $process.ProcessName } else { $null }
            ProcessStartTime = if ($process) { $process.StartTime.ToUniversalTime().ToString("o") } else { $null }
            DeploymentInfo = $deploymentInfo
            Events = @($events)
        }
    } -ErrorAction Stop

    $result.IsOnline = [bool]$remoteResult.IsOnline
    $result.Status = [string]$remoteResult.Status
    $result.Reason = [string]$remoteResult.Reason
    $result.ServiceStatus = $remoteResult.ServiceStatus
    $result.ProcessId = $remoteResult.ProcessId
    $result.ProcessName = $remoteResult.ProcessName
    $result.ProcessStartTime = $remoteResult.ProcessStartTime
    $result.DeploymentInfo = $remoteResult.DeploymentInfo
    if ($null -eq $remoteResult.Events) {
        $result.Events = @()
    }
    else {
        $result.Events = @($remoteResult.Events)
    }

    if ($result.IsOnline) {
        Write-ResultAndExit -Result $result -ExitCode 0
    }

    Write-ResultAndExit -Result $result -ExitCode 1
}
catch {
    $result.Status = "Remote command failed"
    $result.Reason = "Remote session opened, but the status command failed: $($_.Exception.Message)"
    Write-ResultAndExit -Result $result -ExitCode 4
}
finally {
    if ($session) {
        Remove-PSSession -Session $session
    }
}
""";
}
