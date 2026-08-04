#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Allow inbound TCP to the Visa2026 officer manual IIS site (default :8082).

.PARAMETER HttpPort
  Manual site port (default 8082).

.NOTES
  Runbook: docs/USER_MANUAL_RELEASE.md
#>
[CmdletBinding()]
param(
    [int]$HttpPort = 8082
)

$ErrorActionPreference = 'Stop'

$ruleName = "Visa2026 Officer Manual (TCP $HttpPort)"
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue | Select-Object -First 1
if ($existing) {
    Write-Host "Firewall rule exists: $ruleName" -ForegroundColor DarkGray
    exit 0
}

New-NetFirewallRule `
    -DisplayName $ruleName `
    -Description 'Allow LAN access to Visa2026 officer user manual (IIS static site).' `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort $HttpPort `
    -Action Allow `
    -Profile Domain, Private, Public `
    -Enabled True | Out-Null

Write-Host "Added firewall rule: $ruleName (TCP $HttpPort inbound)" -ForegroundColor Green
