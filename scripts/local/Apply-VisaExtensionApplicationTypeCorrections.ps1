#Requires -Version 5.1
param(
    [switch]$DryRun,
    [string]$LegacySource = 'calik-energi',
    [string]$TargetConnection = '',
    [switch]$SkipDatabaseUpdate
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repo
if (-not $SkipDatabaseUpdate) {
    Write-Host "=== Step 1: Sync ApplicationType catalog ===" -ForegroundColor Cyan
    & "$PSScriptRoot/Update-LocalDatabase.ps1" -ForceUpdate
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
Write-Host "=== Step 2: Application type route correction ===" -ForegroundColor Cyan
$cmdArgs = @('run','--project','Visa2026.DataImporter','--no-launch-profile','-c','Debug','--','--correct-visa-application-types','--legacy-source',$LegacySource,'--verbose')
if ($DryRun) { $cmdArgs += '--dry-run' }
if ($TargetConnection) { $cmdArgs += @('--target-connection',$TargetConnection) }
& dotnet @cmdArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "=== Step 3: Verification ===" -ForegroundColor Cyan
$verify = Join-Path $PSScriptRoot 'Verify-VisaExtensionApplicationTypeCorrections.sql'
if (Test-Path $verify) { sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -E -b -I -i $verify }
Write-Host "DONE" -ForegroundColor Green
