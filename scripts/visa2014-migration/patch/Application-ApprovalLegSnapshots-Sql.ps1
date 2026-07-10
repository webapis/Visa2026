#Requires -Version 5.1
<#
.SYNOPSIS
  SQL backfill of Application.ApprovalLegSnapshots (Ministrlik) when EF DataImporter cannot run
  (e.g. prod schema behind Module — missing BorderZoneLocation).

.EXAMPLE
  .\scripts\visa2014-migration\patch\Application-ApprovalLegSnapshots-Sql.ps1 -DryRun
  .\scripts\visa2014-migration\patch\Application-ApprovalLegSnapshots-Sql.ps1 -Apply
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$TargetServer = '10.100.128.25\SQLEXPRESS',
    [string]$TargetDatabase = 'Visa2026DbProd',
    [string]$TargetUser = 'sa',
    [string]$TargetPassword = '',
    [string]$TargetConnection = $env:VISA2026_PROD_SQL_CONNECTION,
    [switch]$DryRun,
    [switch]$Apply,
    [switch]$RunOnServerViaSsh,
    [string]$SshHost = 'visa2026-onprem'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\_lib\Get-RepoRoot.ps1')
$repoRoot = Get-Visa2026RepoRoot
$sqlPath = Join-Path $repoRoot 'scripts\visa2014-migration\cleanup\BackfillApplicationApprovalLegSnapshots.sql'
if (-not (Test-Path $sqlPath)) { throw "Missing $sqlPath" }

if ($Apply -and $DryRun) { throw 'Use either -DryRun or -Apply, not both.' }
if (-not $Apply -and -not $DryRun) { $DryRun = $true }

if ($RunOnServerViaSsh) {
    $remoteSql = 'C:\visa2026-deploy\iis\BackfillApplicationApprovalLegSnapshots.sql'
    scp -o BatchMode=yes $sqlPath "${SshHost}:C:/visa2026-deploy/iis/BackfillApplicationApprovalLegSnapshots.sql"
    $mode = if ($Apply) { 'Apply' } else { 'Preview' }
    ssh -o BatchMode=yes $SshHost "powershell -NoProfile -File C:\visa2026-deploy\iis\Run-SnapshotBackfill.ps1 -Mode $mode"
    exit $LASTEXITCODE
}

if ([string]::IsNullOrWhiteSpace($TargetPassword) -and $TargetConnection) {
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $TargetConnection
    if ($b.DataSource) { $TargetServer = $b.DataSource }
    if ($b.InitialCatalog) { $TargetDatabase = $b.InitialCatalog }
    if ($b.'User ID') { $TargetUser = $b.'User ID' }
    if ($b.Password) { $TargetPassword = $b.Password }
}
if ([string]::IsNullOrWhiteSpace($TargetPassword)) {
    throw 'Set VISA2026_PROD_SQL_CONNECTION or -TargetPassword (or use -RunOnServerViaSsh).'
}

$sql = Get-Content -LiteralPath $sqlPath -Raw
if ($Apply) {
    $sql = $sql -replace 'DECLARE @Apply bit = 0;', 'DECLARE @Apply bit = 1;'
    if (-not $PSCmdlet.ShouldProcess($TargetDatabase, 'Backfill ApprovalLegSnapshots')) { return }
} else {
    Write-Host 'PREVIEW (no writes)' -ForegroundColor Cyan
}

$tmp = [IO.Path]::GetTempFileName() + '.sql'
Set-Content -LiteralPath $tmp -Value $sql -Encoding UTF8
try {
    & sqlcmd -S $TargetServer -U $TargetUser -P $TargetPassword -d $TargetDatabase -C -i $tmp
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $LASTEXITCODE" }
}
finally {
    Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
}