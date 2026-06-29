#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Import-Visa2014PreviewCatalogRows.ps1')

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$preview = Join-Path $repoRoot 'Visa2026.DataImporter\legacy\visa2014\preview-export\Hotel-preview.calik-energi.xlsx'
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$outFile = Join-Path $tenantDir 'hotel.calik-energi.json'

if (-not (Test-Path -LiteralPath $preview)) {
    throw "Missing preview: $preview"
}

$count = Write-Visa2014SiteCatalogJsonFromPreview -PreviewPath $preview -OutputPath $outFile -ScalarProperty Name
Write-Host "Wrote $count hotel row(s) -> $outFile"
