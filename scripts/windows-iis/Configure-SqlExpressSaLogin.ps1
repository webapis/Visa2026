#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Align SQL Server Express SA login and mixed-mode auth with C:\visa2026\.env.prod.

.DESCRIPTION
  Use after a manual SQL Express install when Visa2026 IIS deploy fails with
  "Login failed for user 'sa'". Enables SQL authentication, sets SA password from
  SA_PASSWORD, and creates DB_NAME if missing.

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [string]$EnvFile = "C:\visa2026\.env.prod",
    [string]$InstanceName = "SQLEXPRESS"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

function Read-DotEnvMap([string]$Path) {
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

function Escape-SqlString([string]$Value) {
    $Value.Replace("'", "''")
}

function Invoke-Sql([string]$ConnectionString, [string]$Query) {
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        [void]$command.ExecuteNonQuery()
    }
    finally {
        if ($connection.State -eq [System.Data.ConnectionState]::Open) {
            $connection.Close()
        }
        $connection.Dispose()
    }
}

function Get-Scalar([string]$ConnectionString, [string]$Query) {
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        return $command.ExecuteScalar()
    }
    finally {
        if ($connection.State -eq [System.Data.ConnectionState]::Open) {
            $connection.Close()
        }
        $connection.Dispose()
    }
}

function Get-SqlInstanceKey([string]$Instance) {
    $key = (Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL").$Instance
    if ([string]::IsNullOrWhiteSpace($key)) {
        throw "SQL instance $Instance not found in registry."
    }
    return $key
}

function Add-SqlStartupArgument([string]$InstanceKey, [string]$Argument) {
    $paramsPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$InstanceKey\MSSQLServer\Parameters"
    $props = Get-ItemProperty -Path $paramsPath
    foreach ($p in $props.PSObject.Properties) {
        if ($p.Name -match '^SQLArg\d+$' -and $p.Value -eq $Argument) {
            return $null
        }
    }
    $indices = @()
    foreach ($p in $props.PSObject.Properties) {
        if ($p.Name -match '^SQLArg(\d+)$') {
            $indices += [int]$Matches[1]
        }
    }
    $next = if ($indices.Count -gt 0) { ($indices | Measure-Object -Maximum).Maximum + 1 } else { 0 }
    $name = "SQLArg$next"
    New-ItemProperty -Path $paramsPath -Name $name -Value $Argument -PropertyType String | Out-Null
    return $name
}

function Remove-SqlStartupArgument([string]$InstanceKey, [string]$Argument) {
    $paramsPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$InstanceKey\MSSQLServer\Parameters"
    $props = Get-ItemProperty -Path $paramsPath
    foreach ($p in $props.PSObject.Properties) {
        if ($p.Name -match '^SQLArg\d+$' -and $p.Value -eq $Argument) {
            Remove-ItemProperty -Path $paramsPath -Name $p.Name
            return $p.Name
        }
    }
    return $null
}

function Restart-SqlInstance([string]$Service) {
    Restart-Service -Name $Service -Force
    Start-Sleep -Seconds 5
    $svc = Get-Service -Name $Service
    if ($svc.Status -ne "Running") {
        throw "Service $Service did not restart (status: $($svc.Status))."
    }
}

function Format-SqlLoginName([string]$Login) {
    $escaped = $Login.Replace("]", "]]")
    return "[$escaped]"
}

function Invoke-SqlSingleUserBootstrap(
    [string]$ServiceName,
    [string]$InstanceKey,
    [string]$WinConn,
    [string]$WinLogin,
    [string]$SaPasswordSql
) {
    Write-Host "Current Windows login is not SQL sysadmin; starting single-user bootstrap (-m)..." -ForegroundColor Yellow
    $addedArg = Add-SqlStartupArgument -InstanceKey $InstanceKey -Argument "-m"
    try {
        Restart-SqlInstance $ServiceName
        Invoke-Sql $WinConn $SaPasswordSql
    }
    finally {
        if ($addedArg) {
            Remove-SqlStartupArgument -InstanceKey $InstanceKey -Argument "-m" | Out-Null
            Restart-SqlInstance $ServiceName
        }
    }
}

$envMap = Read-DotEnvMap $EnvFile
$saPassword = $envMap["SA_PASSWORD"]
$dbName = if ($envMap.ContainsKey("DB_NAME")) { $envMap["DB_NAME"] } else { "Visa2026DbProd" }
if ([string]::IsNullOrWhiteSpace($saPassword)) { throw "SA_PASSWORD missing in $EnvFile" }

$server = "localhost\$InstanceName"
$serviceName = "MSSQL`$$InstanceName"
$winConn = "Server=$server;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"
$saConn = "Server=$server;User Id=sa;Password=$saPassword;TrustServerCertificate=True;Encrypt=False"

Write-Host "==> Check SQL auth mode on $server" -ForegroundColor Cyan
try {
    $winAuthOnly = [int](Get-Scalar $winConn "SELECT CAST(SERVERPROPERTY('IsIntegratedSecurityOnly') AS int);")
}
catch {
    throw "Cannot connect with Windows authentication to $server. Ensure the current user is a SQL sysadmin. $($_.Exception.Message)"
}

$instanceKey = Get-SqlInstanceKey $InstanceName

if ($winAuthOnly -eq 1) {
    Write-Host "Enabling mixed-mode authentication (SQL + Windows)..." -ForegroundColor Yellow
    $loginModePath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceKey\MSSQLServer"
    Set-ItemProperty -Path $loginModePath -Name LoginMode -Value 2 -Type DWord
    Restart-SqlInstance $serviceName
    Write-Host "SQL service restarted." -ForegroundColor Green
}

$escapedPwd = Escape-SqlString $saPassword
$winLogin = [string](Get-Scalar $winConn "SELECT SUSER_SNAME();")
$bracketedLogin = Format-SqlLoginName $winLogin
$isSysAdmin = [int](Get-Scalar $winConn "SELECT IS_SRVROLEMEMBER('sysadmin');")

$configureSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$($winLogin.Replace("'", "''"))')
    CREATE LOGIN $bracketedLogin FROM WINDOWS;
ALTER SERVER ROLE sysadmin ADD MEMBER $bracketedLogin;
ALTER LOGIN sa ENABLE;
ALTER LOGIN sa WITH PASSWORD = N'$escapedPwd';
"@

if ($isSysAdmin -ne 1) {
    Invoke-SqlSingleUserBootstrap -ServiceName $serviceName -InstanceKey $instanceKey -WinConn $winConn -WinLogin $winLogin -SaPasswordSql $configureSql
}
else {
    Write-Host "==> Set SA password from $EnvFile" -ForegroundColor Cyan
    Invoke-Sql $winConn $configureSql
}

Write-Host "==> Verify SA login" -ForegroundColor Cyan
try {
    [void](Get-Scalar $saConn "SELECT 1;")
}
catch {
    throw "SA login test failed after configuration. $($_.Exception.Message)"
}

Write-Host "==> Ensure database [$dbName]" -ForegroundColor Cyan
$escapedDb = Escape-SqlString $dbName
Invoke-Sql $saConn "IF DB_ID(N'$escapedDb') IS NULL CREATE DATABASE [$dbName];"

Write-Host "SQL Express ready: $server (sa), database $dbName" -ForegroundColor Green
