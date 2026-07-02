#Requires -Version 5.1
<#
.SYNOPSIS
  Run order.yaml tenantCatalogGeneration steps (VISA2015 → tenant JSON).

.DESCRIPTION
  Canonical order: Visa2026.DataImporter/legacy/visa2014/order.yaml → tenantCatalogGeneration.steps
  1. catalogs/generate/ProjectContract-CalikEnergi.ps1
  2. catalogs/generate/ApprovalLegProfile.ps1

  Invoked automatically by import/OnPrem-Staging.ps1 (before application-domain)
  and optionally by Update-LocalDatabase.ps1 -GenerateTenantCatalogs.

.PARAMETER LegacySource
  --legacy-source passed to DataImporter (default calik-energi).

.PARAMETER Force
  Run even when legacy source is not listed in order.yaml tenantCatalogGeneration.legacySources.

.EXAMPLE
  .\scripts\visa2014-migration\import/Invoke-TenantCatalogGeneration.ps1

.EXAMPLE
  .\scripts\visa2014-migration\import/Invoke-TenantCatalogGeneration.ps1 -LegacySource calik-energi-onprem-staging
#>
[CmdletBinding()]
param(
    [string]$LegacySource = 'calik-energi',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
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
