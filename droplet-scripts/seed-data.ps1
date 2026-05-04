# Visa2026 Seed Data Script (Windows to Droplet)
# Targets one environment stack (prod/dev), temporarily runs app in Development mode for import,
# seeds lookup data, optionally imports data.yaml, then restores original environment.

param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "dev",
    [string]$DropletIp = "167.172.177.93",
    [string]$IdentityFile
)

# --- CONFIGURATION ---
$DROPLET_IP = $DropletIp
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

$composeFile = if ($Environment -eq "prod") { "docker-compose.prod.yml" } else { "docker-compose.dev.yml" }
$envFile = if ($Environment -eq "prod") { ".env.prod" } else { ".env.dev" }
$projectName = if ($Environment -eq "prod") { "visa2026-prod" } else { "visa2026-dev" }
$composePath = Join-Path $LOCAL_REPO $composeFile
$envPath = Join-Path $LOCAL_REPO $envFile
$overrideFile = "dev-override-$Environment.yml"

if (-not (Test-Path $composePath)) {
    Write-Host "ERROR: Missing ${composeFile} at ${composePath}" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $envPath)) {
    Write-Host "ERROR: Missing ${envFile} at ${envPath}" -ForegroundColor Red
    Write-Host "Create it from ${envFile}.example and fill required values." -ForegroundColor Yellow
    exit 1
}

if ($Environment -eq "prod") {
    $prodConfirm = (Read-Host "WARNING: You are targeting PRODUCTION. Continue? (yes/no)").Trim().ToLowerInvariant()
    if ($prodConfirm -notin @("yes", "y")) {
        Write-Host "Cancelled by user." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "0. Pre-flight connectivity check..." -ForegroundColor Cyan
$test = Test-NetConnection -ComputerName $DROPLET_IP -Port 22 -WarningAction SilentlyContinue
if (-not $test.TcpTestSucceeded) {
    Write-Host "ERROR: Cannot reach ${DROPLET_IP} on port 22 (SSH). Server may be down or firewall is blocking SSH." -ForegroundColor Red
    Write-Host "Action: Verify the Droplet is running in the DigitalOcean Dashboard." -ForegroundColor Yellow
    exit 1
}

Write-Host "1. Uploading ${composeFile} and ${envFile}..." -ForegroundColor Cyan
scp @SshKeyArgs $composePath "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Failed to upload ${composeFile}." -ForegroundColor Red; exit 1 }
scp @SshKeyArgs $envPath "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Failed to upload ${envFile}." -ForegroundColor Red; exit 1 }

Write-Host "2. Creating temporary Development override on Droplet..." -ForegroundColor Cyan
$overrideContent = "services:`n  app:`n    environment:`n      - ASPNETCORE_ENVIRONMENT=Development`n"
$tempFile = Join-Path $env:TEMP $overrideFile
[System.IO.File]::WriteAllText($tempFile, $overrideContent)
scp @SshKeyArgs $tempFile "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/${overrideFile}"
Remove-Item $tempFile -ErrorAction SilentlyContinue
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to upload override file to Droplet." -ForegroundColor Red
    exit 1
}

$restoreSucceeded = $false
$shouldImportYaml = $false

try {
    Write-Host "3. Restarting '${projectName}' app in Development mode..." -ForegroundColor Cyan
    ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} stop app && docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} -f ${overrideFile} up -d app"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to restart app in Development mode." -ForegroundColor Red
        exit 1
    }

    Write-Host "4. Waiting 20 seconds for app to fully boot..." -ForegroundColor Cyan
    Start-Sleep -Seconds 20

    Write-Host "5. Seeding baseline lookup data (required)..." -ForegroundColor Yellow
    ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} -f ${overrideFile} --profile tools run --rm db-updater --seed-lookups-only"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: Lookup seed exited with errors. Check output above." -ForegroundColor Yellow
    } else {
        Write-Host "Lookup seed completed successfully." -ForegroundColor Green
    }

    while ($true) {
        $answer = (Read-Host "6. Import optional data from data.yaml? (yes/no)").Trim().ToLowerInvariant()
        if ($answer -in @("yes", "y")) { $shouldImportYaml = $true; break }
        if ($answer -in @("no", "n")) { $shouldImportYaml = $false; break }
        Write-Host "Invalid input. Please type 'yes' or 'no'." -ForegroundColor Yellow
    }

    if ($shouldImportYaml) {
        Write-Host "6. Importing optional YAML scenarios (data.yaml)..." -ForegroundColor Yellow
        ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} -f ${overrideFile} --profile tools run --rm db-updater --import-yaml-only"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "WARNING: YAML import exited with errors. Check output above." -ForegroundColor Yellow
        } else {
            Write-Host "YAML import completed successfully." -ForegroundColor Green
        }
    } else {
        Write-Host "6. Skipping optional YAML import." -ForegroundColor DarkYellow
    }
}
finally {
    Write-Host "7. Restoring app to original environment..." -ForegroundColor Cyan
    ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} up -d --no-deps app && rm -f ${REMOTE_DIR}/${overrideFile}"
    if ($LASTEXITCODE -eq 0) {
        $restoreSucceeded = $true
    }
}

if (-not $restoreSucceeded) {
    Write-Host "ERROR: Failed to restore app to original environment. Run restore manually on the Droplet." -ForegroundColor Red
    Write-Host "Command: docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} up -d --no-deps app" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
if ($shouldImportYaml) {
    Write-Host "Done! Lookup seed + YAML import completed (check warnings), and '${projectName}' is restored." -ForegroundColor Green
} else {
    Write-Host "Done! Lookup seed completed, YAML import skipped, and '${projectName}' is restored." -ForegroundColor Green
}
Write-Host "Visit http://${DROPLET_IP} to verify." -ForegroundColor White
