#Requires -Version 5.1
<#
.SYNOPSIS
  Wipe imported file-wave rows on target Postgres, clear document id-maps, re-run file import.

.DESCRIPTION
  Production/Demo/Staging on .25 sync host:
    1) Wipe-PostgresImportedFileWaves.sql (photos + *Document rows + orphan FileData)
    2) Reset document id-maps to {} (keeps Person/Passport/Visa/… scalar maps)
    3) MedicalRecord file wave
    4) DocumentCopies.ps1 (Photo + Passport/Visa/Education/WorkPermit/Invitation/FamilyProof)

  Run under Win32_Process.Create / schtasks so OpenSSH job-object does not kill the job.

.EXAMPLE
  .\Reimport-OnPremFileWaves.ps1 -Profile Production -SyncHostRoot C:\visa2026-sync
#>
[CmdletBinding()]
param(
    [ValidateSet('Production', 'Staging', 'Demo')]
    [string]$Profile = 'Production',
    [string]$SyncHostRoot = '',
    [string]$PgDatabase = '',
    [string]$LegacySource = '',
    [switch]$SkipWipe,
    [switch]$SkipMedicalRecord,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

function Resolve-OnPremMigrationLibPath {
    param([Parameter(Mandatory)][string]$FileName)
    foreach ($candidate in @(
            (Join-Path $PSScriptRoot "_lib\$FileName"),
            (Join-Path $PSScriptRoot "..\_lib\$FileName")
        )) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
    throw "Lib not found: $FileName"
}

. (Resolve-OnPremMigrationLibPath 'Get-OnPremSyncHostRoot.ps1')

if ([string]::IsNullOrWhiteSpace($SyncHostRoot)) {
    $SyncHostRoot = Get-DefaultOnPremSyncHostRoot -Profile $Profile
}
$SyncHostRoot = (Resolve-Path -LiteralPath $SyncHostRoot).Path

$defaultDb = @{ Production = 'visa2026_prod'; Staging = 'visa2026_staging'; Demo = 'visa2026_demo' }
$defaultSource = @{
    Production = 'calik-energi-onprem-prod'
    Staging    = 'calik-energi-onprem-staging'
    Demo       = 'calik-energi-onprem-demo'
}
if ([string]::IsNullOrWhiteSpace($PgDatabase)) { $PgDatabase = $defaultDb[$Profile] }
if ([string]::IsNullOrWhiteSpace($LegacySource)) { $LegacySource = $defaultSource[$Profile] }

$envFile = Join-Path $SyncHostRoot 'config\sync.env'
$prodEnv = 'C:\visa2026\env\prod.env'
if ($Profile -eq 'Staging') { $prodEnv = 'C:\visa2026\env\staging.env' }
if ($Profile -eq 'Demo') { $prodEnv = 'C:\visa2026\env\demo.env' }

function Get-EnvValue([string]$Path, [string]$Key) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $line = Get-Content -LiteralPath $Path | Where-Object { $_.StartsWith("$Key=") } | Select-Object -First 1
    if (-not $line) { return $null }
    return $line.Substring("$Key=".Length)
}

$pgPassword = Get-EnvValue $prodEnv 'PG_PASSWORD'
if (-not $pgPassword) { $pgPassword = Get-EnvValue $envFile 'PG_PASSWORD' }
if (-not $pgPassword) { throw "PG_PASSWORD not found in $prodEnv or $envFile" }

$pgUser = Get-EnvValue $prodEnv 'PG_USER'
if (-not $pgUser) { $pgUser = 'postgres' }
$pgHost = Get-EnvValue $prodEnv 'PG_HOST'
if (-not $pgHost) { $pgHost = 'localhost' }
$psql = 'C:\PostgreSQL\16\bin\psql.exe'
if (-not (Test-Path -LiteralPath $psql)) { throw "psql not found: $psql" }

$wipeSqlCandidates = @(
    (Join-Path $PSScriptRoot '..\cleanup\Wipe-PostgresImportedFileWaves.sql'),
    (Join-Path $SyncHostRoot 'tools\scripts\cleanup\Wipe-PostgresImportedFileWaves.sql'),
    (Join-Path $SyncHostRoot 'tools\scripts\..\cleanup\Wipe-PostgresImportedFileWaves.sql')
)
$wipeSql = $wipeSqlCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $wipeSql -and -not $SkipWipe) {
    throw "Wipe SQL not found. Copy Wipe-PostgresImportedFileWaves.sql to sync-host tools\scripts\cleanup\"
}

$docMapNames = @(
    'PassportCopy', 'PassportDocument', 'VisaDocument', 'EducationDocument',
    'WorkPermitDocument', 'InvitationDocument', 'FamilyProofDocument',
    'MedicalRecord', 'MedicalRecordDocument'
)
$mapDirs = @(
    (Join-Path $SyncHostRoot "data\id-maps\$LegacySource"),
    (Join-Path $SyncHostRoot "id-maps\$LegacySource"),
    (Join-Path $SyncHostRoot "tools\DataImporter\legacy\visa2014\id-maps\$LegacySource")
) | Where-Object { Test-Path -LiteralPath $_ }

