#Requires -Version 5.1
<#
.SYNOPSIS
  Thin wrapper: VISA2015 → Visa2026 on-prem staging (10.100.128.25:8080).

.DESCRIPTION
  Delegates to OnPrem-Sync.ps1 -Profile Staging. See OnPrem-Sync.ps1 for parameters.

.EXAMPLE
  .\scripts\visa2014-migration\import\OnPrem-Staging.ps1 -TargetConnection $env:VISA2026_STAGING_SQL_CONNECTION
#>
[CmdletBinding()]
param(
    [string]$LegacySource = "",
    [string]$TargetConnection = $(if ($env:VISA2026_STAGING_SQL_CONNECTION) { $env:VISA2026_STAGING_SQL_CONNECTION } else { "" }),
    [string]$ApiBaseUrl = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [int]$BatchSize = 50,
    [string[]]$Entity = @(),
    [string]$StartAt = "",
    [switch]$DryRun,
    [switch]$ContinueOnError,
    [switch]$SkipTenantCatalogGeneration,
    [switch]$SkipPostImportCorrections,
    [switch]$IncludeFileWaves
)

$syncParams = @{
    Profile                    = "Staging"
    Configuration              = $Configuration
    BatchSize                  = $BatchSize
    Entity                     = $Entity
    StartAt                    = $StartAt
    DryRun                     = $DryRun
    ContinueOnError            = $ContinueOnError
    SkipTenantCatalogGeneration = $SkipTenantCatalogGeneration
    SkipPostImportCorrections  = $SkipPostImportCorrections
    IncludeFileWaves           = $IncludeFileWaves
}
if (-not [string]::IsNullOrWhiteSpace($LegacySource)) { $syncParams['LegacySource'] = $LegacySource }
if (-not [string]::IsNullOrWhiteSpace($TargetConnection)) { $syncParams['TargetConnection'] = $TargetConnection }
if (-not [string]::IsNullOrWhiteSpace($ApiBaseUrl)) { $syncParams['ApiBaseUrl'] = $ApiBaseUrl }

& (Join-Path $PSScriptRoot 'OnPrem-Sync.ps1') @syncParams
exit $LASTEXITCODE
