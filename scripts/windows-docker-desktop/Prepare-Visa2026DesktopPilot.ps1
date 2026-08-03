#Requires -Version 5.1
<#
.SYNOPSIS
  Copy docker-compose.prod.yml (+ HTTPS helpers) and a starter .env.prod to a client-style folder.

.EXAMPLE
  .\Prepare-Visa2026DesktopPilot.ps1
  .\Prepare-Visa2026DesktopPilot.ps1 -TargetDir 'E:\visa2026-staging' -ProjectName visa2026-staging -DbName visa2026_staging_docker -AppPort 9080 -PgHostPort 5434
#>
[CmdletBinding()]
param(
  [string] $TargetDir = 'C:\visa2026-pilot',
  [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
  [string] $ProjectName = 'visa2026-pilot',
  [string] $DbName = 'visa2026_pilot',
  [int] $AppPort = 9080,
  [int] $PgHostPort = 5433,
  [string] $AppImageTag = '1.0.0.644'
)

$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding $false

New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $TargetDir 'backups') | Out-Null

$composeSrc = Join-Path $RepoRoot 'docker-compose.prod.yml'
$envExample = Join-Path $RepoRoot '.env.prod.example'
$httpsDir = Join-Path $RepoRoot 'docs\windows-docker-desktop'

if (-not (Test-Path $composeSrc)) { throw "Missing $composeSrc" }

Copy-Item -Force $composeSrc (Join-Path $TargetDir 'docker-compose.prod.yml')
Copy-Item -Force (Join-Path $httpsDir 'docker-compose.https.override.yml') (Join-Path $TargetDir 'docker-compose.https.override.yml')
Copy-Item -Force (Join-Path $httpsDir 'Caddyfile.example') (Join-Path $TargetDir 'Caddyfile.example')

$envDest = Join-Path $TargetDir '.env.prod'
if (-not (Test-Path $envDest)) {
  $envBody = @"
PG_PASSWORD=CHANGE_ME_STRONG_PASSWORD
PG_USER=postgres
DEVEXPRESS_LICENSEKEY=CHANGE_ME_LICENSE_KEY
APP_PORT=$AppPort
DB_NAME=$DbName
APP_IMAGE_TAG=$AppImageTag
IMPORTER_IMAGE_TAG=latest
PG_HOST_PORT=$PgHostPort
FORCE_XAF_DB_UPDATE=true
"@
  [System.IO.File]::WriteAllText($envDest, $envBody.TrimEnd() + "`n", $utf8)
  Write-Host "Created $envDest - edit PG_PASSWORD and DEVEXPRESS_LICENSEKEY"
}
else {
  Write-Host "Keeping existing $envDest"
}

$readme = @"
Visa2026 Docker Desktop layout
Project: $ProjectName
Folder: $TargetDir
APP_IMAGE_TAG default: $AppImageTag
Compose: docker compose -p $ProjectName --env-file .env.prod -f docker-compose.prod.yml up -d
Checklist: docs/windows-docker-desktop/PILOT_CHECKLIST.md
"@
[System.IO.File]::WriteAllText((Join-Path $TargetDir 'README.txt'), $readme.TrimEnd() + "`n", $utf8)

Write-Host "Folder ready: $TargetDir"
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
  Write-Warning 'docker not on PATH. Install/start Docker Desktop before compose up.'
}