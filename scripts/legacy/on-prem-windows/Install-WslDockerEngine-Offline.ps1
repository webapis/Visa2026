#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  [ON-PREM] Install Docker Engine in WSL from local .deb files (offline / air-gapped).

.DESCRIPTION
  Installs Docker from a folder of .deb packages (no apt download during install).
  Prepare packages on an internet-connected machine — see reference-docker-offline-install.md.

.PARAMETER DebDirectory
  Windows path containing *.deb files (default C:\WslDocker-Setup\debs).

.PARAMETER DistroName
  WSL distro name (default Ubuntu).

.PARAMETER HelloWorldTar
  Optional path to hello-world.tar from "docker save" for offline smoke test.

.EXAMPLE
  .\Install-WslDockerEngine-Offline.ps1

.EXAMPLE
  .\Install-WslDockerEngine-Offline.ps1 -DebDirectory D:\transfer\docker-debs -HelloWorldTar C:\WslDocker-Setup\hello-world.tar
#>
[CmdletBinding()]
param(
    [string]$DebDirectory = 'C:\WslDocker-Setup\debs',
    [string]$DistroName = 'Ubuntu',
    [string]$HelloWorldTar = ''
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host ('==> ' + $Message) -ForegroundColor Cyan
}

if (-not (Test-Path -LiteralPath $DebDirectory)) {
    throw @"
Deb folder not found: $DebDirectory

Prepare packages on a machine with internet (same Ubuntu version as this server's WSL).
See: scripts\on-prem\reference-docker-offline-install.md
"@
}

$debs = @(Get-ChildItem -LiteralPath $DebDirectory -Filter '*.deb' -File)
if ($debs.Count -eq 0) {
    throw ('No .deb files in ' + $DebDirectory)
}

Write-Step 'Offline Docker install (WSL)'
Write-Host ('Deb packages: ' + $debs.Count + ' in ' + $DebDirectory)

if ($DebDirectory -notmatch '^([A-Za-z]):\\(.*)$') {
    throw 'DebDirectory must be a Windows path like C:\WslDocker-Setup\debs'
}
$wslDebDir = '/mnt/' + $Matches[1].ToLower() + '/' + ($Matches[2] -replace '\\', '/')

Write-Step 'Check systemd in WSL'
$sys = @(& wsl.exe -d $DistroName -u root -- systemctl is-system-running 2>&1 | ForEach-Object { "$_" })
Write-Host ('systemctl: ' + ($sys -join ' '))
if ($sys -notmatch 'running|degraded') {
    Write-Warning 'systemd may not be running. Enable it first (see Install-WslDockerEngine.ps1 / reference-docker-offline-install.md).'
}

Write-Step 'Installing .deb packages (dpkg)'
$bash = 'set -e; cd ' + $wslDebDir + '; echo "==> dpkg -i *.deb"; dpkg -i *.deb; systemctl enable docker; systemctl start docker; docker --version; docker compose version'
& wsl.exe -d $DistroName -u root -- bash -lc $bash
if ($LASTEXITCODE -ne 0) {
    throw 'dpkg/docker install failed in WSL. Check missing dependencies in reference-docker-offline-install.md'
}

if (-not [string]::IsNullOrWhiteSpace($HelloWorldTar) -and (Test-Path -LiteralPath $HelloWorldTar)) {
    if ($HelloWorldTar -match '^([A-Za-z]):\\(.*)$') {
        $tarWsl = '/mnt/' + $Matches[1].ToLower() + '/' + ($Matches[2] -replace '\\', '/')
        Write-Step 'Load hello-world image from tar'
        & wsl.exe -d $DistroName -u root -- docker load -i $tarWsl
        & wsl.exe -d $DistroName -u root -- docker run --rm hello-world
    }
}
else {
    Write-Host ''
    Write-Host 'Skip hello-world (no -HelloWorldTar). Test when online: wsl -d Ubuntu -u root -- docker run --rm hello-world' -ForegroundColor Yellow
}

Write-Host ''
Write-Host 'Offline Docker install finished.' -ForegroundColor Green
Write-Host 'Verify: wsl -d Ubuntu -u root -- docker ps'
