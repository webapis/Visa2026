# Visa2026 Sync Script (Windows to Droplet)
# This script pushes local configuration files and scripts to the Droplet.

# --- CONFIGURATION ---
$DROPLET_IP = "91.194.54.81"  # Matches your last login IP
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
# ---------------------

Write-Host "1. Ensuring remote directory exists..." -ForegroundColor Cyan
ssh "$REMOTE_USER@$DROPLET_IP" "mkdir -p $REMOTE_DIR"

Write-Host "2. Uploading docker-compose.yml and .env..." -ForegroundColor Cyan
scp "c:\Users\IT\source\repos\Visa2026\docker-compose.yml" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
scp "c:\Users\IT\source\repos\Visa2026\.env" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "3. Uploading shell scripts..." -ForegroundColor Cyan
scp "c:\Users\IT\source\repos\Visa2026\droplet-scripts\*.sh" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "4. Setting execute permissions on Droplet..." -ForegroundColor Cyan
ssh "$REMOTE_USER@$DROPLET_IP" "chmod +x $REMOTE_DIR/*.sh"

Write-Host ""
Write-Host "Sync Complete! You can now run ./fresh-install.sh on the Droplet." -ForegroundColor Green