#Requires -Version 5.1
<#
.SYNOPSIS
  Run legacy -> prod sync on 10.100.128.25 using C:\visa2026-sync layout.

.DESCRIPTION
  Loads C:\visa2026-sync\config\sync.env, resolves prod SQL from IIS appsettings when needed,
  then invokes OnPrem-Sync.ps1 with -SyncHostRoot (published DataImporter.exe — no SDK).

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
    [switch]$SyncFull,
    [switch]$IncludeFileWaves,
    [switch]$SkipTenantCatalogGeneration,
    [switch]$ContinueOnError,
    [string[]]$Entity = @(),
    [string]$StartAt = '',
    [string]$SyncHostRoot = 'C:\visa2026-sync',
    [string]$ConfigFile = '',
    [string]$ProdAppSettings = 'C:\inetpub\visa2026-prod\appsettings.Production.json'
)

$ErrorActionPreference = 'Stop'

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

if ([string]::IsNullOrWhiteSpace($env:VISA2026_PROD_SQL_CONNECTION)) {
    if (-not (Test-Path -LiteralPath $ProdAppSettings)) {
        throw "Set VISA2026_PROD_SQL_CONNECTION in sync.env or ensure $ProdAppSettings exists."
    }
    $cfg = Get-Content -LiteralPath $ProdAppSettings -Raw | ConvertFrom-Json
    $env:VISA2026_PROD_SQL_CONNECTION = $cfg.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace($env:VISA2026_PROD_SQL_CONNECTION)) {
        throw "DefaultConnection missing in $ProdAppSettings"
    }
    Write-Host "INF Prod SQL from $ProdAppSettings (localhost\SQLEXPRESS on server)" -ForegroundColor DarkGray
}

$onPremSync = Join-Path $SyncHostRoot 'tools\scripts\OnPrem-Sync.ps1'
if (-not (Test-Path -LiteralPath $onPremSync)) {
    throw "OnPrem-Sync.ps1 not deployed: $onPremSync. Run Install-OnPremSyncHost.ps1."
}

$args = @(
    '-Profile', 'Production',
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

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $onPremSync @args 2>&1 | Tee-Object -FilePath $taskLog
exit $LASTEXITCODE
