# Deploy.ps1
$RemoteServer = "10.36.155.64"
$RemotePath = "C:\Topeka Portal"
$ServiceName = "ItPortal"
$Username = "C5L9999"
$Password = "KLgjEiNT1R+;t+AW~t/.2K@T&9C3X*"

Write-Host "Using saved administrator username for the server." -ForegroundColor Yellow
$securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential ($Username, $securePassword)
$session = New-PSSession -ComputerName $RemoteServer -Credential $cred

Write-Host "0. Cleaning previous publish output and stale build caches..." -ForegroundColor Cyan
Remove-Item -Path ".\publish-temp" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\publish-temp.zip" -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path ".\src" -Directory -Recurse -Force -Include "bin","obj" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "1. Publishing application..." -ForegroundColor Cyan
dotnet publish src/TopekaIT.Web/TopekaIT.Web.csproj -c Release -r win-x64 --self-contained true -o ./publish-temp

Write-Host "2. Zipping files for faster transfer..." -ForegroundColor Cyan
Compress-Archive -Path ".\publish-temp\*" -DestinationPath ".\publish-temp.zip" -Force

Write-Host "3. Stopping remote service..." -ForegroundColor Cyan
Invoke-Command -Session $session -ScriptBlock { Stop-Service -Name $using:ServiceName }

Write-Host "4. Transferring zip file to $RemoteServer..." -ForegroundColor Cyan
Copy-Item -Path ".\publish-temp.zip" -Destination "C:\temp_deploy.zip" -ToSession $session -Force

Write-Host "5. Extracting files on server..." -ForegroundColor Cyan
Invoke-Command -Session $session -ScriptBlock {
    $ErrorActionPreference = 'Continue'
    Expand-Archive -Path "C:\temp_deploy.zip" -DestinationPath $using:RemotePath -Force
    Remove-Item -Path "C:\temp_deploy.zip" -Force -ErrorAction SilentlyContinue
}

Write-Host "6. Starting remote service..." -ForegroundColor Cyan
Invoke-Command -Session $session -ScriptBlock { Start-Service -Name $using:ServiceName }

Write-Host "7. Cleaning up..." -ForegroundColor Cyan
Remove-Item -Path ".\publish-temp" -Recurse -Force
Remove-Item -Path ".\publish-temp.zip" -Force
Remove-PSSession -Session $session

Write-Host "Deployment complete! Database migrations will apply automatically on startup." -ForegroundColor Green
