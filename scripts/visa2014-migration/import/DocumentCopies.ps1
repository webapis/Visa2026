#Requires -Version 5.1
<#
.SYNOPSIS
  Import VISA2014 document copies / photos into Visa2026 (headless XAF file wave).
.DESCRIPTION
  Runs file waves in dependency order and, when SyncHostRoot is set, records
  machine-readable progress in file-waves-status.json for the run archive.
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = '',
    [string]$LegacySource = 'calik-energi',
    [string]$StartAt = 'Person-Photo',
    [int]$MaxRows = 0,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$DryRun,
    [string]$SyncHostRoot = ''
)

$ErrorActionPreference = 'Stop'
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
if ($SyncHostRoot) {
    if (-not (Test-Path -LiteralPath $SyncHostRoot)) { throw "SyncHostRoot not found: $SyncHostRoot" }
    $SyncHostRoot = (Resolve-Path -LiteralPath $SyncHostRoot).Path
    $repo = $SyncHostRoot
    $dataImporterExe = Join-Path $SyncHostRoot 'tools\DataImporter\Visa2026.DataImporter.exe'
    if (-not (Test-Path -LiteralPath $dataImporterExe)) { throw "Published DataImporter not found: $dataImporterExe" }
}
else {
    . (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
    $repo = Get-Visa2026RepoRoot
    Set-Location $repo
}

if (-not $TargetConnection) {
    $TargetConnection = $env:ConnectionStrings__DefaultConnection
    if (-not $TargetConnection) {
        $TargetConnection = 'Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true'
    }
}
$logDir = if ($SyncHostRoot) { Join-Path $SyncHostRoot 'data\import-logs\document-copies' } else { Join-Path $repo 'artifacts/document-copies-import' }
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

# Sync-host maps live under data\id-maps\<LegacySource>; published DataImporter defaults to legacy\visa2014\id-maps.
$mapRoot = $null
if ($SyncHostRoot) {
    $mapRoot = Join-Path $SyncHostRoot "data\id-maps\$LegacySource"
    if (-not (Test-Path -LiteralPath $mapRoot)) {
        throw "Id-map directory not found: $mapRoot (expected sync-host maps from OnPrem-Sync scalar waves)"
    }
    Write-Host "INF File-wave id-map dir: $mapRoot" -ForegroundColor DarkGray
}
function New-FileWaveExtra {
    param([string[]]$Base, [string]$Flag, [string]$Entity)
    $extra = [System.Collections.Generic.List[string]]::new()
    foreach ($a in $Base) { $extra.Add($a) }
    if ($mapRoot) {
        $extra.Add($Flag)
        $extra.Add((Join-Path $mapRoot "$Entity.json"))
    }
    return ,@($extra.ToArray())
}

$commonArgs = @('--legacy-source', $LegacySource, '--inprocess', '--target-connection', $TargetConnection, '--no-wait')
if ($MaxRows -gt 0) { $commonArgs += @('--max-rows', $MaxRows) }
if ($DryRun) { $commonArgs += '--dry-run' }
$steps = @(
    @{ Key='Person-Photo'; Name='Person.Photo'; Extra=(New-FileWaveExtra -Base @('--import-visa2014-files','--entity','Person','--property','Photo') -Flag '--id-map' -Entity 'Person') },
    @{ Key='PassportDocument'; Name='Passport.PassportDocument'; Extra=(New-FileWaveExtra -Base @('--import-visa2014-files','--entity','Passport','--property','PassportDocument') -Flag '--passport-id-map' -Entity 'Passport') },
    @{ Key='VisaDocument'; Name='Visa.VisaDocument'; Extra=(New-FileWaveExtra -Base @('--import-visa2014-files','--entity','Visa','--property','VisaDocument') -Flag '--visa-id-map' -Entity 'Visa') },
    @{ Key='EducationDocument'; Name='Education.EducationDocument'; Extra=(New-FileWaveExtra -Base @('--import-visa2014-files','--entity','Education','--property','EducationDocument') -Flag '--education-id-map' -Entity 'Education') },
    @{ Key='WorkPermitDocument'; Name='WorkPermit.WorkPermitDocument'; Extra=(New-FileWaveExtra -Base @('--import-visa2014-files','--entity','WorkPermit','--property','WorkPermitDocument') -Flag '--work-permit-id-map' -Entity 'WorkPermit') },
    @{ Key='InvitationDocument'; Name='Invitation.InvitationDocument'; Extra=(New-FileWaveExtra -Base @('--import-visa2014-files','--entity','Invitation','--property','InvitationDocument') -Flag '--invitation-id-map' -Entity 'Invitation') },
    @{ Key='FamilyProofDocument'; Name='Person.FamilyProofDocument'; Extra=(New-FileWaveExtra -Base @('--import-visa2014-files','--entity','Person','--property','FamilyProofDocument') -Flag '--person-id-map' -Entity 'Person') }
)
$statusPath = if ($SyncHostRoot) { Join-Path $SyncHostRoot 'file-waves-status.json' } else { '' }
$fileWaveStatus = [ordered]@{
    Included=$true; StartedUtc=(Get-Date).ToUniversalTime().ToString('o'); CompletedUtc=$null; OverallStatus='Running'
    Steps=@($steps | ForEach-Object { [ordered]@{ Key=$_.Key; Name=$_.Name; Status='Pending'; ExitCode=$null; Prepared=''; Posted=''; ElapsedSeconds=$null } })
}
function Write-FileWaveStatus {
    if (-not $statusPath) { return }
    [System.IO.File]::WriteAllText($statusPath, (ConvertTo-Json -InputObject $fileWaveStatus -Depth 8), $utf8NoBom)
}
function Complete-FileWaveStatus([ValidateSet('Completed','Failed')][string]$OverallStatus) {
    $fileWaveStatus.OverallStatus = $OverallStatus
    $fileWaveStatus.CompletedUtc = (Get-Date).ToUniversalTime().ToString('o')
    Write-FileWaveStatus
}

Write-FileWaveStatus
$finalExitCode = 0
$summary = @()
try {
    if (-not $SyncHostRoot) {
        Write-Host "=== Build ($Configuration) ===" -ForegroundColor Cyan
        dotnet build Visa2026.DataImporter/Visa2026.DataImporter.csproj -c $Configuration /p:BuildProjectReferences=false
        if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    }

    # Prefer env for SQL auth. CLI --target-connection breaks on "User Id=..." (space)
    # and host startup can otherwise fall back to LocalDB from published appsettings.
    # Npgsql (Host= / Username=): do not rewrite Password→PWD — that breaks Postgres drivers.
    if (-not [string]::IsNullOrWhiteSpace($TargetConnection)) {
        $env:ConnectionStrings__DefaultConnection = $TargetConnection
        $env:ASPNETCORE_ENVIRONMENT = 'Production'
        $isPostgres = $TargetConnection -match '(?i)(^|;)\s*Host\s*=' -or
            $TargetConnection -match '(?i)EFCoreProvider\s*=\s*Postgres'
        $safeCs = if ($isPostgres) {
            $TargetConnection
        } else {
            $TargetConnection `
                -replace '(?i)\bUser Id=', 'UID=' `
                -replace '(?i)\bPassword=', 'PWD='
        }
        for ($ci = 0; $ci -lt $commonArgs.Count; $ci++) {
            if ($commonArgs[$ci] -eq '--target-connection' -and ($ci + 1) -lt $commonArgs.Count) {
                $commonArgs[$ci + 1] = $safeCs
                break
            }
        }
    }
    if ($SyncHostRoot -and [string]::IsNullOrWhiteSpace($env:VISA2026_MIGRATION_IMPORT_URLS)) {
        # Avoid colliding with prod DataImporter on :5002 (Demo uses :5012).
        $env:VISA2026_MIGRATION_IMPORT_URLS = 'http://127.0.0.1:5012'
    }

    $started = $false
    for ($i=0; $i -lt $steps.Count; $i++) {
        $step = $steps[$i]; $stepStatus = $fileWaveStatus.Steps[$i]
        if (-not $started) { if ($step.Key -eq $StartAt) { $started=$true } else { continue } }
        $log = Join-Path $logDir "$($step.Key).log"
        $stepStarted = Get-Date
        $stepStatus.Status = 'Running'; Write-FileWaveStatus
        Write-Host "==================== $($step.Name) ====================" -ForegroundColor Cyan
        if ($SyncHostRoot) {
            & $dataImporterExe @($step.Extra + $commonArgs) *>&1 | Tee-Object -FilePath $log
        } else {
            $runArgs = @('run','--project','Visa2026.DataImporter','--no-launch-profile','--no-build','-c',$Configuration,'--') + $step.Extra + $commonArgs
            & dotnet @runArgs *>&1 | Tee-Object -FilePath $log
        }
        $code = $LASTEXITCODE
        $preparedMatch = Select-String -Path $log -Pattern 'Prepared:' | Select-Object -Last 1
        $postedMatch = Select-String -Path $log -Pattern 'Posted:|Patched:' | Select-Object -Last 1
        $preparedLine = if ($preparedMatch) { $preparedMatch.Line.Trim() } else { '' }
        $postedLine = if ($postedMatch) { $postedMatch.Line.Trim() } else { '' }
        $stepStatus.ExitCode=$code; $stepStatus.Prepared=$preparedLine; $stepStatus.Posted=$postedLine
        $stepStatus.ElapsedSeconds=[int]((Get-Date)-$stepStarted).TotalSeconds
        $stepStatus.Status=if($code -eq 0){'Completed'}else{'Failed'}
        Write-FileWaveStatus
        $summary += "  $($step.Key) -> exit=$code  |  $preparedLine  |  $postedLine"
        if ($code -ne 0) { $finalExitCode=$code; Write-Host "STEP_FAILED $($step.Key) exit=$code" -ForegroundColor Red; break }
        Write-Host "STEP_OK $($step.Key)" -ForegroundColor Green
    }
    if ($finalExitCode -eq 0 -and -not $started) { throw "StartAt file wave not found: $StartAt" }
    Write-Host '==================== SUMMARY ====================' -ForegroundColor Cyan
    $summary | ForEach-Object { Write-Host $_ }
    if ($finalExitCode -eq 0) { Complete-FileWaveStatus Completed; Write-Host 'DOCUMENT_COPIES_COMPLETE' -ForegroundColor Green }
    else { Complete-FileWaveStatus Failed }
}
catch {
    $finalExitCode = if ($finalExitCode -ne 0) { $finalExitCode } else { 1 }
    Complete-FileWaveStatus Failed
    Write-Host "DOCUMENT_COPIES_FAILED $($_.Exception.Message)" -ForegroundColor Red
}
$global:LASTEXITCODE = $finalExitCode
