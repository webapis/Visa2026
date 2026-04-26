# Visa2026 Seed Data Script (Windows to Droplet)
# Restarts the app in Development mode, seeds lookup data, optionally imports data.yaml, then restores Production.

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
# Write with LF line endings locally, then scp — avoids CRLF/heredoc issues over SSH
$overrideContent = "services:`n  app:`n    environment:`n      - ASPNETCORE_ENVIRONMENT=Development`n"
$tempFile = Join-Path $env:TEMP "dev-override.yml"
[System.IO.File]::WriteAllText($tempFile, $overrideContent)
scp $tempFile "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/dev-override.yml"
Remove-Item $tempFile

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

Write-Host "4. Seeding baseline lookup data (required)..." -ForegroundColor Yellow
ssh "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose -f docker-compose.yml -f dev-override.yml --profile tools run --rm db-updater --seed-lookups-only"

if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Lookup seed exited with errors. Check output above." -ForegroundColor Yellow
} else {
    Write-Host "Lookup seed completed successfully." -ForegroundColor Green
}

$shouldImportYaml = $false
while ($true) {
    $answer = (Read-Host "5. Import optional data from data.yaml? (yes/no)").Trim().ToLowerInvariant()
    if ($answer -in @("yes", "y")) {
        $shouldImportYaml = $true
        break
    }
    if ($answer -in @("no", "n")) {
        $shouldImportYaml = $false
        break
    }
    Write-Host "Invalid input. Please type 'yes' or 'no'." -ForegroundColor Yellow
}

if ($shouldImportYaml) {
    Write-Host "5. Importing optional YAML scenarios (data.yaml)..." -ForegroundColor Yellow
    ssh "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose -f docker-compose.yml -f dev-override.yml --profile tools run --rm db-updater --import-yaml-only"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: YAML import exited with errors. Check output above." -ForegroundColor Yellow
    } else {
        Write-Host "YAML import completed successfully." -ForegroundColor Green
    }
} else {
    Write-Host "5. Skipping optional YAML import." -ForegroundColor DarkYellow
}

Write-Host "6. Restoring app to Production mode..." -ForegroundColor Cyan
ssh "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose stop app && docker compose up -d app && rm -f ${REMOTE_DIR}/dev-override.yml"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore Production mode. Run 'docker compose up -d app' manually on the Droplet." -ForegroundColor Red
    exit 1
}

Write-Host ""
if ($shouldImportYaml) {
    Write-Host "Done! Lookup seed + YAML import completed (check warnings), and app is back in Production mode." -ForegroundColor Green
} else {
    Write-Host "Done! Lookup seed completed, YAML import skipped, and app is back in Production mode." -ForegroundColor Green
}
Write-Host "Visit http://${DROPLET_IP} to verify." -ForegroundColor White
