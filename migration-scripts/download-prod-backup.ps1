# Download a .bak (or any file) from the Visa2026 droplet via scp.
# Uses the same SSH key pattern as droplet-scripts/update-prod.ps1.

param(
    [string]$RemotePath = "~/visa2026/visa2026-prod.bak",
    [string]$LocalPath = "./visa2026-prod.bak",
    [string]$IdentityFile = "$env:USERPROFILE\.ssh\id_ed25519_visa",
    [string]$DropletIp = "167.172.177.93",
    [string]$RemoteUser = "root"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $IdentityFile)) {
    Write-Host "ERROR: SSH identity file not found: $IdentityFile" -ForegroundColor Red
    Write-Host "Override with -IdentityFile or create the key pair." -ForegroundColor Yellow
    exit 1
}

$key = (Resolve-Path -LiteralPath $IdentityFile).Path
$remote = "${RemoteUser}@${DropletIp}:${RemotePath}"

Write-Host "Downloading: $remote -> $LocalPath" -ForegroundColor Cyan
scp -i $key -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new $remote $LocalPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: scp failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Saved: $(Resolve-Path -LiteralPath $LocalPath)" -ForegroundColor Green
