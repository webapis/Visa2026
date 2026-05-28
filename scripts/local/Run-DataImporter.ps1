#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL] Interactive launcher for Visa2026.DataImporter.

.DESCRIPTION
  Provides an easier “dialog-like” workflow than remembering CLI flags.
  Uses Out-GridView picker when available, otherwise falls back to console prompts.

  This script runs the importer locally (not in docker). For docker seeding see:
  scripts/local/Seed-DataYaml.ps1

.EXAMPLE
  .\scripts\local\Run-DataImporter.ps1
#>

[CmdletBinding()]
param(
    [string]$SeedPath = "Visa2026.DataImporter/seed/scenarios.index.yaml"
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    return Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

function Has-Command([string]$Name) {
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Pick-One([string]$Title, [string[]]$Options) {
    if (Has-Command "Out-GridView") {
        try {
            return $Options | Out-GridView -Title $Title -OutputMode Single
        } catch {
            # fall back to console
        }
    }

    Write-Host ""
    Write-Host $Title -ForegroundColor Cyan
    for ($i = 0; $i -lt $Options.Count; $i++) {
        Write-Host ("[{0}] {1}" -f ($i + 1), $Options[$i])
    }
    while ($true) {
        $raw = Read-Host "Choose (1-$($Options.Count))"
        if ([int]::TryParse($raw, [ref]$n) -and $n -ge 1 -and $n -le $Options.Count) {
            return $Options[$n - 1]
        }
    }
}

function Prompt-YesNo([string]$Question, [bool]$DefaultNo = $true) {
    $suffix = if ($DefaultNo) { "[y/N]" } else { "[Y/n]" }
    $ans = Read-Host "$Question $suffix"
    if ([string]::IsNullOrWhiteSpace($ans)) { return -not $DefaultNo }
    return $ans.Trim().ToLowerInvariant().StartsWith("y")
}

$repoRoot = Resolve-RepoRoot
Set-Location $repoRoot

if (-not (Has-Command "dotnet")) {
    throw "dotnet SDK not found in PATH."
}

$actions = @(
    "Import (default)",
    "Clear scenario then import",
    "Sync scenario (PATCH) then import",
    "Validate seed only",
    "Prune seed (rewrite yaml) only"
)

$action = Pick-One 'Visa2026.DataImporter - choose action' $actions

$skipPreflight = Prompt-YesNo "Skip visibility preflight (NOT recommended)?" -DefaultNo $true

$seedSpec = $SeedPath
if (Prompt-YesNo "Use custom seed path? (default: $SeedPath)" -DefaultNo $true) {
    $seedSpec = Read-Host 'Enter seed path (index, directory, or data.yaml)'
    if ([string]::IsNullOrWhiteSpace($seedSpec)) { $seedSpec = $SeedPath }
}

$extraFlags = @()
if ($skipPreflight) { $extraFlags += "--skip-visibility-preflight" }

switch ($action) {
    "Import (default)" {
        $importerArgs = @()
        if (-not [string]::IsNullOrWhiteSpace($seedSpec)) { $importerArgs += $seedSpec }
        Write-Host "`nRunning importer..." -ForegroundColor Cyan
        & dotnet run --project Visa2026.DataImporter -c Debug -- @importerArgs @extraFlags
        exit $LASTEXITCODE
    }
    "Clear scenario then import" {
        $name = Read-Host "Scenario name (e.g. InvitationEmployee)"
        if ([string]::IsNullOrWhiteSpace($name)) { throw "Scenario name is required." }
        $importerArgs = @("--clear-scenario", $name)
        if (-not [string]::IsNullOrWhiteSpace($seedSpec)) { $importerArgs = @($seedSpec) + $importerArgs }
        Write-Host "`nRunning importer (clear scenario)..." -ForegroundColor Cyan
        & dotnet run --project Visa2026.DataImporter -c Debug -- @importerArgs @extraFlags
        exit $LASTEXITCODE
    }
    "Sync scenario (PATCH) then import" {
        $name = Read-Host "Scenario name (e.g. InvitationEmployee)"
        if ([string]::IsNullOrWhiteSpace($name)) { throw "Scenario name is required." }
        $importerArgs = @("--sync-scenario", $name)
        if (-not [string]::IsNullOrWhiteSpace($seedSpec)) { $importerArgs = @($seedSpec) + $importerArgs }
        Write-Host "`nRunning importer (sync scenario)..." -ForegroundColor Cyan
        & dotnet run --project Visa2026.DataImporter -c Debug -- @importerArgs @extraFlags
        exit $LASTEXITCODE
    }
    "Validate seed only" {
        $args = @("--validate-seed", $seedSpec)
        Write-Host "`nValidating seed..." -ForegroundColor Cyan
        & dotnet run --project Visa2026.DataImporter -c Debug -- @args
        exit $LASTEXITCODE
    }
    "Prune seed (rewrite yaml) only" {
        $args = @("--prune-seed", $seedSpec)
        Write-Host "`nPruning seed (will rewrite yaml files)..." -ForegroundColor Yellow
        & dotnet run --project Visa2026.DataImporter -c Debug -- @args
        exit $LASTEXITCODE
    }
    default {
        throw "Unknown action: $action"
    }
}

