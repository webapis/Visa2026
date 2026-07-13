#Requires -Version 5.1
<#
.SYNOPSIS
  VISA2015 (10.100.128.15) → Visa2026 on-prem IIS (10.100.128.25) — ordered import waves.

.DESCRIPTION
  Runs Visa2026.DataImporter --import-visa2014 entity-by-entity per order.yaml.
  All scalar writes use --inprocess (headless XAF ObjectSpace).
  Application / ApplicationItem / ApplicationProgress post sequentially
  (ParallelImportPoster / shared batch-size>1 hung Prod on LatestProgress commits).
  --parallelism is ignored for those waves; kept for CLI compatibility.

  Profiles:
    Staging    — Visa2026DbStaging (:8080), calik-energi-onprem-staging id-maps
    Production — Visa2026DbProd (:443), calik-energi-onprem-prod id-maps
    Demo       — Visa2026DbDemo (:8081), calik-energi-onprem-demo id-maps

  Import-only (--import-visa2014). No delta Sync / --sync-visa2014 path.

  Runbook: docs/VISA2014_MIGRATION/ON_PREM_IIS_MIGRATION_RUNBOOK.md
  Agent skill: .cursor/skills/visa2014-to-visa2026-import/SKILL.md

.PARAMETER Profile
  Target IIS slot. Maps legacy source, id-map dir, and default connection env var.

.PARAMETER TargetConnection
  SQL connection to target DB. Default: VISA2026_STAGING_SQL_CONNECTION or VISA2026_PROD_SQL_CONNECTION.

.PARAMETER IncludeFileWaves
  After scalar waves, run DocumentCopies.ps1 (photos, passport/visa scans, etc.).

.PARAMETER SyncHostRoot
  Server layout root (e.g. C:\visa2026-sync on 10.100.128.25). Uses published Visa2026.DataImporter.exe
  instead of dotnet run. See Install-OnPremSyncHost.ps1.

.PARAMETER SkipLookupPreflight
  Skip the mandatory lookup audit gate. Use only when resuming after a verified preflight
  in the same session, or for emergency catch-up.

