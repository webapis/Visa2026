#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Keep WSL Ubuntu alive with a hidden background process (prevents idle Stop on Server).

.EXAMPLE
  .\Start-OnPremWslPersistent.ps1
#>
[CmdletBinding()]
param(
    [string]$DistroName = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'

$marker = "Visa2026-WslPersistent-$DistroName"
$existing = Get-CimInstance Win32_Process -Filter "Name='wsl.exe'" |
    Where-Object { $_.CommandLine -like "*sleep infinity*" -and $_.CommandLine -like "*$DistroName*" }

if ($existing) {
    Write-Host "WSL keepalive already running (PID $($existing[0].ProcessId))."
    exit 0
}

Write-Host "Starting hidden WSL keepalive for $DistroName ..."
Start-Process -FilePath wsl.exe `
    -ArgumentList @('-d', $DistroName, '-u', 'root', '--', 'sleep', 'infinity') `
    -WindowStyle Hidden `
    -PassThru | Out-Null

Start-Sleep -Seconds 3
$ping = (& wsl.exe -d $DistroName -u root -- echo ok 2>&1 | Out-String).Trim()
if ($ping -ne 'ok') {
    throw "WSL $DistroName did not respond after keepalive start. Output: $ping"
}
Write-Host "WSL keepalive started ($marker)."
