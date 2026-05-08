#Requires -Version 5.1
<#
.SYNOPSIS
  [DROPLET] Post-deploy health check for Visa2026 on the droplet (from Windows).

.DESCRIPTION
  Runs a small set of SSH commands to verify the droplet deployment is healthy:
  - docker compose ps for the target environment (prod/dev)
  - curl localhost on the published APP_PORT (default 80 for prod)
  - tail app container logs (first-line triage)
  - show the image name configured on the running app container

  This script is read-only (no docker compose up/down) and is intended to be run
  immediately after droplet-scripts/update-prod.ps1 or update-app.ps1.

.PARAMETER Environment
  prod or dev (default: prod).

.PARAMETER IdentityFile
  Optional SSH private key path (fixes "Permission denied (publickey)").

.PARAMETER CurlPath
  Path to request on the app (default: /).

.PARAMETER ExpectedHttpCodes
  Acceptable status codes. Default: 200, 302, 303, 307, 308, 401, 403.

.PARAMETER Tail
  How many log lines to tail (default: 200).
#>
[CmdletBinding()]
param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",
    [string]$IdentityFile,
    [string]$CurlPath = "/",
    [int[]]$ExpectedHttpCodes = @(200, 302, 303, 307, 308, 401, 403),
    [int]$Tail = 200
)

$ErrorActionPreference = "Stop"

# --- CONFIGURATION (match droplet-scripts/update-app.ps1) ---
$DROPLET_IP = "167.172.177.93"
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
# -----------------------------------------------------------

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
$defaultPort = if ($Environment -eq "prod") { 80 } else { 8081 }

Write-Host "0. Pre-flight connectivity check..." -ForegroundColor Cyan
$test = Test-NetConnection -ComputerName $DROPLET_IP -Port 22 -WarningAction SilentlyContinue
if (-not $test.TcpTestSucceeded) {
    Write-Host "ERROR: Cannot reach ${DROPLET_IP} on port 22 (SSH)." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Target: $Environment ($projectName)" -ForegroundColor Cyan
Write-Host "Droplet: ${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}" -ForegroundColor DarkGray

Write-Host ""
Write-Host "1. docker compose ps" -ForegroundColor Cyan
$psCmd = "cd ${REMOTE_DIR} && docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} ps"
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" $psCmd
if ($LASTEXITCODE -ne 0) { throw "SSH compose ps failed (exit $LASTEXITCODE)." }

Write-Host ""
Write-Host "2. Discover app container name" -ForegroundColor Cyan
$nameCmd = "docker ps --format '{{.Names}}' | grep '^${projectName}-app-' | head -n 1"
$appContainer = (ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" $nameCmd | Select-Object -First 1).Trim()
if ($LASTEXITCODE -ne 0) { throw "SSH container discovery failed (exit $LASTEXITCODE)." }
if ([string]::IsNullOrWhiteSpace($appContainer)) {
    throw "Could not find a running app container for project '$projectName' (expected '${projectName}-app-*')."
}
Write-Host "App container: $appContainer" -ForegroundColor Gray

Write-Host ""
Write-Host "3. HTTP check (loopback on droplet)" -ForegroundColor Cyan
$codeCmd = "PORT=\${APP_PORT:-$defaultPort}; curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:\$PORT$CurlPath || true"
$httpCodeRaw = (ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "cd ${REMOTE_DIR} && set -a && . ./${envFile} >/dev/null 2>&1 || true; set +a; $codeCmd").Trim()
if ($httpCodeRaw -notmatch '^\d{3}$') {
    Write-Host "WARN: Could not parse HTTP status code output: '$httpCodeRaw'" -ForegroundColor Yellow
} else {
    $httpCode = [int]$httpCodeRaw
    if ($ExpectedHttpCodes -contains $httpCode) {
        Write-Host "HTTP OK: $httpCode" -ForegroundColor Green
    } else {
        Write-Host "HTTP UNEXPECTED: $httpCode (expected one of: $($ExpectedHttpCodes -join ', '))" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "4. App logs (tail $Tail)" -ForegroundColor Cyan
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "docker logs $appContainer --tail $Tail"
if ($LASTEXITCODE -ne 0) { throw "SSH docker logs failed (exit $LASTEXITCODE)." }

Write-Host ""
Write-Host "5. App image reference" -ForegroundColor Cyan
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "docker inspect --format='{{.Config.Image}}' $appContainer"
if ($LASTEXITCODE -ne 0) { throw "SSH docker inspect failed (exit $LASTEXITCODE)." }

Write-Host ""
Write-Host "Health check complete." -ForegroundColor Green

