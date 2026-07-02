#Requires -Version 5.1
<#
.SYNOPSIS
  VISA2015 (10.100.128.15) → Visa2026 on-prem staging (10.100.128.25:8080) — ordered import waves.

.DESCRIPTION
  Runs Visa2026.DataImporter --import-visa2014 entity-by-entity per order.yaml.
  Uses OData for most entities; --inprocess (headless XAF) for Application and ApplicationItem.

  Prerequisites:
    - Repo checkout on a machine that can reach 10.100.128.15:1433 and 10.100.128.25:8080
    - Visa2026DbStaging empty or pre-migration backup; Module updaters ran once on staging
    - User env: VISA2014_SQL_PASSWORD, VISA2026_STAGING_IMPORT_PASSWORD
    - Param or env: Target SQL connection string for Visa2026DbStaging

  Id-maps: Visa2026.DataImporter/legacy/visa2014/id-maps/calik-energi-onprem-staging/
  Logs:    Visa2026.DataImporter/legacy/visa2014/import-logs/ (gitignored)

  Runbook: docs/VISA2014_MIGRATION/ON_PREM_IIS_MIGRATION_RUNBOOK.md
  After entity waves: order.yaml postImportCorrections (subcontractor, relationship, address PIA) unless -SkipPostImportCorrections.

.PARAMETER TargetConnection
  SQL connection to Visa2026DbStaging. Default: env VISA2026_STAGING_SQL_CONNECTION.

.PARAMETER Entity
  Run only these entities (must still respect order if multiple). Omit for full wave.

.PARAMETER DryRun
  Pass --dry-run to each importer invocation (transform/count only).

.PARAMETER SkipPostImportCorrections
  Skip order.yaml postImportCorrections (subcontractor, relationship, address PIA) after entity waves.

.PARAMETER StartAt
  Skip entities before this name in the built-in order (resume after failure).

