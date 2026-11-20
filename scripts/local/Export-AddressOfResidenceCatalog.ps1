<#
.SYNOPSIS
  Export deduplicated AddressOfResidence rows to tenant JSON (shared lookup catalog).
.NOTES
  Requires a running Postgres with Visa2026 connection. Prefer composing from lodging/hotel
  JSON when DB export is unavailable (see address-of-residence.calik-energi.json).
#>
param(
  [string]$ConnectionString = $env:VISA2026_PG_CONNECTION,
  [string]$OutPath = "$(Join-Path $PSScriptRoot '..\..\Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant\address-of-residence.calik-energi.json')"
)
Write-Host "Use Module updater AddressOfResidenceCatalogDedupeUpdater + LookupCatalogSync for production."
Write-Host "Seed file: $OutPath"