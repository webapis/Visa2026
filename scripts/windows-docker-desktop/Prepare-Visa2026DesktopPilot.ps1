#Requires -Version 5.1
[CmdletBinding()]
param(
  [string] $TargetDir = 'C:\visa2026-pilot',
  [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
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
  $envBody = Get-Content -Raw $envExample
  $envBody = $envBody -replace 'DB_NAME=visa2026_prod', 'DB_NAME=visa2026_pilot'
  $envBody = $envBody -replace 'APP_PORT=80', 'APP_PORT=9080'
  [System.IO.File]::WriteAllText($envDest, $envBody, $utf8)
  Write-Host "Created $envDest - edit PG_PASSWORD, DEVEXPRESS_LICENSEKEY, APP_IMAGE_TAG"
}
else {
  Write-Host "Keeping existing $envDest"
}

$readme = "Visa2026 Docker Desktop PILOT`r`nProject: visa2026-pilot`r`nFolder: $TargetDir`r`nLast known good APP_IMAGE_TAG: (set after first successful up)`r`nChecklist: docs/windows-docker-desktop/PILOT_CHECKLIST.md`r`n"
[System.IO.File]::WriteAllText((Join-Path $TargetDir 'README.txt'), $readme, $utf8)

Write-Host "Pilot folder ready: $TargetDir"
Write-Host 'Next: edit .env.prod, then follow PILOT_CHECKLIST.md'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
  Write-Warning 'docker not on PATH. Install/start Docker Desktop before compose up.'
}