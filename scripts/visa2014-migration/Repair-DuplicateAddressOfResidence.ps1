#Requires -Version 5.1
<#
.SYNOPSIS
  Preview or soft-delete duplicate AddressesOfResidence (same Person + Type + City + FullAddress) on Visa2026 prod.

.DESCRIPTION
  Keeps the oldest row (MIN(ID)) per site key. Repoints ApplicationItems.CurrentAddressOfResidenceID.
  Soft-deletes extras (GCRecord = 1).
  SQL: cleanup/DuplicateAddressOfResidenceByPersonSite.sql

  Default is PREVIEW only. Pass -Apply after reviewing output.

.EXAMPLE
  $env:VISA2026_PROD_SQL_CONNECTION = 'Server=10.100.128.25\SQLEXPRESS;Database=Visa2026DbProd;...'
  .\scripts\visa2014-migration\Repair-DuplicateAddressOfResidence.ps1

.EXAMPLE
  .\scripts\visa2014-migration\Repair-DuplicateAddressOfResidence.ps1 -Apply
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

$sqlPath = Join-Path $PSScriptRoot 'cleanup\DuplicateAddressOfResidenceByPersonSite.sql'
$sql = Get-Content -LiteralPath $sqlPath -Raw
$applyBit = if ($Apply) { 1 } else { 0 }
$sql = $sql -replace 'DECLARE @Apply bit = 0;', "DECLARE @Apply bit = $applyBit;"

$mode = if ($Apply) { 'APPLY (soft-delete extras)' } else { 'PREVIEW' }
Write-Host "=== Duplicate AddressOfResidence by Person+Site ($mode) ===" -ForegroundColor Cyan
Write-Host "Target: $TargetServer / $TargetDatabase" -ForegroundColor DarkGray

if ($Apply) {
    if (-not $PSCmdlet.ShouldProcess($TargetDatabase, 'Soft-delete duplicate AddressesOfResidence')) {
        return
    }
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
    Write-Host 'Review the table above. To apply:' -ForegroundColor Green
    Write-Host '  .\scripts\visa2014-migration\Repair-DuplicateAddressOfResidence.ps1 -Apply' -ForegroundColor Green
}
