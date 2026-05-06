<#
End-to-end convenience wrapper:
  1) Backup DB on droplet (over SSH) to ~/visa2026/*.bak
  2) Download the .bak to this machine
  3) Restore into local dev SQL container

Typical usage:
  .\migration-scripts\Mirror-DropletDbToLocal.ps1 -Environment prod

This script intentionally uses LOCAL SA_PASSWORD (from .env.dev) for restore.
#>

param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",

    [string]$DropletIp = "167.172.177.93",
    [string]$RemoteUser = "root",
    [string]$RemoteDir = "~/visa2026",
    [string]$IdentityFile = "$env:USERPROFILE\.ssh\id_ed25519_visa",

    # Local output file (default in repo root for convenience).
    [string]$LocalBackupFile,

    # Optional overrides
    [string]$SqlContainerName,
    [string]$DatabaseName,

    # Local restore target (default dev stack)
    [string]$LocalEnvFile = ".env.dev",
    [string]$LocalComposeFile = "docker-compose.dev.yml",
    [string]$LocalComposeProject = "visa2026-dev",
    [string]$LocalRestoreDbName
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

if ([string]::IsNullOrWhiteSpace($LocalBackupFile)) {
    $LocalBackupFile = if ($Environment -eq "prod") { ".\visa2026-prod.bak" } else { ".\visa2026-dev.bak" }
}

$remoteBackupFile = if ($Environment -eq "prod") { "./visa2026-prod.bak" } else { "./visa2026-dev.bak" }
$remoteBackupPath = if ($Environment -eq "prod") { "${RemoteDir}/visa2026-prod.bak" } else { "${RemoteDir}/visa2026-dev.bak" }

Write-Host "Step 1/3: Backup on droplet..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "Invoke-DropletSqlBackup.ps1") `
    -Environment $Environment `
    -DropletIp $DropletIp `
    -RemoteUser $RemoteUser `
    -RemoteDir $RemoteDir `
    -IdentityFile $IdentityFile `
    -SqlContainerName $SqlContainerName `
    -DatabaseName $DatabaseName `
    -RemoteBackupFile $remoteBackupFile

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Step 2/3: Download backup to Windows..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "download-prod-backup.ps1") `
    -DropletIp $DropletIp `
    -RemoteUser $RemoteUser `
    -IdentityFile $IdentityFile `
    -RemotePath $remoteBackupPath `
    -LocalPath $LocalBackupFile

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Step 3/3: Restore into local Docker SQL..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "Restore-BackupToLocalSql.ps1") `
    -BackupFile $LocalBackupFile `
    -ComposeProject $LocalComposeProject `
    -ComposeFile $LocalComposeFile `
    -EnvFile $LocalEnvFile `
    -DatabaseName $LocalRestoreDbName

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. Local database is now mirrored from droplet backup." -ForegroundColor Green

