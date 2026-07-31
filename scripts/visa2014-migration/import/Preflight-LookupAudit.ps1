#Requires -Version 5.1
<#
.SYNOPSIS
  Pre-import lookup gate: audit live VISA2015 values, translate, verify Visa2026 catalogs.

.DESCRIPTION
  Wraps DataImporter --preflight-visa2014-lookups.
  Exit 0 = no blocking gaps. Exit 2 = blocking gaps (fix seeds/translations before Import).

.EXAMPLE
  .\scripts\visa2014-migration\import\Preflight-LookupAudit.ps1 `
    -LegacySource calik-energi-onprem-demo `
    -TargetConnection $env:VISA2026_DEMO_SQL_CONNECTION
#>
[CmdletBinding()]
param(
    [string]$LegacySource = "calik-energi",
    [string]$TargetConnection = "",
    [switch]$CatalogOnly,
    [switch]$SkipTargetCheck,
    [string]$Entity = "",
    [int]$MaxRows = 0,
    [string]$Output = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$SyncHostRoot = "",
    [switch]$VerboseCli
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "..\_lib\Get-RepoRoot.ps1")
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

$cliArgs = @(
    "--preflight-visa2014-lookups",
    "--legacy-source", $LegacySource
)

if (-not [string]::IsNullOrWhiteSpace($TargetConnection)) {
    $cliArgs += @("--target-connection", $TargetConnection)
}
if ($CatalogOnly) { $cliArgs += "--catalog-only" }
if ($SkipTargetCheck) { $cliArgs += "--skip-target-check" }
if (-not [string]::IsNullOrWhiteSpace($Entity)) { $cliArgs += @("--entity", $Entity) }
if ($MaxRows -gt 0) { $cliArgs += @("--max-rows", "$MaxRows") }
if (-not [string]::IsNullOrWhiteSpace($Output)) { $cliArgs += @("--output", $Output) }
if ($VerboseCli) { $cliArgs += "--verbose" }

if ($SyncHostRoot) {
    $exe = Join-Path $SyncHostRoot "tools\DataImporter\Visa2026.DataImporter.exe"
    if (-not (Test-Path -LiteralPath $exe)) {
        throw "Published DataImporter not found: $exe"
    }
    Write-Host ">>> Lookup preflight (published): $exe" -ForegroundColor Green
    & $exe @cliArgs
    exit $LASTEXITCODE
}

Write-Host ">>> Lookup preflight (dotnet run)" -ForegroundColor Green
$proj = Join-Path $repoRoot "Visa2026.DataImporter\Visa2026.DataImporter.csproj"
dotnet build $proj -c $Configuration -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet run --project $proj -c $Configuration --no-build -- @cliArgs
exit $LASTEXITCODE