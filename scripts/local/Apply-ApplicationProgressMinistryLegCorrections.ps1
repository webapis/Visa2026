#Requires -Version 5.1
param(
    [switch]$DryRun,
    [string]$LegacySource = 'calik-energi',
    [string]$TargetConnection = ''
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repo

Write-Host "=== ApplicationProgress ministry-leg correction ===" -ForegroundColor Cyan
$cmdArgs = @(
    'run', '--project', 'Visa2026.DataImporter', '--no-launch-profile', '-c', 'Debug', '--',
    '--correct-application-progress-ministry-legs',
    '--legacy-source', $LegacySource,
    '--verbose'
)
if ($DryRun) { $cmdArgs += '--dry-run' }
if ($TargetConnection) { $cmdArgs += @('--target-connection', $TargetConnection) }
& dotnet @cmdArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "=== Verification ===" -ForegroundColor Cyan
$verify = Join-Path $PSScriptRoot 'Verify-ApplicationProgressMinistryLegCorrections.sql'
if (Test-Path $verify) {
    sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -E -b -I -i $verify
}
Write-Host "DONE" -ForegroundColor Green
