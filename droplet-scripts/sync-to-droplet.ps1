# Visa2026 Sync Script (Windows to Droplet)
# This script pushes local configuration files and scripts to the Droplet.

# --- CONFIGURATION ---
$DROPLET_IP = "64.226.112.29"
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
# ---------------------

Write-Host "0. Pre-flight connectivity check..." -ForegroundColor Cyan
$test = Test-NetConnection -ComputerName $DROPLET_IP -Port 22 -WarningAction SilentlyContinue

if (-not $test.TcpTestSucceeded) {
    Write-Host "ERROR: Cannot reach ${DROPLET_IP} on port 22 (SSH). Server may be down or firewall is blocking SSH." -ForegroundColor Red
    Write-Host "Action: Verify the Droplet is running in the DigitalOcean Dashboard." -ForegroundColor Yellow
    exit
}

Write-Host "1. Ensuring remote directory exists..." -ForegroundColor Cyan
ssh -o ConnectTimeout=5 "${REMOTE_USER}@${DROPLET_IP}" "mkdir -p ${REMOTE_DIR}"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Could not connect to the Droplet at ${DROPLET_IP}." -ForegroundColor Red
    Write-Host "Please check if the Droplet is powered on and port 22 is open in the firewall." -ForegroundColor Yellow
    exit
}

Write-Host "2. Uploading docker-compose.yml and .env..." -ForegroundColor Cyan
scp "c:\Users\IT\source\repos\Visa2026\docker-compose.yml" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
scp "c:\Users\IT\source\repos\Visa2026\.env" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "3. Uploading shell scripts..." -ForegroundColor Cyan
scp "c:\Users\IT\source\repos\Visa2026\droplet-scripts\*.sh" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "4. Setting execute permissions on Droplet..." -ForegroundColor Cyan
ssh "${REMOTE_USER}@${DROPLET_IP}" "chmod +x ${REMOTE_DIR}/*.sh"

Write-Host "5. Executing Fresh Install on Droplet..." -ForegroundColor Yellow
ssh "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && ./fresh-install.sh"

Write-Host ""
Write-Host "Sync and Fresh Install Complete!" -ForegroundColor Green
Write-Host "Your application is now running with the latest configuration." -ForegroundColor White