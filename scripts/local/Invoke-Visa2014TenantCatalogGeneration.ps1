#Requires -Version 5.1
<#
.SYNOPSIS
  Run order.yaml tenantCatalogGeneration steps (VISA2015 → tenant JSON).

.DESCRIPTION
  Canonical order: Visa2026.DataImporter/legacy/visa2014/order.yaml → tenantCatalogGeneration.steps
  1. Generate-ProjectContractCalikEnergiCatalog.ps1
  2. Generate-ApprovalLegProfileCatalog.ps1

  Invoked automatically by Import-Visa2014OnPremStaging.ps1 (before application-domain)
  and optionally by Update-LocalDatabase.ps1 -GenerateTenantCatalogs.

.PARAMETER LegacySource
  --legacy-source passed to DataImporter (default calik-energi).

.PARAMETER Force
  Run even when legacy source is not listed in order.yaml tenantCatalogGeneration.legacySources.

.EXAMPLE
  .\scripts\local\Invoke-Visa2014TenantCatalogGeneration.ps1

.EXAMPLE
  .\scripts\local\Invoke-Visa2014TenantCatalogGeneration.ps1 -LegacySource calik-energi-onprem-staging
#>
[CmdletBinding()]
param(
    [string]$LegacySource = 'calik-energi',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

if (-not $env:VISA2014_SQL_PASSWORD) {
    $env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD', 'User')
}

$dotnetArgs = @(
    'run', '--project', 'Visa2026.DataImporter', '-c', $Configuration, '--',
    '--generate-visa2014-tenant-catalogs',
    '--legacy-source', $LegacySource
)
if ($Force) { $dotnetArgs += '--force' }

& dotnet @dotnetArgs
exit $LASTEXITCODE
