#Requires -Version 5.1
. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '..\Import-PreviewCatalogRows.ps1')
$preview = Join-Path $repoRoot 'Visa2026.DataImporter\legacy\visa2014\preview-export\Lodging-preview.calik-energi.xlsx'
$tenantDir = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'
$outFile = Join-Path $tenantDir 'lodging.calik-energi.json'

if (-not (Test-Path -LiteralPath $preview)) {
    throw "Missing preview: $preview"
}

$count = Write-Visa2014SiteCatalogJsonFromPreview -PreviewPath $preview -OutputPath $outFile -ScalarProperty FullAddress
Write-Host "Wrote $count lodging row(s) -> $outFile"
