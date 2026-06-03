#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy published Visa2026 to visa2026-onprem over SSH (IIS path).

.DESCRIPTION
  Copies publish output + scripts, installs SQL Express + IIS (native), stops Docker
  app container, configures IIS, runs DB update against localhost\SQLEXPRESS.

.PARAMETER SshHost
  SSH config host (default visa2026-onprem).

.PARAMETER PublishPath
  Local publish folder. Default: latest dist\visa2026-iis-*.

.PARAMETER SkipPublish
  Do not run Publish-Visa2026ForIis.ps1 locally.

.PARAMETER SkipDbUpdate
  Skip --updateDatabase on server.
#>
param(
    [string]$SshHost = "visa2026-onprem",
    [string]$PublishPath = "",
    [switch]$SkipPublish,
    [switch]$SkipDbUpdate
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

if (-not $SkipPublish) {
    & (Join-Path $PSScriptRoot "Publish-Visa2026ForIis.ps1")
}

if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    $PublishPath = Get-ChildItem (Join-Path $RepoRoot "dist") -Directory -Filter "visa2026-iis-*" |
        Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $PublishPath -or -not (Test-Path -LiteralPath $PublishPath)) {
    throw "Publish folder not found. Run Publish-Visa2026ForIis.ps1 first."
}

Write-Host "==> SSH test $SshHost" -ForegroundColor Cyan
ssh -o BatchMode=yes -o ConnectTimeout=15 $SshHost "whoami"

$remoteRootWin = "C:\inetpub\visa2026"
$remoteDeployWin = "C:\visa2026-deploy\iis"
$remoteRootScp = "C:/inetpub/visa2026"
$remoteDeployScp = "C:/visa2026-deploy/iis"

Write-Host "==> Create server folders" -ForegroundColor Cyan
ssh $SshHost 'powershell -NoProfile -Command "New-Item -ItemType Directory -Force -Path C:\visa2026-deploy\iis,C:\inetpub\visa2026 | Out-Null"'

Write-Host "==> Copy scripts to server" -ForegroundColor Cyan
$scriptFiles = @(
    "Install-SqlServerExpress.ps1",
    "Install-Visa2026ServerPrerequisites.ps1",
    "Install-Visa2026IisSite.ps1",
    "Configure-Visa2026Production.ps1",
    "Set-Visa2026AppPoolEnvironment.ps1",
    "Update-Visa2026Database.ps1",
    "Run-Visa2026DbUpdateOnServer.ps1",
    "Set-Visa2026EnvDbName.ps1",
    "Remove-Visa2026ForceXafDbUpdate.ps1",
    "Get-Visa2026RecentIisErrors.ps1",
    "Test-Visa2026Startup.ps1",
    "Enable-Visa2026StdoutLog.ps1"
)
foreach ($f in $scriptFiles) {
    scp -q (Join-Path $PSScriptRoot $f) "${SshHost}:${remoteDeployScp}/$f"
}

Write-Host "==> Copy publish output (may take a few minutes)" -ForegroundColor Cyan
scp -r -q "$PublishPath/*" "${SshHost}:${remoteRootScp}/"

Write-Host "==> Stop Docker containers (use native SQL + IIS)" -ForegroundColor Cyan
ssh $SshHost "wsl -d Ubuntu -u root -- docker stop visa2026-prod-app-1 visa2026-prod-sqlserver-1 2>nul"

Write-Host "==> Install SQL Server Express (native)" -ForegroundColor Cyan
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Install-SqlServerExpress.ps1"

Write-Host "==> Install IIS + Hosting Bundle" -ForegroundColor Cyan
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Install-Visa2026ServerPrerequisites.ps1"

Write-Host "==> Configure production settings" -ForegroundColor Cyan
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Configure-Visa2026Production.ps1 -PublishPath C:\inetpub\visa2026 -EnvFile C:\visa2026\.env.prod -SqlServer 'localhost\SQLEXPRESS'"

Write-Host "==> IIS site + app pool" -ForegroundColor Cyan
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Install-Visa2026IisSite.ps1 -PublishPath C:\inetpub\visa2026"
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Set-Visa2026AppPoolEnvironment.ps1"

ssh $SshHost "icacls C:\ProgramData\Visa2026\DataProtection-Keys /grant `"IIS AppPool\Visa2026:(OI)(CI)M`" 2>nul"

if (-not $SkipDbUpdate) {
    Write-Host "==> Database update" -ForegroundColor Cyan
    ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Run-Visa2026DbUpdateOnServer.ps1 -ForceUpdate"
}

Write-Host "==> Start site" -ForegroundColor Cyan
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -Command \"Import-Module WebAdministration; Start-WebAppPool -Name Visa2026; Start-Website -Name Visa2026\""

Write-Host "==> Smoke test" -ForegroundColor Cyan
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -Command \"try { (Invoke-WebRequest -Uri http://127.0.0.1/LoginPage -UseBasicParsing -TimeoutSec 120).StatusCode } catch { Write-Output $_.Exception.Message }\""

Write-Host ""
Write-Host "Deploy finished. Browser: http://10.100.128.25/LoginPage" -ForegroundColor Green
