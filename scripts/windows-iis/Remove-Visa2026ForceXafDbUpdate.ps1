#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Remove FORCE_XAF_DB_UPDATE from the Visa2026 IIS app pool (one-shot flag must not stay on).
#>
param(
    [string]$AppPoolName = "Visa2026"
)

$ErrorActionPreference = "Stop"
$appcmd = Join-Path $env:Windir "System32\inetsrv\appcmd.exe"

& $appcmd set config -section:system.applicationHost/applicationPools `
    /-"[name='$AppPoolName'].environmentVariables.[name='FORCE_XAF_DB_UPDATE']" 2>$null | Out-Null

Write-Host "Removed FORCE_XAF_DB_UPDATE from app pool $AppPoolName (if it was set)."
& $appcmd recycle apppool $AppPoolName | Out-Null
Write-Host "Recycled app pool $AppPoolName."
