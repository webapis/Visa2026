<#
Unattended wrapper: mirror production droplet DB into local Docker SQL.

Runs (in order):
  1) Enable-VisaSshAgent.ps1 (non-interactive if VISA_SSH_KEY_PASSPHRASE is set)
  2) Mirror-DropletDbToLocal.ps1 -Environment prod

Prereqs:
- Docker running locally
- .env.dev exists and has SA_PASSWORD (local restore password)
- VISA_SSH_KEY_PASSPHRASE set in Windows environment (User or Machine) for zero prompts
#>

param(
    [string]$PassphraseEnvVar = "VISA_SSH_KEY_PASSPHRASE"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$passphrase = [Environment]::GetEnvironmentVariable($PassphraseEnvVar, "User")
if ([string]::IsNullOrWhiteSpace($passphrase)) {
    $passphrase = [Environment]::GetEnvironmentVariable($PassphraseEnvVar, "Machine")
}

if ([string]::IsNullOrWhiteSpace($passphrase)) {
    Write-Host "ERROR: Missing env var '$PassphraseEnvVar'." -ForegroundColor Red
    Write-Host "Set it for unattended runs, then open a new PowerShell:" -ForegroundColor Yellow
    Write-Host "  setx $PassphraseEnvVar \"your-passphrase\"" -ForegroundColor Yellow
    exit 1
}

Write-Host "1/2: Loading SSH key into ssh-agent (unattended)..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "Enable-VisaSshAgent.ps1") -PassphraseEnvVar $PassphraseEnvVar
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "2/2: Mirroring droplet prod DB to local..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "Mirror-DropletDbToLocal.ps1") -Environment prod
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done." -ForegroundColor Green

