#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  [ON-PREM WINDOWS SERVER] Install WSL 2 (Ubuntu) and Docker Engine for Visa2026 Linux containers.

.DESCRIPTION
  Prepares Windows Server for docker-compose.prod.yml (Linux app + Linux SQL Server images).

  Steps:
  1. Install WSL 2 + Ubuntu if missing (may require one reboot).
  2. Enable systemd in WSL (/etc/wsl.conf) and restart WSL.
  3. Install Docker Engine + Compose plugin inside Ubuntu (official Docker apt repo).
  4. Verify: docker run hello-world, docker compose version.

  Full bootstrap (WSL/Ubuntu/systemd): Windows server prep skill. Docker-only: setup-docker-engine skill (-SkipWslInstall -SkipSystemdConfig).
  Not for DigitalOcean droplets (Linux).

.PARAMETER DistroName
  WSL distribution name (default Ubuntu). Must match "wsl -l -v".

.PARAMETER WorkDirectory
  Windows folder for helper scripts (default C:\WslDocker-Setup).

.PARAMETER SkipWslInstall
  Do not run "wsl --install" (WSL + distro already present).

.PARAMETER SkipSystemdConfig
  Do not write /etc/wsl.conf or run wsl --shutdown.

.PARAMETER SkipDockerInstall
  Only configure WSL/systemd; do not install Docker packages.

.EXAMPLE
  .\scripts\on-prem\Install-WslDockerEngine.ps1

.EXAMPLE
  # After first run asked for reboot:
  .\scripts\on-prem\Install-WslDockerEngine.ps1 -SkipWslInstall
#>
[CmdletBinding()]
param(
    [string]$DistroName = 'Ubuntu',
    [string]$WorkDirectory = 'C:\WslDocker-Setup',
    [switch]$SkipWslInstall,
    [switch]$SkipSystemdConfig,
    [switch]$SkipDockerInstall
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host ('==> ' + $Message) -ForegroundColor Cyan
}

function Invoke-WslExe {
    param(
        [string[]]$Args,
        [switch]$LiveOutput
    )
    if ($LiveOutput) {
        Invoke-WslExeLive -Args $Args | Out-Null
        return
    }
    $output = & wsl.exe @Args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ('wsl.exe failed (exit ' + $LASTEXITCODE + '): wsl ' + ($Args -join ' ') + "`n" + ($output | Out-String))
    }
    return $output
}

function Invoke-WslExeLive {
    param([string[]]$Args)
    & wsl.exe @Args 2>&1 | ForEach-Object {
        Write-Host $_
    }
    if ($LASTEXITCODE -ne 0) {
        throw ('wsl.exe failed (exit ' + $LASTEXITCODE + '): wsl ' + ($Args -join ' '))
    }
}

function Test-WslOptionalComponentInstalled {
    $status = @(& wsl.exe --status 2>&1 | ForEach-Object { "$_" })
    $text = ($status -join ' ')
    if ($text -match 'WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED' -or $text -match 'Windows Subsystem for Linux Optional Component') {
        return $false
    }
    return ($LASTEXITCODE -eq 0)
}

function Install-WslOptionalComponent {
    Write-Step 'Installing WSL optional component (no distro yet)'
    Write-Host 'Command: wsl.exe --install --no-distribution'
    Write-Host 'A reboot is usually required before installing Ubuntu and Docker.'

    & wsl.exe --install --no-distribution 2>&1 | ForEach-Object { Write-Host $_ }

    if ($LASTEXITCODE -ne 0) {
        Write-Host 'wsl --install --no-distribution failed; trying Enable-WindowsOptionalFeature (may need reboot).'
        $features = @(
            'Microsoft-Windows-Subsystem-Linux',
            'VirtualMachinePlatform'
        )
        foreach ($feature in $features) {
            try {
                Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart | Out-Null
                Write-Host ('Enabled feature: ' + $feature)
            }
            catch {
                Write-Warning ('Enable-WindowsOptionalFeature ' + $feature + ' failed: ' + $_.Exception.Message)
            }
        }
    }

    throw @"
WSL optional component install started.

REBOOT the server, then run:
  wsl --status
  .\Install-WslDockerEngine.ps1

Do not use -SkipWslInstall on the first run after reboot.
"@
}

function Get-WslDistroNames {
    # Prefer "wsl -l -v" parsing; "wsl -l -q" can return UTF-16-like spacing in Windows PowerShell.
    $names = @()
    $verbose = @(& wsl.exe -l -v 2>&1 | ForEach-Object { "$_" })
    foreach ($line in $verbose) {
        if ($line -match '^\s*\*?\s*(\S+)\s+(Running|Stopped|Installing)\s+') {
            $candidate = $Matches[1]
            if ($candidate -ne 'NAME' -and $candidate -notlike '---*') {
                $names += $candidate
            }
        }
    }
    if ($names.Count -gt 0) {
        return $names
    }

    $raw = @(& wsl.exe -l -q 2>&1 | ForEach-Object { "$_" })
    foreach ($line in $raw) {
        $n = $line.Trim().Trim([char]0xFEFF).Trim([char]0x00)
        if ([string]::IsNullOrWhiteSpace($n)) { continue }
        if ($n -eq 'Windows' -or $n -eq 'NAME') { continue }
        $names += $n
    }
    return $names
}

