#Requires -Version 5.1
<#
.SYNOPSIS
  Monitor WSL Ubuntu + Visa2026 container uptime (on-prem triage).
#>
[CmdletBinding()]
param(
    [int]$Checks = 12,
    [int]$IntervalSeconds = 10
)

$ErrorActionPreference = 'Continue'

for ($i = 1; $i -le $Checks; $i++) {
    Write-Host "--- check $i ---"
    wsl -l -v
    wsl -d Ubuntu -u root -- docker ps --format 'table {{.Names}}\t{{.Status}}' 2>&1
    if ($i -lt $Checks) {
        Start-Sleep -Seconds $IntervalSeconds
    }
}
