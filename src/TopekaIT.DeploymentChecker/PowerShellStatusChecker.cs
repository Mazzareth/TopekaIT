using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TopekaIT.DeploymentChecker;

/// <summary>
/// Runs remote status checks through PowerShell and turns the script output into UI-friendly results.
/// </summary>
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
        ServiceStartName = $null
        ServicePathName = $null
        ServiceExitCode = $null
        ServiceSpecificExitCode = $null
        ProcessId = $null
        ProcessName = $null
        ProcessStartTime = $null
        DeploymentInfo = $null
        ConfigurationHints = @()
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

        function Convert-EventForResult {
            param([Parameter(Mandatory = $true)]$Event)

            [pscustomobject]@{
                TimeCreated = $Event.TimeCreated.ToString("o")
                LogName = $Event.LogName
                ProviderName = $Event.ProviderName
                Id = $Event.Id
                LevelDisplayName = $Event.LevelDisplayName
                Message = $Event.Message -replace "`r?`n", " "
            }
        }

        function Get-RecentDiagnosticEvents {
            param(
                [Parameter(Mandatory = $true)][string]$ServiceName,
                [Parameter(Mandatory = $true)][string]$RemotePath
            )

            $events = @()
            $startTime = (Get-Date).AddDays(-2)

            try {
                $events += Get-WinEvent -FilterHashtable @{
                    LogName = "System"
                    ProviderName = "Service Control Manager"
                    StartTime = $startTime
                } -MaxEvents 100 -ErrorAction Stop |
                    Where-Object { $_.Message -like "*$ServiceName*" }
            }
            catch {
            }

            try {
                $remotePathPattern = if ([string]::IsNullOrWhiteSpace($RemotePath)) { $null } else { "*$RemotePath*" }
                $events += Get-WinEvent -FilterHashtable @{
                    LogName = "Application"
                    StartTime = $startTime
                } -MaxEvents 200 -ErrorAction Stop |
                    Where-Object {
                        $_.ProviderName -in @(".NET Runtime", "Application Error", "Windows Error Reporting") -or
                        $_.Message -like "*$ServiceName*" -or
                        $_.Message -like "*TopekaIT.Web*" -or
                        ($remotePathPattern -and $_.Message -like $remotePathPattern)
                    }
            }
            catch {
            }

            @($events) |
                Sort-Object TimeCreated -Descending |
                Select-Object -First 12 |
                ForEach-Object { Convert-EventForResult -Event $_ }
        }

        function Get-ConfigurationHints {
            param(
                [Parameter(Mandatory = $true)][string]$RemotePath,
                [Parameter(Mandatory = $true)]$ServiceDetails,
                [Parameter(Mandatory = $false)]$Process
            )

            $hints = @()
            $appSettingsPath = Join-Path $RemotePath "appsettings.json"

            if (-not (Test-Path $appSettingsPath)) {
                $hints += "Remote appsettings.json was not found at $appSettingsPath."
                return $hints
            }

            $settings = $null
            try {
                $settings = Get-Content -Path $appSettingsPath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
            }
            catch {
                $hints += "Remote appsettings.json could not be parsed: $($_.Exception.Message)"
                return $hints
            }

            foreach ($name in @("Topeka", "Master")) {
                $value = $settings.ConnectionStrings.$name
                if ([string]::IsNullOrWhiteSpace($value)) {
                    $hints += "ConnectionStrings:$name is missing or blank in remote appsettings.json."
                }
                elseif ($value -match "(?i)\(localdb\)|MSSQLLocalDB") {
                    $logon = if ($ServiceDetails -and -not [string]::IsNullOrWhiteSpace($ServiceDetails.StartName)) { $ServiceDetails.StartName } else { "the service account" }
                    $hints += "ConnectionStrings:$name points at LocalDB. Windows services running as $logon often cannot use a user-scoped LocalDB instance."
                }
            }

            try {
                $rawSettings = Get-Content -Path $appSettingsPath -Raw -ErrorAction Stop
                if ($rawSettings -match "5117") {
                    $listeners = @(Get-NetTCPConnection -LocalPort 5117 -State Listen -ErrorAction SilentlyContinue)
                    if ($listeners.Count -gt 0) {
                        $owners = @()
                        foreach ($listener in $listeners) {
                            $owner = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
                            if ($owner) {
                                $owners += "$($owner.ProcessName) ($($owner.Id))"
                            }
                        }

                        $currentProcessId = if ($Process) { $Process.Id } else { $null }
                        $otherListeners = @($listeners | Where-Object { -not $currentProcessId -or $_.OwningProcess -ne $currentProcessId })
                        if ($otherListeners.Count -gt 0) {
                            $ownerSummary = if ($owners.Count -gt 0) { $owners -join ", " } else { "unknown process" }
                            $hints += "Port 5117 is already listening on this server: $ownerSummary."
                        }
                    }
                }
            }
            catch {
            }

            return $hints
        }

        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        $serviceDetails = Get-CimInstance -ClassName Win32_Service | Where-Object { $_.Name -eq $ServiceName } | Select-Object -First 1
        $deploymentInfo = $null
        $deploymentInfoPath = Join-Path $RemotePath "deployment-info.txt"

        if (Test-Path $deploymentInfoPath) {
            $deploymentInfo = Get-Content -Path $deploymentInfoPath -Raw -ErrorAction SilentlyContinue
        }

        $configurationHints = @(Get-ConfigurationHints -RemotePath $RemotePath -ServiceDetails $serviceDetails)
        $events = @()

        if ($null -eq $service) {
            $events = @(Get-RecentDiagnosticEvents -ServiceName $ServiceName -RemotePath $RemotePath)

            return [pscustomobject]@{
                IsOnline = $false
                Status = "Service missing"
                Reason = "Service '$ServiceName' was not found on the remote server."
                ServiceStatus = $null
                ServiceStartName = $null
                ServicePathName = $null
                ServiceExitCode = $null
                ServiceSpecificExitCode = $null
                ProcessId = $null
                ProcessName = $null
                ProcessStartTime = $null
                DeploymentInfo = $deploymentInfo
                ConfigurationHints = @($configurationHints)
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

        $configurationHints = @(Get-ConfigurationHints -RemotePath $RemotePath -ServiceDetails $serviceDetails -Process $process)

        if (-not $isOnline) {
            $events = @(Get-RecentDiagnosticEvents -ServiceName $ServiceName -RemotePath $RemotePath)
        }

        [pscustomobject]@{
            IsOnline = $isOnline
            Status = if ($isOnline) { "Online" } else { "Offline" }
            Reason = $reason
            ServiceStatus = $service.Status.ToString()
            ServiceStartName = if ($serviceDetails) { $serviceDetails.StartName } else { $null }
            ServicePathName = if ($serviceDetails) { $serviceDetails.PathName } else { $null }
            ServiceExitCode = if ($serviceDetails) { $serviceDetails.ExitCode } else { $null }
            ServiceSpecificExitCode = if ($serviceDetails) { $serviceDetails.ServiceSpecificExitCode } else { $null }
            ProcessId = if ($process) { $process.Id } else { $null }
            ProcessName = if ($process) { $process.ProcessName } else { $null }
            ProcessStartTime = if ($process) { $process.StartTime.ToUniversalTime().ToString("o") } else { $null }
            DeploymentInfo = $deploymentInfo
            ConfigurationHints = @($configurationHints)
            Events = @($events)
        }
    } -ErrorAction Stop

    $result.IsOnline = [bool]$remoteResult.IsOnline
    $result.Status = [string]$remoteResult.Status
    $result.Reason = [string]$remoteResult.Reason
    $result.ServiceStatus = $remoteResult.ServiceStatus
    $result.ServiceStartName = $remoteResult.ServiceStartName
    $result.ServicePathName = $remoteResult.ServicePathName
    $result.ServiceExitCode = $remoteResult.ServiceExitCode
    $result.ServiceSpecificExitCode = $remoteResult.ServiceSpecificExitCode
    $result.ProcessId = $remoteResult.ProcessId
    $result.ProcessName = $remoteResult.ProcessName
    $result.ProcessStartTime = $remoteResult.ProcessStartTime
    $result.DeploymentInfo = $remoteResult.DeploymentInfo
    if ($null -eq $remoteResult.ConfigurationHints) {
        $result.ConfigurationHints = @()
    }
    else {
        $result.ConfigurationHints = @($remoteResult.ConfigurationHints)
    }
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
