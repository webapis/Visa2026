#Requires -Version 5.1
# Orchestration: full VISA2014 -> Visa2026 import chain via headless XAF (scalar + file waves).
param(
    [string]$StartAt = 'Person',
    [string]$TargetConnection = '',
    [string]$LegacySource = 'calik-energi'
)

. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repo = Get-Visa2026RepoRoot
$ErrorActionPreference = 'Stop'

Set-Location $repo

if (-not $TargetConnection) {
    $TargetConnection = $env:ConnectionStrings__DefaultConnection
    if (-not $TargetConnection) {
        $TargetConnection = 'Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True'
    }
}

$logDir = Join-Path $repo 'artifacts/headless-import'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$commonArgs = @(
    '--legacy-source', $LegacySource,
    '--inprocess',
    '--target-connection', $TargetConnection,
    '--no-wait'
)

# Scalar entities + file waves in dependency order (order.yaml + FILE_AND_IMAGE_IMPORT.md)
$steps = @(
    @{ Key = 'Person';                  Kind = 'scalar'; Name = 'Person';                  Extra = @() },
    @{ Key = 'Person-Photo';            Kind = 'files';  Name = 'Person.Photo';             Extra = @('--import-visa2014-files','--entity','Person','--property','Photo') },
    @{ Key = 'Passport';                Kind = 'scalar'; Name = 'Passport';                Extra = @() },
    @{ Key = 'PassportDocument';        Kind = 'files';  Name = 'Passport.PassportDocument'; Extra = @('--import-visa2014-files','--entity','Passport','--property','PassportDocument') },
    @{ Key = 'Visa';                    Kind = 'scalar'; Name = 'Visa';                    Extra = @() },
    @{ Key = 'VisaDocument';            Kind = 'files';  Name = 'Visa.VisaDocument';         Extra = @('--import-visa2014-files','--entity','Visa','--property','VisaDocument') },
    @{ Key = 'Education';               Kind = 'scalar'; Name = 'Education';               Extra = @() },
    @{ Key = 'EducationDocument';       Kind = 'files';  Name = 'Education.EducationDocument'; Extra = @('--import-visa2014-files','--entity','Education','--property','EducationDocument') },
    @{ Key = 'EmployeePositionHistory'; Kind = 'scalar'; Name = 'EmployeePositionHistory'; Extra = @() },
    @{ Key = 'EmployeeSalary';          Kind = 'scalar'; Name = 'EmployeeSalary';          Extra = @() },
    @{ Key = 'AddressOfResidence';      Kind = 'scalar'; Name = 'AddressOfResidence';      Extra = @() },
    @{ Key = 'MedicalRecordDocument';   Kind = 'files';  Name = 'MedicalRecord.MedicalRecordDocument'; Extra = @('--import-visa2014-files','--entity','MedicalRecord','--property','MedicalRecordDocument') },
    @{ Key = 'Application';             Kind = 'scalar'; Name = 'Application';             Extra = @('--skip-tenant-catalog-generation') },
    @{ Key = 'WorkPermit';              Kind = 'scalar'; Name = 'WorkPermit';              Extra = @() },
    @{ Key = 'WorkPermitDocument';      Kind = 'files';  Name = 'WorkPermit.WorkPermitDocument'; Extra = @('--import-visa2014-files','--entity','WorkPermit','--property','WorkPermitDocument') },
    @{ Key = 'Invitation';              Kind = 'scalar'; Name = 'Invitation';              Extra = @() },
    @{ Key = 'InvitationDocument';      Kind = 'files';  Name = 'Invitation.InvitationDocument'; Extra = @('--import-visa2014-files','--entity','Invitation','--property','InvitationDocument') },
    @{ Key = 'FamilyProofDocument';     Kind = 'files';  Name = 'Person.FamilyProofDocument'; Extra = @('--import-visa2014-files','--entity','Person','--property','FamilyProofDocument') },
    @{ Key = 'ApplicationItem';         Kind = 'scalar'; Name = 'ApplicationItem';         Extra = @() },
    @{ Key = 'ApplicationProgress';     Kind = 'scalar'; Name = 'ApplicationProgress';     Extra = @() }
)

$started = $false
$summary = @()
foreach ($step in $steps) {
    $key = $step.Key
    if (-not $started) {
        if ($key -eq $StartAt) { $started = $true } else { continue }
    }
    $log = Join-Path $logDir "$key.log"
    Write-Host "==================== $($step.Name) ====================" -ForegroundColor Cyan
    if ($step.Kind -eq 'files') {
        $args = @('run','--project','Visa2026.DataImporter','--no-launch-profile','--no-build','-c','Debug','--') + $step.Extra + $commonArgs
    } else {
        $args = @('run','--project','Visa2026.DataImporter','--no-launch-profile','--no-build','-c','Debug','--',
                  '--import-visa2014','--entity',$step.Name) + $commonArgs + $step.Extra
    }
    & dotnet @args *>&1 | Tee-Object -FilePath $log | Out-Null
    $code = $LASTEXITCODE
    $postedLine = ((Select-String -Path $log -Pattern 'Posted:|Patched:' | Select-Object -Last 1).Line) ?? ''
    $preparedLine = ((Select-String -Path $log -Pattern 'Prepared:' | Select-Object -Last 1).Line) ?? ''
    $summary += "  $key -> exit=$code  |  $($preparedLine.Trim())  |  $($postedLine.Trim())"
    $posted = if ($postedLine -match '(?:Posted|Patched):\s*(\d+)') { [int]$matches[1] } else { -1 }
    $prepared = if ($preparedLine -match 'Prepared:\s*(\d+)') { [int]$matches[1] } else { -1 }
    if ($posted -le 0 -and $prepared -gt 0) {
        Write-Host "STEP_FAILED $key exit=$code (0 posted/patched / $prepared prepared)" -ForegroundColor Red
        $summary | ForEach-Object { Write-Host $_ }
        exit 1
    }
    Write-Host "STEP_OK $key (posted/patched=$posted)" -ForegroundColor Green
}

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
                  $corr.Flag,'--legacy-source',$LegacySource,'--verbose',
                  '--target-connection',$TargetConnection)
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
