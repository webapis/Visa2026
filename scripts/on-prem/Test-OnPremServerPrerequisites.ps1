#Requires -Version 5.1
<#
.SYNOPSIS
  [ON-PREM WINDOWS SERVER] Check host prerequisites before Visa2026 WSL/Docker deploy.

.DESCRIPTION
  Read-only report: OS, RAM, CPU, disk, OpenSSH/sshd, WSL, Docker in WSL, C:\visa2026 files.
  Does not install anything. Run on the server (Administrator recommended).

.PARAMETER ServerIp
  Optional: also run Test-NetConnection to port 22 from this machine (use from admin PC).

.PARAMETER MinRamGb
  Minimum RAM warning threshold (default 8).

.PARAMETER MinFreeDiskGb
  Minimum free disk GB on system drive (default 100).

.EXAMPLE
  .\Test-OnPremServerPrerequisites.ps1

.EXAMPLE
  .\Test-OnPremServerPrerequisites.ps1 -ServerIp 10.100.128.25
#>
[CmdletBinding()]
param(
    [string]$ServerIp = '',
    [int]$MinRamGb = 8,
    [int]$MinFreeDiskGb = 100,
    [string]$DeployRoot = 'C:\visa2026',
    [string]$DistroName = 'Ubuntu'
)

$ErrorActionPreference = 'Continue'
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

function Write-CheckResult {
    param(
        [string]$Name,
        [ValidateSet('PASS', 'WARN', 'FAIL', 'INFO')]
        [string]$Status,
        [string]$Detail
    )
    $color = switch ($Status) {
        'PASS' { 'Green' }
        'WARN' { 'Yellow' }
        'FAIL' { 'Red' }
        'INFO' { 'Gray' }
    }
    Write-Host ('[{0}] {1}' -f $Status, $Name) -ForegroundColor $color
    if ($Detail) { Write-Host ('      ' + $Detail) }
}

$failCount = 0
$warnCount = 0

Write-Host ''
Write-Host '=== Visa2026 on-prem prerequisite check ===' -ForegroundColor Cyan
Write-Host ''

# OS
$os = Get-CimInstance Win32_OperatingSystem
$ramGb = [math]::Round($os.TotalVisibleMemorySize / 1MB, 1)
$cpu = (Get-CimInstance Win32_ComputerSystem).NumberOfLogicalProcessors
Write-CheckResult -Name 'OS' -Status 'INFO' -Detail ($os.Caption + ' (' + $os.Version + ')')
if ($ramGb -ge $MinRamGb) {
    Write-CheckResult -Name 'RAM' -Status 'PASS' -Detail ($ramGb.ToString() + ' GB visible (min ' + $MinRamGb + ' GB)')
}
else {
    Write-CheckResult -Name 'RAM' -Status 'WARN' -Detail ($ramGb.ToString() + ' GB visible; recommend ' + $MinRamGb + '+ GB')
    $warnCount++
}
if ($cpu -ge 2) {
    Write-CheckResult -Name 'CPU (logical)' -Status 'PASS' -Detail ($cpu.ToString() + ' processors (min 2)')
}
else {
    Write-CheckResult -Name 'CPU (logical)' -Status 'WARN' -Detail ($cpu.ToString() + ' processors; recommend 4')
    $warnCount++
}

$sysDrive = $env:SystemDrive
$disk = Get-CimInstance Win32_LogicalDisk -Filter ("DeviceID='" + $sysDrive + "'")
if ($disk) {
    $freeGb = [math]::Round($disk.FreeSpace / 1GB, 1)
    if ($freeGb -ge $MinFreeDiskGb) {
        Write-CheckResult -Name ('Disk free ' + $sysDrive) -Status 'PASS' -Detail ($freeGb.ToString() + ' GB free (min ' + $MinFreeDiskGb + ' GB)')
    }
    else {
        Write-CheckResult -Name ('Disk free ' + $sysDrive) -Status 'WARN' -Detail ($freeGb.ToString() + ' GB free; recommend ' + $MinFreeDiskGb + '+ GB')
        $warnCount++
    }
}

# Admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) {
    Write-CheckResult -Name 'Administrator PowerShell' -Status 'PASS' -Detail 'Current session is elevated'
}
else {
    Write-CheckResult -Name 'Administrator PowerShell' -Status 'WARN' -Detail 'Re-run as Administrator for install scripts'
    $warnCount++
}

# OpenSSH / sshd
$sshd = Get-Service -Name 'sshd' -ErrorAction SilentlyContinue
if ($sshd -and $sshd.Status -eq 'Running') {
    Write-CheckResult -Name 'sshd service' -Status 'PASS' -Detail 'Running, StartType=' + $sshd.StartType
    $listen22 = Get-NetTCPConnection -LocalPort 22 -State Listen -ErrorAction SilentlyContinue
    if ($listen22) {
        Write-CheckResult -Name 'TCP 22 listen' -Status 'PASS' -Detail 'Port 22 is listening'
    }
    else {
        Write-CheckResult -Name 'TCP 22 listen' -Status 'WARN' -Detail 'sshd running but port 22 not in Listen state'
        $warnCount++
    }
}
else {
    Write-CheckResult -Name 'sshd service' -Status 'FAIL' -Detail 'Not running — run Install-WindowsOpenSshServer.ps1'
    $failCount++
}

