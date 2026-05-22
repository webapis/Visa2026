#Requires -Version 5.1
<#
.SYNOPSIS
  [DROPLET] Fix prod schema drift for PdfGenerationBatch.RequestedCulture.

.DESCRIPTION
  Follows visa2026-droplet-prod-deploy skill for SqlException Invalid column name 'RequestedCulture':
  1) One-off XAF DB update (--updateDatabase --forceUpdate --silent) on the prod app image.
  2) Force-recreate the app container.
  3) Disable FORCE_XAF_DB_UPDATE (if left on from a prior attempt).
  4) Verify logs contain zero 'Invalid column' errors.

  Requires SSH key with access to the droplet (passphrase prompt is normal).

.EXAMPLE
  .\droplet-scripts\prod-deploy\Fix-ProdRequestedCultureSchema.ps1 -IdentityFile "C:\Users\webap\.ssh\id_ed25519_visa"
#>
[CmdletBinding()]
param(
    [string]$IdentityFile = "C:\Users\webap\.ssh\id_ed25519_visa"
)

$ErrorActionPreference = "Stop"

$LOCAL_REPO = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$enableAgent = Join-Path $LOCAL_REPO "migration-scripts\Enable-VisaSshAgent.ps1"
if (Test-Path -LiteralPath $enableAgent) {
    Write-Host "=== 0. Load SSH key (VISA_SSH_KEY_PASSPHRASE / ssh-agent) ===" -ForegroundColor Cyan
    if ($IdentityFile) {
        & $enableAgent -IdentityFile $IdentityFile
    } else {
        & $enableAgent
    }
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$DROPLET_IP = "167.172.177.93"
$REMOTE_USER = "root"
$REMOTE_DIR = "~/visa2026"
$projectName = "visa2026-prod"
$composeFile = "docker-compose.prod.yml"
$envFile = ".env.prod"

$SshKeyArgs = @()
if (-not [string]::IsNullOrWhiteSpace($IdentityFile)) {
    $keyPath = (Resolve-Path -LiteralPath $IdentityFile).Path
    $SshKeyArgs = @("-i", $keyPath)
    $ScpKeyArgs = @("-i", $keyPath)
}

function Invoke-Ssh([string]$RemoteCommand) {
    & ssh @SshKeyArgs "${REMOTE_USER}@${DROPLET_IP}" $RemoteCommand
    if ($LASTEXITCODE -ne 0) {
        throw "SSH failed (exit $LASTEXITCODE): $RemoteCommand"
    }
}

Write-Host "=== 1. Remote env (FORCE_XAF / APP_IMAGE_TAG) ===" -ForegroundColor Cyan
Invoke-Ssh "grep -E 'FORCE_XAF|APP_IMAGE_TAG' ${REMOTE_DIR}/${envFile} || true"

Write-Host "`n=== 2. One-off XAF database update (adds missing EF columns) ===" -ForegroundColor Cyan
Write-Host "This may take several minutes..." -ForegroundColor Yellow
$updateCmd = @"
cd ${REMOTE_DIR} && docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} run --rm --no-deps app --updateDatabase --forceUpdate --silent
"@
Invoke-Ssh $updateCmd

Write-Host "`n=== 3. Recreate app container ===" -ForegroundColor Cyan
Invoke-Ssh "cd ${REMOTE_DIR} && docker compose -p ${projectName} --env-file ${envFile} -f ${composeFile} up -d --force-recreate --no-deps app"

Write-Host "`n=== 4. Disable FORCE_XAF_DB_UPDATE locally and on droplet ===" -ForegroundColor Cyan
& (Join-Path $LOCAL_REPO "droplet-scripts\Set-ForceXafDbUpdate.ps1") -Disable -Environment prod -IdentityFile $IdentityFile

Write-Host "`n=== 5. Verify (expect Invalid column count = 0) ===" -ForegroundColor Cyan
Start-Sleep -Seconds 15
Invoke-Ssh "docker logs visa2026-prod-app-1 2>&1 | grep -c 'Invalid column' || true"
Invoke-Ssh "docker compose -p ${projectName} --env-file ${REMOTE_DIR}/${envFile} -f ${REMOTE_DIR}/${composeFile} ps"

Write-Host "`nDone. Run Test-DropletProdHealth.ps1 for full checks." -ForegroundColor Green
