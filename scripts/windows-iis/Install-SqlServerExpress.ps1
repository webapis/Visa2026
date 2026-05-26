#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Install SQL Server Express on Windows Server (native, not Docker/WSL).

.DESCRIPTION
  Silent install of SQL Server 2022 Express (named instance SQLEXPRESS).
  Reads SA_PASSWORD from C:\visa2026\.env.prod (same as Docker .env.prod).
  Creates Visa2026DbProd if missing.

.PARAMETER EnvFile
  Default C:\visa2026\.env.prod

.PARAMETER InstanceName
  Default SQLEXPRESS (SQL Server Express default).

.PARAMETER DatabaseName
  Default from DB_NAME in env file or Visa2026DbProd.

.PARAMETER SkipDownload
  Use existing installer in C:\visa2026-deploy\sql\

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
  Requires outbound HTTPS to download.microsoft.com (or pre-copy SQLEXPR_x64_ENU.exe).
#>
param(
    [string]$EnvFile = "C:\visa2026\.env.prod",
    [string]$InstanceName = "SQLEXPRESS",
    [string]$DatabaseName = "",
    [switch]$SkipDownload
)

$ErrorActionPreference = "Stop"

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

function Get-SqlCmdPath {
    $candidates = @(
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\160\Tools\Binn\sqlcmd.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path -LiteralPath $p) { return $p }
    }
    $found = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    return $null
}

$envMap = Read-DotEnvMap $EnvFile
$saPassword = $envMap["SA_PASSWORD"]
if ([string]::IsNullOrWhiteSpace($saPassword)) {
    throw "SA_PASSWORD missing in $EnvFile"
}
if ([string]::IsNullOrWhiteSpace($DatabaseName)) {
    $DatabaseName = if ($envMap.ContainsKey("DB_NAME")) { $envMap["DB_NAME"] } else { "Visa2026DbProd" }
}

$serviceName = "MSSQL`$$InstanceName"
$sqlDeployDir = "C:\visa2026-deploy\sql"
$installerPath = Join-Path $sqlDeployDir "SQLEXPR_x64_ENU.exe"
$configPath = Join-Path $sqlDeployDir "SqlExpress.ini"
$logPath = Join-Path $sqlDeployDir "SqlExpress-install.log"

New-Item -ItemType Directory -Force -Path $sqlDeployDir | Out-Null

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService -and $existingService.Status -eq "Running") {
    Write-Host "SQL Server instance $InstanceName already installed (service $serviceName running)." -ForegroundColor Green
}
else {
    if (-not $SkipDownload -and -not (Test-Path -LiteralPath $installerPath)) {
        Write-Host "==> Download SQL Server 2022 Express installer" -ForegroundColor Cyan
        # Official SQL Server 2022 Express bootstrap (ENU x64)
        $url = "https://go.microsoft.com/fwlink/?linkid=2216019"
        Invoke-WebRequest -Uri $url -OutFile $installerPath -UseBasicParsing
    }
    if (-not (Test-Path -LiteralPath $installerPath)) {
        throw "Installer not found: $installerPath. Copy SQLEXPR_x64_ENU.exe there or allow download."
    }

    # Escape double quotes for setup ini
    $saEscaped = $saPassword -replace '"', '""'

    @"
[OPTIONS]
ACTION="Install"
IACCEPTSQLSERVERLICENSETERMS="True"
ENU="True"
FEATURES=SQLENGINE
INSTANCENAME="$InstanceName"
SECURITYMODE="SQL"
SAPWD="$saEscaped"
TCPENABLED=1
NPENABLED=1
BROWSERSVCSTARTUPTYPE="Automatic"
SQLSVCACCOUNT="NT AUTHORITY\NETWORK SERVICE"
SQLSVCSTARTUPTYPE="Automatic"
SQLSYSADMINACCOUNTS="BUILTIN\Administrators"
"@ | Set-Content -LiteralPath $configPath -Encoding ASCII

    Write-Host "==> Install SQL Server Express ($InstanceName) - may take 10-20 minutes" -ForegroundColor Cyan
    $setupArgs = "/ConfigurationFile=`"$configPath`" /QS"
    $p = Start-Process -FilePath $installerPath -ArgumentList $setupArgs -Wait -PassThru -NoNewWindow
    if ($p.ExitCode -notin 0, 3010) {
        if (Test-Path $logPath) { Get-Content $logPath -Tail 40 }
        throw "SQL setup exit code: $($p.ExitCode). Check summary log under C:\Program Files\Microsoft SQL Server\*\Setup Bootstrap\Log\"
    }

    $svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        throw "Install finished but service $serviceName not found."
    }
    if ($svc.Status -ne "Running") {
        Start-Service -Name $serviceName
    }
    Write-Host "SQL Server Express installed." -ForegroundColor Green
}

$sqlcmd = Get-SqlCmdPath
if (-not $sqlcmd) {
    Write-Warning "sqlcmd not found yet; create database manually: CREATE DATABASE [$DatabaseName]"
}
else {
    $server = "localhost\$InstanceName"
    Write-Host "==> Ensure database [$DatabaseName] on $server" -ForegroundColor Cyan
    $query = "IF DB_ID(N'$DatabaseName') IS NULL CREATE DATABASE [$DatabaseName];"
    & $sqlcmd -S $server -U sa -P $saPassword -C -Q $query
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed (exit $LASTEXITCODE). Verify SA password and instance $server"
    }
    Write-Host "Database ready: $DatabaseName" -ForegroundColor Green
}

Write-Host ""
Write-Host "Connection string server part: localhost\$InstanceName" -ForegroundColor DarkGray
Write-Host "Re-run Configure-Visa2026Production.ps1 with -SqlServer 'localhost\$InstanceName'" -ForegroundColor DarkGray