# WSL
if (Get-Command wsl.exe -ErrorAction SilentlyContinue) {
    $wslStatus = @(wsl.exe --status 2>&1 | ForEach-Object { "$_" }) -join ' '
    if ($wslStatus -match 'WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED') {
        Write-CheckResult -Name 'WSL component' -Status 'FAIL' -Detail 'Run: wsl.exe --install --no-distribution, then reboot'
        $failCount++
    }
    else {
        Write-CheckResult -Name 'WSL component' -Status 'PASS' -Detail 'wsl --status OK'
    }
    $distroLines = @(wsl.exe -l -v 2>&1 | ForEach-Object { "$_" })
    $hasDistro = $false
    foreach ($line in $distroLines) {
        if ($line -match '^\s*\*?\s*(\S+)\s+(Running|Stopped)') {
            $n = $Matches[1]
            if ($n -ne 'NAME' -and $n -notlike '---*') {
                $hasDistro = $true
                if ($n -eq $DistroName -or $n -like 'Ubuntu*') {
                    Write-CheckResult -Name ('WSL distro ' + $n) -Status 'PASS' -Detail $line.Trim()
                }
            }
        }
    }
    if (-not $hasDistro) {
        Write-CheckResult -Name 'WSL distro' -Status 'FAIL' -Detail 'No distro — wsl --install Ubuntu or Install-WslDockerEngine.ps1'
        $failCount++
    }
}
else {
    Write-CheckResult -Name 'wsl.exe' -Status 'FAIL' -Detail 'Not found'
    $failCount++
}

# Docker in WSL
if ($hasDistro) {
    $dockerVer = @(wsl.exe -d $DistroName -u root -- docker --version 2>&1 | ForEach-Object { "$_" }) -join ' '
    if ($LASTEXITCODE -eq 0 -and $dockerVer -match 'Docker version') {
        Write-CheckResult -Name 'Docker in WSL' -Status 'PASS' -Detail $dockerVer.Trim()
        $composeVer = @(wsl.exe -d $DistroName -u root -- docker compose version 2>&1 | ForEach-Object { "$_" }) -join ' '
        if ($LASTEXITCODE -eq 0) {
            Write-CheckResult -Name 'Docker Compose plugin' -Status 'PASS' -Detail $composeVer.Trim()
        }
        else {
            Write-CheckResult -Name 'Docker Compose plugin' -Status 'FAIL' -Detail 'Missing — run Install-WslDockerEngine.ps1'
            $failCount++
        }
    }
    else {
        Write-CheckResult -Name 'Docker in WSL' -Status 'FAIL' -Detail 'Not installed — run Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig (if Ubuntu ready)'
        $failCount++
    }
}

# Deploy files
$composePath = Join-Path $DeployRoot 'docker-compose.prod.yml'
$envPath = Join-Path $DeployRoot '.env.prod'
if (Test-Path -LiteralPath $composePath) {
    Write-CheckResult -Name 'docker-compose.prod.yml' -Status 'PASS' -Detail $composePath
}
else {
    Write-CheckResult -Name 'docker-compose.prod.yml' -Status 'WARN' -Detail ('Missing: ' + $composePath)
    $warnCount++
}
if (Test-Path -LiteralPath $envPath) {
    Write-CheckResult -Name '.env.prod' -Status 'PASS' -Detail $envPath
}
else {
    Write-CheckResult -Name '.env.prod' -Status 'WARN' -Detail ('Missing: ' + $envPath + ' (copy from .env.prod.example)')
    $warnCount++
}

# Optional: test SSH from this PC
if (-not [string]::IsNullOrWhiteSpace($ServerIp)) {
    Write-Host ''
    Write-Host ('=== From this PC to ' + $ServerIp + ' ===') -ForegroundColor Cyan
    $t = Test-NetConnection -ComputerName $ServerIp -Port 22 -WarningAction SilentlyContinue
    if ($t.TcpTestSucceeded) {
        Write-CheckResult -Name 'PC -> server TCP 22' -Status 'PASS' -Detail 'TcpTestSucceeded'
    }
    else {
        Write-CheckResult -Name 'PC -> server TCP 22' -Status 'FAIL' -Detail 'Cannot reach SSH from this PC (firewall/LAN/ssh not listening)'
        $failCount++
    }
}

Write-Host ''
Write-Host ('Summary: FAIL=' + $failCount + ' WARN=' + $warnCount) -ForegroundColor Cyan
if ($failCount -eq 0) {
    Write-Host 'Prerequisite phase: ready for next step (SSH remote test, Docker, or compose).' -ForegroundColor Green
}
else {
    Write-Host 'Fix FAIL items before compose. See docs/ON_PREM_WINDOWS_SERVER.md and learnings.md.' -ForegroundColor Yellow
}
Write-Host ''

if ($failCount -gt 0) { exit 1 }
exit 0
