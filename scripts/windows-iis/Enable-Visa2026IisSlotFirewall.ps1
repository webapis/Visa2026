#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Allow inbound TCP to Visa2026 Staging (:8080) and Demo (:8081) IIS slots.

.PARAMETER Profile
  Staging, Demo, or All (default All). Production (:80) is usually open already via IIS/HTTP.

.NOTES
  Idempotent: skips rules that already exist. Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [ValidateSet("Staging", "Demo", "All")]
    [string]$Profile = "All"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

function Ensure-Visa2026FirewallRule {
    param(
        [Parameter(Mandatory = $true)][string]$DisplayName,
        [Parameter(Mandatory = $true)][int]$Port,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $existing = Get-NetFirewallRule -DisplayName $DisplayName -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($existing) {
        Write-Host "Firewall rule exists: $DisplayName (port $Port)" -ForegroundColor DarkGray
        return
    }

    New-NetFirewallRule `
        -DisplayName $DisplayName `
        -Description $Description `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort $Port `
        -Action Allow `
        -Profile Domain, Private, Public `
        -Enabled True | Out-Null

    Write-Host "Added firewall rule: $DisplayName (TCP $Port inbound)" -ForegroundColor Green
}

$profiles = if ($Profile -eq "All") { @("Staging", "Demo") } else { @($Profile) }

foreach ($name in $profiles) {
    $ctx = Resolve-Visa2026IisSlotContext -Profile $name
    $ruleName = "Visa2026 IIS $($ctx.Profile) (TCP $($ctx.HttpPort))"
    $description = "Allow LAN access to Visa2026 $($ctx.Profile) on IIS port $($ctx.HttpPort)."
    Ensure-Visa2026FirewallRule -DisplayName $ruleName -Port $ctx.HttpPort -Description $description
}

Write-Host ""
Write-Host "Visa2026 slot firewall rules ready." -ForegroundColor Green
