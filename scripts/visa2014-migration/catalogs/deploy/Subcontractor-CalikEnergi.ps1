#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy Calik Energi Subcontractor tenant catalog for local F5 / updateDatabase sync.
#>
param(
    [int]$OverlayManifestVersion = 37
)


. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$srcCalik = Join-Path $tenantDir 'subcontractor.calik-energi.json'
$embedded = Join-Path $tenantDir 'subcontractor.json'
$srcManifest = Join-Path $tenantDir 'manifest.json'
$blazorBin = Join-Path $repoRoot 'Visa2026.Blazor.Server\bin\Debug\net8.0'
$overlayDir = Join-Path $blazorBin 'LookupCatalogs\tenant'

if (-not (Test-Path $srcCalik)) {
    throw "Missing catalog source: $srcCalik - run catalogs/generate/Subcontractor-CalikEnergi.ps1 first."
}

Write-Host "Copy subcontractor.calik-energi.json -> embedded tenant subcontractor.json"
Copy-Item -Force $srcCalik $embedded

if (-not (Test-Path $blazorBin)) {
    Write-Host "Building Debug (bin missing)..."
    dotnet build (Join-Path $repoRoot 'Visa2026.slnx') -c Debug | Out-Null
}

New-Item -ItemType Directory -Force -Path $overlayDir | Out-Null
Copy-Item -Force $embedded (Join-Path $overlayDir 'subcontractor.json')
Copy-Item -Force $srcManifest (Join-Path $overlayDir 'manifest.json')

$overlayManifest = Join-Path $overlayDir 'manifest.json'
$manifestText = [System.IO.File]::ReadAllText($overlayManifest)
if ($manifestText -match '"version"\s*:\s*(\d+)') {
    $current = [int]$Matches[1]
    if ($OverlayManifestVersion -gt $current) {
        $manifestText = $manifestText -replace '"version"\s*:\s*\d+', "`"version`": $OverlayManifestVersion"
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($overlayManifest, $manifestText, $utf8NoBom)
        [System.IO.File]::WriteAllText($srcManifest, $manifestText, $utf8NoBom)
        Write-Host "Manifest version: $current -> $OverlayManifestVersion"
    }
} else {
    throw "Could not parse version in tenant manifest.json"
}

Write-Host "Rebuild Module + Blazor.Server..."
dotnet build (Join-Path $repoRoot 'Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj') -c Debug | Out-Null

Write-Host @"

Next: sync catalogs to LocalDB:
  `$env:FORCE_XAF_DB_UPDATE = 'true'
  dotnet run --project Visa2026.Blazor.Server -c Debug --no-build --no-launch-profile -- --updateDatabase --forceUpdate --silent
"@