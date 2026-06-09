#Requires -Version 5.1
<#
.SYNOPSIS
  Set DB_NAME in a slot env file (on server). Prefer -Profile; legacy default is C:\visa2026\.env.prod.
  For prod/staging/demo isolation use separate slots (Install-Visa2026IisSlots.ps1) instead of swapping DB_NAME.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$DatabaseName,

    [ValidateSet("Production", "Staging", "Demo", "Legacy", "")]
    [string]$Profile = "",

    [string]$EnvFile = ""
)

. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")
if ([string]::IsNullOrWhiteSpace($EnvFile)) {
    $EnvFile = (Resolve-Visa2026IisSlotContext -Profile $Profile).EnvFile
}

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $EnvFile)) {
    throw "Env file not found: $EnvFile"
}

$lines = Get-Content -LiteralPath $EnvFile
$found = $false
$out = foreach ($line in $lines) {
    if ($line -match '^\s*DB_NAME\s*=') {
        $found = $true
        "DB_NAME=$DatabaseName"
    }
    else {
        $line
    }
}
if (-not $found) {
    $out = @($out) + "DB_NAME=$DatabaseName"
}
$out | Set-Content -LiteralPath $EnvFile -Encoding UTF8
Write-Host "Updated $EnvFile -> DB_NAME=$DatabaseName"
