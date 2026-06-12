#Requires -Version 5.1
<#
.SYNOPSIS
  Backup one Visa2026 IIS slot database to C:\visa2026\backups\{slot}\.
#>
param(
    [ValidateSet("Production", "Staging", "Demo")]
    [string]$Profile = "Production"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")
$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile

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

$envMap = Read-DotEnvMap $ctx.EnvFile
$pwd = $envMap["SA_PASSWORD"]
$db = if ($envMap["DB_NAME"]) { $envMap["DB_NAME"] } else { $ctx.DbName }
if (-not $pwd) { throw "SA_PASSWORD missing in $($ctx.EnvFile)" }

$dir = Join-Path "C:\visa2026\backups" $ctx.BackupSubfolder
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$bak = Join-Path $dir "$db-predeploy-$stamp.bak"

Add-Type -AssemblyName System.Data
$cs = "Server=localhost\SQLEXPRESS;Database=master;User ID=sa;Password=$pwd;TrustServerCertificate=True;"
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
try {
    $cmd = $cn.CreateCommand()
    $cmd.CommandTimeout = 600
    $cmd.CommandText = "BACKUP DATABASE [$db] TO DISK = N'$bak' WITH INIT"
    [void]$cmd.ExecuteNonQuery()
}
finally {
    $cn.Close()
}

if (-not (Test-Path -LiteralPath $bak)) { throw "Backup file not created: $bak" }
$sizeMb = [math]::Round((Get-Item -LiteralPath $bak).Length / 1MB, 1)
Write-Host "Backup OK ($Profile): $bak ($sizeMb MB)"
