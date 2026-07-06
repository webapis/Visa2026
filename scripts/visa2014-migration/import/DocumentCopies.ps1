#Requires -Version 5.1
<#
.SYNOPSIS
  Import VISA2014 document copies / photos into Visa2026 (headless XAF file wave).

.DESCRIPTION
  Runs file waves in dependency order: Photo, PassportDocument, VisaDocument,
  EducationDocument, WorkPermitDocument, InvitationDocument, FamilyProofDocument.
  Idempotent via per-wave id-maps (skips already imported legacy Oids).

.EXAMPLE
  .\scripts\visa2014-migration\import\DocumentCopies.ps1
  .\scripts\visa2014-migration\import\DocumentCopies.ps1 -StartAt WorkPermitDocument
  .\scripts\visa2014-migration\import\DocumentCopies.ps1 -MaxRows 50 -DryRun
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = '',
    [string]$LegacySource = 'calik-energi',
    [string]$StartAt = 'Person-Photo',
    [int]$MaxRows = 0,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repo = Get-Visa2026RepoRoot
Set-Location $repo

if (-not $TargetConnection) {
    $TargetConnection = $env:ConnectionStrings__DefaultConnection
    if (-not $TargetConnection) {
        $TargetConnection = 'Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true'
    }
}

$logDir = Join-Path $repo 'artifacts/document-copies-import'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$commonArgs = @(
    '--legacy-source', $LegacySource,
    '--inprocess',
    '--target-connection', $TargetConnection,
    '--no-wait'
)
if ($MaxRows -gt 0) { $commonArgs += @('--max-rows', $MaxRows) }
if ($DryRun) { $commonArgs += '--dry-run' }

$steps = @(
    @{ Key = 'Person-Photo';        Name = 'Person.Photo';              Extra = @('--import-visa2014-files', '--entity', 'Person', '--property', 'Photo') },
    @{ Key = 'PassportDocument';    Name = 'Passport.PassportDocument'; Extra = @('--import-visa2014-files', '--entity', 'Passport', '--property', 'PassportDocument') },
    @{ Key = 'VisaDocument';        Name = 'Visa.VisaDocument';         Extra = @('--import-visa2014-files', '--entity', 'Visa', '--property', 'VisaDocument') },
    @{ Key = 'EducationDocument';   Name = 'Education.EducationDocument'; Extra = @('--import-visa2014-files', '--entity', 'Education', '--property', 'EducationDocument') },
    @{ Key = 'WorkPermitDocument';  Name = 'WorkPermit.WorkPermitDocument'; Extra = @('--import-visa2014-files', '--entity', 'WorkPermit', '--property', 'WorkPermitDocument') },
    @{ Key = 'InvitationDocument';  Name = 'Invitation.InvitationDocument'; Extra = @('--import-visa2014-files', '--entity', 'Invitation', '--property', 'InvitationDocument') },
    @{ Key = 'FamilyProofDocument'; Name = 'Person.FamilyProofDocument';  Extra = @('--import-visa2014-files', '--entity', 'Person', '--property', 'FamilyProofDocument') }
)

Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
dotnet build Visa2026.DataImporter/Visa2026.DataImporter.csproj -c $Configuration /p:BuildProjectReferences=false
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$started = $false
$summary = @()
foreach ($step in $steps) {
    if (-not $started) {
        if ($step.Key -eq $StartAt) { $started = $true } else { continue }
    }
    $log = Join-Path $logDir "$($step.Key).log"
    Write-Host "==================== $($step.Name) ====================" -ForegroundColor Cyan
    $runArgs = @('run', '--project', 'Visa2026.DataImporter', '--no-launch-profile', '--no-build', '-c', $Configuration, '--') + $step.Extra + $commonArgs
    & dotnet @runArgs *>&1 | Tee-Object -FilePath $log
    $code = $LASTEXITCODE
    $postedMatch = Select-String -Path $log -Pattern 'Posted:|Patched:' | Select-Object -Last 1
    $postedLine = if ($postedMatch) { $postedMatch.Line } else { '' }
    $preparedMatch = Select-String -Path $log -Pattern 'Prepared:' | Select-Object -Last 1
    $preparedLine = if ($preparedMatch) { $preparedMatch.Line } else { '' }
    $summary += "  $($step.Key) -> exit=$code  |  $($preparedLine.Trim())  |  $($postedLine.Trim())"
    if ($code -ne 0) {
        Write-Host "STEP_FAILED $($step.Key) exit=$code" -ForegroundColor Red
        $summary | ForEach-Object { Write-Host $_ }
        exit $code
    }
    Write-Host "STEP_OK $($step.Key)" -ForegroundColor Green
}

Write-Host '==================== SUMMARY ====================' -ForegroundColor Cyan
$summary | ForEach-Object { Write-Host $_ }
Write-Host 'DOCUMENT_COPIES_COMPLETE' -ForegroundColor Green