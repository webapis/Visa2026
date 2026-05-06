<#
Restores a .bak file into the local Docker SQL Server container for this repository's dev stack.

Typical usage:
  .\migration-scripts\Restore-BackupToLocalSql.ps1 -BackupFile .\visa2026-prod.bak

Notes:
- Uses LOCAL SA_PASSWORD from .env.dev by default (never uses the droplet password).
- By default restores into DB_NAME from .env.dev (default Visa2026DbDev).
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,

    [string]$Environment = "dev",
    [string]$ComposeProject = "visa2026-dev",
    [string]$ComposeFile = "docker-compose.dev.yml",
    [string]$EnvFile = ".env.dev",

    # Optional: override local container name (otherwise auto-detected).
    [string]$SqlContainerName,

    # Optional: restore into a specific DB name (otherwise read from EnvFile / defaults).
    [string]$DatabaseName
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $BackupFile)) {
    Write-Host "ERROR: Backup file not found: $BackupFile" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $EnvFile)) {
    Write-Host "ERROR: Missing $EnvFile (create it from .env.dev.example and set SA_PASSWORD)." -ForegroundColor Red
    exit 1
}

function Get-EnvValue([string]$Path, [string]$Key) {
    $line = Select-String -LiteralPath $Path -Pattern ("^{0}=" -f [Regex]::Escape($Key)) -SimpleMatch:$false | Select-Object -First 1
    if (-not $line) { return $null }
    return ($line.Line -split "=", 2)[1]
}

$localSaPassword = Get-EnvValue -Path $EnvFile -Key "SA_PASSWORD"
if ([string]::IsNullOrWhiteSpace($localSaPassword)) {
    Write-Host "ERROR: SA_PASSWORD not found in $EnvFile" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($DatabaseName)) {
    $DatabaseName = (Get-EnvValue -Path $EnvFile -Key "DB_NAME")
    if ([string]::IsNullOrWhiteSpace($DatabaseName)) {
        $DatabaseName = "Visa2026DbDev"
    }
}

if ([string]::IsNullOrWhiteSpace($SqlContainerName)) {
    $SqlContainerName = docker ps --filter "name=sqlserver" --format "{{.Names}}" | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($SqlContainerName)) {
        Write-Host "Local SQL container not found. Starting SQL via compose..." -ForegroundColor Yellow
        docker compose -p $ComposeProject --env-file $EnvFile -f $ComposeFile up -d sqlserver | Out-Null
        $SqlContainerName = docker ps --filter "name=sqlserver" --format "{{.Names}}" | Select-Object -First 1
    }
}

if ([string]::IsNullOrWhiteSpace($SqlContainerName)) {
    Write-Host "ERROR: Could not detect local SQL container name." -ForegroundColor Red
    exit 1
}

$backupPath = (Resolve-Path -LiteralPath $BackupFile).Path
$containerBak = "/var/opt/mssql/visa2026-restore.bak"

Write-Host "Local SQL container: $SqlContainerName" -ForegroundColor Cyan
Write-Host "Restoring into database: $DatabaseName" -ForegroundColor Cyan

Write-Host "Copying backup into container..." -ForegroundColor Cyan
docker cp $backupPath "${SqlContainerName}:${containerBak}" | Out-Null

Write-Host "Restoring (this can take a while)..." -ForegroundColor Yellow
# Avoid -t (TTY) so this works in non-interactive terminals/CI.
docker exec -i $SqlContainerName /opt/mssql-tools18/bin/sqlcmd `
    -S localhost -C -U sa -P $localSaPassword `
    -Q "RESTORE DATABASE [$DatabaseName] FROM DISK = N'$containerBak' WITH REPLACE"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Restore failed. If this is a path/logical-name issue, see migration-scripts/docs/troubleshooting.md" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Restore complete." -ForegroundColor Green

