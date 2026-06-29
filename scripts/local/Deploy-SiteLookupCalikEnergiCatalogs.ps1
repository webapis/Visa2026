#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy Çalik Energi site tenant catalogs (Lodging, Hotel, Hospital, OtherSite) for local DB sync.
#>
param(
    [int]$OverlayManifestVersion = 31
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$srcManifest = Join-Path $tenantDir 'manifest.json'
$blazorBin = Join-Path $repoRoot 'Visa2026.Blazor.Server\bin\Debug\net8.0'
$overlayDir = Join-Path $blazorBin 'LookupCatalogs\tenant'

$pairs = @(
    @{ Calik = 'lodging.calik-energi.json'; Embedded = 'lodging.json'; Generate = 'Generate-LodgingCalikEnergiCatalog.ps1' },
    @{ Calik = 'hotel.calik-energi.json'; Embedded = 'hotel.json'; Generate = 'Generate-HotelCalikEnergiCatalog.ps1' },
    @{ Calik = 'hospital.calik-energi.json'; Embedded = 'hospital.json'; Generate = 'Generate-HospitalCalikEnergiCatalog.ps1' },
    @{ Calik = 'other-site.calik-energi.json'; Embedded = 'other-site.json'; Generate = 'Generate-OtherSiteCalikEnergiCatalog.ps1' }
)

foreach ($pair in $pairs) {
    $srcCalik = Join-Path $tenantDir $pair.Calik
    if (-not (Test-Path -LiteralPath $srcCalik)) {
        Write-Host "Missing $($pair.Calik) - running $($pair.Generate)..."
        & (Join-Path $PSScriptRoot $pair.Generate)
    }
    $embedded = Join-Path $tenantDir $pair.Embedded
    Write-Host "Copy $($pair.Calik) -> $($pair.Embedded)"
    Copy-Item -Force $srcCalik $embedded
}

if (-not (Test-Path $blazorBin)) {
    Write-Host "Building Debug (bin missing)..."
    dotnet build (Join-Path $repoRoot 'Visa2026.slnx') -c Debug | Out-Null
}

New-Item -ItemType Directory -Force -Path $overlayDir | Out-Null
foreach ($pair in $pairs) {
    Copy-Item -Force (Join-Path $tenantDir $pair.Embedded) (Join-Path $overlayDir $pair.Embedded)
}
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

if ($OverlayManifestVersion -gt 0) {
    $m = [System.IO.File]::ReadAllText($srcManifest) -replace '"version"\s*:\s*\d+', "`"version`": $OverlayManifestVersion"
    [System.IO.File]::WriteAllText($srcManifest, $m)
}

Write-Host "Rebuild Module + Blazor.Server..."
dotnet build (Join-Path $repoRoot 'Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj') -c Debug | Out-Null

Write-Host ""
Write-Host "Next: sync site catalogs to LocalDB:"
Write-Host "  .\scripts\local\Update-LocalDatabase.ps1 -ForceUpdate -SkipBuild"