function Test-WslDistroExists {
    param([string]$Name)
    $distros = Get-WslDistroNames
    foreach ($d in $distros) {
        if ($d -eq $Name) { return $true }
    }
    return $false
}

function Resolve-WslDistroName {
    param([string]$Preferred)
    $all = @(Get-WslDistroNames)
    if ($all.Count -eq 0) {
        return $null
    }
    if (-not [string]::IsNullOrWhiteSpace($Preferred) -and ($all -contains $Preferred)) {
        return $Preferred
    }
    foreach ($d in $all) {
        if ($d -like 'Ubuntu*') {
            Write-Host ('Using WSL distro: ' + $d + ' (Ubuntu family)')
            return $d
        }
    }
    Write-Host ('Using WSL distro: ' + $all[0])
    return $all[0]
}

function Install-WslUbuntuIfNeeded {
    if ($SkipWslInstall) {
        Write-Host 'SkipWslInstall: not running wsl --install.'
        return
    }

    if (Test-WslDistroExists -Name $DistroName) {
        Write-Host ('WSL distro already present: ' + $DistroName)
        return
    }

    Write-Step ('Installing WSL 2 and distro: ' + $DistroName)
    Write-Host 'This can take several minutes. A reboot may be required before Docker install completes.'

    $installArgs = @('--install', '--distribution', $DistroName, '--no-launch')
    & wsl.exe @installArgs 2>&1 | ForEach-Object { Write-Host $_ }

    if ($LASTEXITCODE -ne 0) {
        Write-Host 'wsl --install --distribution --no-launch failed; trying without --no-launch.'
        & wsl.exe --install --distribution $DistroName 2>&1 | ForEach-Object { Write-Host $_ }
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Trying wsl --install (default distro).'
        & wsl.exe --install 2>&1 | ForEach-Object { Write-Host $_ }
    }

    if (-not (Test-WslDistroExists -Name $DistroName)) {
        $available = Get-WslDistroNames
        if ($available.Count -gt 0) {
            Write-Host ('Distros registered: ' + ($available -join ', '))
            if ($available -notcontains $DistroName) {
                Write-Host ('Hint: rerun with -DistroName ' + $available[0])
            }
        }

        throw @"
WSL distro '$DistroName' is not available yet.

If this was the first wsl --install on this server:
  1. Reboot the server.
  2. Rerun: .\Install-WslDockerEngine.ps1 -SkipWslInstall

See: https://learn.microsoft.com/en-us/windows/wsl/install-on-server
"@
    }
}

function Enable-WslSystemd {
    Write-Step 'Enabling systemd in WSL (/etc/wsl.conf)'
    $bash = @'
set -e
if [ -f /etc/wsl.conf ] && grep -q '^systemd=true' /etc/wsl.conf 2>/dev/null; then
  echo 'systemd already enabled in /etc/wsl.conf'
  exit 0
fi
printf '%s\n' '[boot]' 'systemd=true' > /etc/wsl.conf
cat /etc/wsl.conf
'@
    Invoke-WslExe -Args @('-d', $DistroName, '-u', 'root', '--', 'bash', '-c', $bash)

    Write-Step 'Restarting WSL (wsl --shutdown)'
    & wsl.exe --shutdown 2>&1 | Out-Null
    Start-Sleep -Seconds 5

    $status = Invoke-WslExe -Args @('-d', $DistroName, '-u', 'root', '--', 'systemctl', 'is-system-running')
    Write-Host ('systemctl is-system-running: ' + ($status | Out-String).Trim())
}

