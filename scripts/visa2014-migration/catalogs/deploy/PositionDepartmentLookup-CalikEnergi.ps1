#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy Çalik Energi Position + Department tenant catalogs for local F5 / updateDatabase sync.

.NOTES
  LookupCatalogResourceLoader loads embedded tenant/*.json before disk overlay.
  Copies position.calik-energi.json and department.calik-energi.json into embedded
  source files, rebuilds, and writes bin overlay + bumps overlay manifest version so sync runs.
#>
param(
    [int]$OverlayManifestVersion = 25
)


. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$srcPosCalik = Join-Path $tenantDir 'position.calik-energi.json'
$srcDepCalik = Join-Path $tenantDir 'department.calik-energi.json'
$embeddedPos = Join-Path $tenantDir 'position.json'
$embeddedDep = Join-Path $tenantDir 'department.json'
$srcManifest = Join-Path $tenantDir 'manifest.json'
$blazorBin = Join-Path $repoRoot 'Visa2026.Blazor.Server\bin\Debug\net8.0'
$overlayDir = Join-Path $blazorBin 'LookupCatalogs\tenant'

foreach ($path in @($srcPosCalik, $srcDepCalik)) {
    if (-not (Test-Path $path)) {
        throw "Missing catalog source: $path"
    }
}

Write-Host "Copy calik-energi catalogs -> embedded tenant position.json + department.json"
Copy-Item -Force $srcPosCalik $embeddedPos
Copy-Item -Force $srcDepCalik $embeddedDep

if (-not (Test-Path $blazorBin)) {
    Write-Host "Building Debug (bin missing)..."
    dotnet build (Join-Path $repoRoot 'Visa2026.slnx') -c Debug | Out-Null
}

New-Item -ItemType Directory -Force -Path $overlayDir | Out-Null
Copy-Item -Force $embeddedPos (Join-Path $overlayDir 'position.json')
Copy-Item -Force $embeddedDep (Join-Path $overlayDir 'department.json')
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

Write-Host "Rebuild Module + Blazor.Server so embedded catalogs are in Visa2026.Module.dll..."
dotnet build (Join-Path $repoRoot 'Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj') -c Debug | Out-Null

Write-Host @"

Next: sync catalogs to LocalDB (Visa2026) — use silent update (no Enter prompt):
  .\scripts\visa2014-migration\Update-LocalDatabase.ps1 -ForceUpdate -SkipBuild
  # Or manually:
  `$env:FORCE_XAF_DB_UPDATE = 'true'
  dotnet run --project Visa2026.Blazor.Server -c Debug --no-build --no-launch-profile -- --updateDatabase --forceUpdate --silent

Expect LookupCatalogSyncUpdater: position created~1579, department created~74 (first calik deploy).
"@
