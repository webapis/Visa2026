#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Register a startup scheduled task to wait for SQL Express and recycle the Visa2026 app pool.

.DESCRIPTION
  Belt-and-suspenders for boot race: IIS app pool may start before MSSQL$SQLEXPRESS is ready (HTTP 500.30).
  Runs Set-Visa2026IisAutoStart.ps1 2 minutes after boot with a long SQL wait.

.NOTES
  Run once on the server. Complements W3SVC depend= on SQL in Set-Visa2026IisAutoStart.ps1.
#>
param(
    [string]$TaskName = "Visa2026-IisAfterBoot",
    [int]$StartupDelayMinutes = 2
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$autoStartScript = Join-Path $scriptDir "Set-Visa2026IisAutoStart.ps1"
if (-not (Test-Path -LiteralPath $autoStartScript)) {
    throw "Not found: $autoStartScript"
}

$action = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$autoStartScript`" -WaitForSqlSeconds 180"

$trigger = New-ScheduledTaskTrigger -AtStartup
$trigger.Delay = "PT${StartupDelayMinutes}M"

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null

Write-Host "Registered scheduled task: $TaskName (At startup + ${StartupDelayMinutes} min delay)" -ForegroundColor Green
Write-Host "Action: $autoStartScript -WaitForSqlSeconds 180" -ForegroundColor DarkGray