.EXAMPLE
  $env:VISA2014_SQL_PASSWORD = '...'
  .\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Staging `
    -TargetConnection $env:VISA2026_STAGING_SQL_CONNECTION

.EXAMPLE
  .\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Demo -SkipTenantCatalogGeneration

.EXAMPLE
  .\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Staging -DryRun -Entity Person
#>
[CmdletBinding()]
param(
    [ValidateSet("Staging", "Production", "Demo")]
    [string]$Profile = "Staging",
    [string]$LegacySource = "",
    [string]$TargetConnection = "",
    [string]$ApiBaseUrl = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [int]$BatchSize = 50,
    [int]$Parallelism = 1,
    [string[]]$Entity = @(),
    [string]$StartAt = "",
    [switch]$DryRun,
    # When set, keep running later waves after a wave fails. Default (unset) = stop on first failure.
    [switch]$ContinueOnError,
    [switch]$SkipTenantCatalogGeneration,
    [switch]$SkipPostImportCorrections,
    [switch]$SkipLookupPreflight,
    [switch]$IncludeFileWaves,
    [string]$SyncHostRoot = ""
)

$ErrorActionPreference = "Stop"

$script:DataImporterExe = $null
if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    . (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
    $repoRoot = Get-Visa2026RepoRoot
    Set-Location $repoRoot
}
else {
    if (-not (Test-Path -LiteralPath $SyncHostRoot)) {
        throw "SyncHostRoot not found: $SyncHostRoot"
    }
    $SyncHostRoot = (Resolve-Path -LiteralPath $SyncHostRoot).Path
    $repoRoot = $SyncHostRoot
    $dataImporterRoot = Join-Path $SyncHostRoot 'tools\DataImporter'
    $script:DataImporterExe = Join-Path $dataImporterRoot 'Visa2026.DataImporter.exe'
    if (-not (Test-Path -LiteralPath $script:DataImporterExe)) {
        throw "Published DataImporter not found: $($script:DataImporterExe). Run Install-OnPremSyncHost.ps1 on .25 or deploy from dev."
    }
}

$profileConfig = @{
    Staging = @{
        LegacySource         = "calik-energi-onprem-staging"
        IdMapSubDir          = "calik-energi-onprem-staging"
        ApiBaseUrl           = "http://10.100.128.25:8080"
        TargetConnectionEnv  = "VISA2026_STAGING_SQL_CONNECTION"
        LogPrefix            = "staging"
    }
    Production = @{
        LegacySource         = "calik-energi-onprem-prod"
        IdMapSubDir          = "calik-energi-onprem-prod"
        ApiBaseUrl           = "http://10.100.128.25"
        TargetConnectionEnv  = "VISA2026_PROD_SQL_CONNECTION"
        LogPrefix            = "prod"
    }
    Demo = @{
        LegacySource         = "calik-energi-onprem-demo"
        IdMapSubDir          = "calik-energi-onprem-demo"
        ApiBaseUrl           = "http://10.100.128.25:8081"
        TargetConnectionEnv  = "VISA2026_DEMO_SQL_CONNECTION"
        LogPrefix            = "demo"
    }
}

$cfg = $profileConfig[$Profile]
if ([string]::IsNullOrWhiteSpace($LegacySource)) { $LegacySource = $cfg.LegacySource }
if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) { $ApiBaseUrl = $cfg.ApiBaseUrl }
if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
    $TargetConnection = [Environment]::GetEnvironmentVariable($cfg.TargetConnectionEnv, "Process")
    if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
        $TargetConnection = [Environment]::GetEnvironmentVariable($cfg.TargetConnectionEnv, "User")
    }
}

if (-not $script:DataImporterExe) {
    $dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
}
if ($SyncHostRoot) {
    $mapRoot = Join-Path $SyncHostRoot "data\id-maps\$($cfg.IdMapSubDir)"
    $logRoot = Join-Path $SyncHostRoot 'data\import-logs'
}
else {
    $mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$($cfg.IdMapSubDir)"
    $logRoot = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
}
$logPrefix = $cfg.LogPrefix
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"

if (-not $env:VISA2014_SQL_PASSWORD) {
    $env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable("VISA2014_SQL_PASSWORD", "User")
}

if ([string]::IsNullOrWhiteSpace($TargetConnection) -and -not $DryRun) {
    throw "TargetConnection (or $($cfg.TargetConnectionEnv)) required for headless in-process import."
}
if ([string]::IsNullOrWhiteSpace($env:VISA2014_SQL_PASSWORD) -and -not $DryRun) {
    throw "Set VISA2014_SQL_PASSWORD (user env) for ReadOnlyUser on 10.100.128.15."
}

New-Item -ItemType Directory -Force -Path $mapRoot, $logRoot | Out-Null

# Fresh Import: Visa2014IdMapHelper.Load requires the file to exist (even empty {}).
# Create missing stubs so greenfield Demo/Staging loads do not fail on Person.
$idMapStubNames = @(
    'Person', 'Passport', 'Visa', 'Education', 'EmployeePositionHistory', 'EmployeeSalary',
    'AddressOfResidence', 'Application', 'WorkPermit', 'WorkPermitItem', 'Invitation', 'InvitationItem',
    'ApplicationItem', 'ApplicationProgress', 'MedicalRecord', 'MedicalRecordDocument',
    'EducationDocument', 'PassportCopy', 'VisaDocument', 'FamilyProofDocument',
    'InvitationDocument', 'WorkPermitDocument'
)
foreach ($stubName in $idMapStubNames) {
    $stubPath = Join-Path $mapRoot "$stubName.json"
    if (-not (Test-Path -LiteralPath $stubPath)) {
        [System.IO.File]::WriteAllText($stubPath, '{}', [System.Text.UTF8Encoding]::new($false))
    }
}

function Resolve-OnPremMigrationLibPath {
    param([Parameter(Mandatory)][string]$FileName)
    foreach ($candidate in @(
            (Join-Path $PSScriptRoot "_lib\$FileName"),
            (Join-Path $PSScriptRoot "..\_lib\$FileName")
        )) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
    throw "Lib not found: $FileName under $PSScriptRoot\_lib or ..\_lib (sync-host vs repo layout)."
}
. (Resolve-OnPremMigrationLibPath 'OnPremSyncRunStatus.ps1')
. (Resolve-OnPremMigrationLibPath 'OnPremImportRunArchive.ps1')
$syncStatusRoot = if ($SyncHostRoot) { $SyncHostRoot } else { Join-Path $dataImporterRoot 'legacy/visa2014' }

function Invoke-ArchiveCurrentImportRun {
    param(
        [string]$Reason = '',
        [switch]$Force
    )
    $flags = @()
    if ($DryRun) { $flags += 'DryRun' }
    if ($SkipLookupPreflight) { $flags += 'SkipLookupPreflight' }
    if ($SkipTenantCatalogGeneration) { $flags += 'SkipTenantCatalogGeneration' }
    if ($ContinueOnError) { $flags += 'ContinueOnError' }
    if ($IncludeFileWaves) { $flags += 'IncludeFileWaves' }
    if ($StartAt) { $flags += "StartAt=$StartAt" }
    if ($Reason) { $flags += $Reason }
    try {
        Save-OnPremImportRunArchive `
            -SyncHostRoot $syncStatusRoot `
            -Profile $Profile `
            -RunId $stamp `
            -StartAt $StartAt `
            -Flags $flags `
            -Force:$Force | Out-Null
    }
    catch {
        Write-Warning "Import run archive failed: $($_.Exception.Message)"
    }
}

function Get-MapPath([string]$name) {
    Join-Path $mapRoot "$name.json"
}

function Invoke-DataImporterCli {
    param(
        [string[]]$CliArgs,
        [string]$LogFile = ''
    )

    # Prefer env for SQL auth. CLI --target-connection breaks on "User Id=..." (space)
    # and yields "Invalid value for key 'Encrypt'" / "Login failed for user 'sa'".
    if (-not [string]::IsNullOrWhiteSpace($TargetConnection)) {
        $env:ConnectionStrings__DefaultConnection = $TargetConnection
        $env:ASPNETCORE_ENVIRONMENT = 'Production'
        # Encrypt=False breaks some Microsoft.Data.SqlClient builds ("Invalid value for key 'Encrypt'").
        $safeCs = $TargetConnection `
            -replace '(?i)\bUser Id=', 'UID=' `
            -replace '(?i)\bPassword=', 'PWD=' `
            -replace '(?i)\bEncrypt\s*=\s*False\b', 'Encrypt=Optional' `
            -replace '(?i)\bEncrypt\s*=\s*True\b', 'Encrypt=Mandatory'
        if ($safeCs -notmatch '(?i)\bEncrypt\s*=') {
            $safeCs = $safeCs.TrimEnd(';') + ';Encrypt=Optional'
        }
        $env:ConnectionStrings__DefaultConnection = $safeCs
        for ($i = 0; $i -lt $CliArgs.Count; $i++) {
            if ($CliArgs[$i] -eq '--target-connection' -and ($i + 1) -lt $CliArgs.Count) {
                $CliArgs[$i + 1] = $safeCs
                break
            }
        }
    }

    # HeadlessMigrationHost defaults to :5002. Parallel slot imports need distinct ports
    # (VISA2026_MIGRATION_IMPORT_URLS). Keep Production on 5002; Staging/Demo offset.
    if ([string]::IsNullOrWhiteSpace($env:VISA2026_MIGRATION_IMPORT_URLS)) {
        $env:VISA2026_MIGRATION_IMPORT_URLS = switch ($Profile) {
            'Staging' { 'http://127.0.0.1:5011' }
            'Demo' { 'http://127.0.0.1:5012' }
            default { 'http://127.0.0.1:5002' }
        }
    }

    # Stream to log via Start-Process redirects. Do NOT capture `& exe 2>&1` into
    # a PowerShell variable — Education/Visa waves emit thousands of stderr lines
    # ("incomplete payload") and that + Start-Transcript kills the orchestrator
    # host with no wave log (Tee-Object only ran after exit).
    $outLog = $LogFile
    $errLog = ''
    if ([string]::IsNullOrWhiteSpace($outLog)) {
        $tmpDir = Join-Path ([System.IO.Path]::GetTempPath()) 'visa2026-sync'
        if (-not (Test-Path -LiteralPath $tmpDir)) {
            New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null
        }
        $outLog = Join-Path $tmpDir ("di-{0}.out.log" -f [guid]::NewGuid().ToString('N'))
        $errLog = Join-Path $tmpDir ("di-{0}.err.log" -f [guid]::NewGuid().ToString('N'))
    }
    else {
        $errLog = "$LogFile.err"
    }

    $filePath = $null
    $argumentList = $null
    if ($script:DataImporterExe) {
        $filePath = $script:DataImporterExe
        $argumentList = $CliArgs
    }
    else {
        $filePath = 'dotnet'
        $argumentList = @(
            'run', '--project', (Join-Path $repoRoot 'Visa2026.DataImporter\Visa2026.DataImporter.csproj'),
            '-c', $Configuration, '--'
        ) + $CliArgs
    }

    $workDir = if ($script:DataImporterExe) {
        Split-Path -Parent $script:DataImporterExe
    } else {
        $repoRoot
    }

    # Escape args for ProcessStartInfo (spaces in --target-connection etc.)
    $argString = ($argumentList | ForEach-Object {
        $a = [string]$_
        if ($a -match '[\s"]') { '"' + ($a -replace '"', '\"') + '"' } else { $a }
    }) -join ' '

    $p = Start-Process -FilePath $filePath -ArgumentList $argString `
        -WorkingDirectory $workDir `
        -RedirectStandardOutput $outLog `
        -RedirectStandardError $errLog `
        -PassThru -NoNewWindow -Wait
    $exitCode = $p.ExitCode
    if ($null -eq $exitCode) { $exitCode = 1 }

    if ($LogFile -and (Test-Path -LiteralPath $errLog) -and (Get-Item -LiteralPath $errLog).Length -gt 0) {
        Add-Content -LiteralPath $LogFile -Value (Get-Content -LiteralPath $errLog -Raw) -Encoding UTF8
    }

    if (-not $LogFile) {
        if (Test-Path -LiteralPath $outLog) {
            Get-Content -LiteralPath $outLog | ForEach-Object { Write-Host $_ }
        }
        if (Test-Path -LiteralPath $errLog) {
            Get-Content -LiteralPath $errLog | ForEach-Object { Write-Host $_ -ForegroundColor DarkYellow }
        }
        Remove-Item -LiteralPath $outLog, $errLog -Force -ErrorAction SilentlyContinue
    }

    return ,[int]$exitCode
}

$scalarEntities = @{
    Person = $true; Passport = $true; Visa = $true; Education = $true
    EmployeePositionHistory = $true; EmployeeSalary = $true; AddressOfResidence = $true
    Application = $true; WorkPermit = $true; WorkPermitItem = $true
    Invitation = $true; InvitationItem = $true
    ApplicationItem = $true; ApplicationProgress = $true
}

$applicationDomainEntities = @{
    Application = $true; WorkPermit = $true; WorkPermitItem = $true
    Invitation = $true; InvitationItem = $true
    ApplicationItem = $true; ApplicationProgress = $true
}
$tenantCatalogGenerationDone = $false

function Invoke-PostImportCorrections {
    param([string]$Conn)
    if ($DryRun -or $SkipPostImportCorrections) { return }
    if ([string]::IsNullOrWhiteSpace($Conn)) {
        throw "TargetConnection required for postImportCorrections (order.yaml)."
    }

    $corrections = @(
        @{ Name = "PersonSubcontractor"; Flag = "--correct-person-subcontractor" },
        @{ Name = "PersonRelationship"; Flag = "--correct-person-relationship" },
        @{ Name = "PersonAddressPia"; Flag = "--correct-person-address-of-residence" },
        @{ Name = "ApplicationItemPersonCurrent"; Flag = "--correct-application-item-person-current" }
    )

    Write-Host ""
    Write-Host ">>> postImportCorrections (order.yaml)" -ForegroundColor Green
    foreach ($corr in $corrections) {
        $logFile = Join-Path $logRoot "$logPrefix-post-$($corr.Name)-$stamp.log"
        Write-Host ">>> $($corr.Name)  ->  $logFile" -ForegroundColor Green
        $corrArgs = @(
            $corr.Flag,
            "--legacy-source", $LegacySource,
            "--target-connection", $Conn,
            "--verbose"
        )
        $exit = Invoke-DataImporterCli -CliArgs $corrArgs -LogFile $logFile
        if ($exit -ne 0) {
            Write-Host "ERR postImportCorrection $($corr.Name) failed (exit $exit). Log: $logFile" -ForegroundColor Red
            if (-not $ContinueOnError) { exit $exit }
        }
    }
}

function Invoke-TenantCatalogGenerationIfNeeded {
    param([string]$WaveName)
    if ($SkipTenantCatalogGeneration -or $DryRun -or $tenantCatalogGenerationDone) { return }
    if (-not $applicationDomainEntities.ContainsKey($WaveName)) { return }

    Write-Host ""
    Write-Host ">>> tenantCatalogGeneration (order.yaml) before $WaveName" -ForegroundColor Green
    $genArgs = @(
        '--generate-visa2014-tenant-catalogs',
        '--legacy-source', $LegacySource
    )
    $exit = Invoke-DataImporterCli -CliArgs $genArgs
    if ($exit -ne 0) {
        throw "tenantCatalogGeneration failed (exit $exit). Fix VISA2014_SQL_PASSWORD / legacy SQL reachability."
    }
    $script:tenantCatalogGenerationDone = $true
    Write-Host "INF Re-run target DB update (Module updaters) if approval-leg-profile.json changed." -ForegroundColor Yellow
}

function Invoke-ImportWave {
    param(
        [string]$WaveName,
        [string[]]$ExtraArgs,
        [ValidateSet("Scalar", "File")]
        [string]$Kind = "Scalar"
    )

    Invoke-TenantCatalogGenerationIfNeeded -WaveName $WaveName

    $logFile = Join-Path $logRoot "$logPrefix-$WaveName-$stamp.log"
    Write-Host ""
    Write-Host ">>> $WaveName  ->  $logFile" -ForegroundColor Green
    Set-OnPremSyncRunWaveStarted -Root $syncStatusRoot -WaveName $WaveName -LogFile $logFile

    if ($Kind -eq "Scalar") {
        $waveCli = @(
            "--import-visa2014",
            "--legacy-source", $LegacySource,
            "--no-wait"
        ) + $ExtraArgs

        if (-not $scalarEntities.ContainsKey($WaveName)) {
            throw "Entity $WaveName is not configured for scalar in-process import."
        }
        if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
            throw "TargetConnection required for in-process entity $WaveName."
        }
        $waveCli += @("--inprocess", "--batch-size", $BatchSize, "--parallelism", "$Parallelism")
        # Prefer ConnectionStrings__DefaultConnection env (set below via Invoke-DataImporterCli).
        # Do NOT pass --target-connection on the CLI: Start-Process single-string ArgumentList
        # mangles "User Id=..." / long CS values and can corrupt Boolean keywords.
    }
    else {
        $waveCli = @(
            "--legacy-source", $LegacySource,
            "--inprocess", "--target-connection", $TargetConnection,
            "--no-wait"
        ) + $ExtraArgs
    }

    if ($DryRun) { $waveCli += "--dry-run" }
    if ($SkipTenantCatalogGeneration) { $waveCli += "--skip-tenant-catalog-generation" }

    $exit = 0
    try {
        $exit = Invoke-DataImporterCli -CliArgs $waveCli -LogFile $logFile
    }
    catch {
        Write-Host "ERR ${WaveName}: $_" -ForegroundColor Red
        $exit = 1
    }

    Set-OnPremSyncRunWaveCompleted -Root $syncStatusRoot -WaveName $WaveName -ExitCode $exit -LogFile $logFile

    if ($exit -ne 0) {
        Write-Host "ERR Wave $WaveName failed (exit $exit). Log: $logFile" -ForegroundColor Red
        if (-not $ContinueOnError) {
            Write-Host "INF Re-run with -StartAt $WaveName after fixing the issue." -ForegroundColor Yellow
            Complete-OnPremSyncRunStatus -Root $syncStatusRoot -OverallStatus Failed
            Invoke-ArchiveCurrentImportRun -Reason "WaveFailed=$WaveName"
            exit $exit
        }
    }
}

# order.yaml topological order (on-prem scalar + MedicalRecord file wave)
$waves = @(
    @{ Name = "Person"; Kind = "Scalar"; Args = @("--entity", "Person", "--id-map-output", (Get-MapPath "Person")) }
    @{
        Name = "Passport"; Kind = "Scalar"
        Args = @(
            "--entity", "Passport",
            "--id-map-output", (Get-MapPath "Passport"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "Visa"; Kind = "Scalar"
        Args = @(
            "--entity", "Visa",
            "--id-map-output", (Get-MapPath "Visa"),
            "--passport-id-map", (Get-MapPath "Passport")
        )
    }
    @{
        Name = "Education"; Kind = "Scalar"
        Args = @(
            "--entity", "Education",
            "--id-map-output", (Get-MapPath "Education"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "EmployeePositionHistory"; Kind = "Scalar"
        Args = @(
            "--entity", "EmployeePositionHistory",
            "--id-map-output", (Get-MapPath "EmployeePositionHistory"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "AddressOfResidence"; Kind = "Scalar"
        Args = @(
            "--entity", "AddressOfResidence",
            "--id-map-output", (Get-MapPath "AddressOfResidence"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "EmployeeSalary"; Kind = "Scalar"
        Args = @(
            "--entity", "EmployeeSalary",
            "--id-map-output", (Get-MapPath "EmployeeSalary"),
            "--person-id-map", (Get-MapPath "Person"),
            "--position-history-id-map", (Get-MapPath "EmployeePositionHistory")
        )
    }
    @{
        Name = "MedicalRecord"; Kind = "File"
        Args = @(
            "--import-visa2014-files", "--entity", "MedicalRecord", "--property", "MedicalRecordDocument",
            "--person-id-map", (Get-MapPath "Person"),
            "--medical-record-id-map-output", (Get-MapPath "MedicalRecord"),
            "--document-id-map-output", (Get-MapPath "MedicalRecordDocument")
        )
    }
    @{
        Name = "Application"; Kind = "Scalar"
        Args = @(
            "--entity", "Application",
            "--id-map-output", (Get-MapPath "Application"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "WorkPermit"; Kind = "Scalar"
        Args = @(
            "--entity", "WorkPermit",
            "--id-map-output", (Get-MapPath "WorkPermit"),
            "--application-id-map", (Get-MapPath "Application")
        )
    }
    @{
        Name = "WorkPermitItem"; Kind = "Scalar"
        Args = @(
            "--entity", "WorkPermitItem",
            "--id-map-output", (Get-MapPath "WorkPermitItem"),
            "--person-id-map", (Get-MapPath "Person"),
            "--passport-id-map", (Get-MapPath "Passport"),
            "--position-history-id-map", (Get-MapPath "EmployeePositionHistory"),
            "--work-permit-id-map", (Get-MapPath "WorkPermit")
        )
    }
    @{
        Name = "Invitation"; Kind = "Scalar"
        Args = @(
            "--entity", "Invitation",
            "--id-map-output", (Get-MapPath "Invitation"),
            "--application-id-map", (Get-MapPath "Application")
        )
    }
    @{
        Name = "InvitationItem"; Kind = "Scalar"
        Args = @(
            "--entity", "InvitationItem",
            "--id-map-output", (Get-MapPath "InvitationItem"),
            "--person-id-map", (Get-MapPath "Person"),
            "--passport-id-map", (Get-MapPath "Passport"),
            "--invitation-id-map", (Get-MapPath "Invitation")
        )
    }
    @{
        Name = "ApplicationItem"; Kind = "Scalar"
        Args = @(
            "--entity", "ApplicationItem",
            "--id-map-output", (Get-MapPath "ApplicationItem"),
            "--person-id-map", (Get-MapPath "Person"),
            "--application-id-map", (Get-MapPath "Application"),
            "--passport-id-map", (Get-MapPath "Passport"),
            "--visa-id-map", (Get-MapPath "Visa"),
            "--position-history-id-map", (Get-MapPath "EmployeePositionHistory"),
            "--address-id-map", (Get-MapPath "AddressOfResidence"),
            "--education-id-map", (Get-MapPath "Education"),
            "--employee-salary-id-map", (Get-MapPath "EmployeeSalary"),
            "--work-permit-item-id-map", (Get-MapPath "WorkPermitItem"),
            "--invitation-item-id-map", (Get-MapPath "InvitationItem")
        )
    }
    @{
        Name = "ApplicationProgress"; Kind = "Scalar"
        Args = @(
            "--entity", "ApplicationProgress",
            "--id-map-output", (Get-MapPath "ApplicationProgress"),
            "--application-id-map", (Get-MapPath "Application")
        )
    }
)

if ($Entity.Count -gt 0) {
    $wanted = $Entity | ForEach-Object { $_.Trim() }
    $waves = $waves | Where-Object { $wanted -contains $_.Name }
    if ($waves.Count -eq 0) {
        throw "No matching entities in: $($Entity -join ', ')"
    }
}

$started = [string]::IsNullOrWhiteSpace($StartAt)
if (-not $started) {
    Write-Host "INF Resume mode: skipping entities until '$StartAt'..." -ForegroundColor Yellow
}

Write-Host "=== VISA2014 on-prem Import ($Profile) ===" -ForegroundColor Cyan
if ($SyncHostRoot) { Write-Host "INF Sync host: $SyncHostRoot (published exe)" -ForegroundColor DarkGray }
Write-Host "INF Legacy source: $LegacySource (SQL 10.100.128.15 / VISA2015)"
Write-Host "INF Target API:    $ApiBaseUrl"
Write-Host "INF Id-map dir:    $mapRoot"
Write-Host "INF Log dir:       $logRoot"
Write-Host "INF Mode: Import (--import-visa2014)" -ForegroundColor DarkGray
if ($DryRun) { Write-Host "INF Dry-run" -ForegroundColor Yellow }
if ($IncludeFileWaves) { Write-Host "INF File waves: DocumentCopies.ps1 after scalar chain" -ForegroundColor Yellow }

$activeWaves = @($waves | ForEach-Object { $_.Name })
$previousFileWaveStatus = Join-Path $syncStatusRoot 'file-waves-status.json'
if (Test-Path -LiteralPath $previousFileWaveStatus) { Remove-Item -LiteralPath $previousFileWaveStatus -Force }

Initialize-OnPremSyncRunStatus `
    -Root $syncStatusRoot `
    -RunId $stamp `
    -LegacySource $LegacySource `
    -Profile $Profile `
    -WaveNames $activeWaves

