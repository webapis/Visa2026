#Requires -Version 5.1
<#
.SYNOPSIS
  Apply NearDuplicates Decision column from AddressCity-HumanReview.xlsx.

.DESCRIPTION
  High-confidence empty Decisions use SuggestedDecision.
  Other empty Decisions default to KeepBoth when -FillEmptyKeepBoth is set.
  Updates city.json, tenant site catalogs, lookup-translations CityByName, manifest version,
  and optionally runs heal SQL on Production via SSH.
#>
[CmdletBinding()]
param(
    [string]$Workbook = '',
    [switch]$FillEmptyKeepBoth,
    [switch]$ApplyProdHealViaSsh,
    [string]$SshHost = 'visa2026-onprem'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$outDir = Join-Path $repoRoot 'Visa2026.DataImporter\legacy\visa2014\preview-export'
if ([string]::IsNullOrWhiteSpace($Workbook)) {
    $Workbook = Join-Path $outDir 'AddressCity-HumanReview.xlsx'
}
$toolDir = Join-Path $PSScriptRoot 'tools\ApplyAddressCityDecisions'
$healSql = Join-Path $outDir 'AddressCity-prod-heal.sql'
$markedWorkbook = Join-Path $outDir 'AddressCity-HumanReview.decisions.xlsx'
$utf8 = New-Object System.Text.UTF8Encoding $false

$applyArgs = @(
    'run', '--project', $toolDir, '-c', 'Release', '--',
    '--workbook', $Workbook,
    '--write-workbook', $markedWorkbook,
    '--city-json', (Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\city.json'),
    '--tenant-dir', (Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\tenant'),
    '--translations', (Join-Path $repoRoot 'docs\VISA2014_MIGRATION\lookup-translations.yaml'),
    '--manifest', (Join-Path $repoRoot 'Visa2026.Module\DatabaseUpdate\LookupCatalogs\manifest.json'),
    '--heal-sql', $healSql
)
if ($FillEmptyKeepBoth) { $applyArgs += '--fill-empty-keep-both' }

Write-Host '>>> Applying decisions...' -ForegroundColor Cyan
& dotnet @applyArgs
if ($LASTEXITCODE -ne 0) { throw "ApplyAddressCityDecisions failed exit=$LASTEXITCODE" }

# Copy marked decisions back over the main workbook for continuity
Copy-Item -LiteralPath $markedWorkbook -Destination $Workbook -Force

if ($ApplyProdHealViaSsh) {
    Write-Host '>>> Running heal SQL on Production...' -ForegroundColor Cyan
    scp $healSql "${SshHost}:C:/visa2026-sync/logs/AddressCity-prod-heal.sql" | Out-Null
    $remote = @'
$ErrorActionPreference = "Stop"
$pgPass = $null
Get-Content "C:\visa2026\env\prod.env" | ForEach-Object {
  if ($_ -match "^\s*PG_PASSWORD=(.*)$") { $pgPass = $Matches[1].Trim().Trim([char]34) }
}
$env:PGPASSWORD = $pgPass
& "C:\PostgreSQL\16\bin\psql.exe" -h localhost -U postgres -d visa2026_prod -v ON_ERROR_STOP=1 -f "C:\visa2026-sync\logs\AddressCity-prod-heal.sql"
if ($LASTEXITCODE -ne 0) { throw "psql heal failed" }
Write-Host "PROD_HEAL_OK"
'@
    $rp = Join-Path $env:TEMP 'run-city-heal.ps1'
    [System.IO.File]::WriteAllText($rp, $remote, $utf8)
    scp $rp "${SshHost}:C:/visa2026-sync/logs/run-city-heal.ps1" | Out-Null
    ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File C:\visa2026-sync\logs\run-city-heal.ps1"
}

Write-Host 'OK Apply-AddressCityHumanReviewDecisions complete' -ForegroundColor Green
