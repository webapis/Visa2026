# Visa2026 convenience wrapper: update PROD on droplet using the visa SSH key.

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

& .\update-app.ps1 -Environment prod -IdentityFile "C:\Users\webap\.ssh\id_ed25519_visa"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Running post-deploy health check..." -ForegroundColor Cyan
& .\Test-DropletProdHealth.ps1 -Environment prod -IdentityFile "C:\Users\webap\.ssh\id_ed25519_visa"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

