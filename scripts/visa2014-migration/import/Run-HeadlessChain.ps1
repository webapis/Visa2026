#Requires -Version 5.1
# TEMP orchestration: run the full VISA2014 -> Visa2026 import chain via the headless XAF host (in-process).
# Sequential, dependency-ordered; stops on first failure.
param(
    [string]$StartAt = 'Person',
    [string]$TargetConnection = ''
)

. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repo = Get-Visa2026RepoRoot
$ErrorActionPreference = 'Stop'

Set-Location $repo

$source = 'calik-energi'
$logDir = Join-Path $repo 'artifacts/headless-import'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

# entity + extra args, in dependency order
$steps = @(
    @{ Name = 'Person';                  Extra = @() },
    @{ Name = 'Passport';                Extra = @() },
    @{ Name = 'Visa';                    Extra = @() },
    @{ Name = 'Education';               Extra = @() },
    @{ Name = 'EmployeePositionHistory'; Extra = @() },
    @{ Name = 'EmployeeSalary';          Extra = @() },
    @{ Name = 'AddressOfResidence';      Extra = @() },
    @{ Name = 'Application';             Extra = @('--skip-tenant-catalog-generation') },
    @{ Name = 'ApplicationItem';         Extra = @() },
    @{ Name = 'ApplicationProgress';     Extra = @() }
)

$started = $false
$summary = @()
foreach ($step in $steps) {
    $name = $step.Name
    if (-not $started) {
        if ($name -eq $StartAt) { $started = $true } else { continue }
    }
    $log = Join-Path $logDir "$name.log"
    Write-Host "==================== $name ====================" -ForegroundColor Cyan
    $args = @('run','--project','Visa2026.DataImporter','--no-launch-profile','--no-build','-c','Debug','--',
              '--import-visa2014','--entity',$name,'--legacy-source',$source,'--inprocess') + $step.Extra
    & dotnet @args *>&1 | Tee-Object -FilePath $log | Out-Null
    $code = $LASTEXITCODE
    # ApplicationProgress prints "Posted:" on a "Seeds removed:" line, so match "Posted:" anywhere.
    $preparedLine = ((Select-String -Path $log -Pattern 'Prepared:' | Select-Object -Last 1).Line) ?? ''
    $postedLine = ((Select-String -Path $log -Pattern 'Posted:' | Select-Object -Last 1).Line) ?? ''
    $summary += "  $name -> exit=$code  |  $($preparedLine.Trim())  |  $($postedLine.Trim())"
    # Hard-fail only when nothing was posted for a non-empty batch (systemic error);
    # tolerate a minority of unimportable legacy rows.
    $posted = if ($postedLine -match 'Posted:\s*(\d+)') { [int]$matches[1] } else { -1 }
    $prepared = if ($preparedLine -match 'Prepared:\s*(\d+)') { [int]$matches[1] } else { -1 }
    if ($posted -le 0 -and $prepared -gt 0) {
        Write-Host "STEP_FAILED $name exit=$code (0 posted / $prepared prepared)" -ForegroundColor Red
        $summary | ForEach-Object { Write-Host $_ }
        exit 1
    }
    Write-Host "STEP_OK $name (posted=$posted)" -ForegroundColor Green
}


# postImportCorrections (order.yaml) — idempotent headless patches after application-domain
$postCorrections = @(
    @{ Name = 'PersonSubcontractor'; Flag = '--correct-person-subcontractor' },
    @{ Name = 'PersonRelationship'; Flag = '--correct-person-relationship' },
    @{ Name = 'PersonAddressPia'; Flag = '--correct-person-address-of-residence' },
    @{ Name = 'ApplicationItemPersonCurrent'; Flag = '--correct-application-item-person-current' }
)
Write-Host "==================== postImportCorrections ====================" -ForegroundColor Cyan
foreach ($corr in $postCorrections) {
    $log = Join-Path $logDir "post-$($corr.Name).log"
    Write-Host "--- $($corr.Name) ---" -ForegroundColor Cyan
    $corrArgs = @('run','--project','Visa2026.DataImporter','--no-launch-profile','-c','Debug','--',
                  $corr.Flag,'--legacy-source',$source,'--verbose')
    if ($TargetConnection) { $corrArgs += @('--target-connection', $TargetConnection) }
    & dotnet @corrArgs *>&1 | Tee-Object -FilePath $log | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "POST_CORRECTION_FAILED $($corr.Name) exit=$LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "POST_CORRECTION_OK $($corr.Name)" -ForegroundColor Green
}

Write-Host "==================== SUMMARY ====================" -ForegroundColor Cyan
$summary | ForEach-Object { Write-Host $_ }
Write-Host "CHAIN_COMPLETE" -ForegroundColor Green
