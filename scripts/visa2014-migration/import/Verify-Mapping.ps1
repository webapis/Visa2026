<#
.SYNOPSIS
  Post-import mapping verify (expected vs actual) for a VISA2014 scalar wave.

.DESCRIPTION
  Thin wrapper around DataImporter --verify-visa2014-mapping.
  Entities: Application | ApplicationProgress (see docs/VISA2014_MIGRATION/MAPPING_VERIFICATION.md).

.EXAMPLE
  .\scripts\visa2014-migration\import\Verify-Mapping.ps1 `
    -LegacySource calik-energi-local-pg `
    -TargetConnection $env:ConnectionStrings__DefaultConnection `
    -Entity Application -Sample 50
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$LegacySource,

    [Parameter(Mandatory = $true)]
    [string]$TargetConnection,

    [ValidateSet('Application', 'ApplicationProgress')]
    [string]$Entity = 'Application',

    [ValidateSet('A', 'B', 'C')]
    [string]$Tier = 'B',

    [int]$Sample = 50,

    [switch]$Full,

    [string]$ApplicationIdMap,

    [string]$ProgressIdMap,

    [string]$Report,

    [string]$ReportHtml,

    [int]$MaxRows,

    [switch]$VerboseCli,

    [string]$Configuration = 'Debug',

    [string]$SyncHostRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$diProject = Join-Path $repoRoot 'Visa2026.DataImporter\Visa2026.DataImporter.csproj'

$cli = @(
    '--verify-visa2014-mapping',
    '--entity', $Entity,
    '--legacy-source', $LegacySource,
    '--target-connection', $TargetConnection,
    '--tier', $Tier
)

if ($Full) { $cli += '--full' }
else { $cli += @('--sample', "$Sample") }

if (-not [string]::IsNullOrWhiteSpace($ApplicationIdMap)) {
    $cli += @('--application-id-map', $ApplicationIdMap)
}
if (-not [string]::IsNullOrWhiteSpace($ProgressIdMap)) {
    $cli += @('--progress-id-map', $ProgressIdMap)
}
if (-not [string]::IsNullOrWhiteSpace($Report)) {
    $cli += @('--report', $Report)
}
if (-not [string]::IsNullOrWhiteSpace($ReportHtml)) {
    $cli += @('--report-html', $ReportHtml)
}
if ($MaxRows -gt 0) {
    $cli += @('--max-rows', "$MaxRows")
}
if ($VerboseCli) { $cli += '--verbose' }

Write-Host "=== Mapping verify ($Entity, tier $Tier) ===" -ForegroundColor Cyan
Write-Host "INF Legacy source: $LegacySource"

if (-not [string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $exe = Join-Path $SyncHostRoot 'Visa2026.DataImporter.exe'
    if (-not (Test-Path -LiteralPath $exe)) {
        throw "Published DataImporter not found: $exe"
    }
    & $exe @cli
    exit $LASTEXITCODE
}

dotnet run --project $diProject -c $Configuration -- @cli
exit $LASTEXITCODE