#Requires -Version 5.1
<#
.SYNOPSIS
  Backfill Lodging.CityID from plurality AddressesOfResidence.CityID on prod.

.DESCRIPTION
  Fixes lodgings left without City after duplicate merge (keeper had more AOR refs but null CityID).
  Optionally soft-deletes zero-AOR duplicate addresses when a sibling has AOR refs.
  SQL: cleanup/BackfillLodgingCityFromAor.sql

.EXAMPLE
  .\scripts\visa2014-migration\Backfill-LodgingCityFromAor.ps1
.EXAMPLE
  .\scripts\visa2014-migration\Backfill-LodgingCityFromAor.ps1 -Apply
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$TargetConnection = $env:VISA2026_PROD_SQL_CONNECTION,
    [string]$TargetServer = '10.100.128.25\SQLEXPRESS',
    [string]$TargetDatabase = 'Visa2026DbProd',
    [string]$TargetUser = 'sa',
    [string]$TargetPassword = '',
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_lib\Get-RepoRoot.ps1')

function Get-SqlConnectionParts {
    param([string]$ConnectionString)
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString
    return [ordered]@{
        Server   = if ($builder.ContainsKey('Data Source') -and $builder.'Data Source') { $builder.'Data Source' } else { $builder.Server }
        Database = if ($builder.ContainsKey('Initial Catalog') -and $builder.'Initial Catalog') { $builder.'Initial Catalog' } else { $builder.Database }
        User     = $builder.'User ID'
        Password = $builder.Password
    }
}

if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
    $TargetConnection = [Environment]::GetEnvironmentVariable('VISA2026_PROD_SQL_CONNECTION', 'User')
}
if (-not [string]::IsNullOrWhiteSpace($TargetConnection)) {
    $parts = Get-SqlConnectionParts $TargetConnection
    $TargetServer = $parts.Server
    $TargetDatabase = $parts.Database
    if ($parts.User) { $TargetUser = $parts.User }
    if ($parts.Password) { $TargetPassword = $parts.Password }
}
if ([string]::IsNullOrWhiteSpace($TargetPassword)) {
    throw 'Set VISA2026_PROD_SQL_CONNECTION or -TargetPassword for prod SQL.'
}

$sqlPath = Join-Path $PSScriptRoot 'cleanup\BackfillLodgingCityFromAor.sql'
$sql = Get-Content -LiteralPath $sqlPath -Raw
$applyBit = if ($Apply) { 1 } else { 0 }
$sql = $sql -replace 'DECLARE @Apply bit = 0;', "DECLARE @Apply bit = $applyBit;"

$mode = if ($Apply) { 'APPLY' } else { 'PREVIEW' }
Write-Host "=== Backfill Lodging City from AOR ($mode) ===" -ForegroundColor Cyan
Write-Host "Target: $TargetServer / $TargetDatabase" -ForegroundColor DarkGray

if ($Apply) {
    if (-not $PSCmdlet.ShouldProcess($TargetDatabase, 'Backfill Lodging.CityID from AOR')) { return }
    Write-Host 'Applying in 5 seconds... Ctrl+C to abort.' -ForegroundColor Yellow
    Start-Sleep -Seconds 5
}

$tmp = [System.IO.Path]::GetTempFileName() + '.sql'
Set-Content -LiteralPath $tmp -Value $sql -Encoding UTF8
try {
    & sqlcmd -S $TargetServer -U $TargetUser -P $TargetPassword -d $TargetDatabase -C -i $tmp
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE" }
}
finally {
    Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
}

if (-not $Apply) {
    Write-Host ''
    Write-Host 'To apply:' -ForegroundColor Green
    Write-Host '  .\scripts\visa2014-migration\Backfill-LodgingCityFromAor.ps1 -Apply' -ForegroundColor Green
}