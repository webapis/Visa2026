#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  [ON-PREM WINDOWS SERVER] Install and start OpenSSH Server (sshd) for remote administration.

.DESCRIPTION
  Fixes the common case where OpenSSH.Server capability is Installed but only client
  binaries exist under C:\Windows\System32\OpenSSH (no sshd.exe / no sshd service).

  Steps:
  1. Ensure OpenSSH.Server Windows capability (optional repair: remove + re-add).
  2. If sshd service is still missing, install from Win32-OpenSSH release zip.
  3. Open Windows Firewall for the SSH port.
  4. Start sshd and set startup type to Automatic.
  5. Print service status and verify TCP listen on the SSH port.

  Run on the server (RDP/console) or copy to the server and execute locally.
  Not for DigitalOcean droplets (Linux) - see droplet-scripts/.

.PARAMETER Port
  TCP port for sshd (default 22).

.PARAMETER WorkDirectory
  Folder used to download/extract Win32-OpenSSH when needed (default C:\OpenSSH-Setup).

.PARAMETER ZipPath
  Path to a pre-downloaded OpenSSH-Win64.zip (offline / no GitHub access on server).
  Skips Invoke-WebRequest when the file exists.

.PARAMETER SkipCapabilityRepair
  Do not touch the OpenSSH.Server Windows capability (recommended when sshd.exe is missing).

.PARAMETER ForceCapabilityRepair
  Try remove/re-add OpenSSH.Server capability. May fail with "Element not found" on some servers; Win32 zip install is used anyway.

.PARAMETER SkipFirewall
  Do not create the inbound firewall rule.

.PARAMETER OpenSshDownloadUrl
  Override download URL for OpenSSH-Win64.zip.

.EXAMPLE
  .\scripts\on-prem\Install-WindowsOpenSshServer.ps1

.EXAMPLE
  .\scripts\on-prem\Install-WindowsOpenSshServer.ps1 -ZipPath C:\Temp\OpenSSH-Win64.zip
#>
[CmdletBinding()]
param(
    [int]$Port = 22,
    [string]$WorkDirectory = 'C:\OpenSSH-Setup',
    [string]$ZipPath = '',
    [switch]$SkipCapabilityRepair,
    [switch]$ForceCapabilityRepair,
    [switch]$SkipFirewall,
    [string]$OpenSshDownloadUrl = 'https://github.com/PowerShell/Win32-OpenSSH/releases/latest/download/OpenSSH-Win64.zip'
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host ('==> ' + $Message) -ForegroundColor Cyan
}

function Test-SshdListening {
    param([int]$LocalPort)
    $conn = Get-NetTCPConnection -LocalPort $LocalPort -State Listen -ErrorAction SilentlyContinue
    return ($null -ne $conn -and $conn.Count -gt 0)
}

function Get-OpenSshServerCapability {
    Get-WindowsCapability -Online | Where-Object { $_.Name -like 'OpenSSH.Server*' } | Select-Object -First 1
}

function Repair-OpenSshServerCapability {
    $cap = Get-OpenSshServerCapability
    if (-not $cap) {
        Write-Step 'Installing OpenSSH.Server Windows capability'
        Add-WindowsCapability -Online -Name 'OpenSSH.Server~~~~0.0.1.0' | Out-Null
        return
    }

    if ($cap.State -eq 'Installed') {
        Write-Host 'OpenSSH.Server capability already Installed.'
        if (Test-Path -LiteralPath 'C:\Windows\System32\OpenSSH\sshd.exe') {
            return
        }
        Write-Host 'Installed but sshd.exe is missing; skipping DISM remove/re-add (use Win32-OpenSSH install below).'
        if (-not $ForceCapabilityRepair) {
            return
        }
        Write-Step 'ForceCapabilityRepair: removing OpenSSH.Server capability'
        try {
            Remove-WindowsCapability -Online -Name 'OpenSSH.Server~~~~0.0.1.0' | Out-Null
        }
        catch {
            Write-Warning ('Remove-WindowsCapability failed (continuing with Win32 zip): ' + $_.Exception.Message)
            return
        }
        Write-Step 'Re-adding OpenSSH.Server capability'
        Add-WindowsCapability -Online -Name 'OpenSSH.Server~~~~0.0.1.0' | Out-Null
        return
    }

    Write-Step 'Installing OpenSSH.Server Windows capability'
    Add-WindowsCapability -Online -Name 'OpenSSH.Server~~~~0.0.1.0' | Out-Null
}