.EXAMPLE
  $env:VISA2014_SQL_PASSWORD = '...'
  $env:VISA2026_STAGING_IMPORT_PASSWORD = '...'
  .\scripts\visa2014-migration\import/OnPrem-Staging.ps1 `
    -TargetConnection "Server=10.100.128.25;Database=Visa2026DbStaging;User Id=visa_import;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=true"

.EXAMPLE
  .\scripts\visa2014-migration\import/OnPrem-Staging.ps1 -StartAt Application -DryRun
#>
[CmdletBinding()]
param(
    [string]$LegacySource = "calik-energi-onprem-staging",
    [string]$TargetConnection = $(if ($env:VISA2026_STAGING_SQL_CONNECTION) { $env:VISA2026_STAGING_SQL_CONNECTION } else { "" }),
    [string]$ApiBaseUrl = "http://10.100.128.25:8080",
    [string]$ImportUser = "Admin",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [int]$BatchSize = 50,
    [string[]]$Entity = @(),
    [string]$StartAt = "",
    [switch]$DryRun,
    [switch]$ContinueOnError,
    [switch]$SkipTenantCatalogGeneration,
    [switch]$SkipPostImportCorrections
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/calik-energi-onprem-staging"
$logRoot = Join-Path $dataImporterRoot "legacy/visa2014/import-logs"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"

if (-not $env:VISA2014_SQL_PASSWORD) {
    $env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable("VISA2014_SQL_PASSWORD", "User")
}
if (-not $env:VISA2026_STAGING_IMPORT_PASSWORD) {
    $env:VISA2026_STAGING_IMPORT_PASSWORD = [Environment]::GetEnvironmentVariable("VISA2026_STAGING_IMPORT_PASSWORD", "User")
}

$importPassword = $env:VISA2026_STAGING_IMPORT_PASSWORD
if ([string]::IsNullOrWhiteSpace($importPassword) -and -not $DryRun) {
    throw "Set VISA2026_STAGING_IMPORT_PASSWORD (user env) for OData import user '$ImportUser'."
}
if ([string]::IsNullOrWhiteSpace($env:VISA2014_SQL_PASSWORD) -and -not $DryRun) {
    throw "Set VISA2014_SQL_PASSWORD (user env) for legacy ReadOnlyUser on 10.100.128.15."
}

New-Item -ItemType Directory -Force -Path $mapRoot, $logRoot | Out-Null

function Get-MapPath([string]$name) {
    Join-Path $mapRoot "$name.json"
}

# In-process entities need target SQL; OData entities need API URL.
$inProcessEntities = @{ Application = $true; ApplicationItem = $true }

# order.yaml tenantCatalogGeneration.runBeforeImportPhase: application-domain
$applicationDomainEntities = @{ Application = $true; ApplicationItem = $true; ApplicationProgress = $true }
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
        $logFile = Join-Path $logRoot "staging-post-$($corr.Name)-$stamp.log"
        Write-Host ">>> $($corr.Name)  ->  $logFile" -ForegroundColor Green
        $args = @(
            "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
            $corr.Flag,
            "--legacy-source", $LegacySource,
            "--target-connection", $Conn,
            "--verbose"
        )
        & dotnet @args 2>&1 | Tee-Object -FilePath $logFile
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

$waves = @(
    @{ Name = "Person"; Args = @("--entity", "Person", "--id-map-output", (Get-MapPath "Person")) }
    @{
        Name = "Passport"
        Args = @(
            "--entity", "Passport",
            "--id-map-output", (Get-MapPath "Passport"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "Visa"
        Args = @(
            "--entity", "Visa",
            "--id-map-output", (Get-MapPath "Visa"),
            "--passport-id-map", (Get-MapPath "Passport")
        )
    }
    @{
        Name = "Education"
        Args = @(
            "--entity", "Education",
            "--id-map-output", (Get-MapPath "Education"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "EmployeePositionHistory"
        Args = @(
            "--entity", "EmployeePositionHistory",
            "--id-map-output", (Get-MapPath "EmployeePositionHistory"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "AddressOfResidence"
        Args = @(
            "--entity", "AddressOfResidence",
            "--id-map-output", (Get-MapPath "AddressOfResidence"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "EmployeeSalary"
        Args = @(
            "--entity", "EmployeeSalary",
            "--id-map-output", (Get-MapPath "EmployeeSalary"),
            "--person-id-map", (Get-MapPath "Person"),
            "--position-history-id-map", (Get-MapPath "EmployeePositionHistory")
        )
    }
    @{
        Name = "Application"
        Args = @(
            "--entity", "Application",
            "--id-map-output", (Get-MapPath "Application"),
            "--person-id-map", (Get-MapPath "Person")
        )
    }
    @{
        Name = "ApplicationItem"
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
            "--employee-salary-id-map", (Get-MapPath "EmployeeSalary")
        )
    }
    @{
        Name = "ApplicationProgress"
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

Write-Host "=== VISA2014 on-prem staging import ===" -ForegroundColor Cyan
Write-Host "INF Legacy source: $LegacySource (SQL 10.100.128.15 / VISA2015)"
Write-Host "INF Staging API:   $ApiBaseUrl"
Write-Host "INF Id-map dir:    $mapRoot"
Write-Host "INF Log dir:       $logRoot"
if ($DryRun) { Write-Host "INF Mode: dry-run" -ForegroundColor Yellow }

foreach ($wave in $waves) {
    if (-not $started) {
        if ($wave.Name -eq $StartAt) { $started = $true }
        else {
            Write-Host "INF Skip (resume): $($wave.Name)" -ForegroundColor DarkGray
            continue
        }
    }

    Invoke-TenantCatalogGenerationIfNeeded -WaveName $wave.Name

    $logFile = Join-Path $logRoot "staging-$($wave.Name)-$stamp.log"
    Write-Host ""
    Write-Host ">>> $($wave.Name)  ->  $logFile" -ForegroundColor Green

    $args = @(
        "run", "--project", "Visa2026.DataImporter", "-c", $Configuration, "--",
        "--import-visa2014",
        "--legacy-source", $LegacySource,
        "--no-wait"
    ) + $wave.Args

    if ($inProcessEntities.ContainsKey($wave.Name)) {
        if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
            throw "TargetConnection (or VISA2026_STAGING_SQL_CONNECTION) required for in-process entity $($wave.Name)."
        }
        $args += @("--inprocess", "--target-connection", $TargetConnection, "--batch-size", $BatchSize)
    }
    else {
        $args += @(
            "--api-base-url", $ApiBaseUrl,
            "--user", $ImportUser,
            "--password", $importPassword
        )
    }

    if ($DryRun) { $args += "--dry-run" }

    $exit = 0
    try {
        & dotnet @args 2>&1 | Tee-Object -FilePath $logFile
        $exit = $LASTEXITCODE
    }
    catch {
        Write-Host "ERR $($wave.Name): $_" -ForegroundColor Red
        $exit = 1
    }

    if ($exit -ne 0) {
        Write-Host "ERR Wave $($wave.Name) failed (exit $exit). Log: $logFile" -ForegroundColor Red
        if (-not $ContinueOnError) {
            Write-Host "INF Re-run with -StartAt $($wave.Name) after fixing the issue." -ForegroundColor Yellow
            exit $exit
        }
    }
}

Write-Host ""
Invoke-PostImportCorrections -Conn $TargetConnection

Write-Host "=== Staging import waves finished ===" -ForegroundColor Cyan
Write-Host "INF Reconcile counts in SSMS / OData, then officer read-only UAT on $ApiBaseUrl"
