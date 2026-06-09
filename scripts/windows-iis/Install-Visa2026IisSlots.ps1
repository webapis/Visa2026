#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Install or refresh IIS sites for Production (:80), Staging (:8080), and Demo (:8081).

.PARAMETER Profile
  Production, Staging, Demo, or All (default All).

.PARAMETER SourceEnvFile
  Copy SA_PASSWORD / DEVEXPRESS_LICENSEKEY into missing slot env files (e.g. C:\visa2026\.env.prod).

.PARAMETER SkipConfigure
  Only create folders, sites, and app pools; do not run Configure-Visa2026Production.ps1.

.PARAMETER SkipAutoStart
  Do not run Set-Visa2026IisSlotsAutoStart.ps1 at the end.

.PARAMETER SqlServer
  Default localhost\SQLEXPRESS.

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "All")]
    [string]$Profile = "All",

    [string]$SourceEnvFile = "C:\visa2026\.env.prod",
    [switch]$SkipConfigure,
    [switch]$SkipAutoStart,
    [string]$SqlServer = "localhost\SQLEXPRESS"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

Initialize-Visa2026IisServerFolders | Out-Null

# Legacy single-site install binds :80 as "Visa2026" — stop it before slot sites use port 80.
$appcmd = "$env:windir\System32\inetsrv\appcmd.exe"
& $appcmd stop site Visa2026 2>$null | Out-Null
& $appcmd stop apppool Visa2026 2>$null | Out-Null

$profiles = if ($Profile -eq "All") { Get-Visa2026IisSlotProfiles } else { @($Profile) }

foreach ($name in $profiles) {
    Ensure-Visa2026IisSlotEnvFile -Profile $name -SourceEnvFile $SourceEnvFile | Out-Null
}

& (Join-Path $PSScriptRoot "Ensure-Visa2026SlotDatabases.ps1") -Profile $Profile -EnvFile $(
    if (Test-Path -LiteralPath $SourceEnvFile) { $SourceEnvFile }
    else { (Resolve-Visa2026IisSlotContext -Profile Production).EnvFile }
)

$installSite = Join-Path $PSScriptRoot "Install-Visa2026IisSite.ps1"
$configure = Join-Path $PSScriptRoot "Configure-Visa2026Production.ps1"
$setPoolEnv = Join-Path $PSScriptRoot "Set-Visa2026AppPoolEnvironment.ps1"

foreach ($name in $profiles) {
    $ctx = Resolve-Visa2026IisSlotContext -Profile $name
    Write-Host ""
    Write-Host "========== Slot: $($ctx.Profile) (port $($ctx.HttpPort)) ==========" -ForegroundColor Cyan

    if (-not (Test-Path -LiteralPath $ctx.PublishPath)) {
        New-Item -ItemType Directory -Force -Path $ctx.PublishPath | Out-Null
        Write-Warning "Publish folder empty: $($ctx.PublishPath) - copy publish output before opening the site."
    }

    & $installSite `
        -PublishPath $ctx.PublishPath `
        -SiteName $ctx.SiteName `
        -AppPoolName $ctx.AppPoolName `
        -HttpPort $ctx.HttpPort `
        -SkipAutoStart

    Grant-Visa2026IisAppPoolDataProtectionAcl -AppPoolName $ctx.AppPoolName -DataProtectionKeysPath $ctx.DataProtectionKeysPath

    if (-not $SkipConfigure) {
        & $configure -Profile $name -SqlServer $SqlServer
        & $setPoolEnv -Profile $name
    }

    Write-Host "  Site: $($ctx.SiteName)  Pool: $($ctx.AppPoolName)  DB: $($ctx.DbName)  URL: $($ctx.LoginPageUrl)" -ForegroundColor Green
}

if (-not $SkipConfigure) {
    $autoStart = Join-Path $PSScriptRoot "Set-Visa2026IisSlotsAutoStart.ps1"
    if (Test-Path -LiteralPath $autoStart) {
        & $autoStart
    }
}

Write-Host ""
Write-Host 'IIS slots installed. Deploy publish output to each inetpub folder, then Run-Visa2026DbUpdateOnServer.ps1 -Profile Production, Staging, or Demo.' -ForegroundColor Green
