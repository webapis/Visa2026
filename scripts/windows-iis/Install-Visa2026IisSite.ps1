#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  [WINDOWS SERVER] Prepare IIS app pool, site, and folders for Visa2026 (native, no Docker).

.DESCRIPTION
  Run on the server AFTER copying publish output to -PublishPath (default C:\inetpub\visa2026).
  Does not install SQL Server or .NET Hosting Bundle - see docs/ON_PREM_WINDOWS_IIS.md.

.PARAMETER PublishPath
  IIS physical path (default C:\inetpub\visa2026).

.PARAMETER SiteName
  IIS site name (default Visa2026).

.PARAMETER AppPoolName
  IIS app pool name (default Visa2026).

.PARAMETER HttpPort
  HTTP binding port (default 80).

.EXAMPLE
  .\Install-Visa2026IisSite.ps1 -PublishPath C:\inetpub\visa2026

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [string]$PublishPath = "C:\inetpub\visa2026",
    [string]$SiteName = "Visa2026",
    [string]$AppPoolName = "Visa2026",
    [int]$HttpPort = 80
)

$ErrorActionPreference = "Stop"

Import-Module WebAdministration -ErrorAction Stop

$dataProtectionPath = "C:\ProgramData\Visa2026\DataProtection-Keys"
$publishPathFull = [System.IO.Path]::GetFullPath($PublishPath)

if (-not (Test-Path -LiteralPath $publishPathFull)) {
    throw "Publish folder not found: $publishPathFull - copy publish output first."
}

Write-Host "==> Create Data Protection keys folder" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $dataProtectionPath | Out-Null

Write-Host "==> App pool $AppPoolName" -ForegroundColor Cyan
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value "AlwaysRunning"
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.idleTimeout -Value ([TimeSpan]::FromMinutes(0))

Write-Host "==> Site $SiteName on port $HttpPort" -ForegroundColor Cyan
if (Test-Path "IIS:\Sites\$SiteName") {
    Remove-Website -Name $SiteName
}
New-Website -Name $SiteName -PhysicalPath $publishPathFull -ApplicationPool $AppPoolName -Port $HttpPort | Out-Null

Write-Host "==> Enable WebSockets for site" -ForegroundColor Cyan
Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" -Location "$SiteName" `
    -Filter "system.webServer/webSocket" -Name "enabled" -Value "True"

Write-Host ""
Write-Host "IIS site prepared." -ForegroundColor Green
Write-Host "  Physical path: $publishPathFull"
Write-Host "  URL: http://localhost:$HttpPort/LoginPage"
Write-Host ""
Write-Host "Before starting for users:" -ForegroundColor Yellow
Write-Host "  1. Install .NET 8 Hosting Bundle if not done (then iisreset)"
Write-Host "  2. Create appsettings.Production.json + app pool env (DEVEXPRESS_LICENSEKEY, ConnectionStrings__DefaultConnection)"
Write-Host "  3. Set ASPNETCORE_DATA_PROTECTION_KEYS=$dataProtectionPath on app pool"
Write-Host ('  4. icacls ' + $dataProtectionPath + ' /grant "IIS AppPool\' + $AppPoolName + ':(OI)(CI)M"')
Write-Host "  5. .\Update-Visa2026Database.ps1 -PublishPath $publishPathFull -ForceUpdate -Silent"
Write-Host "  6. Start-WebAppPool $AppPoolName; see docs/ON_PREM_WINDOWS_IIS.md"
