#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Register nightly prod legacy sync on 10.100.128.25 (Task Scheduler).

.EXAMPLE
  .\Register-OnPremLegacySyncTask.ps1 -ScheduledTime 02:30
#>
[CmdletBinding()]
param(
    [string]$TaskName = 'Visa2026-OnPrem-LegacySync',
    [string]$SyncHostRoot = 'C:\visa2026-sync',
    [string]$ScheduledTime = '02:30'
)

$ErrorActionPreference = 'Stop'

$runScript = Join-Path $SyncHostRoot 'tools\scripts\Run-OnPremSyncOnServer.ps1'
if (-not (Test-Path -LiteralPath $runScript)) {
    throw "Not found: $runScript. Run Install-OnPremSyncHost.ps1 first."
}

$logDir = Join-Path $SyncHostRoot 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$argString = "-NoProfile -ExecutionPolicy Bypass -File `"$runScript`" -Mode Sync -SkipTenantCatalogGeneration -ContinueOnError"
$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $argString -WorkingDirectory (Split-Path $runScript)

$trigger = New-ScheduledTaskTrigger -Daily -At $ScheduledTime
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 8)

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null

Write-Host "Registered scheduled task: $TaskName daily at $ScheduledTime" -ForegroundColor Green
Write-Host "Action: $runScript -Mode Sync -SkipTenantCatalogGeneration -ContinueOnError" -ForegroundColor DarkGray
Write-Host "Logs: $logDir\sync-run-*.log" -ForegroundColor DarkGray