# Lookup preflight gate: mandatory for Import (audit → translate → target map).
if (-not $SkipLookupPreflight) {
    Write-Host ""
    Write-Host ">>> lookupPreflight (audit → translate → attach/map)" -ForegroundColor Green
    $preflightLog = Join-Path $logRoot "$logPrefix-lookup-preflight-$stamp.log"
    $preflightArgs = @(
        "--preflight-visa2014-lookups",
        "--legacy-source", $LegacySource,
        "--verbose"
    )
    if (-not [string]::IsNullOrWhiteSpace($TargetConnection)) {
        $preflightArgs += @("--target-connection", $TargetConnection)
    }
    $preflightExit = Invoke-DataImporterCli -CliArgs $preflightArgs -LogFile $preflightLog
    if ($preflightExit -ne 0) {
        Write-Host "ERR Lookup preflight failed (exit $preflightExit). Fix catalogs/translations, then re-run." -ForegroundColor Red
        Write-Host "INF Report/log: $preflightLog" -ForegroundColor Yellow
        Write-Host "INF Bypass only with -SkipLookupPreflight after an approved exception." -ForegroundColor Yellow
        Complete-OnPremSyncRunStatus -Root $syncStatusRoot -OverallStatus Failed
        Invoke-ArchiveCurrentImportRun -Reason 'LookupPreflightFailed'
        exit $preflightExit
    }
    Write-Host "INF Lookup preflight passed." -ForegroundColor Green
}
else {
    Write-Host "WRN Lookup preflight skipped (-SkipLookupPreflight)." -ForegroundColor Yellow
}

