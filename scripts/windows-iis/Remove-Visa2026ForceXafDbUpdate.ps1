#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Remove FORCE_XAF_DB_UPDATE from the Visa2026 IIS app pool (one-shot flag must not stay on).
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy", "")]
    [string]$Profile = "",

    [string]$AppPoolName = ""
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile -AppPoolName $AppPoolName
$AppPoolName = $ctx.AppPoolName

$appcmd = Join-Path $env:Windir "System32\inetsrv\appcmd.exe"

& $appcmd set config -section:system.applicationHost/applicationPools `
    /-"[name='$AppPoolName'].environmentVariables.[name='FORCE_XAF_DB_UPDATE']" 2>$null | Out-Null

Write-Host "Removed FORCE_XAF_DB_UPDATE from app pool $AppPoolName (if it was set)."
& $appcmd recycle apppool $AppPoolName | Out-Null
Write-Host "Recycled app pool $AppPoolName."
