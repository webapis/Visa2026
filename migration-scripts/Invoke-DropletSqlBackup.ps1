<#
Creates a SQL Server .bak on the droplet host by SSH'ing in and running migration-scripts/droplet-backup.sh.

Typical usage (prod):
  .\migration-scripts\Invoke-DropletSqlBackup.ps1 -Environment prod

Notes:
- This does NOT download the backup to Windows. Use download-prod-backup.ps1 next.
- SA_PASSWORD is read from the droplet's env file (.env.prod / .env.dev) on the droplet.
#>

param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",

    [string]$DropletIp = "167.172.177.93",
    [string]$RemoteUser = "root",
    [string]$RemoteDir = "~/visa2026",
    [string]$IdentityFile = "$env:USERPROFILE\.ssh\id_ed25519_visa",

    # Optional overrides if your droplet uses non-default names.
    [string]$SqlContainerName,
    [string]$DatabaseName,

    # Output path on the droplet host filesystem (under RemoteDir).
    [string]$RemoteBackupFile
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $IdentityFile)) {
    Write-Host "ERROR: SSH identity file not found: $IdentityFile" -ForegroundColor Red
    exit 1
}

$key = (Resolve-Path -LiteralPath $IdentityFile).Path

if ([string]::IsNullOrWhiteSpace($SqlContainerName)) {
    $SqlContainerName = if ($Environment -eq "prod") { "visa2026-prod-sqlserver-1" } else { "visa2026-dev-sqlserver-1" }
}

if ([string]::IsNullOrWhiteSpace($DatabaseName)) {
    $DatabaseName = if ($Environment -eq "prod") { "Visa2026DbProd" } else { "Visa2026DbDev" }
}

if ([string]::IsNullOrWhiteSpace($RemoteBackupFile)) {
    # Use a relative path because the bash helper quotes args (tilde would not expand).
    $RemoteBackupFile = if ($Environment -eq "prod") { "./visa2026-prod.bak" } else { "./visa2026-dev.bak" }
}

Write-Host "Droplet: ${RemoteUser}@${DropletIp}" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "SQL container: $SqlContainerName" -ForegroundColor Cyan
Write-Host "Database: $DatabaseName" -ForegroundColor Cyan
Write-Host "Remote output (relative to ${RemoteDir}): $RemoteBackupFile" -ForegroundColor Cyan

$envFile = if ($Environment -eq "prod") { ".env.prod" } else { ".env.dev" }
$remote = "${RemoteUser}@${DropletIp}"
$sshArgs = @(
    "-i", $key,
    "-o", "IdentitiesOnly=yes",
    "-o", "StrictHostKeyChecking=accept-new"
)

# Read SA_PASSWORD from the droplet env file without printing it.
$cmdTemplate = @'
set -euo pipefail
cd __REMOTE_DIR__
if [ ! -f "__ENV_FILE__" ]; then
  echo "ERROR: Missing __REMOTE_DIR__/__ENV_FILE__" >&2
  exit 1
fi
SA_PASSWORD=$(grep -E '^SA_PASSWORD=' "__ENV_FILE__" | head -n 1 | cut -d '=' -f2-)
if [ -z "${SA_PASSWORD}" ]; then
  echo "ERROR: SA_PASSWORD not found in __ENV_FILE__" >&2
  exit 1
fi
export SA_PASSWORD
if [ -f "./migration-scripts/droplet-backup.sh" ]; then
  chmod +x ./migration-scripts/droplet-backup.sh 2>/dev/null || true
  ./migration-scripts/droplet-backup.sh "__SQL_CONTAINER__" "__DB_NAME__" "__REMOTE_BAK__"
else
  echo "NOTE: ./migration-scripts/droplet-backup.sh not found on droplet - running inline backup."
  CONTAINER="__SQL_CONTAINER__"
  DATABASE="__DB_NAME__"
  HOST_OUT="__REMOTE_BAK__"
  CONTAINER_BAK="/var/opt/mssql/visa2026-backup-temp.bak"
  docker exec "${CONTAINER}" /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -C -U sa -P "${SA_PASSWORD}" -Q "BACKUP DATABASE [${DATABASE}] TO DISK = N'${CONTAINER_BAK}' WITH INIT, FORMAT"
  docker cp "${CONTAINER}:${CONTAINER_BAK}" "${HOST_OUT}"
  echo "Done: ${HOST_OUT}"
fi
'@

$cmd = $cmdTemplate
$cmd = $cmd.Replace("__REMOTE_DIR__", $RemoteDir)
$cmd = $cmd.Replace("__ENV_FILE__", $envFile)
$cmd = $cmd.Replace("__SQL_CONTAINER__", $SqlContainerName)
$cmd = $cmd.Replace("__DB_NAME__", $DatabaseName)
$cmd = $cmd.Replace("__REMOTE_BAK__", $RemoteBackupFile)
$cmd = $cmd -replace "`r", ""

# Run under bash explicitly by sending the script over stdin.
# This avoids brittle quoting issues when the script contains quotes/newlines.
$sshExe = "ssh"
$sshArgList = @()
$sshArgList += $sshArgs
$sshArgList += $remote
$sshArgList += "bash -s"

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $sshExe
$psi.Arguments = ($sshArgList | ForEach-Object { $_.ToString() } | ForEach-Object { if ($_ -match '\s') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } }) -join ' '
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true

$p = New-Object System.Diagnostics.Process
$p.StartInfo = $psi

if (-not $p.Start()) {
    Write-Host "ERROR: Failed to start ssh process." -ForegroundColor Red
    exit 1
}

# Write as UTF-8 without BOM to avoid shell parsing issues.
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$bytes = $utf8NoBom.GetBytes($cmd)
$p.StandardInput.BaseStream.Write($bytes, 0, $bytes.Length)
$p.StandardInput.Close()

$stdout = $p.StandardOutput.ReadToEnd()
$stderr = $p.StandardError.ReadToEnd()
$p.WaitForExit()

if ($stdout) { Write-Host $stdout }
if ($stderr) { Write-Host $stderr }

$global:LASTEXITCODE = $p.ExitCode

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Backup failed on droplet." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Backup created on droplet: $RemoteBackupFile" -ForegroundColor Green

