#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Install IIS static site for the officer user manual (HTML + media).

.DESCRIPTION
  Creates Visa2026-Manual on port 8082 with two applications:
    /manual       -> MANUAL_SITE_ROOT  (MkDocs output)
    /manual-media   -> MANUAL_MEDIA_ROOT (screenshots/, videos/)

  Run once on the Windows Server, then publish with Publish-Visa2026UserManualRelease.ps1.

.PARAMETER HttpPort
  HTTP binding port (default 8082).

.PARAMETER SiteRoot
  Parent folder (default C:\visa2026\manual\root).

.PARAMETER SitePath
  MkDocs publish target (default C:\visa2026\manual\site).

.PARAMETER MediaPath
  Media publish target (default C:\visa2026\manual\media).

.EXAMPLE
  .\Install-Visa2026ManualIisSite.ps1

.NOTES
  Runbook: docs/USER_MANUAL_RELEASE.md
#>
[CmdletBinding()]
param(
    [int]$HttpPort = 8082,
    [string]$SiteRoot = 'C:\visa2026\manual\root',
    [string]$SitePath = 'C:\visa2026\manual\site',
    [string]$MediaPath = 'C:\visa2026\manual\media',
    [string]$SiteName = 'Visa2026-Manual',
    [string]$AppPoolName = 'Visa2026-Manual'
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration -ErrorAction Stop

$siteRoot = [System.IO.Path]::GetFullPath($SiteRoot)
$sitePath = [System.IO.Path]::GetFullPath($SitePath)
$mediaPath = [System.IO.Path]::GetFullPath($MediaPath)

foreach ($path in @($siteRoot, $sitePath, $mediaPath)) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

$redirectConfig = Join-Path $siteRoot 'web.config'
if (-not (Test-Path -LiteralPath $redirectConfig)) {
    @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <httpRedirect enabled="true" destination="/manual/" httpResponseStatus="Found" />
  </system.webServer>
</configuration>
'@ | Set-Content -LiteralPath $redirectConfig -Encoding UTF8
}

Write-Host "==> App pool $AppPoolName" -ForegroundColor Cyan
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value 'AlwaysRunning'

Write-Host "==> Site $SiteName on port $HttpPort" -ForegroundColor Cyan
if (Test-Path "IIS:\Sites\$SiteName") {
    Remove-Website -Name $SiteName
}
New-Website -Name $SiteName -PhysicalPath $siteRoot -ApplicationPool $AppPoolName -Port $HttpPort | Out-Null

function Ensure-ManualWebApplication {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$PhysicalPath
    )

    $appPath = "IIS:\Sites\$SiteName\$Name"
    if (Test-Path -LiteralPath $appPath) {
        Remove-WebApplication -Site $SiteName -Name $Name
    }
    New-WebApplication -Site $SiteName -Name $Name -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName | Out-Null
}

Ensure-ManualWebApplication -Name 'manual' -PhysicalPath $sitePath
Ensure-ManualWebApplication -Name 'manual-media' -PhysicalPath $mediaPath

Write-Host ''
Write-Host 'Officer manual IIS site ready.' -ForegroundColor Green
Write-Host "  Manual : http://localhost:$HttpPort/manual/"
Write-Host "  Media  : http://localhost:$HttpPort/manual-media/"
Write-Host "  Site path  : $sitePath"
Write-Host "  Media path : $mediaPath"
Write-Host ''
Write-Host 'Next: Enable-Visa2026ManualFirewall.ps1, then Publish-Visa2026UserManualRelease.ps1' -ForegroundColor Yellow
