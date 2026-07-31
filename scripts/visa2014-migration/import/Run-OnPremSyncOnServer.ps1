#Requires -Version 5.1
<#
.SYNOPSIS
  Run legacy → Visa2026 Import on 10.100.128.25 using the per-slot sync-host layout.

.DESCRIPTION
  Loads <SyncHostRoot>\config\sync.env, resolves target SQL from the slot IIS appsettings when needed,
  then invokes OnPrem-Sync.ps1 with -SyncHostRoot (published DataImporter.exe — no SDK).
  Import-only (--import-visa2014). No nightly Sync / --sync-visa2014.

  Default sync roots (override with -SyncHostRoot):
    Production  C:\visa2026-sync
    Staging     C:\visa2026-sync-staging
    Demo        C:\visa2026-sync-demo

.EXAMPLE
  C:\visa2026-sync\tools\scripts\Run-OnPremSyncOnServer.ps1 -SkipTenantCatalogGeneration

.EXAMPLE
  C:\visa2026-sync-demo\tools\scripts\Run-OnPremSyncOnServer.ps1 -Profile Demo -ContinueOnError
#>
[CmdletBinding()]
param(
    [ValidateSet('Production', 'Staging', 'Demo')]
    [string]$Profile = 'Production',
    [switch]$IncludeFileWaves,
    [switch]$SkipTenantCatalogGeneration,
    [switch]$SkipLookupPreflight,
    [switch]$ContinueOnError,
    [string[]]$Entity = @(),
    [string]$StartAt = '',
    [int]$Parallelism = 0,
    [string]$SyncHostRoot = '',
    [string]$ConfigFile = '',
    [string]$AppSettings = ''
)

$ErrorActionPreference = 'Stop'
function Resolve-OnPremMigrationLibPath {
    param([Parameter(Mandatory)][string]$FileName)
    foreach ($candidate in @(
            (Join-Path $PSScriptRoot "_lib\$FileName"),
            (Join-Path $PSScriptRoot "..\_lib\$FileName")
        )) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
    throw "Lib not found: $FileName under $PSScriptRoot\_lib or ..\_lib (sync-host vs repo layout)."
}
. (Resolve-OnPremMigrationLibPath 'Get-OnPremSyncHostRoot.ps1')

if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $SyncHostRoot = Get-DefaultOnPremSyncHostRoot -Profile $Profile
}
if ([string]::IsNullOrWhiteSpace($AppSettings)) {
    $AppSettings = Get-OnPremSyncHostAppSettingsPath -Profile $Profile
}
$targetConnectionEnv = Get-OnPremSyncHostTargetConnectionEnv -Profile $Profile

if ([string]::IsNullOrWhiteSpace($ConfigFile)) {
    $ConfigFile = Join-Path $SyncHostRoot 'config\sync.env'
}

function Read-TextFileAutoEncoding {
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
        return [System.Text.Encoding]::Unicode.GetString($bytes)
    }
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
        return [System.Text.Encoding]::BigEndianUnicode.GetString($bytes)
    }
    return [System.Text.Encoding]::UTF8.GetString($bytes)
}

function Import-SyncEnvFile {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Config not found: $Path. Copy onprem-sync.env.example to sync.env and set VISA2014_SQL_PASSWORD."
    }
    # Parentheses required: without them, `$Path -split` binds first and the whole
    # file becomes one "line" → only the first KEY is set (value = rest of file).
    (Read-TextFileAutoEncoding -Path $Path) -split "`r?`n" | ForEach-Object {
        $line = $_.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { return }
        $idx = $line.IndexOf('=')
        if ($idx -lt 1) { return }
        $name = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        if ($value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        Set-Item -Path "Env:$name" -Value $value
    }
}

Import-SyncEnvFile -Path $ConfigFile
$env:VISA2026_SYNC_HOST_ROOT = $SyncHostRoot
# Scrub accidental newlines inside connection env values (corrupted sync.env / copy paste).
foreach ($connKey in @(
        'VISA2014_SQL_CONNECTION',
        'VISA2026_PROD_SQL_CONNECTION',
        'VISA2026_STAGING_SQL_CONNECTION',
        'VISA2026_DEMO_SQL_CONNECTION',
        'ConnectionStrings__DefaultConnection'
    )) {
    $connVal = [Environment]::GetEnvironmentVariable($connKey, 'Process')
    if (-not [string]::IsNullOrWhiteSpace($connVal) -and $connVal -match '[\r\n]') {
        $clean = ($connVal -replace '[\r\n]+', '').Trim()
        Set-Item -Path "Env:$connKey" -Value $clean
        Write-Host "WRN Scrubbed embedded newlines from $connKey" -ForegroundColor Yellow
    }
}
# In-process DataImporter hosts Blazor Startup; Production appsettings has SQLEXPRESS (not LocalDB).
if ([string]::IsNullOrWhiteSpace($env:ASPNETCORE_ENVIRONMENT)) {
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
}

if ([string]::IsNullOrWhiteSpace($env:VISA2014_SQL_PASSWORD)) {
    throw 'VISA2014_SQL_PASSWORD missing in sync.env (ReadOnlyUser on 10.100.128.15).'
}

if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($targetConnectionEnv, 'Process')) -and
    [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($targetConnectionEnv, 'User'))) {
    if (-not (Test-Path -LiteralPath $AppSettings)) {
        throw "Set $targetConnectionEnv in sync.env or ensure $AppSettings exists."
    }
    $cfg = Get-Content -LiteralPath $AppSettings -Raw | ConvertFrom-Json
    $conn = $cfg.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace($conn)) {
        throw "DefaultConnection missing in $AppSettings"
    }
    Set-Item -Path "Env:$targetConnectionEnv" -Value $conn
    Write-Host "INF Target SQL from $AppSettings (localhost\SQLEXPRESS on server)" -ForegroundColor DarkGray
}

$onPremSync = Join-Path $SyncHostRoot 'tools\scripts\OnPrem-Sync.ps1'
if (-not (Test-Path -LiteralPath $onPremSync)) {
    throw "OnPrem-Sync.ps1 not deployed: $onPremSync. Run Install-OnPremSyncHost.ps1."
}

$args = @(
    '-Profile', $Profile,
    '-SyncHostRoot', $SyncHostRoot,
    '-Configuration', 'Release',
    '-SkipPostImportCorrections'
)
if ($IncludeFileWaves) { $args += '-IncludeFileWaves' }
if ($SkipTenantCatalogGeneration) { $args += '-SkipTenantCatalogGeneration' }
if ($SkipLookupPreflight) { $args += '-SkipLookupPreflight' }
if ($ContinueOnError) { $args += '-ContinueOnError' }
if ($Entity.Count -gt 0) { $args += @('-Entity') + $Entity }
if ($StartAt) { $args += @('-StartAt', $StartAt) }
if ($Parallelism -gt 0) { $args += @('-Parallelism', $Parallelism) }

$taskLog = Join-Path $SyncHostRoot "logs\sync-run-$(Get-Date -Format yyyyMMdd-HHmmss).log"
New-Item -ItemType Directory -Force -Path (Split-Path $taskLog) | Out-Null

Write-Host "=== Run-OnPremSyncOnServer (Import) ===" -ForegroundColor Cyan
Write-Host "INF Log: $taskLog" -ForegroundColor DarkGray

$syncOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $onPremSync @args 2>&1
$exitCode = $LASTEXITCODE
if ($syncOutput) {
    $syncOutput | Tee-Object -FilePath $taskLog | Out-Null
}
exit $exitCode
