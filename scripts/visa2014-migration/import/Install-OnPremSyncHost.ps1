#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy C:\visa2026-sync on 10.100.128.25 for legacy sync without .NET SDK on the server.

.DESCRIPTION
  From a dev PC with the Visa2026 repo + .NET 8 SDK:
    1. dotnet publish Visa2026.DataImporter
    2. Copy published bits + OnPrem-Sync scripts to SyncHostRoot
    3. Optionally copy id-maps from repo or C:\visa2026-sync on server
    4. Create config\sync.env from example

  Run on the server after files are present to register Task Scheduler (optional).

.PARAMETER SyncHostRoot
  Default C:\visa2026-sync on 10.100.128.25.

.PARAMETER CopyIdMapsFromRepo
  Copy id-maps/calik-energi-onprem-prod from dev repo into SyncHostRoot\data\id-maps\.

.EXAMPLE
  # From dev PC (build + deploy to local folder, then robocopy to server):
  .\scripts\visa2014-migration\import\Install-OnPremSyncHost.ps1 -PublishFromRepo

.EXAMPLE
  # On .25 after files copied - register nightly 02:30 task:
  .\Install-OnPremSyncHost.ps1 -RegisterScheduledTask
#>
[CmdletBinding()]
param(
    [string]$SyncHostRoot = 'C:\visa2026-sync',
    [string]$RepoRoot = '',
    [switch]$PublishFromRepo,
    [switch]$CopyIdMapsFromRepo,
    [switch]$RegisterScheduledTask,
    [string]$ScheduledTime = '02:30',
    [string]$LegacySource = 'calik-energi-onprem-prod'
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    . (Join-Path $scriptDir '..\_lib\Get-RepoRoot.ps1')
    $RepoRoot = Get-Visa2026RepoRoot
}

$dirs = @(
    "$SyncHostRoot\tools\DataImporter",
    "$SyncHostRoot\tools\scripts",
    "$SyncHostRoot\data\id-maps\$LegacySource",
    "$SyncHostRoot\data\sync-state",
    "$SyncHostRoot\data\import-logs",
    "$SyncHostRoot\config",
    "$SyncHostRoot\logs"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Force -Path $d | Out-Null
}

if ($PublishFromRepo) {
    Write-Host ">>> dotnet publish DataImporter (Release) ..." -ForegroundColor Cyan
    $publishDir = Join-Path $SyncHostRoot 'tools\DataImporter'
    dotnet publish (Join-Path $RepoRoot 'Visa2026.DataImporter\Visa2026.DataImporter.csproj') `
        -c Release -o $publishDir --self-contained false
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
}

$scriptsToCopy = @(
    'OnPrem-Sync.ps1',
    'Run-OnPremSyncOnServer.ps1',
    'Register-OnPremLegacySyncTask.ps1'
)
foreach ($name in $scriptsToCopy) {
    Copy-Item -LiteralPath (Join-Path $scriptDir $name) -Destination (Join-Path $SyncHostRoot "tools\scripts\$name") -Force
}
Copy-Item -LiteralPath (Join-Path $scriptDir 'onprem-sync.env.example') `
    -Destination (Join-Path $SyncHostRoot 'config\onprem-sync.env.example') -Force

$libDir = Join-Path $SyncHostRoot 'tools\scripts\_lib'
New-Item -ItemType Directory -Force -Path $libDir | Out-Null
Copy-Item -LiteralPath (Join-Path $scriptDir '..\_lib\Get-RepoRoot.ps1') -Destination (Join-Path $libDir 'Get-RepoRoot.ps1') -Force

$lookupDst = Join-Path $SyncHostRoot 'tools\DataImporter\legacy\visa2014\lookup-translations'
New-Item -ItemType Directory -Force -Path $lookupDst | Out-Null
foreach ($lookupName in @('lookup-translations.yaml', 'lookup-translations.calik-energi.yaml')) {
    $lookupSrc = Join-Path $RepoRoot "docs\VISA2014_MIGRATION\$lookupName"
    if (Test-Path -LiteralPath $lookupSrc) {
        Copy-Item -LiteralPath $lookupSrc -Destination (Join-Path $lookupDst $lookupName) -Force
    }
}
$migrationArtifactsDst = Join-Path $SyncHostRoot 'tools\DataImporter\legacy\visa2014\migration-artifacts'
New-Item -ItemType Directory -Force -Path $migrationArtifactsDst | Out-Null
$migrationInferenceSrc = Join-Path $RepoRoot 'docs\VISA2014_MIGRATION\migration-service-inference.yaml'
if (Test-Path -LiteralPath $migrationInferenceSrc) {
    Copy-Item -LiteralPath $migrationInferenceSrc -Destination (Join-Path $migrationArtifactsDst 'migration-service-inference.yaml') -Force
}

if ($CopyIdMapsFromRepo) {
    $src = Join-Path $RepoRoot "Visa2026.DataImporter\legacy\visa2014\id-maps\$LegacySource"
    $dst = Join-Path $SyncHostRoot "data\id-maps\$LegacySource"
    if (-not (Test-Path -LiteralPath $src)) {
        throw "Id-maps not found: $src"
    }
    Copy-Item -Path (Join-Path $src '*') -Destination $dst -Force
    $count = (Get-ChildItem -LiteralPath $dst -Filter '*.json').Count
    Write-Host "INF Id-maps copied: $count JSON files -> $dst" -ForegroundColor Green
}

$configPath = Join-Path $SyncHostRoot 'config\sync.env'
if (-not (Test-Path -LiteralPath $configPath)) {
    Copy-Item -LiteralPath (Join-Path $SyncHostRoot 'config\onprem-sync.env.example') -Destination $configPath
    Write-Host "WRN Created $configPath - set VISA2014_SQL_PASSWORD before first sync." -ForegroundColor Yellow
}

Write-Host "INF Sync host layout ready: $SyncHostRoot" -ForegroundColor Green
Write-Host "INF Manual run on server:" -ForegroundColor DarkGray
Write-Host "  C:\visa2026-sync\tools\scripts\Run-OnPremSyncOnServer.ps1 -Mode Sync -SkipTenantCatalogGeneration" -ForegroundColor DarkGray

if ($RegisterScheduledTask) {
    & (Join-Path $SyncHostRoot 'tools\scripts\Register-OnPremLegacySyncTask.ps1') `
        -SyncHostRoot $SyncHostRoot -ScheduledTime $ScheduledTime
}