function Install-DockerEngineInWsl {
    Write-Step 'Installing Docker Engine inside WSL (Ubuntu)'
    Write-Host 'This can take 10-20+ minutes. Progress lines appear below as the Linux script runs.'

    New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
    $linuxScript = Join-Path $WorkDirectory 'install-docker-engine.sh'
    if ($WorkDirectory -match '^([A-Za-z]):\\(.*)$') {
        $drive = $Matches[1].ToLower()
        $rest = $Matches[2] -replace '\\', '/'
        $wslScriptPath = '/mnt/' + $drive + '/' + $rest + '/install-docker-engine.sh'
    }
    else {
        throw ('WorkDirectory must be a Windows path like C:\WslDocker-Setup')
    }

    $bashScript = @'
#!/bin/bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive
LOG=/var/log/visa-docker-install.log
echo "==> visa-docker-install $(date -Iseconds)" | tee "$LOG"

if command -v docker >/dev/null 2>&1; then
  echo "==> Docker already installed: $(docker --version)"
  systemctl enable docker
  systemctl start docker
  docker compose version
  docker run --rm hello-world
  exit 0
fi

echo "==> apt-get update (1/4) — may take several minutes on first run" | tee -a "$LOG"
apt-get update 2>&1 | tee -a "$LOG"
echo "==> apt-get install ca-certificates curl" | tee -a "$LOG"
apt-get install -y ca-certificates curl 2>&1 | tee -a "$LOG"

echo "==> Docker apt repository setup" | tee -a "$LOG"
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc

. /etc/os-release
tee /etc/apt/sources.list.d/docker.sources <<EOF
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: ${UBUNTU_CODENAME:-$VERSION_CODENAME}
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF
echo "==> wrote /etc/apt/sources.list.d/docker.sources" | tee -a "$LOG"

echo "==> apt-get update (2/4)" | tee -a "$LOG"
apt-get update 2>&1 | tee -a "$LOG"
echo "==> apt-get install docker packages (3/4) — largest step" | tee -a "$LOG"
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin 2>&1 | tee -a "$LOG"

echo "==> systemctl enable/start docker" | tee -a "$LOG"
systemctl enable docker 2>&1 | tee -a "$LOG"
systemctl start docker 2>&1 | tee -a "$LOG"

docker --version 2>&1 | tee -a "$LOG"
docker compose version 2>&1 | tee -a "$LOG"
echo "==> docker run hello-world (4/4)" | tee -a "$LOG"
docker run --rm hello-world 2>&1 | tee -a "$LOG"
echo "==> Docker install finished OK" | tee -a "$LOG"
'@
    $bashScript = $bashScript -replace "`r`n", "`n"
    [System.IO.File]::WriteAllText($linuxScript, $bashScript, (New-Object System.Text.UTF8Encoding $false))

    Write-Host ('Running: bash ' + $wslScriptPath)
    Write-Host ('Live log (optional second window): wsl -d ' + $DistroName + ' -u root -- tail -f /var/log/visa-docker-install.log') -ForegroundColor Yellow
    Invoke-WslExe -Args @('-d', $DistroName, '-u', 'root', '--', 'bash', $wslScriptPath) -LiveOutput
}

function Show-WslDockerSummary {
    Write-Step 'Summary'
    & wsl.exe -l -v 2>&1 | ForEach-Object { Write-Host $_ }
    Write-Host ''
    Invoke-WslExe -Args @('-d', $DistroName, '-u', 'root', '--', 'docker', 'info', '--format', '{{.OSType}}/{{.Architecture}} - {{.ServerVersion}}')
    Write-Host ''
    Write-Host 'Deploy Visa2026 from Windows path via WSL, e.g.:' -ForegroundColor Yellow
    Write-Host '  mkdir C:\visa2026'
    Write-Host '  copy docker-compose.prod.yml and .env.prod to C:\visa2026'
    Write-Host ('  wsl -d ' + $DistroName + ' -e bash -c "cd /mnt/c/visa2026 && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d"')
    Write-Host ''
    Write-Host 'See docs/ENVIRONMENTS.md and docker-compose.prod.yml.' -ForegroundColor DarkGray
}

# --- Main ---
Write-Step 'WSL 2 + Docker Engine setup (on-prem Windows Server)'

if (-not (Get-Command wsl.exe -ErrorAction SilentlyContinue)) {
    throw 'wsl.exe not found. Install WSL on Windows Server first (Server 2022: wsl --install).'
}

if (-not (Test-WslOptionalComponentInstalled)) {
    if ($SkipWslInstall) {
        throw @"
WSL optional component is not installed (wsl --status reports WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED).

Run WITHOUT -SkipWslInstall:
  .\Install-WslDockerEngine.ps1

Or manually:
  wsl.exe --install --no-distribution
Then reboot and rerun this script.
"@
    }
    Install-WslOptionalComponent
}

if (-not $SkipWslInstall) {
    Install-WslUbuntuIfNeeded
}

$names = @(Get-WslDistroNames)
if ($names.Count -eq 0) {
    if ($SkipWslInstall) {
        throw @"
No WSL Linux distribution is installed.

You used -SkipWslInstall, but this server has no WSL distro yet.

Run once WITHOUT -SkipWslInstall (may require reboot):
  .\Install-WslDockerEngine.ps1

Then check:
  wsl -l -v
"@
    }
    throw 'No WSL distro found after wsl --install. Reboot the server, then rerun: .\Install-WslDockerEngine.ps1 -SkipWslInstall'
}

$resolvedDistro = Resolve-WslDistroName -Preferred $DistroName
if (-not $resolvedDistro) {
    throw ('Could not resolve WSL distro. Listed: ' + ($names -join ', '))
}
if ($resolvedDistro -ne $DistroName) {
    Write-Host ('DistroName parameter "' + $DistroName + '" adjusted to "' + $resolvedDistro + '"')
}
$DistroName = $resolvedDistro

if (-not $SkipSystemdConfig) {
    Enable-WslSystemd
}

if (-not $SkipDockerInstall) {
    Install-DockerEngineInWsl
}

Show-WslDockerSummary
