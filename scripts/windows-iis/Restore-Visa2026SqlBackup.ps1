#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Restore a .bak file into Visa2026DbProd on SQL Server Express (IIS on-prem).

.PARAMETER BackupPath
  Full path to the .bak on the server.

.PARAMETER EnvFile
  Reads SA_PASSWORD and DB_NAME (default C:\visa2026\.env.prod).

.PARAMETER InstanceName
  Default SQLEXPRESS.

.PARAMETER StopAppPool
  Stop IIS app pool Visa2026 during restore (default: true).

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,
    [string]$EnvFile = "C:\visa2026\.env.prod",
    [string]$InstanceName = "SQLEXPRESS",
    [string]$AppPoolName = "Visa2026",
    [switch]$StopAppPool = $true
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

function Read-DotEnvMap([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { throw "Env file not found: $Path" }
    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -match '^\s*#' -or $line -eq "") { return }
        if ($line -match '^\s*([^#=]+)=(.*)$') {
            $k = $matches[1].Trim()
            $v = $matches[2].Trim()
            if ($v.Length -ge 2 -and $v.StartsWith('"') -and $v.EndsWith('"')) {
                $v = $v.Substring(1, $v.Length - 2)
            }
            $map[$k] = $v
        }
    }
    $map
}

function Escape-SqlString([string]$Value) {
    $Value.Replace("'", "''")
}

function Invoke-SqlNonQuery([string]$ConnectionString, [string]$Query) {
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 0
        $command.CommandText = $Query
        [void]$command.ExecuteNonQuery()
    }
    finally {
        if ($connection.State -eq [System.Data.ConnectionState]::Open) { $connection.Close() }
        $connection.Dispose()
    }
}

function Get-RestoreFileList([string]$ConnectionString, [string]$BackupPathEscaped) {
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 0
        $command.CommandText = "RESTORE FILELISTONLY FROM DISK = N'$BackupPathEscaped';"
        $reader = $command.ExecuteReader()
        $files = @()
        while ($reader.Read()) {
            $files += [pscustomobject]@{
                LogicalName   = [string]$reader["LogicalName"]
                PhysicalName  = [string]$reader["PhysicalName"]
                Type          = [string]$reader["Type"]
            }
        }
        $reader.Close()
        return $files
    }
    finally {
        if ($connection.State -eq [System.Data.ConnectionState]::Open) { $connection.Close() }
        $connection.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $BackupPath)) {
    throw "Backup not found: $BackupPath"
}

$envMap = Read-DotEnvMap $EnvFile
$saPassword = $envMap["SA_PASSWORD"]
$dbName = if ($envMap.ContainsKey("DB_NAME")) { $envMap["DB_NAME"] } else { "Visa2026DbProd" }
if ([string]::IsNullOrWhiteSpace($saPassword)) { throw "SA_PASSWORD missing in $EnvFile" }

$server = "localhost\$InstanceName"
$saConn = "Server=$server;User Id=sa;Password=$saPassword;TrustServerCertificate=True;Encrypt=False"
$backupEscaped = Escape-SqlString $BackupPath

Write-Host "==> Backup: $BackupPath" -ForegroundColor Cyan
Write-Host "==> Target: $dbName on $server" -ForegroundColor Cyan

if ($StopAppPool) {
    Write-Host "==> Stop IIS app pool $AppPoolName" -ForegroundColor Cyan
    & "$env:windir\System32\inetsrv\appcmd" stop apppool $AppPoolName | Out-Null
    Start-Sleep -Seconds 3
}

$conn = New-Object System.Data.SqlClient.SqlConnection $saConn
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT
  CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(260)),
  CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(260));
"@
$reader = $cmd.ExecuteReader()
$reader.Read() | Out-Null
$dataPath = [string]$reader.GetValue(0)
$logPath = [string]$reader.GetValue(1)
$reader.Close()
$conn.Close()

if ([string]::IsNullOrWhiteSpace($dataPath)) { $dataPath = "C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\" }
if ([string]::IsNullOrWhiteSpace($logPath)) { $logPath = $dataPath }
if (-not $dataPath.EndsWith('\')) { $dataPath += '\' }
if (-not $logPath.EndsWith('\')) { $logPath += '\' }

Write-Host "==> Read backup file list" -ForegroundColor Cyan
$fileList = Get-RestoreFileList $saConn $backupEscaped
if ($fileList.Count -lt 1) { throw "RESTORE FILELISTONLY returned no files." }

$moveClauses = @()
$dataFile = $fileList | Where-Object { $_.Type -eq 'D' } | Select-Object -First 1
$logFile = $fileList | Where-Object { $_.Type -eq 'L' } | Select-Object -First 1
if (-not $dataFile) { throw "No data file entry in backup." }

$dataTarget = "${dataPath}${dbName}.mdf"
$moveClauses += "MOVE N'$($dataFile.LogicalName.Replace("'", "''"))' TO N'$(Escape-SqlString $dataTarget)'"
if ($logFile) {
    $logTarget = "${logPath}${dbName}_log.ldf"
    $moveClauses += "MOVE N'$($logFile.LogicalName.Replace("'", "''"))' TO N'$(Escape-SqlString $logTarget)'"
}

$escapedDb = Escape-SqlString $dbName
$restoreSql = @"
RESTORE DATABASE [$dbName]
FROM DISK = N'$backupEscaped'
WITH REPLACE, RECOVERY, STATS = 10,
$($moveClauses -join ",`n");
"@

Write-Host "==> Restore (may take several minutes)..." -ForegroundColor Cyan
Invoke-SqlNonQuery $saConn $restoreSql
Write-Host "Restore completed: $dbName" -ForegroundColor Green

if ($StopAppPool) {
    Write-Host "==> Start IIS app pool $AppPoolName" -ForegroundColor Cyan
    & "$env:windir\System32\inetsrv\appcmd" start apppool $AppPoolName | Out-Null
}

Write-Host "Run Update-Visa2026Database.ps1 if the app build is newer than the backup schema." -ForegroundColor DarkGray
