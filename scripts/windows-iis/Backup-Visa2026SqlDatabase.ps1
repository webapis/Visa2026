#Requires -Version 5.1
#Requires -RunAsAdministrator
param(
    [string]$EnvFile = "C:\visa2026\.env.prod",
    [string]$BackupDir = "C:\visa2026\backups",
    [string]$InstanceName = "SQLEXPRESS"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

function Read-DotEnvMap([string]$Path) {
    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -match '^\s*#' -or $line -eq "") { return }
        if ($line -match '^\s*([^#=]+)=(.*)$') {
            $k = $matches[1].Trim()
            $v = $matches[2].Trim()
            if ($v.Length -ge 2 -and $v.StartsWith('"') -and $v.EndsWith('"')) { $v = $v.Substring(1, $v.Length - 2) }
            $map[$k] = $v
        }
    }
    $map
}

$envMap = Read-DotEnvMap $EnvFile
$saPassword = $envMap["SA_PASSWORD"]
$dbName = if ($envMap.ContainsKey("DB_NAME")) { $envMap["DB_NAME"] } else { "Visa2026DbProd" }
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmm"
$bakPath = Join-Path $BackupDir "$dbName-predeploy-$stamp.bak"
$server = "localhost\$InstanceName"
$conn = "Server=$server;User Id=sa;Password=$saPassword;TrustServerCertificate=True;Encrypt=False"
$sql = "BACKUP DATABASE [$dbName] TO DISK = N'$($bakPath.Replace("'","''"))' WITH INIT;"
$c = New-Object System.Data.SqlClient.SqlConnection $conn
$c.Open()
$cmd = $c.CreateCommand()
$cmd.CommandTimeout = 0
$cmd.CommandText = $sql
[void]$cmd.ExecuteNonQuery()
$c.Close()
Write-Host "Backup: $bakPath" -ForegroundColor Green
