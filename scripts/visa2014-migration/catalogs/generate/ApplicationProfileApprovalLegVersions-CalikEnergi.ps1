#Requires -Version 5.1
<#
.SYNOPSIS
  Generate Application Profile approval-leg version seed JSON + frequency matrix from VISA2015.

.DESCRIPTION
  Phase A: ApplicationType x ApprovalLegProfile frequency for via-ministry Calik profiles.
  Writes tenant application-profile-approval-leg-versions.calik-energi.json and
  docs/VISA2014_MIGRATION/lookup-comparisons/ApplicationProfileApprovalLegVersions.calik-energi.md.

  Calik live legacy is 10.100.128.15 / VISA2015 (not localhost SQLEXPRESS unless you override).

.EXAMPLE
  .\scripts\visa2014-migration\catalogs\generate\ApplicationProfileApprovalLegVersions-CalikEnergi.ps1
#>
param(
    [string]$LegacySource = 'calik-energi',
    [string]$SqlServer = '10.100.128.15',
    [string]$Database = 'VISA2015',
    [int]$MaxRows = 0
)

. (Join-Path $PSScriptRoot '..\..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:VISA2014_SQL_PASSWORD)) {
    throw 'Set VISA2014_SQL_PASSWORD before running this script.'
}

$connection = $env:VISA2014_SQL_CONNECTION
if ([string]::IsNullOrWhiteSpace($connection)) {
    $connection = "Server=$SqlServer;Database=$Database;User Id=ReadOnlyUser;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
}

$argsList = @(
    '--export-visa2014-application-profile-approval-leg-version-matrix',
    '--legacy-source', $LegacySource,
    '--connection', $connection
)
if ($MaxRows -gt 0) {
    $argsList += @('--max-rows', "$MaxRows")
}

Write-Host "INF Legacy SQL: $connection" -ForegroundColor DarkGray

Push-Location $repoRoot
try {
    & dotnet run --project (Join-Path $repoRoot 'Visa2026.DataImporter\Visa2026.DataImporter.csproj') --configuration Debug --no-build -- @argsList
    if ($LASTEXITCODE -ne 0) {
        & dotnet run --project (Join-Path $repoRoot 'Visa2026.DataImporter\Visa2026.DataImporter.csproj') --configuration Debug -- @argsList
    }
    if ($LASTEXITCODE -ne 0) { throw "DataImporter exited with code $LASTEXITCODE" }
}
finally {
    Pop-Location
}