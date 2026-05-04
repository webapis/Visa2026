# Visa2026 App Update Script (Windows to Droplet)
# Use this after pushing a new image to Docker Hub (i.e. after pushing to the droplet branch).
# Pulls the latest app image and restarts ONLY the app container.
# SQL Server and its data volume are NOT touched.

param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",
    # Optional: path to private key if ssh/scp do not pick up the right one (fixes "Permission denied (publickey)").
    [string]$IdentityFile
)

# --- CONFIGURATION ---
$DROPLET_IP = "167.172.177.93"
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
# Repo root = parent of this folder (any clone path).
$LOCAL_REPO = Split-Path -Parent $PSScriptRoot
# Droplet must have your *public* key (e.g. DigitalOcean SSH keys or ~/.ssh/authorized_keys on the server).
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
$updateScriptPath = Join-Path $LOCAL_REPO "droplet-scripts\update-app.sh"

if (-not (Test-Path $composePath)) {
    Write-Host "ERROR: Missing ${composeFile} at ${composePath}" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $envPath)) {
    Write-Host "ERROR: Missing ${envFile} at ${envPath}" -ForegroundColor Red
    Write-Host "Create it from ${envFile}.example and fill required values." -ForegroundColor Yellow
    exit 1
}

Write-Host "0. Pre-flight connectivity check..." -ForegroundColor Cyan
$test = Test-NetConnection -ComputerName $DROPLET_IP -Port 22 -WarningAction SilentlyContinue

if (-not $test.TcpTestSucceeded) {
    Write-Host "ERROR: Cannot reach ${DROPLET_IP} on port 22 (SSH)." -ForegroundColor Red
    Write-Host "Action: Verify the Droplet is running in the DigitalOcean Dashboard." -ForegroundColor Yellow
    exit 1
}

Write-Host "1. Uploading ${composeFile} and ${envFile}..." -ForegroundColor Cyan
scp @SshKeyArgs $composePath "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
scp @SshKeyArgs $envPath "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"

Write-Host "2. Uploading update script..." -ForegroundColor Cyan
scp @SshKeyArgs $updateScriptPath "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/"
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "chmod +x ${REMOTE_DIR}/update-app.sh"

Write-Host "3. Running ${Environment} update on Droplet (database data preserved)..." -ForegroundColor Yellow
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && ./update-app.sh ${Environment}"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Update failed. Check output above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Update complete! '${projectName}' app is running with the latest image." -ForegroundColor Green
Write-Host "Database data is untouched." -ForegroundColor White
Write-Host "Visit http://${DROPLET_IP} to verify." -ForegroundColor White
