#Requires -Version 5.1
<#
.SYNOPSIS
  [ON-PREM WINDOWS SERVER] Check host prerequisites before Visa2026 WSL/Docker deploy.

.DESCRIPTION
  Read-only report against docs/ON_PREM_PREREQUISITES.md: OS, RAM, CPU, disk, sshd (optional),
  WSL 2, Ubuntu, systemd, Docker (optional), C:\visa2026 files. visa2026-windows-server-setup skill.

.PARAMETER ServerIp
  Optional: also run Test-NetConnection to port 22 from this machine (use from admin PC).

.PARAMETER MinRamGb
  Minimum RAM warning threshold (default 8).

.PARAMETER MinFreeDiskGb
  Minimum free disk GB on system drive (default 100).

.PARAMETER RequireDocker
  When set, missing Docker/Compose in WSL is FAIL (use before compose). Default: WARN only (WSL-bootstrap phase).

.PARAMETER RequireDeployFiles
  When set, missing C:\visa2026 compose/env files is FAIL. Default: WARN only.

.EXAMPLE
  .\Test-OnPremServerPrerequisites.ps1

.EXAMPLE
  .\Test-OnPremServerPrerequisites.ps1 -ServerIp 10.100.128.25

.EXAMPLE
  .\Test-OnPremServerPrerequisites.ps1 -RequireDocker -RequireDeployFiles
#>
[CmdletBinding()]
param(
    [string]$ServerIp = '',
    [int]$MinRamGb = 8,
    [int]$MinFreeDiskGb = 100,
    [string]$DeployRoot = 'C:\visa2026',
    [string]$DistroName = 'Ubuntu',
    [switch]$RequireDocker,
    [switch]$RequireDeployFiles
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
    Write-CheckResult -Name 'sshd service' -Status 'WARN' -Detail 'Not running — optional; setup-openssh-server / Install-WindowsOpenSshServer.ps1'
    $warnCount++
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
                    $st = $Matches[2]
                    if ($st -eq 'Running') {
                        Write-CheckResult -Name ('WSL distro ' + $n) -Status 'PASS' -Detail $line.Trim()
                    }
                    else {
                        Write-CheckResult -Name ('WSL distro ' + $n) -Status 'WARN' -Detail ($line.Trim() + ' — set .wslconfig vmIdleTimeout=-1; start WSL before compose')
                        $warnCount++
                    }
                    if ($line -notmatch '\s2\s*$') {
                        Write-CheckResult -Name ('WSL version ' + $n) -Status 'WARN' -Detail 'Expect VERSION 2 (WSL 1 not supported for Docker)'
                        $warnCount++
                    }
                }
            }
        }
    }
    if (-not $hasDistro) {
        Write-CheckResult -Name 'WSL distro' -Status 'FAIL' -Detail 'No distro — Windows server prep skill (wsl --install Ubuntu)'
        $failCount++
    }
}
else {
    Write-CheckResult -Name 'wsl.exe' -Status 'FAIL' -Detail 'Not found'
    $failCount++
}

# systemd + Docker in WSL
if ($hasDistro) {
    $sysd = @(wsl.exe -d $DistroName -u root -- systemctl is-system-running 2>&1 | ForEach-Object { "$_" }) -join ' '
    if ($LASTEXITCODE -eq 0 -and $sysd -match 'running|degraded') {
        Write-CheckResult -Name 'systemd in WSL' -Status 'PASS' -Detail $sysd.Trim()
    }
    else {
        Write-CheckResult -Name 'systemd in WSL' -Status 'FAIL' -Detail 'Not running — Install-WslDockerEngine.ps1 (enable systemd in /etc/wsl.conf)'
        $failCount++
    }

    $dockerVer = @(wsl.exe -d $DistroName -u root -- docker --version 2>&1 | ForEach-Object { "$_" }) -join ' '
    if ($LASTEXITCODE -eq 0 -and $dockerVer -match 'Docker version') {
        Write-CheckResult -Name 'Docker in WSL' -Status 'PASS' -Detail $dockerVer.Trim()
        $composeVer = @(wsl.exe -d $DistroName -u root -- docker compose version 2>&1 | ForEach-Object { "$_" }) -join ' '
        if ($LASTEXITCODE -eq 0) {
            Write-CheckResult -Name 'Docker Compose plugin' -Status 'PASS' -Detail $composeVer.Trim()
        }
        else {
            $st = if ($RequireDocker) { 'FAIL' } else { 'WARN' }
            Write-CheckResult -Name 'Docker Compose plugin' -Status $st -Detail 'Missing — setup-docker-engine skill after WSL prep'
            if ($RequireDocker) { $failCount++ } else { $warnCount++ }
        }
    }
    else {
        $st = if ($RequireDocker) { 'FAIL' } else { 'WARN' }
        Write-CheckResult -Name 'Docker in WSL' -Status $st -Detail 'Not installed yet — setup-docker-engine skill (after this skill completes WSL)'
        if ($RequireDocker) { $failCount++ } else { $warnCount++ }
    }
}

# Deploy files
$composePath = Join-Path $DeployRoot 'docker-compose.prod.yml'
$envPath = Join-Path $DeployRoot '.env.prod'
if (Test-Path -LiteralPath $composePath) {
    Write-CheckResult -Name 'docker-compose.prod.yml' -Status 'PASS' -Detail $composePath
}
else {
    $st = if ($RequireDeployFiles) { 'FAIL' } else { 'WARN' }
    Write-CheckResult -Name 'docker-compose.prod.yml' -Status $st -Detail ('Missing: ' + $composePath)
    if ($RequireDeployFiles) { $failCount++ } else { $warnCount++ }
}
if (Test-Path -LiteralPath $envPath) {
    Write-CheckResult -Name '.env.prod' -Status 'PASS' -Detail $envPath
}
else {
    $st = if ($RequireDeployFiles) { 'FAIL' } else { 'WARN' }
    Write-CheckResult -Name '.env.prod' -Status $st -Detail ('Missing: ' + $envPath + ' (copy from .env.prod.example)')
    if ($RequireDeployFiles) { $failCount++ } else { $warnCount++ }
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
    Write-Host 'Host/WSL prerequisite phase: OK for next skill (setup-docker-engine or optional SSH).' -ForegroundColor Green
}
else {
    Write-Host 'Fix FAIL items. See visa2026-windows-server-setup skill and docs/ON_PREM_WINDOWS_SERVER.md.' -ForegroundColor Yellow
}
Write-Host ''

if ($failCount -gt 0) { exit 1 }
exit 0
