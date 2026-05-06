<#
Ensures Windows OpenSSH ssh-agent is running and loads the Visa2026 SSH key.

Why:
- id_ed25519_visa is passphrase-protected, so unattended ssh/scp will prompt unless the key is loaded in an agent.

Typical usage:
  .\migration-scripts\Enable-VisaSshAgent.ps1
  .\migration-scripts\Mirror-DropletDbToLocal.ps1 -Environment prod

Notes:
- You may be prompted for the key passphrase once per Windows session (or until the agent is restarted).
- This script does not print the passphrase; it only invokes ssh-add.
#>

param(
    [string]$IdentityFile = "$env:USERPROFILE\.ssh\id_ed25519_visa",

    # Optional: if set, the script will try to load the key non-interactively.
    # Example (PowerShell): setx VISA_SSH_KEY_PASSPHRASE "your-passphrase"
    [string]$PassphraseEnvVar = "VISA_SSH_KEY_PASSPHRASE"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $IdentityFile)) {
    Write-Host "ERROR: SSH identity file not found: $IdentityFile" -ForegroundColor Red
    exit 1
}

Write-Host "Ensuring ssh-agent is running..." -ForegroundColor Cyan

try {
    $svc = Get-Service -Name "ssh-agent" -ErrorAction Stop
} catch {
    Write-Host "ERROR: OpenSSH ssh-agent service not found. Install 'OpenSSH Client' in Windows optional features." -ForegroundColor Red
    exit 1
}

if ($svc.StartType -eq "Disabled") {
    Write-Host "ssh-agent is disabled. Enabling it (requires admin privileges)..." -ForegroundColor Yellow
    try {
        Set-Service -Name "ssh-agent" -StartupType Manual -ErrorAction Stop
    } catch {
        Write-Host "ERROR: Could not enable ssh-agent. Re-run PowerShell as Administrator." -ForegroundColor Red
        exit 1
    }
}

if ($svc.Status -ne "Running") {
    try {
        Start-Service -Name "ssh-agent" -ErrorAction Stop
    } catch {
        Write-Host "ERROR: Could not start ssh-agent. Re-run PowerShell as Administrator." -ForegroundColor Red
        exit 1
    }
}

$keyPath = (Resolve-Path -LiteralPath $IdentityFile).Path
Write-Host "Loading key into ssh-agent: $keyPath" -ForegroundColor Cyan

$passphrase = [Environment]::GetEnvironmentVariable($PassphraseEnvVar, "User")
if ([string]::IsNullOrWhiteSpace($passphrase)) {
    $passphrase = [Environment]::GetEnvironmentVariable($PassphraseEnvVar, "Machine")
}

if ([string]::IsNullOrWhiteSpace($passphrase)) {
    Write-Host "If prompted, enter the key passphrase." -ForegroundColor Yellow
    ssh-add $keyPath
} else {
    Write-Host "Passphrase found in env var '$PassphraseEnvVar'. Loading key non-interactively..." -ForegroundColor Yellow
    Write-Host "Security note: storing SSH passphrases in environment variables is risky; prefer interactive entry where possible." -ForegroundColor Yellow

    # Use SSH_ASKPASS to provide the passphrase without printing it.
    # This pattern works when ssh-add requires a tty by forcing askpass mode.
    $tmp = [IO.Path]::GetTempPath()
    $askpassPath = Join-Path $tmp ("visa-ssh-askpass-{0}.cmd" -f ([Guid]::NewGuid().ToString("N")))
    try {
        # cmd file that prints the passphrase to stdout (ssh-add reads it via SSH_ASKPASS).
        # Do not write the passphrase to the console.
        Set-Content -LiteralPath $askpassPath -Encoding Ascii -Value @(
            "@echo off"
            "echo $passphrase"
        )

        $oldAskPass = $env:SSH_ASKPASS
        $oldAskPassRequire = $env:SSH_ASKPASS_REQUIRE
        $oldDisplay = $env:DISPLAY

        $env:SSH_ASKPASS = $askpassPath
        $env:SSH_ASKPASS_REQUIRE = "force"
        $env:DISPLAY = "1"

        # Redirect stdin from NUL so ssh-add is forced to use SSH_ASKPASS.
        cmd /c "ssh-add ""$keyPath"" < NUL"
        $global:LASTEXITCODE = $LASTEXITCODE

        $env:SSH_ASKPASS = $oldAskPass
        $env:SSH_ASKPASS_REQUIRE = $oldAskPassRequire
        $env:DISPLAY = $oldDisplay
    } finally {
        Remove-Item -LiteralPath $askpassPath -Force -ErrorAction SilentlyContinue
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: ssh-add failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Key loaded. You can now run droplet scripts without repeated passphrase prompts." -ForegroundColor Green