$targetEnvKey = Get-OnPremSyncHostTargetConnectionEnv -Profile $Profile
$targetCs = Get-EnvValue $envFile $targetEnvKey
if ([string]::IsNullOrWhiteSpace($targetCs)) {
    $targetCs = Get-EnvValue $envFile 'VISA2026_SQL_CONNECTION'
}
if ([string]::IsNullOrWhiteSpace($targetCs)) {
    $targetCs = Get-EnvValue $envFile 'ConnectionStrings__DefaultConnection'
}
if ([string]::IsNullOrWhiteSpace($targetCs)) {
    throw "Target connection missing in $envFile (tried $targetEnvKey / VISA2026_SQL_CONNECTION)"
}

$logDir = Join-Path $SyncHostRoot 'data\import-logs\file-reimport'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$masterLog = Join-Path $logDir "reimport-$stamp.log"
function Write-ReimportLog([string]$Message) {
    $line = "$(Get-Date -Format o) $Message"
    Add-Content -LiteralPath $masterLog -Value $line -Encoding utf8
    Write-Host $Message
}

Write-ReimportLog "=== Reimport file waves ($Profile) ==="
Write-ReimportLog "INF SyncHostRoot=$SyncHostRoot DB=$PgDatabase Source=$LegacySource TargetEnv=$targetEnvKey"

if (-not $SkipWipe) {
    if ($DryRun) {
        Write-ReimportLog "DRY RUN: would run $wipeSql"
    }
    else {
        Write-ReimportLog ">>> Wipe imported file waves ($wipeSql)"
        $env:PGPASSWORD = $pgPassword
        & $psql -h $pgHost -U $pgUser -d $PgDatabase -v ON_ERROR_STOP=1 -f $wipeSql *>&1 |
            Tee-Object -FilePath $masterLog -Append
        if ($LASTEXITCODE -ne 0) { throw "Wipe SQL failed exit=$LASTEXITCODE" }
    }
}

Write-ReimportLog ">>> Reset document id-maps to {}"
foreach ($dir in $mapDirs) {
    foreach ($name in $docMapNames) {
        $path = Join-Path $dir "$name.json"
        if (-not (Test-Path -LiteralPath $path)) { continue }
        if ($DryRun) {
            Write-ReimportLog "DRY RUN: reset $path"
        }
        else {
            [System.IO.File]::WriteAllText($path, '{}', $utf8NoBom)
            Write-ReimportLog "INF Reset $path"
        }
    }
}

if ($DryRun) {
    Write-ReimportLog 'DRY RUN: skip MedicalRecord + DocumentCopies'
    exit 0
}

$dataImporterExe = Join-Path $SyncHostRoot 'tools\DataImporter\Visa2026.DataImporter.exe'
if (-not (Test-Path -LiteralPath $dataImporterExe)) { throw "DataImporter missing: $dataImporterExe" }

$env:ConnectionStrings__DefaultConnection = $targetCs
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:VISA2026_MIGRATION_IMPORT_URLS = 'http://127.0.0.1:5012'
$env:EFCORE_PROVIDER = 'Postgres'

$mapRoot = Join-Path $SyncHostRoot "data\id-maps\$LegacySource"

function Invoke-Logged {
    param([string]$Title, [scriptblock]$Action)
    Write-ReimportLog ">>> $Title"
    Add-Content -LiteralPath $masterLog -Value "=== $Title $(Get-Date -Format o) ==="
    & $Action *>&1 | Tee-Object -FilePath $masterLog -Append
    if ($LASTEXITCODE -ne 0) { throw "$Title failed exit=$LASTEXITCODE" }
}

if (-not $SkipMedicalRecord) {
    $personMap = Join-Path $mapRoot 'Person.json'
    $mrMap = Join-Path $mapRoot 'MedicalRecord.json'
    $mrDocMap = Join-Path $mapRoot 'MedicalRecordDocument.json'
    Invoke-Logged 'MedicalRecordDocument' {
        & $dataImporterExe @(
            '--import-visa2014-files', '--entity', 'MedicalRecord', '--property', 'MedicalRecordDocument',
            '--legacy-source', $LegacySource, '--inprocess', '--no-wait',
            '--target-connection', $targetCs,
            '--person-id-map', $personMap,
            '--medical-record-id-map-output', $mrMap,
            '--document-id-map-output', $mrDocMap
        )
    }
}

$docCopies = Join-Path $PSScriptRoot 'DocumentCopies.ps1'
if (-not (Test-Path -LiteralPath $docCopies)) {
    $docCopies = Join-Path $SyncHostRoot 'tools\scripts\DocumentCopies.ps1'
}
Invoke-Logged 'DocumentCopies' {
    & $docCopies `
        -TargetConnection $targetCs `
        -LegacySource $LegacySource `
        -SyncHostRoot $SyncHostRoot `
        -StartAt 'Person-Photo'
}

Write-Host "FILE_REIMPORT_COMPLETE log=$masterLog" -ForegroundColor Green
