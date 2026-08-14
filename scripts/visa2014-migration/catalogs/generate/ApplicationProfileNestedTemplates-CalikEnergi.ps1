#Requires -Version 5.1

<#
.SYNOPSIS
  Wave 3 — export nested ApplicationProfileTemplate proposal from target Visa2026 DB.

.DESCRIPTION
  Uses seeded UserReportTemplate visibility + Wave 0b/1 tenant profile catalog on the target DB.
  Writes Excel for developer sign-off, then tenant JSON for deploy sync / patch.

.EXAMPLE
  .\scripts\visa2014-migration\catalogs\generate\ApplicationProfileNestedTemplates-CalikEnergi.ps1

.EXAMPLE
  .\scripts\visa2014-migration\catalogs\generate\ApplicationProfileNestedTemplates-CalikEnergi.ps1 -ExportTenantJson
#>
param(
    [string]$TargetConnection = $(if ($env:ConnectionStrings__DefaultConnection) { $env:ConnectionStrings__DefaultConnection } else { "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=Visa2026Local;Persist Security Info=True;EFCoreProvider=Postgres" }),
    [string]$PreviewOutputPath,
    [string]$TenantJsonOutputPath,
    [switch]$ExportTenantJson,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$importerProj = Join-Path $repoRoot 'Visa2026.DataImporter\Visa2026.DataImporter.csproj'
$defaultPreview = Join-Path $repoRoot 'Visa2026.DataImporter\legacy\visa2014\preview-export\ApplicationProfileNestedTemplates-proposal.calik-energi.xlsx'
$defaultJson = Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant\application-profile-nested-templates.calik-energi.json'
if (-not $PreviewOutputPath) { $PreviewOutputPath = $defaultPreview }
elseif (-not [System.IO.Path]::IsPathRooted($PreviewOutputPath)) {
    $PreviewOutputPath = Join-Path $repoRoot $PreviewOutputPath
}
if (-not $TenantJsonOutputPath) { $TenantJsonOutputPath = $defaultJson }
elseif (-not [System.IO.Path]::IsPathRooted($TenantJsonOutputPath)) {
    $TenantJsonOutputPath = Join-Path $repoRoot $TenantJsonOutputPath
}

Write-Host '=== Wave 3: ApplicationProfile nested templates (target Visa2026 DB) ===' -ForegroundColor Cyan
Write-Host "INF Target: $($TargetConnection -replace '(Password|Pwd)\s*=\s*[^;]+', '$1=***')"
Write-Host "INF Preview output: $PreviewOutputPath"

Push-Location $repoRoot
try {
    Write-Host "=== Build Module + DataImporter ($Configuration) ===" -ForegroundColor Cyan
    dotnet build Visa2026.Module/Visa2026.Module.csproj -c $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet build $importerProj -c $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $previewArgs = @(
        'run', '--project', $importerProj, '-c', $Configuration, '--no-build', '--',
        '--export-visa2014-application-profile-nested-template-preview',
        '--target-connection', $TargetConnection,
        '--output', $PreviewOutputPath
    )
    & dotnet @previewArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "OK Preview workbook: $PreviewOutputPath" -ForegroundColor Green

    if ($ExportTenantJson) {
        Write-Host "INF Tenant JSON output: $TenantJsonOutputPath"
        $jsonArgs = @(
            'run', '--project', $importerProj, '-c', $Configuration, '--no-build', '--',
            '--export-visa2014-application-profile-nested-template-tenant-json',
            '--target-connection', $TargetConnection,
            '--output', $TenantJsonOutputPath
        )
        & dotnet @jsonArgs
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        Write-Host "OK Tenant JSON: $TenantJsonOutputPath" -ForegroundColor Green
        Write-Host 'INF Set SignOff to approved on rows, rebuild Module, then patch or deploy.'
    }
    else {
        Write-Host 'INF Re-run with -ExportTenantJson after Excel sign-off to promote tenant JSON.'
    }
}
finally {
    Pop-Location
}
