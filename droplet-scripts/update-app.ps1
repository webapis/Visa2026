# Visa2026 App Update Script (Windows to Droplet)
# Use this after pushing a new image to Docker Hub (i.e. after pushing to the droplet branch).
# Pulls the latest app image and restarts ONLY the app container.
# SQL Server and its data volume are NOT touched.

# --- CONFIGURATION ---
$DROPLET_IP = "64.226.112.29"
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
# ---------------------

Write-Host "0. Pre-flight connectivity check..." -ForegroundColor Cyan
$test = Test-NetConnection -ComputerName $DROPLET_IP -Port 22 -WarningAction SilentlyContinue

if (-not $test.TcpTestSucceeded) {
    Write-Host "ERROR: Cannot reach ${DROPLET_IP} on port 22 (SSH)." -ForegroundColor Red
    Write-Host "Action: Verify the Droplet is running in the DigitalOcean Dashboard." -ForegroundColor Yellow
    exit 1
}

Write-Host "1. Uploading latest docker-compose.yml and .env..." -ForegroundColor Cyan
scp "c:\Users\IT\source\repos\Visa2026\docker-compose.yml" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
scp "c:\Users\IT\source\repos\Visa2026\.env" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "2. Uploading update script..." -ForegroundColor Cyan
scp "c:\Users\IT\source\repos\Visa2026\droplet-scripts\update-app.sh" "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
ssh "${REMOTE_USER}@${DROPLET_IP}" "chmod +x ${REMOTE_DIR}/update-app.sh"

Write-Host "3. Running update on Droplet (database data preserved)..." -ForegroundColor Yellow
ssh "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && ./update-app.sh"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Update failed. Check output above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Update complete! App is running with the latest image." -ForegroundColor Green
Write-Host "Database data is untouched." -ForegroundColor White
Write-Host "Visit http://${DROPLET_IP} to verify." -ForegroundColor White
