#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy published Visa2026 to one IIS slot on visa2026-onprem (Production / Staging / Demo).

.PARAMETER Profile
  Production (:80), Staging (:8080), or Demo (:8081). Default Production.

.PARAMETER SshHost
  SSH config host (default visa2026-onprem).

.PARAMETER ForceUpdate
  Run --updateDatabase --forceUpdate on the server.

.PARAMETER EnableForceXafDbUpdate
  Set FORCE_XAF_DB_UPDATE=true on the slot app pool before DB update (remove with Remove-Visa2026ForceXafDbUpdate.ps1 -Profile ...).

.PARAMETER SkipPublish
  Do not run Publish-Visa2026ForIis.ps1 locally.

.PARAMETER SkipDbUpdate
  Skip database update on server.

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [ValidateSet("Production", "Staging", "Demo")]
    [string]$Profile = "Production",

    [string]$SshHost = "visa2026-onprem",
    [string]$PublishPath = "",
    [switch]$SkipPublish,
    [switch]$SkipDbUpdate,
    [switch]$ForceUpdate,
    [switch]$EnableForceXafDbUpdate
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

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

$ctx = Resolve-Visa2026IisSlotContext -Profile $Profile
$remoteDeployWin = $ctx.DeployScriptsPath
$remoteDeployScp = "C:/visa2026-deploy/iis"
$remotePublishWin = $ctx.PublishPath
$remotePublishScp = ($ctx.PublishPath -replace '\\', '/')

Write-Host "==> Deploy slot: $($ctx.Profile) (port $($ctx.HttpPort), DB $($ctx.DbName))" -ForegroundColor Cyan
Write-Host "    Publish -> $remotePublishWin" -ForegroundColor DarkGray
Write-Host "    Smoke     -> $($ctx.LoginPageUrl)" -ForegroundColor DarkGray

Write-Host "==> SSH test $SshHost" -ForegroundColor Cyan
ssh -o BatchMode=yes -o ConnectTimeout=15 $SshHost "whoami"

$scriptFiles = @(
    "Visa2026-IisSlots.ps1",
    "Install-SqlServerExpress.ps1",
    "Install-Visa2026ServerPrerequisites.ps1",
    "Install-Visa2026IisSite.ps1",
    "Install-Visa2026IisSlots.ps1",
    "Ensure-Visa2026SlotDatabases.ps1",
    "Configure-Visa2026Production.ps1",
    "Set-Visa2026AppPoolEnvironment.ps1",
    "Set-Visa2026IisSlotsAutoStart.ps1",
    "Update-Visa2026Database.ps1",
    "Run-Visa2026DbUpdateOnServer.ps1",
    "Set-Visa2026EnvDbName.ps1",
    "Remove-Visa2026ForceXafDbUpdate.ps1",
    "Get-Visa2026RecentIisErrors.ps1",
    "Test-Visa2026Startup.ps1",
    "Enable-Visa2026StdoutLog.ps1",
    "Diagnose-Port80.ps1",
    "Enable-Visa2026IisSlotFirewall.ps1",
    "Get-Visa2026RuntimeErrorsForPull.ps1"
)
Write-Host "==> Copy scripts to server" -ForegroundColor Cyan
foreach ($f in $scriptFiles) {
    $local = Join-Path $PSScriptRoot $f
    if (Test-Path -LiteralPath $local) {
        scp -q $local "${SshHost}:${remoteDeployScp}/$f"
    }
}

Write-Host "==> Ensure slot folders, env, IIS site, database" -ForegroundColor Cyan
$legacyEnv = "C:\visa2026\.env.prod"
$installArgs = "-Profile $Profile -SkipConfigure -SkipAutoStart"
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Install-Visa2026IisSlots.ps1 $installArgs -SourceEnvFile $legacyEnv"
if ($LASTEXITCODE -ne 0) {
    throw "Install-Visa2026IisSlots.ps1 failed (exit $LASTEXITCODE)."
}

Write-Host "==> Stop app pool $($ctx.AppPoolName)" -ForegroundColor Cyan
ssh $SshHost "C:\Windows\System32\inetsrv\appcmd stop apppool $($ctx.AppPoolName) 2>nul"

Write-Host "==> Copy publish output" -ForegroundColor Cyan
scp -r -q "$PublishPath/*" "${SshHost}:${remotePublishScp}/"

Write-Host "==> Configure slot" -ForegroundColor Cyan
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Configure-Visa2026Production.ps1 -Profile $Profile -SqlServer 'localhost\SQLEXPRESS'"
if ($LASTEXITCODE -ne 0) { throw "Configure-Visa2026Production.ps1 failed (exit $LASTEXITCODE)." }
ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Set-Visa2026AppPoolEnvironment.ps1 -Profile $Profile"
if ($LASTEXITCODE -ne 0) { throw "Set-Visa2026AppPoolEnvironment.ps1 failed (exit $LASTEXITCODE)." }

if ($EnableForceXafDbUpdate) {
    Write-Host "==> Enable FORCE_XAF_DB_UPDATE on $($ctx.AppPoolName)" -ForegroundColor Cyan
    $forceCmd = @"
`$appcmd = Join-Path `$env:Windir 'System32\inetsrv\appcmd.exe'
& `$appcmd set config -section:system.applicationHost/applicationPools /-"[name='$($ctx.AppPoolName)'].environmentVariables.[name='FORCE_XAF_DB_UPDATE']" 2>`$null | Out-Null
& `$appcmd set config -section:system.applicationHost/applicationPools /+"[name='$($ctx.AppPoolName)'].environmentVariables.[name='FORCE_XAF_DB_UPDATE',value='true']"
"@
    ssh $SshHost "powershell -NoProfile -Command `"$forceCmd`""
}

if (-not $SkipDbUpdate) {
    Write-Host "==> Database update ($Profile)" -ForegroundColor Cyan
    $dbArgs = "-Profile $Profile"
    if ($ForceUpdate) { $dbArgs += " -ForceUpdate" }
    ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteDeployWin\Run-Visa2026DbUpdateOnServer.ps1 $dbArgs"
    if ($LASTEXITCODE -ne 0) { throw "Run-Visa2026DbUpdateOnServer.ps1 failed (exit $LASTEXITCODE)." }
}

Write-Host "==> Start app pool + site" -ForegroundColor Cyan
ssh $SshHost "C:\Windows\System32\inetsrv\appcmd start apppool $($ctx.AppPoolName)"
ssh $SshHost "C:\Windows\System32\inetsrv\appcmd start site $($ctx.SiteName)"

Write-Host "==> Smoke test" -ForegroundColor Cyan
$smokeUrl = $ctx.LoginPageUrl
$smokeResult = ssh $SshHost "powershell -NoProfile -Command `"try { (Invoke-WebRequest -Uri '$smokeUrl' -UseBasicParsing -TimeoutSec 180).StatusCode } catch { `$_.Exception.Message }`""
Write-Host $smokeResult
if ($smokeResult -ne "200") {
    throw "Smoke test failed for $($ctx.Profile): $smokeResult"
}

Write-Host ""
Write-Host "Deploy finished - $($ctx.Profile) on port $($ctx.HttpPort)." -ForegroundColor Green
Write-Host "  $($ctx.LoginPageUrl)" -ForegroundColor Green
if ($EnableForceXafDbUpdate) {
    Write-Host "  FORCE_XAF_DB_UPDATE is ON for $($ctx.AppPoolName). Remove after verify:" -ForegroundColor Yellow
    Write-Host "  Remove-Visa2026ForceXafDbUpdate.ps1 -Profile $Profile" -ForegroundColor Yellow
}
