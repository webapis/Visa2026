#Requires -Version 5.1
<#
.SYNOPSIS
  [LOCAL / OPS] Query XAF ModuleInfo rows from SQL Server (compare DB version to app AssemblyVersion).

.DESCRIPTION
  Reads the DevExpress EF table ModuleInfo (Version, Name, AssemblyFileName, etc.).
  Use this to see why UpdateOldDatabase may skip ReportsUpdater: DB version should be
  lower than Visa2026.Module AssemblyVersion to trigger module updaters.

  SQL Server must be reachable from this machine (e.g. publish port 1433, VPN, or run on the droplet).

.PARAMETER ConnectionString
  Full ADO.NET connection string (overrides Server/Database/User/Password).

.PARAMETER EnvFile
  Path to .env.local / .env.prod relative to repo root, or absolute. Reads SA_PASSWORD and DB_NAME.

.PARAMETER Server
  SQL host (default: localhost).

.PARAMETER Port
  SQL port (default: 1433).

.PARAMETER Database
  Database name. If omitted and -EnvFile is set, uses DB_NAME from the file; otherwise required.

.PARAMETER User
  SQL login (default: sa).

.PARAMETER Password
  SQL password. If omitted with -EnvFile, uses SA_PASSWORD from the file.

.EXAMPLE
  .\scripts\local\Get-ModuleInfoFromSql.ps1 -EnvFile .env.local

.EXAMPLE
  .\scripts\local\Get-ModuleInfoFromSql.ps1 -Server db.example.com -Database Visa2026DbProd -User sa -Password '***'
#>
param(
    [string]$ConnectionString = "",
    [string]$EnvFile = "",
    [string]$Server = "localhost",
    [int]$Port = 1433,
    [string]$Database = "",
    [string]$User = "sa",
    [string]$Password = ""
)

$ErrorActionPreference = "Stop"

function Read-DotEnvMap {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }
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

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

if ($EnvFile -ne "") {
    $envPath = if ([System.IO.Path]::IsPathRooted($EnvFile)) { $EnvFile } else { Join-Path $RepoRoot $EnvFile }
    $m = Read-DotEnvMap $envPath
    if ($Database -eq "" -and $m.ContainsKey("DB_NAME")) { $Database = $m["DB_NAME"] }
    if ($Password -eq "" -and $m.ContainsKey("SA_PASSWORD")) { $Password = $m["SA_PASSWORD"] }
}

if ($ConnectionString -eq "") {
    if ($Database -eq "") {
        throw "Specify -Database or -EnvFile with DB_NAME."
    }
    if ($Password -eq "") {
        throw "Specify -Password or -EnvFile with SA_PASSWORD."
    }
    $ConnectionString = "Server=tcp:$Server,$Port;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;Encrypt=False"
}

$query = @"
SELECT *
FROM [ModuleInfo]
ORDER BY [Name];
"@

Write-Host "Querying ModuleInfo (table must exist after XAF has created the database)..." -ForegroundColor Cyan

try {
    Add-Type -AssemblyName "System.Data" -ErrorAction Stop
}
catch {
    throw "System.Data.SqlClient is not available in this PowerShell host. Use Windows PowerShell 5.1, or install Microsoft.Data.SqlClient and adjust this script."
}

$conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
$conn.Open()
try {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $query
    $reader = $cmd.ExecuteReader()
    $table = New-Object System.Data.DataTable
    $table.Load($reader)
    $reader.Dispose()
    if ($table.Rows.Count -eq 0) {
        Write-Warning "ModuleInfo returned no rows (empty database or table not populated yet)."
    }
    $table | Format-Table -AutoSize
}
finally {
    $conn.Close()
}

$asmPath = Join-Path $RepoRoot "Visa2026.Module\Visa2026.Module.csproj"
if (Test-Path -LiteralPath $asmPath) {
    $xml = [xml](Get-Content -Raw -LiteralPath $asmPath)
    $v = [string]$xml.Project.PropertyGroup.AssemblyVersion
    if ($v) {
        Write-Host ""
        Write-Host "Visa2026.Module AssemblyVersion in repo (target after deploy): $v" -ForegroundColor Green
        Write-Host "If ModuleInfo.Version for Visa2026Module is already equal or higher, UpdateOldDatabase will not run ReportsUpdater." -ForegroundColor DarkGray
    }
}
