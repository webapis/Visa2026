#Requires -Version 5.1
<#
.SYNOPSIS
  Preview duplicate / near-duplicate ProjectContract and Subcontractor lookup rows.

.DESCRIPTION
  Read-only. SQL Server targets use cleanup/DuplicateProjectContractAndSubcontractor.sql.
  Local PostgreSQL (visa2026) uses cleanup/DuplicateProjectContractAndSubcontractor.postgres.sql.
  Does not merge. Review output / local-pg-preview.md before any apply step.

.EXAMPLE
  .\scripts\visa2014-migration\Preview-DuplicateProjectContractSubcontractor.ps1 -Profile Local

.EXAMPLE
  .\scripts\visa2014-migration\Preview-DuplicateProjectContractSubcontractor.ps1 -Profile Demo
#>
[CmdletBinding()]
param(
    [ValidateSet('Local', 'Demo', 'Staging', 'Production', 'Custom')]
    [string]$Profile = 'Local',
    [string]$TargetConnection = '',
    [string]$TargetServer = '10.100.128.25\SQLEXPRESS',
    [string]$TargetDatabase = '',
    [string]$TargetUser = 'sa',
    [string]$TargetPassword = '',
    [string]$PgHost = 'localhost',
    [int]$PgPort = 5432,
    [string]$PgDatabase = 'visa2026',
    [string]$PgUser = 'postgres',
    [string]$PgPassword = 'Visa2026Local',
    [string]$PsqlPath = 'C:\PostgreSQL\16\bin\psql.exe'
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

Write-Host '=== Preview duplicate ProjectContract / Subcontractor (read-only) ===' -ForegroundColor Cyan
Write-Host "Profile: $Profile" -ForegroundColor DarkGray

if ($Profile -eq 'Local') {
    if (-not (Test-Path -LiteralPath $PsqlPath)) {
        throw "psql not found at $PsqlPath. Install PostgreSQL or pass -PsqlPath."
    }
    $sqlPath = Join-Path $PSScriptRoot 'cleanup\DuplicateProjectContractAndSubcontractor.postgres.sql'
    if (-not (Test-Path -LiteralPath $sqlPath)) { throw "SQL not found: $sqlPath" }

    Write-Host "Target: PostgreSQL $PgHost`:$PgPort / $PgDatabase" -ForegroundColor DarkGray
    $env:PGPASSWORD = $PgPassword
    & $PsqlPath -h $PgHost -p $PgPort -U $PgUser -d $PgDatabase -v ON_ERROR_STOP=1 -f $sqlPath
    if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE" }

    $md = Join-Path $PSScriptRoot 'cleanup\DuplicateProjectContractSubcontractor.local-pg-preview.md'
    if (Test-Path -LiteralPath $md) {
        Write-Host ''
        Write-Host "Human summary: $md" -ForegroundColor Green
    }
    return
}

if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
    $envName = switch ($Profile) {
        'Demo' { 'VISA2026_DEMO_SQL_CONNECTION' }
        'Staging' { 'VISA2026_STAGING_SQL_CONNECTION' }
        'Production' { 'VISA2026_PROD_SQL_CONNECTION' }
        default { 'VISA2026_PROD_SQL_CONNECTION' }
    }
    $TargetConnection = [Environment]::GetEnvironmentVariable($envName, 'Process')
    if ([string]::IsNullOrWhiteSpace($TargetConnection)) {
        $TargetConnection = [Environment]::GetEnvironmentVariable($envName, 'User')
    }
}

if (-not [string]::IsNullOrWhiteSpace($TargetConnection)) {
    $parts = Get-SqlConnectionParts $TargetConnection
    $TargetServer = $parts.Server
    $TargetDatabase = $parts.Database
    if ($parts.User) { $TargetUser = $parts.User }
    if ($parts.Password) { $TargetPassword = $parts.Password }
}

if ([string]::IsNullOrWhiteSpace($TargetDatabase)) {
    $TargetDatabase = switch ($Profile) {
        'Demo' { 'Visa2026DbDemo' }
        'Staging' { 'Visa2026DbStaging' }
        'Production' { 'Visa2026DbProd' }
        default { 'Visa2026DbDemo' }
    }
}

if ([string]::IsNullOrWhiteSpace($TargetPassword)) {
    throw "Set connection env or -TargetPassword for $Profile SQL Server."
}

$sqlPath = Join-Path $PSScriptRoot 'cleanup\DuplicateProjectContractAndSubcontractor.sql'
if (-not (Test-Path -LiteralPath $sqlPath)) { throw "SQL not found: $sqlPath" }

Write-Host "Target: $TargetServer / $TargetDatabase" -ForegroundColor DarkGray
$tmp = [System.IO.Path]::GetTempFileName() + '.sql'
Copy-Item -LiteralPath $sqlPath -Destination $tmp -Force
try {
    & sqlcmd -S $TargetServer -U $TargetUser -P $TargetPassword -d $TargetDatabase -C -W -s "`t" -i $tmp
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE" }
}
finally {
    Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host 'Review groups above before any merge. Local PG summary also in cleanup\DuplicateProjectContractSubcontractor.local-pg-preview.md' -ForegroundColor Green