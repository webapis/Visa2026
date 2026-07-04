#Requires -Version 5.1
param(
    [switch]$DryRun,
    [string]$LegacySource = 'calik-energi',
    [string]$TargetConnection = ''
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $repo

$dmArgs = @(
    'run', '--project', 'Visa2026.DataImporter', '-c', 'Debug', '--no-launch-profile', '--',
    '--correct-person-relationship',
    '--legacy-source', $LegacySource,
    '--verbose'
)
if ($DryRun) { $dmArgs += '--dry-run' }
if ($TargetConnection) { $dmArgs += @('--target-connection', $TargetConnection) }

Write-Host "=== Person Relationship correction ===" -ForegroundColor Cyan
dotnet @dmArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`n=== Verification ===" -ForegroundColor Cyan
$verify = Join-Path $PSScriptRoot 'Verify-PersonRelationshipCorrections.sql'
if (Test-Path $verify) {
    sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -E -b -i $verify
}

Write-Host "DONE" -ForegroundColor Green