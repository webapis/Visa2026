#Requires -Version 5.1
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
$sqlScript = Join-Path $PSScriptRoot "..\cleanup\ImportedPersonDomain.sql"
Write-Host "=== Stop running importers ===" -ForegroundColor Cyan
Get-Process -Name "Visa2026.DataImporter" -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Host "=== Delete imported person-domain + application scope ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -i $sqlScript -W -b
if ($LASTEXITCODE -ne 0) { throw "SQL cleanup failed (exit $LASTEXITCODE)" }
Write-Host "=== Clear id-maps ===" -ForegroundColor Cyan
if (Test-Path $mapRoot) {
    Get-ChildItem $mapRoot -Filter "*.json" -ErrorAction SilentlyContinue | Remove-Item -Force
}
if ($DryRun) { Write-Host "DryRun: done after cleanup." -ForegroundColor Yellow; exit 0 }
Write-Host "=== Build DataImporter ($Configuration) ===" -ForegroundColor Cyan
dotnet msbuild (Join-Path $repoRoot "Visa2026.DataImporter/Visa2026.DataImporter.csproj") /t:Build /p:BuildProjectReferences=false /p:Configuration=$Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "=== Headless import chain from Person ===" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "..\import\Run-HeadlessChain.ps1") -StartAt Person -LegacySource $LegacySource -TargetConnection $TargetConnection
if ($LASTEXITCODE -ne 0) { throw "Headless chain failed (exit $LASTEXITCODE)" }
Write-Host "=== Reconcile ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d Visa2026 -E -Q "SET NOCOUNT ON; SELECT COUNT(*) AS total_people FROM People; SELECT COUNT(*) AS employees FROM People WHERE PersonRole=0;" -W -h-1
Write-Host "=== Person domain reimport complete ===" -ForegroundColor Green
