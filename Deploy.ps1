$RemoteServer = "10.36.155.64"
$RemotePath = "C:\Topeka Portal"
$ServiceName = "ItPortal"
$Username = "C5L9999"
$DeploymentStarted = Get-Date
$DeploymentStamp = $DeploymentStarted.ToString("yyyyMMdd-HHmmss")
$DeploymentStartedUtc = $DeploymentStarted.ToUniversalTime().ToString("o")
$LocalArchive = ".\publish-temp-$DeploymentStamp.zip"
$RemoteArchive = "C:\temp_deploy-$DeploymentStamp.zip"
$DeploymentInfo = @(
    "DeploymentStamp=$DeploymentStamp",
    "StartedLocal=$($DeploymentStarted.ToString("yyyy-MM-dd HH:mm:ss"))",
    "StartedUtc=$DeploymentStartedUtc",
    "SourceMachine=$env:COMPUTERNAME",
    "SourceUser=$env:USERNAME"
)

Write-Host "Using saved administrator username for the server." -ForegroundColor Yellow
Write-Host "Deployment timestamp: $DeploymentStamp" -ForegroundColor Yellow
$passwordFromEnvironment = $env:TOPEKA_DEPLOY_PASSWORD
if ([string]::IsNullOrWhiteSpace($passwordFromEnvironment)) {
    $securePassword = Read-Host "Remote administrator password" -AsSecureString
}
else {
    $securePassword = ConvertTo-SecureString $passwordFromEnvironment -AsPlainText -Force
}

$cred = New-Object System.Management.Automation.PSCredential ($Username, $securePassword)
$session = New-PSSession -ComputerName $RemoteServer -Credential $cred

Write-Host "0. Cleaning previous publish output and stale build caches..." -ForegroundColor Cyan
Remove-Item -Path ".\publish-temp" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\publish-temp.zip" -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\publish-temp-*.zip" -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path ".\src" -Directory -Recurse -Force -Include "bin","obj" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "1. Publishing application..." -ForegroundColor Cyan
dotnet publish src/TopekaIT.Web/TopekaIT.Web.csproj -c Release -r win-x64 --self-contained true -o ./publish-temp

Write-Host "2. Zipping files for faster transfer..." -ForegroundColor Cyan
Compress-Archive -Path ".\publish-temp\*" -DestinationPath $LocalArchive -Force

Write-Host "3. Stopping remote service..." -ForegroundColor Cyan
Invoke-Command -Session $session -ScriptBlock { Stop-Service -Name $using:ServiceName }

Write-Host "4. Transferring zip file to $RemoteServer..." -ForegroundColor Cyan
Copy-Item -Path $LocalArchive -Destination $RemoteArchive -ToSession $session -Force

Write-Host "5. Extracting files on server..." -ForegroundColor Cyan
Invoke-Command -Session $session -ScriptBlock {
    $ErrorActionPreference = 'Continue'
    if (-not (Test-Path $using:RemotePath)) {
        New-Item -ItemType Directory -Path $using:RemotePath -Force | Out-Null
    }

    $configBackupDirectory = Join-Path $using:RemotePath (Join-Path "deployment-config-backups" $using:DeploymentStamp)
    $serverConfigFiles = @(Get-ChildItem -Path $using:RemotePath -Filter "appsettings*.json" -File -ErrorAction SilentlyContinue)
    if ($serverConfigFiles.Count -gt 0) {
        New-Item -ItemType Directory -Path $configBackupDirectory -Force | Out-Null
        foreach ($configFile in $serverConfigFiles) {
            Copy-Item -Path $configFile.FullName -Destination (Join-Path $configBackupDirectory $configFile.Name) -Force
        }

        Write-Output "Preserved $($serverConfigFiles.Count) existing remote appsettings file(s)."
    }

    Expand-Archive -Path $using:RemoteArchive -DestinationPath $using:RemotePath -Force

    if (Test-Path $configBackupDirectory) {
        foreach ($configFile in Get-ChildItem -Path $configBackupDirectory -Filter "appsettings*.json" -File -ErrorAction SilentlyContinue) {
            Copy-Item -Path $configFile.FullName -Destination (Join-Path $using:RemotePath $configFile.Name) -Force
        }

        Write-Output "Restored existing remote appsettings file(s)."
    }

    $deploymentInfoPath = Join-Path $using:RemotePath "deployment-info.txt"
    $using:DeploymentInfo | Set-Content -Path $deploymentInfoPath -Encoding UTF8
    Remove-Item -Path $using:RemoteArchive -Force -ErrorAction SilentlyContinue
}

Write-Host "6. Starting remote service..." -ForegroundColor Cyan
Invoke-Command -Session $session -ScriptBlock { Start-Service -Name $using:ServiceName }

Write-Host "7. Cleaning up..." -ForegroundColor Cyan
Remove-Item -Path ".\publish-temp" -Recurse -Force
Remove-Item -Path $LocalArchive -Force
Remove-PSSession -Session $session

Write-Host "Deployment complete! Timestamp: $DeploymentStamp. Database migrations will apply automatically on startup." -ForegroundColor Green
