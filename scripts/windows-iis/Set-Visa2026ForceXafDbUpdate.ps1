#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Set FORCE_XAF_DB_UPDATE=true on a Visa2026 IIS app pool and recycle (login-page banner + UpdateDatabaseAlways).

.NOTES
  Pair with Remove-Visa2026ForceXafDbUpdate.ps1 after ModuleUpdaters have run.
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy", "")]
    [string]$Profile = "Staging",

    [string]$AppPoolName = ""
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile -AppPoolName $AppPoolName
$AppPoolName = $ctx.AppPoolName

$appcmd = Join-Path $env:Windir "System32\inetsrv\appcmd.exe"

& $appcmd set config -section:system.applicationHost/applicationPools `
    /-"[name='$AppPoolName'].environmentVariables.[name='FORCE_XAF_DB_UPDATE']" 2>$null | Out-Null
& $appcmd set config -section:system.applicationHost/applicationPools `
    /+"[name='$AppPoolName'].environmentVariables.[name='FORCE_XAF_DB_UPDATE',value='true']"
if ($LASTEXITCODE -ne 0) {
    throw "appcmd failed setting FORCE_XAF_DB_UPDATE on $AppPoolName (exit $LASTEXITCODE)."
}

Write-Host "FORCE_XAF_DB_UPDATE=true on app pool $AppPoolName ($($ctx.Profile), port $($ctx.HttpPort))." -ForegroundColor Green
& $appcmd recycle apppool $AppPoolName | Out-Null
Write-Host "Recycled app pool $AppPoolName. Refresh $($ctx.LoginPageUrl) to see the login-page banner."
