#Requires -Version 5.1
<#
.SYNOPSIS
  Set DB_NAME in C:\visa2026\.env.prod (on server) without changing other keys.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$DatabaseName,
    [string]$EnvFile = "C:\visa2026\.env.prod"
)

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