function Install-OpenSshFromWin32Zip {
    param(
        [string]$WorkDir,
        [string]$LocalZipPath,
        [string]$DownloadUrl
    )

    New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null

    $zip = $LocalZipPath
    if ([string]::IsNullOrWhiteSpace($zip)) {
        $zip = Join-Path $WorkDir 'OpenSSH-Win64.zip'
    }
    elseif (-not (Test-Path -LiteralPath $zip)) {
        throw ('ZipPath not found: ' + $zip)
    }

    if (-not (Test-Path -LiteralPath $zip)) {
        Write-Step ('Downloading Win32-OpenSSH from ' + $DownloadUrl)
        try {
            Invoke-WebRequest -Uri $DownloadUrl -OutFile $zip -UseBasicParsing
        }
        catch {
            $msg = $_.Exception.Message
            throw @"
Failed to download OpenSSH-Win64.zip: $msg

Copy the zip manually to the server, then rerun with:
  -ZipPath C:\Path\To\OpenSSH-Win64.zip
"@
        }
    }
    else {
        Write-Host ('Using zip: ' + $zip)
    }

    Write-Step 'Extracting OpenSSH-Win64.zip'
    Expand-Archive -Path $zip -DestinationPath $WorkDir -Force

    $extractedDir = Join-Path $WorkDir 'OpenSSH-Win64'
    if (-not (Test-Path -LiteralPath $extractedDir)) {
        throw ('Expected folder not found after extract: ' + $extractedDir)
    }

    $installScript = Join-Path $extractedDir 'install-sshd.ps1'
    $sshdExe = Join-Path $extractedDir 'sshd.exe'
    if (-not (Test-Path -LiteralPath $installScript)) {
        throw ('install-sshd.ps1 not found in ' + $extractedDir)
    }
    if (-not (Test-Path -LiteralPath $sshdExe)) {
        throw ('sshd.exe not found in ' + $extractedDir)
    }

    Write-Step 'Running install-sshd.ps1 (registers sshd Windows service)'
    Push-Location $extractedDir
    try {
        & $installScript
        if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
            throw ('install-sshd.ps1 exited with code ' + $LASTEXITCODE)
        }
    }
    finally {
        Pop-Location
    }
}

function Ensure-SshFirewallRule {
    param([int]$LocalPort)

    $ruleName = 'OpenSSH-Server-In-TCP'
    $existing = Get-NetFirewallRule -Name $ruleName -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host ('Firewall rule already exists: ' + $ruleName + ' Enabled=' + $existing.Enabled)
        return
    }

    Write-Step ('Creating firewall rule ' + $ruleName + ' for TCP ' + $LocalPort)
    New-NetFirewallRule -Name $ruleName -DisplayName 'OpenSSH Server (sshd)' `
        -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort $LocalPort | Out-Null
}

function Start-SshdService {
    $svc = Get-Service -Name 'sshd' -ErrorAction SilentlyContinue
    if (-not $svc) {
        throw 'sshd service not found after install. Check install-sshd.ps1 output.'
    }

    Write-Step 'Starting sshd service'
    if ($svc.Status -ne 'Running') {
        Start-Service sshd
    }
    Set-Service -Name sshd -StartupType Automatic
    Get-Service sshd | Format-Table Status, Name, StartType -AutoSize
}

function Set-OpenSshDefaultShell {
    $shellPath = 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    $keyPath = 'HKLM:\SOFTWARE\OpenSSH'
    if (-not (Test-Path -LiteralPath $keyPath)) {
        New-Item -Path $keyPath -Force | Out-Null
    }
    New-ItemProperty -Path $keyPath -Name 'DefaultShell' -Value $shellPath -PropertyType String -Force | Out-Null
    Write-Host ('DefaultShell set to ' + $shellPath)
    Restart-Service sshd
}

Write-Step 'OpenSSH Server setup (on-prem Windows Server)'

if (-not $SkipCapabilityRepair) {
    Repair-OpenSshServerCapability
}
else {
    Write-Host 'SkipCapabilityRepair: skipping Windows capability steps.'
}

$sshdService = Get-Service -Name 'sshd' -ErrorAction SilentlyContinue
if (-not $sshdService) {
    Write-Step 'sshd service missing; installing from Win32-OpenSSH package'
    Install-OpenSshFromWin32Zip -WorkDir $WorkDirectory -LocalZipPath $ZipPath -DownloadUrl $OpenSshDownloadUrl
}
else {
    Write-Host 'sshd Windows service already registered.'
}

if (-not $SkipFirewall) {
    Ensure-SshFirewallRule -LocalPort $Port
}

Start-SshdService
Set-OpenSshDefaultShell

Write-Step 'Verification'
if (Test-SshdListening -LocalPort $Port) {
    Write-Host ('OK: Port ' + $Port + ' is listening.') -ForegroundColor Green
    Get-NetTCPConnection -LocalPort $Port -State Listen | Select-Object -First 3 LocalAddress, LocalPort, State
}
else {
    Write-Warning ('sshd is running but port ' + $Port + ' is not in Listen state yet.')
    Write-Host ('  Get-NetTCPConnection -LocalPort ' + $Port + ' -State Listen')
}

Write-Host ''
Write-Host 'From another machine on the network:' -ForegroundColor Yellow
Write-Host ('  Test-NetConnection -ComputerName <server-ip> -Port ' + $Port)
Write-Host '  ssh <username>@<server-ip>'
Write-Host ''
Write-Host 'Next: WSL 2 + Docker Engine (see docs/ENVIRONMENTS.md).' -ForegroundColor DarkGray
