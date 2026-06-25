#Requires -Version 5.1
<#
.SYNOPSIS
  Ensure HTTPS_ENABLED and HTTPS_PORT exist in a slot env file (idempotent).
#>
param(
    [ValidateSet("Production", "Staging", "Demo")]
    [string]$Profile = "Staging",

    [string]$UncHost = ""
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$slot = Get-Visa2026IisSlotDefinition -Profile $Profile
$envFile = $slot.EnvFile
$httpsPort = Resolve-Visa2026DefaultHttpsPortForProfile -Profile $Profile

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Env file not found: $envFile"
}

$lines = Get-Content -LiteralPath $envFile
$map = @{}
foreach ($line in $lines) {
    if ($line -match '^\s*([^#=]+?)\s*=\s*(.*)$') {
        $map[$Matches[1].Trim()] = $true
    }
}

$append = @()
if (-not $map.ContainsKey("HTTPS_ENABLED")) {
    $append += "HTTPS_ENABLED=true"
}
if (-not $map.ContainsKey("HTTPS_PORT")) {
    $append += "HTTPS_PORT=$httpsPort"
}
if ($UncHost -and -not $map.ContainsKey("TEMPLATE_EDIT_UNC_HOST")) {
    $append += "TEMPLATE_EDIT_UNC_HOST=$UncHost"
}

if ($append.Count -gt 0) {
    Add-Content -LiteralPath $envFile -Value ("`r`n" + ($append -join "`r`n"))
    Write-Host "Updated $envFile :" -ForegroundColor Green
    $append | ForEach-Object { Write-Host "  + $_" }
}
else {
    Write-Host "No HTTPS env changes needed for $envFile" -ForegroundColor DarkGray
}

Select-String -LiteralPath $envFile -Pattern "HTTPS_|TEMPLATE_EDIT_UNC_HOST" | ForEach-Object { $_.Line }
