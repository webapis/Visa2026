#Requires -Version 5.1
<#
.SYNOPSIS
  VISA2015 (10.100.128.15) → Visa2026 on-prem IIS (10.100.128.25) — ordered import waves.

.DESCRIPTION
  Runs Visa2026.DataImporter --import-visa2014 entity-by-entity per order.yaml.
  All scalar writes use --inprocess (headless XAF ObjectSpace).

  Profiles:
    Staging    — Visa2026DbStaging (:8080), calik-energi-onprem-staging id-maps
    Production — Visa2026DbProd (:80), calik-energi-onprem-prod id-maps

  Runbook: docs/VISA2014_MIGRATION/ON_PREM_IIS_MIGRATION_RUNBOOK.md
  Agent skill: .cursor/skills/visa2026-onprem-legacy-sync/SKILL.md

.PARAMETER Profile
  Target IIS slot. Maps legacy source, id-map dir, and default connection env var.

.PARAMETER TargetConnection
  SQL connection to target DB. Default: VISA2026_STAGING_SQL_CONNECTION or VISA2026_PROD_SQL_CONNECTION.

.PARAMETER IncludeFileWaves
  After scalar waves, run DocumentCopies.ps1 (photos, passport/visa scans, etc.).

.EXAMPLE
  $env:VISA2014_SQL_PASSWORD = '...'
  .\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Staging `
    -TargetConnection $env:VISA2026_STAGING_SQL_CONNECTION

.PARAMETER Mode
  Import — initial load / new-row catch-up (--import-visa2014).
  Sync — delta upsert (--sync-visa2014): inserts, updates, soft-deletes.

.PARAMETER SyncFull
  With -Mode Sync: update all id-mapped rows (first manual run). Default: audit incremental since last watermark.

.PARAMETER SyncStateDir
  Optional directory for per-slot sync watermark JSON (default: Visa2026.DataImporter/legacy/visa2014/sync-state/).

.EXAMPLE
  .\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Production -Mode Sync -SyncFull

.EXAMPLE
  .\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Staging -DryRun -Entity Person
#>
[CmdletBinding()]
param(
    [ValidateSet("Staging", "Production")]
    [string]$Profile = "Staging",
    [string]$LegacySource = "",
    [string]$TargetConnection = "",
    [string]$ApiBaseUrl = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [int]$BatchSize = 50,
    [string[]]$Entity = @(),
    [string]$StartAt = "",
    [switch]$DryRun,
    [switch]$ContinueOnError,
    [switch]$SkipTenantCatalogGeneration,
    [switch]$SkipPostImportCorrections,
    [switch]$IncludeFileWaves,
    [ValidateSet("Import", "Sync")]
    [string]$Mode = "Import",
    [switch]$SyncFull,
    [string]$SyncStateDir = ""
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

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

$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$($cfg.IdMapSubDir)"
$logRoot = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
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

$syncStateDirResolved = if ([string]::IsNullOrWhiteSpace($SyncStateDir)) {
    Join-Path $dataImporterRoot "legacy/visa2014/sync-state"
} else { $SyncStateDir }

if ($Mode -eq "Sync" -and $Profile -ne "Production") {
    Write-Host "WRN Legacy sync is prod-only; -Profile Staging is for import/catch-up testing only." -ForegroundColor Yellow
}

New-Item -ItemType Directory -Force -Path $mapRoot, $logRoot, $syncStateDirResolved | Out-Null

function Get-MapPath([string]$name) {
    Join-Path $mapRoot "$name.json"
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
            "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
            $corr.Flag,
            "--legacy-source", $LegacySource,
            "--target-connection", $Conn,
            "--verbose"
        )
        & dotnet @corrArgs 2>&1 | Tee-Object -FilePath $logFile
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERR postImportCorrection $($corr.Name) failed (exit $LASTEXITCODE). Log: $logFile" -ForegroundColor Red
            if (-not $ContinueOnError) { exit $LASTEXITCODE }
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
        'run', '--project', 'Visa2026.DataImporter', '-c', $Configuration, '--',
        '--generate-visa2014-tenant-catalogs',
        '--legacy-source', $LegacySource
    )
    & dotnet @genArgs
    if ($LASTEXITCODE -ne 0) {
        throw "tenantCatalogGeneration failed (exit $LASTEXITCODE). Fix VISA2014_SQL_PASSWORD / legacy SQL reachability."
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

    if ($Kind -eq "Scalar") {
        $cliVerb = if ($Mode -eq "Sync") { "--sync-visa2014" } else { "--import-visa2014" }
        $waveArgs = @(
            "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
            $cliVerb,
            "--legacy-source", $LegacySource,
            "--no-wait"
        ) + $ExtraArgs

        if ($Mode -eq "Sync") {
            $waveArgs += @("--sync-state-dir", $syncStateDirResolved)
            if ($SyncFull) { $waveArgs += "--sync-full" }
        }

        if (-not $scalarEntities.ContainsKey($WaveName)) {
            throw "Entity $WaveName is not configured for scalar in-process import."
        }
        if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
            throw "TargetConnection required for in-process entity $WaveName."
        }
        $waveArgs += @("--inprocess", "--target-connection", $TargetConnection, "--batch-size", $BatchSize)
    }
    else {
        $waveArgs = @(
            "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
            "--legacy-source", $LegacySource,
            "--inprocess", "--target-connection", $TargetConnection,
            "--no-wait"
        ) + $ExtraArgs
    }

    if ($DryRun) { $waveArgs += "--dry-run" }

    $exit = 0
    try {
        & dotnet @waveArgs 2>&1 | Tee-Object -FilePath $logFile
        $exit = $LASTEXITCODE
    }
    catch {
        Write-Host "ERR ${WaveName}: $_" -ForegroundColor Red
        $exit = 1
    }

    if ($exit -ne 0) {
        Write-Host "ERR Wave $WaveName failed (exit $exit). Log: $logFile" -ForegroundColor Red
        if (-not $ContinueOnError) {
            Write-Host "INF Re-run with -StartAt $WaveName after fixing the issue." -ForegroundColor Yellow
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

Write-Host "=== VISA2014 on-prem sync ($Profile) ===" -ForegroundColor Cyan
Write-Host "INF Legacy source: $LegacySource (SQL 10.100.128.15 / VISA2015)"
Write-Host "INF Target API:    $ApiBaseUrl"
Write-Host "INF Id-map dir:    $mapRoot"
Write-Host "INF Log dir:       $logRoot"
if ($DryRun) { Write-Host "INF Mode: dry-run" -ForegroundColor Yellow }
if ($Mode -eq "Sync") { Write-Host "INF Mode: delta sync (--sync-visa2014)$(if ($SyncFull) { ' + --sync-full' })" -ForegroundColor Yellow }
if ($IncludeFileWaves) { Write-Host "INF File waves: DocumentCopies.ps1 after scalar chain" -ForegroundColor Yellow }

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

Write-Host ""
if ($Mode -ne "Sync") {
    Invoke-PostImportCorrections -Conn $TargetConnection
}

if ($IncludeFileWaves -and -not $DryRun) {
    if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
        throw "TargetConnection required for file waves."
    }
    Write-Host ""
    Write-Host ">>> DocumentCopies (file waves)" -ForegroundColor Green
    $docArgs = @{
        TargetConnection = $TargetConnection
        LegacySource     = $LegacySource
        Configuration    = $Configuration
    }
    if ($ContinueOnError) { $docArgs['ErrorAction'] = 'Continue' }
    & (Join-Path $PSScriptRoot 'DocumentCopies.ps1') @docArgs
    if ($LASTEXITCODE -ne 0 -and -not $ContinueOnError) {
        Write-Host "ERR DocumentCopies failed (exit $LASTEXITCODE)." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Write-Host "=== On-prem $Profile import waves finished ===" -ForegroundColor Cyan
Write-Host "INF Reconcile: scripts/visa2014-migration/Compare-LegacyMigratedCounts.ps1"
Write-Host "INF Officer read-only UAT: $ApiBaseUrl/LoginPage"
