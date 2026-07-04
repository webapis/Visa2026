#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy Çalik Energi Lodging tenant catalog for local F5 / updateDatabase sync.
#>
param(
    [int]$OverlayManifestVersion = 31
)


. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$srcCalik = Join-Path $tenantDir 'lodging.calik-energi.json'
$embedded = Join-Path $tenantDir 'lodging.json'
$srcManifest = Join-Path $tenantDir 'manifest.json'
$blazorBin = Join-Path $repoRoot 'Visa2026.Blazor.Server\bin\Debug\net8.0'
$overlayDir = Join-Path $blazorBin 'LookupCatalogs\tenant'

if (-not (Test-Path $srcCalik)) {
    throw "Missing catalog source: $srcCalik - run catalogs/generate/Lodging-CalikEnergi.ps1 first."
}

Write-Host "Copy lodging.calik-energi.json -> embedded tenant lodging.json"
Copy-Item -Force $srcCalik $embedded

if (-not (Test-Path $blazorBin)) {
    Write-Host "Building Debug (bin missing)..."
    dotnet build (Join-Path $repoRoot 'Visa2026.slnx') -c Debug | Out-Null
}

New-Item -ItemType Directory -Force -Path $overlayDir | Out-Null
Copy-Item -Force $embedded (Join-Path $overlayDir 'lodging.json')
Copy-Item -Force $srcManifest (Join-Path $overlayDir 'manifest.json')

$overlayManifest = Join-Path $overlayDir 'manifest.json'
$manifestText = [System.IO.File]::ReadAllText($overlayManifest)
if ($manifestText -match '"version"\s*:\s*(\d+)') {
    $current = [int]$Matches[1]
    if ($OverlayManifestVersion -gt $current) {
        $manifestText = $manifestText -replace '"version"\s*:\s*\d+', "`"version`": $OverlayManifestVersion"
        [System.IO.File]::WriteAllText($overlayManifest, $manifestText)
        Write-Host "Overlay manifest version: $current -> $OverlayManifestVersion"
    }
}

$embeddedManifest = Join-Path $tenantDir 'manifest.json'
if ($OverlayManifestVersion -gt 25) {
    $m = [System.IO.File]::ReadAllText($embeddedManifest) -replace '"version"\s*:\s*\d+', "`"version`": $OverlayManifestVersion"
    [System.IO.File]::WriteAllText($embeddedManifest, $m)
}

Write-Host "Rebuild Module + Blazor.Server..."
dotnet build (Join-Path $repoRoot 'Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj') -c Debug | Out-Null

Write-Host @"

Next: sync catalogs (lodging rows) to LocalDB:
  .\scripts\visa2014-migration\Update-LocalDatabase.ps1 -ForceUpdate -SkipBuild
"@
