#Requires -Version 5.1
<#
.SYNOPSIS
  Partial reimport: person-domain children after Person was reimported (keeps People + Person.json).
#>
[CmdletBinding()]
param(
    [string]$TargetConnection = "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [string]$LegacySource = "calik-energi",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-Location $repoRoot

$dataImporterRoot = Join-Path $repoRoot "Visa2026.DataImporter"
$mapRoot = Join-Path $dataImporterRoot "legacy/visa2014/id-maps/$LegacySource"
$sqlScript = Join-Path $PSScriptRoot "..\cleanup\ImportedPersonDomainChildren.sql"
$logDir = Join-Path $repoRoot "artifacts/headless-import"
New-Item -ItemType Directory -Force -Path $logDir, $mapRoot | Out-Null

$idMapFiles = @(
    "Passport.json",
    "PassportCopy.json",
    "Visa.json",
    "VisaDocument.json",
    "Education.json",
    "EducationDocument.json",
    "EmployeePositionHistory.json",
    "EmployeeSalary.json",
    "AddressOfResidence.json"
)

$entities = @(
    "Passport",
    "Visa",
    "Education",
    "EmployeePositionHistory",
    "EmployeeSalary",
    "AddressOfResidence"
)

Write-Host "=== Stop running importers ===" -ForegroundColor Cyan
Get-Process -Name "Visa2026.DataImporter" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "=== Delete person-domain children (keep People) ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -i $sqlScript -W -b
if ($LASTEXITCODE -ne 0) { throw "SQL cleanup failed (exit $LASTEXITCODE)" }

Write-Host "=== Clear downstream id-maps (keep Person.json) ===" -ForegroundColor Cyan
foreach ($file in $idMapFiles) {
    $path = Join-Path $mapRoot $file
    if (Test-Path $path) {
        Remove-Item $path -Force
        Write-Host "  removed $file"
    }
}

if ($DryRun) {
    Write-Host "DryRun: cleanup + id-map clear done; no import." -ForegroundColor Yellow
    exit 0
}

Write-Host "=== Build DataImporter ($Configuration) ===" -ForegroundColor Cyan
dotnet msbuild (Join-Path $repoRoot "Visa2026.DataImporter/Visa2026.DataImporter.csproj") /t:Build /p:BuildProjectReferences=false /p:Configuration=$Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$commonArgs = @(
    "run", "--project", "Visa2026.DataImporter", "-c", $Configuration,
    "--no-launch-profile", "--no-build", "--",
    "--import-visa2014",
    "--legacy-source", $LegacySource,
    "--inprocess",
    "--target-connection", $TargetConnection,
    "--no-wait"
)

$summary = @()
foreach ($entity in $entities) {
    $log = Join-Path $logDir "reimport-$entity.log"
    Write-Host "==================== $entity ====================" -ForegroundColor Cyan
    $importArgs = $commonArgs + @("--entity", $entity)
    & dotnet @importArgs *>&1 | Tee-Object -FilePath $log
    $code = $LASTEXITCODE
    if ($code -ne 0) { throw "$entity import failed (exit $code)" }

    $postedLine = ""
    $preparedLine = ""
    $matchPosted = Select-String -Path $log -Pattern "Posted:" | Select-Object -Last 1
    if ($matchPosted) { $postedLine = $matchPosted.Line }
    $matchPrepared = Select-String -Path $log -Pattern "Prepared:" | Select-Object -Last 1
    if ($matchPrepared) { $preparedLine = $matchPrepared.Line }
    $summary += "  $entity -> $($preparedLine.Trim()) | $($postedLine.Trim())"
}

Write-Host "=== Reconcile ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -E -C -Q "SET NOCOUNT ON; SELECT COUNT(*) AS people FROM People; SELECT COUNT(*) AS passports FROM Passports; SELECT COUNT(*) AS visas FROM Visas; SELECT COUNT(*) AS educations FROM Educations; SELECT COUNT(*) AS employee_position_histories FROM EmployeePositionHistories; SELECT COUNT(*) AS employee_salaries FROM EmployeeSalaries; SELECT COUNT(*) AS addresses_of_residence FROM AddressesOfResidence;" -W

Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
$summary | ForEach-Object { Write-Host $_ }
Write-Host "=== Person-domain downstream reimport complete ===" -ForegroundColor Green
