#Requires -Version 5.1
<#
.SYNOPSIS
  Run legacy -> prod sync on 10.100.128.25 using the per-slot sync-host layout.

.DESCRIPTION
  Loads <SyncHostRoot>\config\sync.env, resolves target SQL from the slot IIS appsettings when needed,
  then invokes OnPrem-Sync.ps1 with -SyncHostRoot (published DataImporter.exe — no SDK).

  Default sync roots (override with -SyncHostRoot):
    Production  C:\visa2026-sync
    Staging     C:\visa2026-sync-staging
    Demo        C:\visa2026-sync-demo

.EXAMPLE
  # Manual nightly-style sync on .25:
  C:\visa2026-sync\tools\scripts\Run-OnPremSyncOnServer.ps1 -Mode Sync

.EXAMPLE
  # First catch-up after bootstrap:
  C:\visa2026-sync\tools\scripts\Run-OnPremSyncOnServer.ps1 -Mode Sync -SyncFull -SkipTenantCatalogGeneration
#>
[CmdletBinding()]
param(
    [ValidateSet('Import', 'Sync')]
    [string]$Mode = 'Sync',
    [ValidateSet('Production', 'Staging', 'Demo')]
    [string]$Profile = 'Production',
    [switch]$SyncFull,
    [switch]$IncludeFileWaves,
    [switch]$SkipTenantCatalogGeneration,
    [switch]$ContinueOnError,
    [string[]]$Entity = @(),
    [string]$StartAt = '',
    [string]$SyncHostRoot = '',
    [string]$ConfigFile = '',
    [string]$AppSettings = ''
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\_lib\Get-OnPremSyncHostRoot.ps1')

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
    Read-TextFileAutoEncoding -Path $Path -split "`r?`n" | ForEach-Object {
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
    '-Mode', $Mode,
    '-SyncHostRoot', $SyncHostRoot,
    '-Configuration', 'Release',
    '-SkipPostImportCorrections'
)
if ($SyncFull) { $args += '-SyncFull' }
if ($IncludeFileWaves) { $args += '-IncludeFileWaves' }
if ($SkipTenantCatalogGeneration) { $args += '-SkipTenantCatalogGeneration' }
if ($ContinueOnError) { $args += '-ContinueOnError' }
if ($Entity.Count -gt 0) { $args += @('-Entity') + $Entity }
if ($StartAt) { $args += @('-StartAt', $StartAt) }

$taskLog = Join-Path $SyncHostRoot "logs\sync-run-$(Get-Date -Format yyyyMMdd-HHmmss).log"
New-Item -ItemType Directory -Force -Path (Split-Path $taskLog) | Out-Null

Write-Host "=== Run-OnPremSyncOnServer ($Mode) ===" -ForegroundColor Cyan
Write-Host "INF Log: $taskLog" -ForegroundColor DarkGray

$syncOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $onPremSync @args 2>&1
$exitCode = $LASTEXITCODE
if ($syncOutput) {
    $syncOutput | Tee-Object -FilePath $taskLog | Out-Null
}
exit $exitCode
