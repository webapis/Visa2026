#Requires -Version 5.1
<#
.SYNOPSIS
  [DROPLET] Create a SQL backup for production on the droplet (from Windows).

.DESCRIPTION
  Uploads droplet-scripts/backup-db.sh (if needed), then runs it over SSH inside ~/visa2026.
  Optionally downloads the resulting .bak to a local folder.

.PARAMETER Environment
  prod or dev (default: prod).

.PARAMETER IdentityFile
  Optional SSH private key path.

.PARAMETER DownloadTo
  If provided, downloads the resulting .bak to this local folder (created if missing).
#>
[CmdletBinding()]
param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",
    [string]$IdentityFile,
    [string]$DownloadTo = ""
)

$ErrorActionPreference = "Stop"

# --- CONFIGURATION (match droplet-scripts/update-app.ps1) ---
$DROPLET_IP = "167.172.177.93"
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
$LOCAL_REPO = Split-Path -Parent $PSScriptRoot
# -----------------------------------------------------------

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
    Write-Host "ERROR: Cannot reach ${DROPLET_IP} on port 22 (SSH)." -ForegroundColor Red
    exit 1
}

$backupScriptLocal = Join-Path $LOCAL_REPO "droplet-scripts\backup-db.sh"
if (-not (Test-Path -LiteralPath $backupScriptLocal)) {
    Write-Host "ERROR: Missing backup script: $backupScriptLocal" -ForegroundColor Red
    exit 1
}

Write-Host "1. Uploading backup script..." -ForegroundColor Cyan
scp @SshKeyArgs $backupScriptLocal "${REMOTE_USER}@${DROPLET_IP}:${REMOTE_DIR}/backup-db.sh"
if ($LASTEXITCODE -ne 0) { throw "Failed to upload backup-db.sh (exit $LASTEXITCODE)." }
ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" "chmod +x ${REMOTE_DIR}/backup-db.sh"
if ($LASTEXITCODE -ne 0) { throw "Failed to chmod backup-db.sh (exit $LASTEXITCODE)." }

Write-Host "2. Running backup on droplet ($Environment)..." -ForegroundColor Yellow
$remoteCmd = "cd ${REMOTE_DIR} && ./backup-db.sh ${Environment}"
$backupPath = (ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" $remoteCmd | Select-Object -Last 1).Trim()
if ($LASTEXITCODE -ne 0) { throw "Remote backup failed (exit $LASTEXITCODE)." }
if ([string]::IsNullOrWhiteSpace($backupPath) -or -not $backupPath.StartsWith("/")) {
    throw "Could not parse backup path from output: '$backupPath'"
}

Write-Host ""
Write-Host "Backup created on droplet: $backupPath" -ForegroundColor Green

if (-not [string]::IsNullOrWhiteSpace($DownloadTo)) {
    $dest = if ([System.IO.Path]::IsPathRooted($DownloadTo)) { $DownloadTo } else { Join-Path (Get-Location) $DownloadTo }
    if (-not (Test-Path -LiteralPath $dest)) {
        New-Item -ItemType Directory -Path $dest | Out-Null
    }
    $fileName = Split-Path -Leaf $backupPath
    $localPath = Join-Path $dest $fileName

    Write-Host ""
    Write-Host "3. Downloading backup to: $localPath" -ForegroundColor Cyan
    scp @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}:${backupPath}" $localPath
    if ($LASTEXITCODE -ne 0) { throw "Failed to download backup (exit $LASTEXITCODE)." }
    Write-Host "Download complete." -ForegroundColor Green
}

