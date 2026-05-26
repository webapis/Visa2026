#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Register scheduled tasks that keep WSL Ubuntu running and Visa2026 containers up.

.EXAMPLE
  .\Register-Visa2026WslKeepAliveTask.ps1
#>
[CmdletBinding()]
param(
    [string]$DistroName = 'Ubuntu',
    [string]$ComposeProject = 'visa2026-prod',
    [string]$ScriptRoot = 'C:\visa2026-deploy'
)

$ErrorActionPreference = 'Stop'

$persistentName = 'Visa2026-WslPersistent'
$keepAliveName = 'Visa2026-WslKeepAlive'
$startupName = 'Visa2026-Startup'

$persistentCmd = "wsl.exe -d $DistroName -u root -- sleep infinity"
$keepAliveCmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$ScriptRoot\Start-OnPremWslPersistent.ps1`""
$startupCmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$ScriptRoot\Repair-OnPremVisa2026Stack.ps1`""

# Boot: hold WSL VM open (task stays Running).
& schtasks.exe /Create /F /TN $persistentName /SC ONSTART /RU SYSTEM /RL HIGHEST /TR $persistentCmd | Out-Null

# Boot + 1 min: SQL-first compose + port proxy.
& schtasks.exe /Create /F /TN $startupName /SC ONSTART /RU SYSTEM /RL HIGHEST /TR $startupCmd /DELAY 0001:00 | Out-Null

# Every minute: ensure hidden keepalive + WSL responds (backup if persistent task dies).
& schtasks.exe /Create /F /TN $keepAliveName /SC MINUTE /MO 1 /RU SYSTEM /RL HIGHEST /TR $keepAliveCmd | Out-Null

Write-Host "Registered: $persistentName (ONSTART - wsl sleep infinity)"
Write-Host "Registered: $startupName (ONSTART + 1 min - stack repair)"
Write-Host "Registered: $keepAliveName (every 1 minute - WSL keepalive backup)"
Write-Host "Manual recovery: $ScriptRoot\Repair-OnPremVisa2026Stack.ps1"
