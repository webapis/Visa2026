#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Create empty Visa2026 slot databases on PostgreSQL if missing.

.PARAMETER Profile
  Production, Staging, Demo, or All (default All).

.PARAMETER EnvFile
  Optional env file with PG_PASSWORD (used as fallback). Prefer each slot's own env file.

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md — Visa2026 is PostgreSQL-only.
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "All")]
    [string]$Profile = "All",

    [string]$EnvFile = ""
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

function Read-DotEnvMap([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }
    Read-Visa2026DotEnvMap -Path $Path
}

function Get-PsqlPath {
    $candidates = @(
        "C:\PostgreSQL\16\bin\psql.exe",
        "${env:ProgramFiles}\PostgreSQL\16\bin\psql.exe",
        "${env:ProgramFiles}\PostgreSQL\15\bin\psql.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path -LiteralPath $p) { return $p }
    }
    $found = Get-Command psql -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    return $null
}

$psql = Get-PsqlPath
if (-not $psql) {
    throw "psql not found. Install PostgreSQL 16 (Install-PostgreSqlForVisa2026.ps1) and ensure bin is on PATH."
}

$profiles = if ($Profile -eq "All") { Get-Visa2026IisSlotProfiles } else { @($Profile) }
$fallbackMap = $null
if (-not [string]::IsNullOrWhiteSpace($EnvFile) -and (Test-Path -LiteralPath $EnvFile)) {
    $fallbackMap = Read-DotEnvMap $EnvFile
}

foreach ($name in $profiles) {
    $ctx = Resolve-Visa2026IisSlotContext -Profile $name
    $map = if (Test-Path -LiteralPath $ctx.EnvFile) { Read-DotEnvMap $ctx.EnvFile } elseif ($fallbackMap) { $fallbackMap } else {
        throw "Env file missing for $name ($($ctx.EnvFile)). Create it with PG_PASSWORD and DB_NAME."
    }

    $db = if ($map.ContainsKey("DB_NAME") -and -not [string]::IsNullOrWhiteSpace($map["DB_NAME"])) { $map["DB_NAME"] } else { $ctx.DbName }
    $pgHost = if ($map.ContainsKey("PG_HOST") -and -not [string]::IsNullOrWhiteSpace($map["PG_HOST"])) { $map["PG_HOST"].Trim() } else { "localhost" }
    $pgPort = if ($map.ContainsKey("PG_PORT") -and -not [string]::IsNullOrWhiteSpace($map["PG_PORT"])) { $map["PG_PORT"].Trim() } else { "5432" }
    $pgUser = if ($map.ContainsKey("PG_USER") -and -not [string]::IsNullOrWhiteSpace($map["PG_USER"])) { $map["PG_USER"].Trim() } else { "postgres" }
    $pgPassword = if ($map.ContainsKey("PG_PASSWORD") -and -not [string]::IsNullOrWhiteSpace($map["PG_PASSWORD"])) { $map["PG_PASSWORD"] } else {
        throw "PG_PASSWORD missing in $($ctx.EnvFile) (or fallback EnvFile) for $name"
    }

    Write-Host "==> Ensure PostgreSQL database $db ($name) on ${pgHost}:$pgPort" -ForegroundColor Cyan
    $env:PGPASSWORD = $pgPassword
    $exists = & $psql -h $pgHost -p $pgPort -U $pgUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$db'"
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed checking $db (exit $LASTEXITCODE)"
    }
    if ([string]::IsNullOrWhiteSpace($exists)) {
        & $psql -h $pgHost -p $pgPort -U $pgUser -d postgres -c "CREATE DATABASE `"$db`";"
        if ($LASTEXITCODE -ne 0) {
            throw "psql failed creating $db (exit $LASTEXITCODE)"
        }
        Write-Host "Created $db" -ForegroundColor Green
    }
    else {
        Write-Host "Already exists: $db" -ForegroundColor DarkGray
    }
}

Write-Host "Slot databases ready (PostgreSQL)." -ForegroundColor Green