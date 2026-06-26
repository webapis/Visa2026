#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy Çalik Energi ProjectContract catalog for local F5 / updateDatabase sync.

.NOTES
  LookupCatalogResourceLoader loads embedded tenant/project-contract.json before disk overlay.
  This script copies project-contract.calik-energi.json into the embedded source file, rebuilds,
  and also writes bin overlay + bumps overlay manifest version so manifest-only bumps trigger sync.
#>
param(
    [int]$OverlayManifestVersion = 19
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$srcCalik = Join-Path $tenantDir 'project-contract.calik-energi.json'
$embeddedCatalog = Join-Path $tenantDir 'project-contract.json'
$srcManifest = Join-Path $tenantDir 'manifest.json'
$blazorBin = Join-Path $repoRoot 'Visa2026.Blazor.Server\bin\Debug\net8.0'
$overlayDir = Join-Path $blazorBin 'LookupCatalogs\tenant'

if (-not (Test-Path $srcCalik)) {
    throw "Missing catalog source: $srcCalik"
}

Write-Host "Copy calik-energi catalog -> embedded tenant project-contract.json"
Copy-Item -Force $srcCalik $embeddedCatalog

if (-not (Test-Path $blazorBin)) {
    Write-Host "Building Debug (bin missing)..."
    dotnet build (Join-Path $repoRoot 'Visa2026.slnx') -c Debug | Out-Null
}

New-Item -ItemType Directory -Force -Path $overlayDir | Out-Null
Copy-Item -Force $embeddedCatalog (Join-Path $overlayDir 'project-contract.json')
Copy-Item -Force $srcManifest (Join-Path $overlayDir 'manifest.json')

$overlayManifest = Join-Path $overlayDir 'manifest.json'
$manifestText = [System.IO.File]::ReadAllText($overlayManifest)
if ($manifestText -match '"version"\s*:\s*(\d+)') {
    $current = [int]$Matches[1]
    if ($OverlayManifestVersion -le $current) {
        Write-Host "Overlay manifest version already $current (requested $OverlayManifestVersion)."
    } else {
        $manifestText = $manifestText -replace '"version"\s*:\s*\d+', "`"version`": $OverlayManifestVersion"
        [System.IO.File]::WriteAllText($overlayManifest, $manifestText)
        Write-Host "Overlay manifest version: $current -> $OverlayManifestVersion"
    }
} else {
    throw "Could not parse version in overlay manifest."
}

Write-Host "Rebuild Module + Blazor.Server so embedded catalog is in Visa2026.Module.dll..."
dotnet build (Join-Path $repoRoot 'Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj') -c Debug | Out-Null

Write-Host @"

Next: sync catalogs to LocalDB (Visa2026):
  `$env:FORCE_XAF_DB_UPDATE = 'true'
  dotnet run --project Visa2026.Blazor.Server -c Debug --no-build -- --updateDatabase --forceUpdate --silent

Expect LookupCatalogSyncUpdater: project-contract created=73 on first calik deploy.
"@
