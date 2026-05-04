#Requires -Version 5.1
<#
.SYNOPSIS
  [DROPLET] Set or clear FORCE_XAF_DB_UPDATE in the local env file, upload it, and force-recreate the remote app container.

.DESCRIPTION
  Same semantics as scripts/local/Set-ForceXafDbUpdate.ps1, but targets Docker on the droplet (like update-app.ps1):
  edits the env file under LOCAL_REPO, scp to REMOTE_DIR, then docker compose up --force-recreate for service app only.

.PARAMETER Enable
  Add or set FORCE_XAF_DB_UPDATE=true in the env file.

.PARAMETER Disable
  Remove any FORCE_XAF_DB_UPDATE= line from the env file.

.PARAMETER Environment
  prod → visa2026-prod, docker-compose.prod.yml, .env.prod
  dev  → visa2026-dev, docker-compose.dev.yml, .env.dev

.EXAMPLE
  .\droplet-scripts\Set-ForceXafDbUpdate.ps1 -Enable -Environment prod

.EXAMPLE
  .\droplet-scripts\Set-ForceXafDbUpdate.ps1 -Disable -Environment prod
#>
[CmdletBinding(DefaultParameterSetName = 'Enable')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Enable')]
    [switch]$Enable,

    [Parameter(Mandatory = $true, ParameterSetName = 'Disable')]
    [switch]$Disable,

    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",

    [string]$IdentityFile
)

$ErrorActionPreference = "Stop"

# --- CONFIGURATION (match update-app.ps1) ---
$DROPLET_IP = "167.172.177.93"
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
$LOCAL_REPO = Split-Path -Parent $PSScriptRoot
# ---------------------------------------------

$SshKeyArgs = @()
if (-not [string]::IsNullOrWhiteSpace($IdentityFile)) {
    if (-not (Test-Path -LiteralPath $IdentityFile)) {
        Write-Host "ERROR: SSH identity file not found: $IdentityFile" -ForegroundColor Red
        exit 1
    }
    $SshKeyArgs = @("-i", (Resolve-Path -LiteralPath $IdentityFile).Path)
}

$composeFile = if ($Environment -eq "prod") { "docker-compose.prod.yml" } else { "docker-compose.dev.yml" }
$envFileName = if ($Environment -eq "prod") { ".env.prod" } else { ".env.dev" }
$projectName = if ($Environment -eq "prod") { "visa2026-prod" } else { "visa2026-dev" }

$composePath = Join-Path $LOCAL_REPO $composeFile
$envPath = Join-Path $LOCAL_REPO $envFileName
$localSetScript = Join-Path $LOCAL_REPO "scripts\local\Set-ForceXafDbUpdate.ps1"

if (-not (Test-Path -LiteralPath $localSetScript)) {
    Write-Host "ERROR: Missing ${localSetScript}" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $composePath)) {
    Write-Host "ERROR: Missing ${composeFile} at ${composePath}" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $envPath)) {
    Write-Host "ERROR: Missing ${envFileName} at ${envPath}" -ForegroundColor Red
    Write-Host "Create it from ${envFileName}.example and fill required values." -ForegroundColor Yellow
    exit 1
}

Write-Host "0. Pre-flight connectivity check..." -ForegroundColor Cyan
$test = Test-NetConnection -ComputerName $DROPLET_IP -Port 22 -WarningAction SilentlyContinue

if (-not $test.TcpTestSucceeded) {
    Write-Host "ERROR: Cannot reach ${DROPLET_IP} on port 22 (SSH)." -ForegroundColor Red
    Write-Host "Action: Verify the Droplet is running in the DigitalOcean Dashboard." -ForegroundColor Yellow
    exit 1
}

Write-Host "1. Updating FORCE_XAF_DB_UPDATE in local ${envFileName}..." -ForegroundColor Cyan
if ($Enable) {
    & $localSetScript -Enable -EnvFile $envPath -ComposeFile $composeFile -NoCompose
}
else {
    & $localSetScript -Disable -EnvFile $envPath -ComposeFile $composeFile -NoCompose
}

Write-Host "2. Uploading ${envFileName} to droplet..." -ForegroundColor Cyan
scp @SshKeyArgs $envPath "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "3. Force-recreating remote app (${projectName}, SQL untouched)..." -ForegroundColor Yellow
$sshCmd = "cd ${REMOTE_DIR} && docker compose -p ${projectName} --env-file ${envFileName} -f ${composeFile} up -d --force-recreate --no-deps app"
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" $sshCmd

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Remote docker compose failed. Check output above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Done. '${projectName}' app recreated with updated ${envFileName}." -ForegroundColor Green
Write-Host "Remove FORCE_XAF_DB_UPDATE after a healthy start (run again with -Disable)." -ForegroundColor White
