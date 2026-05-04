# Visa2026 Sync Script (Windows to Droplet)
# This script pushes local configuration files and scripts to the Droplet.

param(
    [string]$IdentityFile
)

# --- CONFIGURATION ---
$DROPLET_IP = "64.226.112.29"
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
$LOCAL_REPO = Split-Path -Parent $PSScriptRoot
# ---------------------

$SshKeyArgs = @()
if (-not [string]::IsNullOrWhiteSpace($IdentityFile)) {
    if (-not (Test-Path -LiteralPath $IdentityFile)) {
        Write-Host "ERROR: SSH identity file not found: $IdentityFile" -ForegroundColor Red
        exit 1
    }
    $SshKeyArgs = @("-i", (Resolve-Path -LiteralPath $IdentityFile).Path)
}

Write-Host "0. Pre-flight connectivity check..." -ForegroundColor Cyan
$test = Test-NetConnection -ComputerName $DROPLET_IP -Port 22 -WarningAction SilentlyContinue

if (-not $test.TcpTestSucceeded) {
    Write-Host "ERROR: Cannot reach ${DROPLET_IP} on port 22 (SSH). Server may be down or firewall is blocking SSH." -ForegroundColor Red
    Write-Host "Action: Verify the Droplet is running in the DigitalOcean Dashboard." -ForegroundColor Yellow
    exit
}

Write-Host "1. Ensuring remote directory exists..." -ForegroundColor Cyan
ssh @SshKeyArgs -o ConnectTimeout=5 "${REMOTE_USER}@${DROPLET_IP}" "mkdir -p ${REMOTE_DIR}"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Could not connect to the Droplet at ${DROPLET_IP}." -ForegroundColor Red
    Write-Host "Please check if the Droplet is powered on and port 22 is open in the firewall." -ForegroundColor Yellow
    exit
}

Write-Host "2. Uploading docker-compose.yml and .env..." -ForegroundColor Cyan
scp @SshKeyArgs (Join-Path $LOCAL_REPO "docker-compose.yml") "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
scp @SshKeyArgs (Join-Path $LOCAL_REPO ".env") "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "3. Uploading shell scripts..." -ForegroundColor Cyan
scp @SshKeyArgs (Join-Path $LOCAL_REPO "droplet-scripts\*.sh") "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "4. Setting execute permissions on Droplet..." -ForegroundColor Cyan
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "chmod +x ${REMOTE_DIR}/*.sh"

Write-Host "5. Executing Fresh Install on Droplet..." -ForegroundColor Yellow
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && ./fresh-install.sh"

Write-Host ""
Write-Host "Sync and Fresh Install Complete!" -ForegroundColor Green
Write-Host "Your application is now running with the latest configuration." -ForegroundColor White