foreach ($wave in $waves) {
    if (-not $started) {
        if ($wave.Name -eq $StartAt) { $started = $true }
        else {
            Write-Host "INF Skip (resume): $($wave.Name)" -ForegroundColor DarkGray
            continue
        }
    }

    Invoke-ImportWave -WaveName $wave.Name -ExtraArgs $wave.Args -Kind $wave.Kind
}

$runStatusPath = Get-OnPremSyncRunStatusPath -Root $syncStatusRoot
$finalRunStatus = Read-OnPremSyncRunStatus -Path $runStatusPath
$anyFailed = $false
if ($finalRunStatus -and $finalRunStatus.Waves) {
    foreach ($w in $finalRunStatus.Waves) {
        if ($w.Status -eq 'Failed') { $anyFailed = $true; break }
    }
}

Write-Host ""
Invoke-PostImportCorrections -Conn $TargetConnection

if ($IncludeFileWaves -and -not $DryRun) {
    $documentCopiesExit = 0
    if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
        Write-Host "ERR TargetConnection required for file waves." -ForegroundColor Red
        $documentCopiesExit = 1
    }
    else {
        Write-Host ""
        Write-Host ">>> DocumentCopies (file waves)" -ForegroundColor Green
        $docArgs = @{
            TargetConnection = $TargetConnection
            LegacySource     = $LegacySource
            Configuration    = $Configuration
            SyncHostRoot     = $SyncHostRoot
        }
        try {
            & (Join-Path $PSScriptRoot 'DocumentCopies.ps1') @docArgs
            $documentCopiesExit = $LASTEXITCODE
        }
        catch {
            $documentCopiesExit = 1
            Write-Host "ERR DocumentCopies invocation failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    if ($documentCopiesExit -ne 0) {
        $anyFailed = $true
        Write-Host "ERR DocumentCopies failed (exit $documentCopiesExit)." -ForegroundColor Red
        if (-not $ContinueOnError) {
            Complete-OnPremSyncRunStatus -Root $syncStatusRoot -OverallStatus Failed
            Invoke-ArchiveCurrentImportRun -Reason 'DocumentCopiesFailed' -Force
            exit $documentCopiesExit
        }
    }
}

Complete-OnPremSyncRunStatus -Root $syncStatusRoot -OverallStatus $(if ($anyFailed) { 'Failed' } else { 'Completed' })
Invoke-ArchiveCurrentImportRun -Reason $(if ($anyFailed) { 'FinishedWithFailures' } else { 'Finished' })

Write-Host "=== On-prem $Profile import waves finished ===" -ForegroundColor Cyan
Write-Host "INF Live wave status: scripts/visa2014-migration/Watch-OnPremImportLive.ps1 (-Profile $Profile)"
Write-Host "INF Reimport history: $(Join-Path (Get-OnPremImportRunHistoryRoot -SyncHostRoot $syncStatusRoot) 'index.html')"
Write-Host "INF Compare reimports: scripts/visa2014-migration/Compare-OnPremImportRuns.ps1 (-Profile $Profile)"
Write-Host "INF Reconcile counts: scripts/visa2014-migration/Compare-LegacyMigratedCounts.ps1 (-ShowIdMap)"
Write-Host "INF Officer read-only UAT: $ApiBaseUrl/LoginPage"
