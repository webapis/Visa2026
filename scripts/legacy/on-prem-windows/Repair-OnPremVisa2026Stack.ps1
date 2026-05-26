#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Recover Visa2026 on-prem after WSL stop or ERR_CONNECTION_RESET.

.DESCRIPTION
  1) Start hidden WSL keepalive (sleep infinity)
  2) SQL-first compose up (creates DB if needed)
  3) Refresh Windows port 80 -> WSL IP

.EXAMPLE
  .\Repair-OnPremVisa2026Stack.ps1
#>
[CmdletBinding()]
param(
    [string]$DeployRoot = 'C:\visa2026',
    [string]$ScriptRoot = 'C:\visa2026-deploy'
)

$ErrorActionPreference = 'Stop'

& "$ScriptRoot\Start-OnPremWslPersistent.ps1"
& wsl.exe -d Ubuntu -u root -- bash /mnt/c/WslDocker-Setup/remote-compose-sql-up.sh
& "$ScriptRoot\Set-OnPremWslPortProxy.ps1"

Write-Host ''
Write-Host 'Stack repair complete. Test: http://<server-ip>/LoginPage'
Write-Host 'If still failing, wait 60s and run Monitor-OnPremWslStack.ps1'
