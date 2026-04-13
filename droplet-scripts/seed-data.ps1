# Visa2026 Seed Data Script (Windows to Droplet)
# Restarts the app in Development mode, runs the importer, then restores Production.

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
    exit 1
}

Write-Host "1. Creating Development environment override on Droplet..." -ForegroundColor Cyan
ssh "${REMOTE_USER}@${DROPLET_IP}" @"
cat > ${REMOTE_DIR}/dev-override.yml << 'EOF'
services:
  app:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
EOF
"@

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create override file on Droplet." -ForegroundColor Red
    exit 1
}

Write-Host "2. Restarting app in Development mode..." -ForegroundColor Cyan
ssh "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose stop app && docker compose -f docker-compose.yml -f dev-override.yml up -d app"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restart app in Development mode." -ForegroundColor Red
    exit 1
}

Write-Host "3. Waiting 20 seconds for app to fully boot..." -ForegroundColor Cyan
Start-Sleep -Seconds 20

Write-Host "4. Running data importer (this may take a while)..." -ForegroundColor Yellow
ssh "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose -f docker-compose.yml -f dev-override.yml --profile tools run --rm db-updater"

if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Importer exited with errors. Check output above." -ForegroundColor Yellow
} else {
    Write-Host "Importer completed successfully." -ForegroundColor Green
}

Write-Host "5. Restoring app to Production mode..." -ForegroundColor Cyan
ssh "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose stop app && docker compose up -d app && rm -f ${REMOTE_DIR}/dev-override.yml"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore Production mode. Run 'docker compose up -d app' manually on the Droplet." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Done! Data seeded and app is back in Production mode." -ForegroundColor Green
Write-Host "Visit http://${DROPLET_IP} to verify." -ForegroundColor White
