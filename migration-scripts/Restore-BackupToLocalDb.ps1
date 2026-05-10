<#
Restores a .bak into SQL Server LocalDB (Visual Studio default: Server=(localdb)\mssqllocaldb, Database=Visa2026).

Typical usage (from repo root, after copying visa2026-prod.bak here):
  .\migration-scripts\Restore-BackupToLocalDb.ps1 -BackupFile .\visa2026-prod.bak

This is separate from Restore-BackupToLocalSql.ps1, which restores into the Docker dev SQL container.

Requires: LocalDB instance running, sqlcmd on PATH (for smoke checks optional), .NET Framework System.Data.SqlClient (Windows PowerShell).
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,

    [string]$ServerInstance = "(localdb)\mssqllocaldb",

    # Must match Visa2026.Blazor.Server/appsettings.json DefaultConnection database name.
    [string]$TargetDatabase = "Visa2026"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $BackupFile)) {
    Write-Host "ERROR: Backup file not found: $BackupFile" -ForegroundColor Red
    exit 1
}

$backupPath = (Resolve-Path -LiteralPath $BackupFile).Path
# SQL string literal: double single-quotes
$sqlBakPath = $backupPath.Replace("'", "''")

function Get-LocalDbDataFolder {
    param([System.Data.SqlClient.SqlConnection]$Connection)
    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = @"
SELECT TOP (1) LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name)))
FROM sys.master_files
WHERE database_id = DB_ID(N'model') AND type = 0;
"@
    $result = $cmd.ExecuteScalar()
    if ([string]::IsNullOrWhiteSpace([string]$result)) {
        throw "Could not resolve LocalDB data folder from model database files."
    }
    return [string]$result
}

function Get-RestoreFileList {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$DiskPath
    )
    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = "RESTORE FILELISTONLY FROM DISK = @p"
    $p = $cmd.Parameters.Add("@p", [System.Data.SqlDbType]::NVarChar, 4000)
    $p.Value = $DiskPath
    $reader = $cmd.ExecuteReader()
    $rows = @()
    try {
        while ($reader.Read()) {
            $rows += [pscustomobject]@{
                LogicalName = [string]$reader["LogicalName"]
                Type        = [string]$reader["Type"].ToString().Trim()
            }
        }
    }
    finally {
        $reader.Close()
    }
    return $rows
}

function Escape-SqlBracketName([string]$Name) {
    return ("[" + ($Name -replace '\]', ']]') + "]")
}

$connString = "Server=$ServerInstance;Database=master;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
$cn = New-Object System.Data.SqlClient.SqlConnection($connString)
$cn.Open()
try {
    $dataFolder = Get-LocalDbDataFolder -Connection $cn
    Write-Host "LocalDB data folder: $dataFolder" -ForegroundColor Cyan

    $files = Get-RestoreFileList -Connection $cn -DiskPath $backupPath
    if ($files.Count -eq 0) {
        throw "RESTORE FILELISTONLY returned no files. Is the backup valid?"
    }

    $dataFiles = @($files | Where-Object { $_.Type -eq "D" } | Sort-Object LogicalName)
    $logFiles = @($files | Where-Object { $_.Type -eq "L" } | Sort-Object LogicalName)
    if ($dataFiles.Count -eq 0 -or $logFiles.Count -eq 0) {
        throw "Backup must contain at least one data file (Type D) and one log file (Type L). Got: $($files | ConvertTo-Json -Compress)"
    }

    $moveParts = New-Object System.Collections.Generic.List[string]
    $i = 0
    foreach ($df in $dataFiles) {
        if ($i -eq 0) {
            $dest = Join-Path $dataFolder "$TargetDatabase.mdf"
        }
        else {
            $dest = Join-Path $dataFolder "${TargetDatabase}_$i.ndf"
        }
        $logical = $df.LogicalName.Replace("'", "''")
        $destSql = $dest.Replace("'", "''")
        $moveParts.Add("MOVE N'$logical' TO N'$destSql'")
        $i++
    }

    $j = 0
    foreach ($lf in $logFiles) {
        if ($j -eq 0) {
            $dest = Join-Path $dataFolder "${TargetDatabase}_log.ldf"
        }
        else {
            $dest = Join-Path $dataFolder "${TargetDatabase}_log$j.ldf"
        }
        $logical = $lf.LogicalName.Replace("'", "''")
        $destSql = $dest.Replace("'", "''")
        $moveParts.Add("MOVE N'$logical' TO N'$destSql'")
        $j++
    }

    $dbBr = Escape-SqlBracketName $TargetDatabase
    $moveSql = ($moveParts -join ",`n    ")
    $restoreSql = @"
IF DB_ID(N'$($TargetDatabase.Replace("'", "''"))') IS NOT NULL
BEGIN
    ALTER DATABASE $dbBr SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END

RESTORE DATABASE $dbBr
  FROM DISK = N'$sqlBakPath'
  WITH REPLACE,
    $moveSql;

ALTER DATABASE $dbBr SET MULTI_USER;
"@

    Write-Host "Restoring into $TargetDatabase on $ServerInstance (this may take a while)..." -ForegroundColor Yellow
    $restoreCmd = $cn.CreateCommand()
    $restoreCmd.CommandText = $restoreSql
    $restoreCmd.CommandTimeout = 0
    $null = $restoreCmd.ExecuteNonQuery()
}
finally {
    $cn.Close()
}

Write-Host "Restore complete. Run the Blazor app with the default connection string (Database=$TargetDatabase)." -ForegroundColor Green
