#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Create empty Visa2026 slot databases on SQL Server Express if missing.

.PARAMETER Profile
  Production, Staging, Demo, or All (default All).

.PARAMETER EnvFile
  Env file with SA_PASSWORD. Default: prod slot env, or legacy C:\visa2026\.env.prod.

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "All")]
    [string]$Profile = "All",

    [string]$EnvFile = "",
    [string]$InstanceName = "SQLEXPRESS"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

function Read-DotEnvMap([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }
    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -match '^\s*#' -or $line -eq "") { return }
        if ($line -match '^\s*([^#=]+)=(.*)$') {
            $k = $matches[1].Trim()
            $v = $matches[2].Trim()
            if ($v.Length -ge 2 -and $v.StartsWith('"') -and $v.EndsWith('"')) {
                $v = $v.Substring(1, $v.Length - 2)
            }
            $map[$k] = $v
        }
    }
    $map
}

function Get-SqlCmdPath {
    $candidates = @(
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path -LiteralPath $p) { return $p }
    }
    $found = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    return $null
}

if ([string]::IsNullOrWhiteSpace($EnvFile)) {
    $prodEnv = (Resolve-Visa2026IisSlotContext -Profile Production).EnvFile
    $legacyEnv = "C:\visa2026\.env.prod"
    if (Test-Path -LiteralPath $prodEnv) { $EnvFile = $prodEnv }
    elseif (Test-Path -LiteralPath $legacyEnv) { $EnvFile = $legacyEnv }
    else { $EnvFile = $prodEnv }
}

$envMap = Read-DotEnvMap $EnvFile
$saPassword = $envMap["SA_PASSWORD"]
if ([string]::IsNullOrWhiteSpace($saPassword)) {
    throw "SA_PASSWORD missing in $EnvFile"
}

$sqlcmd = Get-SqlCmdPath
if (-not $sqlcmd) {
    throw "sqlcmd not found. Install SQL Server client tools or SQL Express."
}

$server = "localhost\$InstanceName"
$profiles = if ($Profile -eq "All") { Get-Visa2026IisSlotProfiles } else { @($Profile) }

foreach ($name in $profiles) {
    $ctx = Resolve-Visa2026IisSlotContext -Profile $name
    $db = $ctx.DbName
    $query = @"
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$db')
    CREATE DATABASE [$db];
"@
    Write-Host "==> Ensure database $db ($name)" -ForegroundColor Cyan
    & $sqlcmd -S $server -U sa -P $saPassword -C -Q $query
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed creating $db (exit $LASTEXITCODE)"
    }
}

Write-Host "Slot databases ready." -ForegroundColor Green
