#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  [ON-PREM WINDOWS SERVER] Repair OpenSSH Server when SSH connects then resets (common on admin accounts).

.DESCRIPTION
  Fixes common causes of "Connection reset by ... port 22" on Windows Server OpenSSH:

  1. Renames administrators_authorized_keys when present (bad ACLs break admin logins).
  2. Tightens ACLs on C:\ProgramData\ssh and ssh_host_* private keys.
  3. Ensures PasswordAuthentication yes in sshd_config; validates config with sshd -t.
  4. Verifies OpenSSH binaries (especially sshd-session.exe); can reinstall Win32-OpenSSH.
  5. Fixes .ssh folder ACLs for the test user.
  6. Restarts sshd, recovers port 22 listen (sshd -t + OpenSSH log if needed), handshake test.

  Run after Install-WindowsOpenSshServer.ps1 when ssh resets after accepting the host key.

.PARAMETER Port
  SSH port (default 22).

.PARAMETER SshConfigPath
  Path to sshd_config (default C:\ProgramData\ssh\sshd_config).

.PARAMETER KeepAdministratorsAuthorizedKeys
  Do not rename administrators_authorized_keys (fix ACLs only).

.PARAMETER DefaultShell
  Cmd, PowerShell, or NoChange (default Cmd).

.PARAMETER TestUser
  Windows user for handshake test (default: current user).

.PARAMETER SkipHandshakeTest
  Skip ssh BatchMode handshake test.

.PARAMETER ReinstallWin32OpenSsh
  Re-run Win32-OpenSSH zip install (fixes missing sshd-session.exe / broken partial install).

.PARAMETER ZipPath
  Path to OpenSSH-Win64.zip for -ReinstallWin32OpenSsh (optional).

.EXAMPLE
  .\Repair-WindowsOpenSshServer.ps1 -TestUser adm43419

.EXAMPLE
  .\Repair-WindowsOpenSshServer.ps1 -ReinstallWin32OpenSsh -TestUser adm43419
#>
[CmdletBinding()]
param(
    [int]$Port = 22,
    [string]$SshConfigPath = 'C:\ProgramData\ssh\sshd_config',
    [switch]$KeepAdministratorsAuthorizedKeys,
    [ValidateSet('Cmd', 'PowerShell', 'NoChange')]
    [string]$DefaultShell = 'Cmd',
    [string]$TestUser = '',
    [switch]$SkipHandshakeTest,
    [switch]$ReinstallWin32OpenSsh,
    [string]$ZipPath = ''
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

function Backup-FileIfExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backup = $Path + '.bak-' + $stamp
    Copy-Item -LiteralPath $Path -Destination $backup -Force
    Write-Host ('Backed up: ' + $Path + ' -> ' + $backup)
    return $backup
}

function Get-SshdServiceInfo {
    $svc = Get-CimInstance Win32_Service -Filter "Name='sshd'" -ErrorAction SilentlyContinue
    if (-not $svc) {
        return $null
    }
    $binPath = $svc.PathName
    $exePath = $binPath -replace '^"|"$', '' -replace '"\s+.*$', ''
    if ($exePath -match '^\s*"?([^"]+)"?') {
        $exePath = $Matches[1]
    }
    $installDir = Split-Path -Parent $exePath
    return [PSCustomObject]@{
        Status    = $svc.State
        PathName  = $binPath
        SshdExe   = $exePath
        InstallDir = $installDir
    }
}

function Get-OpenSshVersionLabel {
    param([string]$SshdExe)

    if (-not (Test-Path -LiteralPath $SshdExe)) {
        return $null
    }

    # Do NOT run sshd.exe with no args — it starts the server and blocks the console.
    $item = Get-Item -LiteralPath $SshdExe
    $vi = $item.VersionInfo
    if (-not [string]::IsNullOrWhiteSpace($vi.ProductVersion)) {
        return ('product:' + $vi.ProductVersion.Trim())
    }
    if (-not [string]::IsNullOrWhiteSpace($vi.FileVersion)) {
        return ('filever:' + $vi.FileVersion.Trim())
    }
    return ('mtime:' + $item.LastWriteTime.ToString('yyyy-MM-dd'))
}

function Test-OpenSshRequiresSessionBinary {
    param([string]$SshdExe)

    $label = Get-OpenSshVersionLabel -SshdExe $SshdExe
    if ($label -match '^mtime:(.+)') {
        $dt = Get-Date $Matches[1]
        # OpenSSH 9+ Windows builds are typically 2022+; May 2021 = 8.x (no sshd-session.exe).
        return ($dt -ge [datetime]'2022-06-01')
    }
    if ($label -match '(?:product:|filever:)\s*(\d+)') {
        $major = [int]$Matches[1]
        return ($major -ge 9)
    }
    return $false
}

function Test-OpenSshBinarySet {
    param(
        [string]$InstallDir,
        [string]$SshdExe = ''
    )

    if ([string]::IsNullOrWhiteSpace($SshdExe)) {
        $SshdExe = Join-Path $InstallDir 'sshd.exe'
    }
    $requireSession = Test-OpenSshRequiresSessionBinary -SshdExe $SshdExe

    $required = @(
        'sshd.exe',
        'ssh.exe',
        'sftp-server.exe'
    )
    if ($requireSession) {
        $required += 'sshd-session.exe'
    }

    $results = @()
    foreach ($name in $required) {
        $path = Join-Path $InstallDir $name
        $results += [PSCustomObject]@{
            Name   = $name
            Path   = $path
            Exists = (Test-Path -LiteralPath $path)
        }
    }
    return $results
}

function Show-OpenSshDiagnostics {
    Write-Step 'OpenSSH install diagnostics'

    Write-Host 'sshd service PathName (Win32_Service):' -ForegroundColor DarkGray
    $svcPath = Get-CimInstance Win32_Service -Filter "Name='sshd'" -ErrorAction SilentlyContinue |
        Select-Object Name, State, StartMode, PathName
    if ($svcPath) {
        $svcPath | Format-List
    }
    else {
        Write-Warning 'sshd service not registered in Win32_Service.'
    }

    Write-Host ''
    Write-Host 'C:\Windows\System32\OpenSSH\sshd*.exe' -ForegroundColor DarkGray
    $sys32 = @(Get-ChildItem 'C:\Windows\System32\OpenSSH\sshd*.exe' -ErrorAction SilentlyContinue)
    if ($sys32.Count -gt 0) {
        $sys32 | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
    }
    else {
        Write-Host '  (none found)'
    }

    Write-Host ''
    Write-Host 'C:\Program Files\OpenSSH\OpenSSH-Win64\sshd*.exe' -ForegroundColor DarkGray
    $progFiles = @(Get-ChildItem 'C:\Program Files\OpenSSH\OpenSSH-Win64\sshd*.exe' -ErrorAction SilentlyContinue)
    if ($progFiles.Count -gt 0) {
        $progFiles | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
    }
    else {
        Write-Host '  (none found)'
    }

    $info = Get-SshdServiceInfo
    if ($info) {
        Write-Host ''
        $ver = Get-OpenSshVersionLabel -SshdExe $info.SshdExe
        Write-Host ('OpenSSH build: ' + $(if ($ver) { $ver } else { 'unknown' })) -ForegroundColor DarkGray
        Write-Host ('Parsed install directory from service: ' + $info.InstallDir) -ForegroundColor DarkGray
        Test-OpenSshBinarySet -InstallDir $info.InstallDir -SshdExe $info.SshdExe | Format-Table Name, Exists -AutoSize

        if (Test-OpenSshRequiresSessionBinary -SshdExe $info.SshdExe) {
            $sessionExe = Join-Path $info.InstallDir 'sshd-session.exe'
            if (-not (Test-Path -LiteralPath $sessionExe)) {
                Write-Warning 'MISSING sshd-session.exe (required for OpenSSH 9+). Use -ReinstallWin32OpenSsh.'
            }
        }
        else {
            Write-Host 'Note: OpenSSH 8.x does not use sshd-session.exe — missing file is expected.' -ForegroundColor DarkGray
        }
    }

    $extraDirs = @(
        'C:\Windows\System32\OpenSSH',
        'C:\Program Files\OpenSSH\OpenSSH-Win64'
    )
    foreach ($dir in $extraDirs) {
        if (-not (Test-Path -LiteralPath $dir)) {
            continue
        }
        $sshdInDir = Join-Path $dir 'sshd.exe'
        $missing = @(Test-OpenSshBinarySet -InstallDir $dir -SshdExe $sshdInDir | Where-Object { -not $_.Exists })
        if ($missing.Count -gt 0) {
            Write-Host ''
            Write-Host ('Required binaries missing under ' + $dir + ': ' + ($missing.Name -join ', ')) -ForegroundColor Yellow
        }
    }
}

function Test-SshdConfigSyntax {
    param([string]$ConfigPath)

    $info = Get-SshdServiceInfo
    $sshdExe = if ($info) { $info.SshdExe } else { 'C:\Windows\System32\OpenSSH\sshd.exe' }
    if (-not (Test-Path -LiteralPath $sshdExe)) {
        Write-Warning ('sshd.exe not found for config test: ' + $sshdExe)
        return $false
    }

    Write-Host ('Validating: ' + $sshdExe + ' -t -f ' + $ConfigPath)
    $out = @(& $sshdExe -t -f $ConfigPath 2>&1 | ForEach-Object { "$_" })
    $text = $out -join "`n"
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'sshd_config validation FAILED:' -ForegroundColor Red
        Write-Host $text
        return $false
    }
    if ($text) {
        Write-Host $text
    }
    Write-Host 'sshd_config syntax OK.' -ForegroundColor Green
    return $true
}

function Copy-OpenSshBinariesToDirectory {
    param(
        [string]$SourceDir,
        [string]$TargetDir
    )

    if (-not (Test-Path -LiteralPath $SourceDir)) {
        throw ('Source directory not found: ' + $SourceDir)
    }
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backupDir = Join-Path $TargetDir ('exe-backup-' + $stamp)
    $existing = @(Get-ChildItem -LiteralPath $TargetDir -Filter '*.exe' -File -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 0) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
        $existing | Copy-Item -Destination $backupDir -Force
        Write-Host ('Backed up ' + $existing.Count + ' exe(s) to ' + $backupDir)
    }

    $copied = @(Get-ChildItem -LiteralPath $SourceDir -Filter '*.exe' -File)
    foreach ($exe in $copied) {
        Copy-Item -LiteralPath $exe.FullName -Destination (Join-Path $TargetDir $exe.Name) -Force
        Write-Host ('  copied ' + $exe.Name + ' -> ' + $TargetDir)
    }
}

function Invoke-Win32OpenSshReinstall {
    param(
        [string]$LocalZipPath,
        [string]$WorkDirectory = 'C:\OpenSSH-Setup'
    )

    $installScript = Join-Path $PSScriptRoot 'Install-WindowsOpenSshServer.ps1'
    if (-not (Test-Path -LiteralPath $installScript)) {
        throw ('Install-WindowsOpenSshServer.ps1 not found next to repair script: ' + $installScript)
    }

    Write-Step 'Refreshing Win32-OpenSSH binaries (ForceWin32BinaryInstall)'

    $info = Get-SshdServiceInfo
    $serviceDir = if ($info) { $info.InstallDir } else { 'C:\Windows\System32\OpenSSH' }

    if (Get-Service -Name 'sshd' -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq 'Running' }) {
        Write-Host 'Stopping sshd before binary sync...'
        Stop-Service sshd -Force
        Start-Sleep -Seconds 2
    }

    $installArgs = @{
        SkipCapabilityRepair    = $true
        SkipFirewall            = $true
        ForceWin32BinaryInstall = $true
        WorkDirectory           = $WorkDirectory
    }
    if (-not [string]::IsNullOrWhiteSpace($LocalZipPath)) {
        $installArgs['ZipPath'] = $LocalZipPath
    }

    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $installScript @installArgs
    }
    finally {
        $ErrorActionPreference = $prevEap
    }

    $extractedDir = Join-Path $WorkDirectory 'OpenSSH-Win64'
    if (Test-Path -LiteralPath $extractedDir) {
        Write-Step ('Copying exes from zip to service directory: ' + $serviceDir)
        Copy-OpenSshBinariesToDirectory -SourceDir $extractedDir -TargetDir $serviceDir
    }
    else {
        Write-Warning ('Extracted folder not found: ' + $extractedDir + ' (install may have failed to download/extract).')
    }

    Write-Step 'Ensure sshd listens on port 22 after binary sync'
    Repair-EnsureSshdPortListening -LocalPort 22 -ConfigPath 'C:\ProgramData\ssh\sshd_config' | Out-Null
}

function Repair-AdministratorsAuthorizedKeys {
    param([string]$SshDir)

    $adminKeys = Join-Path $SshDir 'administrators_authorized_keys'
    if (-not (Test-Path -LiteralPath $adminKeys)) {
        Write-Host 'No administrators_authorized_keys file (OK).'
        return
    }

    if ($KeepAdministratorsAuthorizedKeys) {
        Write-Host 'KeepAdministratorsAuthorizedKeys: fixing ACLs only.'
        icacls $adminKeys /inheritance:r | Out-Null
        icacls $adminKeys /grant 'SYSTEM:F' | Out-Null
        icacls $adminKeys /grant 'Administrators:F' | Out-Null
        return
    }

    Backup-FileIfExists -Path $adminKeys | Out-Null
    $renamed = $adminKeys + '.disabled'
    if (Test-Path -LiteralPath $renamed) {
        Remove-Item -LiteralPath $renamed -Force
    }
    Rename-Item -LiteralPath $adminKeys -NewName (Split-Path -Leaf $renamed) -Force
    Write-Host ('Renamed administrators_authorized_keys -> ' + (Split-Path -Leaf $renamed))
}

function Repair-SshDataDirectoryAcl {
    param([string]$SshDir)

    if (-not (Test-Path -LiteralPath $SshDir)) {
        throw ('SSH config directory not found: ' + $SshDir)
    }

    Write-Host ('Fixing ACLs on ' + $SshDir)
    icacls $SshDir /inheritance:r | Out-Null
    icacls $SshDir /grant 'SYSTEM:(OI)(CI)F' | Out-Null
    icacls $SshDir /grant 'Administrators:(OI)(CI)F' | Out-Null

    Get-ChildItem -LiteralPath $SshDir -Filter 'ssh_host_*_key' -File -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host ('  host key: ' + $_.Name)
        icacls $_.FullName /inheritance:r | Out-Null
        icacls $_.FullName /grant 'SYSTEM:R' | Out-Null
        icacls $_.FullName /grant 'Administrators:R' | Out-Null
    }
}

function Repair-UserSshFolder {
    param([string]$UserName)

    $userDir = 'C:\Users\' + $UserName
    if (-not (Test-Path -LiteralPath $userDir)) {
        Write-Warning ('User profile not found: ' + $userDir)
        return
    }

    $sshUserDir = Join-Path $userDir '.ssh'
    if (-not (Test-Path -LiteralPath $sshUserDir)) {
        Write-Host ('No .ssh folder for ' + $UserName + ' (OK).')
        return
    }

    Write-Host ('Fixing ACLs on ' + $sshUserDir)
    icacls $sshUserDir /inheritance:r | Out-Null
    icacls $sshUserDir /grant ('SYSTEM:F') | Out-Null
    icacls $sshUserDir /grant ($UserName + ':(OI)(CI)F') | Out-Null

    $authKeys = Join-Path $sshUserDir 'authorized_keys'
    if (Test-Path -LiteralPath $authKeys) {
        icacls $authKeys /inheritance:r | Out-Null
        icacls $authKeys /grant ('SYSTEM:F') | Out-Null
        icacls $authKeys /grant ($UserName + ':F') | Out-Null
        Write-Host ('  authorized_keys ACLs fixed')
    }
}

function Set-SshdConfigKeyValue {
    param(
        [string[]]$Lines,
        [string]$Name,
        [string]$Value,
        [ref]$Changed
    )

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match ('^\s*#?\s*' + [regex]::Escape($Name) + '\s+')) {
            $Lines[$i] = $Name + ' ' + $Value
            $Changed.Value = $true
            return $Lines
        }
    }
    $script:extraConfigLines += ($Name + ' ' + $Value)
    $Changed.Value = $true
    return $Lines
}

function Set-SshdConfigAuthOptions {
    param([string]$ConfigPath)

    if (-not (Test-Path -LiteralPath $ConfigPath)) {
        throw ('sshd_config not found: ' + $ConfigPath)
    }

    Backup-FileIfExists -Path $ConfigPath | Out-Null

    $script:extraConfigLines = @()
    $lines = @(Get-Content -LiteralPath $ConfigPath -ErrorAction Stop)
    $changed = $false
    $changedRef = [ref]$changed

    # Domain-friendly: GSSAPI/StrictModes often cause connection reset on AD accounts (OpenSSH 8.x).
    $settings = @{
        'PasswordAuthentication' = 'yes'
        'PubkeyAuthentication'   = 'yes'
        'GSSAPIAuthentication'   = 'no'
        'StrictModes'            = 'no'
    }

    foreach ($name in ($settings.Keys | Sort-Object)) {
        $lines = Set-SshdConfigKeyValue -Lines $lines -Name $name -Value $settings[$name] -Changed $changedRef
    }

    if ($script:extraConfigLines.Count -gt 0) {
        $lines += ''
        $lines += '# Added by Repair-WindowsOpenSshServer.ps1'
        $lines += $script:extraConfigLines
    }

    if ($changed) {
        $text = ($lines -join "`r`n") + "`r`n"
        [System.IO.File]::WriteAllText($ConfigPath, $text)
        Write-Host ('Updated ' + $ConfigPath + ' (PasswordAuthentication, PubkeyAuthentication, GSSAPIAuthentication no, StrictModes no)')
    }
}

function Repair-QuarantineUserAuthorizedKeys {
    param([string]$UserName)

    $authKeys = Join-Path ('C:\Users\' + $UserName) '.ssh\authorized_keys'
    if (-not (Test-Path -LiteralPath $authKeys)) {
        Write-Host 'No user authorized_keys to quarantine (OK).'
        return
    }

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $quarantine = $authKeys + '.bak-' + $stamp
    Rename-Item -LiteralPath $authKeys -NewName (Split-Path -Leaf $quarantine) -Force
    Write-Host ('Quarantined ' + $authKeys + ' -> ' + (Split-Path -Leaf $quarantine))
    Write-Host 'Password login can still work; restore file after fixing key format/ACLs.'
}

function Set-OpenSshDefaultShellOption {
    param([string]$Mode)

    if ($Mode -eq 'NoChange') {
        Write-Host 'DefaultShell: no change.'
        return
    }

    $shellPath = if ($Mode -eq 'PowerShell') {
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    }
    else {
        'C:\Windows\System32\cmd.exe'
    }

    $keyPath = 'HKLM:\SOFTWARE\OpenSSH'
    if (-not (Test-Path -LiteralPath $keyPath)) {
        New-Item -Path $keyPath -Force | Out-Null
    }
    New-ItemProperty -Path $keyPath -Name 'DefaultShell' -Value $shellPath -PropertyType String -Force | Out-Null
    Write-Host ('DefaultShell set to ' + $shellPath)
}

function Restart-SshdServiceSafe {
    $svc = Get-Service -Name 'sshd' -ErrorAction SilentlyContinue
    if (-not $svc) {
        throw 'sshd service not found. Run Install-WindowsOpenSshServer.ps1 first.'
    }
    if ($svc.Status -eq 'Running') {
        Restart-Service sshd -Force
    }
    else {
        Start-Service sshd
    }
    Set-Service -Name sshd -StartupType Automatic
    Get-Service sshd | Format-Table Status, Name, StartType -AutoSize
}

function Repair-EnsureSshdPortListening {
    param(
        [int]$LocalPort = 22,
        [string]$ConfigPath = 'C:\ProgramData\ssh\sshd_config'
    )

    $info = Get-SshdServiceInfo
    $sshdExe = if ($info) { $info.SshdExe } else { 'C:\Windows\System32\OpenSSH\sshd.exe' }

    Write-Host '1) Restart sshd and check port listen...' -ForegroundColor DarkGray
    Restart-SshdServiceSafe
    Start-Sleep -Seconds 3
    Get-Service sshd | Format-Table Status, Name, StartType -AutoSize
    $listen = @(Get-NetTCPConnection -LocalPort $LocalPort -State Listen -ErrorAction SilentlyContinue)
    if ($listen.Count -gt 0) {
        Write-Host ('OK: Port ' + $LocalPort + ' is listening.') -ForegroundColor Green
        $listen | Select-Object -First 3 LocalAddress, LocalPort, State | Format-Table -AutoSize
        return $true
    }

    Write-Host ('Port ' + $LocalPort + ' is not listening. Running extended recovery...') -ForegroundColor Yellow

    Write-Host ''
    Write-Host ('2) Test sshd_config: ' + $sshdExe + ' -t -f ' + $ConfigPath) -ForegroundColor DarkGray
    if (Test-Path -LiteralPath $sshdExe) {
        $configOut = @(& $sshdExe -t -f $ConfigPath 2>&1 | ForEach-Object { "$_" })
        if ($configOut) {
            Write-Host ($configOut -join "`n")
        }
        if ($LASTEXITCODE -ne 0) {
            Write-Warning 'sshd_config test failed — restore from C:\ProgramData\ssh\sshd_config.bak-* if needed.'
        }
        else {
            Write-Host 'sshd_config syntax OK.' -ForegroundColor Green
        }
    }
    else {
        Write-Warning ('sshd.exe not found: ' + $sshdExe)
    }

    Write-Host ''
    Write-Host '3) Recent OpenSSH/Operational log entries:' -ForegroundColor DarkGray
    $logName = 'OpenSSH/Operational'
    if (Get-WinEvent -ListLog $logName -ErrorAction SilentlyContinue) {
        Get-WinEvent -LogName $logName -MaxEvents 5 -ErrorAction SilentlyContinue |
            Format-List TimeCreated, Message
    }
    else {
        Write-Host ('  Log not found: ' + $logName)
    }

    Write-Host ''
    Write-Host '4) Start sshd again and re-check port...' -ForegroundColor DarkGray
    Start-Service sshd -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    Get-Service sshd | Format-Table Status, Name, StartType -AutoSize
    $listen = @(Get-NetTCPConnection -LocalPort $LocalPort -State Listen -ErrorAction SilentlyContinue)
    if ($listen.Count -gt 0) {
        Write-Host ('OK: Port ' + $LocalPort + ' is listening after recovery.') -ForegroundColor Green
        $listen | Select-Object -First 3 LocalAddress, LocalPort, State | Format-Table -AutoSize
        return $true
    }

    Write-Warning ('Port ' + $LocalPort + ' is still not listening. See OpenSSH/Operational log and run sshd.exe -d -d -d for debug.')
    return $false
}

function Invoke-SshHandshakeAttempt {
    param(
        [string]$Target,
        [int]$LocalPort,
        [string[]]$ExtraOptions,
        [string]$Label
    )

    Write-Host ('  ' + $Label + ': ssh ' + ($ExtraOptions -join ' ') + ' ' + $Target)

    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $args = @($ExtraOptions) + @('-o', 'BatchMode=yes', '-o', 'ConnectTimeout=10', '-o', 'StrictHostKeyChecking=no', '-p', "$LocalPort", $Target, 'exit')
        $output = @(ssh.exe @args 2>&1 | ForEach-Object { "$_" })
    }
    finally {
        $ErrorActionPreference = $prevEap
    }

    $text = $output -join "`n"
    return [PSCustomObject]@{
        Text   = $text
        ExitCode = $LASTEXITCODE
    }
}

function Test-SshHandshake {
    param(
        [string]$User,
        [int]$LocalPort,
        [string]$UserDomain = ''
    )

    if (-not (Get-Command ssh.exe -ErrorAction SilentlyContinue)) {
        Write-Warning 'ssh.exe not found; skipping handshake test.'
        return $false
    }

    $targets = @($User + '@127.0.0.1')
    if (-not [string]::IsNullOrWhiteSpace($UserDomain)) {
        $targets += ($UserDomain + '\' + $User + '@127.0.0.1')
    }

    $attempts = @(
        @{ Label = 'default'; Options = @() },
        @{ Label = 'password-only'; Options = @('-o', 'PreferredAuthentications=password', '-o', 'PubkeyAuthentication=no') }
    )

    foreach ($target in $targets) {
        Write-Host ('Handshake target: ' + $target)
        foreach ($attempt in $attempts) {
            $result = Invoke-SshHandshakeAttempt -Target $target -LocalPort $LocalPort -ExtraOptions $attempt.Options -Label $attempt.Label
            $text = $result.Text

            if ($text -match 'Connection reset') {
                Write-Host '    -> Connection reset' -ForegroundColor Red
                continue
            }
            if ($text -match 'Permission denied|Authentication failed|Too many authentication failures') {
                Write-Host 'OK: SSH reached authentication (reset is fixed). Log in interactively with password.' -ForegroundColor Green
                Write-Host ('    Use: ssh ' + $target)
                return $true
            }
            if ($result.ExitCode -eq 0) {
                Write-Host 'OK: SSH login succeeded.' -ForegroundColor Green
                return $true
            }
            if ($text) {
                Write-Host ('    -> ' + ($text -replace "`n", ' '))
            }
        }
    }

    Write-Host 'FAIL: Still getting Connection reset on all handshake attempts.' -ForegroundColor Red
    return $false
}

function Show-RecentOpenSshEvents {
    param([int]$Count = 12)

    $logName = 'OpenSSH/Operational'
    if (-not (Get-WinEvent -ListLog $logName -ErrorAction SilentlyContinue)) {
        Write-Host ('Log not found: ' + $logName)
        return
    }
    Write-Step 'Recent OpenSSH/Operational events'
    Get-WinEvent -LogName $logName -MaxEvents $Count -ErrorAction SilentlyContinue |
        Format-List TimeCreated, Id, LevelDisplayName, Message
}

function Get-WindowsAccountDomain {
    param([string]$UserName)

    $wmi = Get-CimInstance Win32_UserAccount -Filter ("Name='{0}'" -f ($UserName -replace "'", "''")) -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($wmi -and -not [string]::IsNullOrWhiteSpace($wmi.Domain)) {
        $computer = $env:COMPUTERNAME
        if ($wmi.Domain -ne $computer -and $wmi.Domain -ne ($computer + '$')) {
            return $wmi.Domain
        }
    }
    return $null
}

function Test-WindowsAccountActive {
    param([string]$UserName)

    $local = Get-LocalUser -Name $UserName -ErrorAction SilentlyContinue
    if ($local) {
        Write-Host ('Account ' + $UserName + ' (local): Enabled=' + $local.Enabled)
        if (-not $local.Enabled) {
            Write-Warning 'Account is disabled — enable it before SSH login.'
        }
        return
    }

    $wmi = Get-CimInstance Win32_UserAccount -Filter ("Name='{0}'" -f ($UserName -replace "'", "''")) -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($wmi) {
        Write-Host ('Account ' + $UserName + ' (Win32_UserAccount): Disabled=' + $wmi.Disabled + ' Domain=' + $wmi.Domain)
        if ($wmi.Disabled) {
            Write-Warning 'Account is disabled — enable it before SSH login.'
        }
        return
    }

    try {
        $nt = New-Object System.Security.Principal.NTAccount($UserName)
        [void]$nt.Translate([System.Security.Principal.SecurityIdentifier])
        Write-Host ('Account ' + $UserName + ' resolves to a SID (may be domain or built-in).')
    }
    catch {
        Write-Warning ('Could not resolve account: ' + $UserName + ' — verify username for SSH.')
    }
}

function Test-OpenSshNeedsReinstall {
    $info = Get-SshdServiceInfo
    if (-not $info) {
        return $true
    }
    $bins = Test-OpenSshBinarySet -InstallDir $info.InstallDir -SshdExe $info.SshdExe
    $missing = @($bins | Where-Object { -not $_.Exists })
    if ($missing.Count -gt 0) {
        Write-Warning ('Missing required binaries in install dir: ' + ($missing.Name -join ', '))
        return $true
    }
    return $false
}

function Show-SshdConfigMatchBlocks {
    param([string]$ConfigPath)

    Write-Step 'sshd_config Match blocks (can affect admin/domain logins)'
    $inMatch = $false
    Get-Content -LiteralPath $ConfigPath -ErrorAction SilentlyContinue | ForEach-Object {
        $line = "$_"
        if ($line -match '^\s*Match\s+') {
            $inMatch = $true
            Write-Host $line -ForegroundColor Yellow
        }
        elseif ($inMatch) {
            if ($line -match '^\s*\S') {
                Write-Host ('  ' + $line)
            }
            if ([string]::IsNullOrWhiteSpace($line)) {
                $inMatch = $false
            }
        }
    }
}

function Show-OpenSshAdminErrors {
    param([int]$Count = 15)

    $logName = 'OpenSSH/Admin'
    if (-not (Get-WinEvent -ListLog $logName -ErrorAction SilentlyContinue)) {
        Write-Host ('Log not found: ' + $logName + ' (enable if missing)')
        return
    }
    Write-Step 'OpenSSH/Admin errors (auth / logon failures)'
    $events = @(Get-WinEvent -LogName $logName -MaxEvents 50 -ErrorAction SilentlyContinue |
        Where-Object { $_.LevelDisplayName -in @('Error', 'Warning', 'Critical') } |
        Select-Object -First $Count)
    if ($events.Count -eq 0) {
        Write-Host 'No recent Error/Warning in OpenSSH/Admin. Trigger one failed ssh login, then re-run.'
        return
    }
    $events | Format-List TimeCreated, Id, LevelDisplayName, Message
}

function Test-UserInAdministratorsGroup {
    param([string]$UserName)

    Write-Step 'Local Administrators group membership'
    $inAdmins = $false
    $lines = @(net localgroup Administrators 2>&1 | ForEach-Object { "$_" })
    foreach ($line in $lines) {
        if ($line -match ('\b' + [regex]::Escape($UserName) + '\b')) {
            $inAdmins = $true
            break
        }
    }
    if ($inAdmins) {
        Write-Host ('User ' + $UserName + ' is listed in local Administrators.')
        Write-Host 'Check sshd_config for "Match Group administrators" (shown above).'
    }
    else {
        Write-Host ('User ' + $UserName + ' is not listed in local Administrators (net localgroup).')
    }
}

function Show-ConnectionResetDiagnostics {
    param(
        [string]$User,
        [string]$UserDomain,
        [string]$ConfigPath,
        [int]$LocalPort
    )

    Show-OpenSshAdminErrors
    Show-SshdConfigMatchBlocks -ConfigPath $ConfigPath
    Test-UserInAdministratorsGroup -UserName $User

    Write-Step 'Test local Administrator SSH (isolates domain vs sshd)'
    $adminOk = Invoke-SshHandshakeAttempt -Target 'Administrator@127.0.0.1' -LocalPort $LocalPort -ExtraOptions @(
        '-o', 'PreferredAuthentications=password', '-o', 'PubkeyAuthentication=no'
    ) -Label 'Administrator password-only'
    if ($adminOk.Text -match 'Permission denied|Authentication failed') {
        Write-Host 'OK: Administrator reaches auth (sshd works; problem is likely domain user / rights / profile).' -ForegroundColor Green
    }
    elseif ($adminOk.Text -match 'Connection reset') {
        Write-Host 'FAIL: Administrator also gets reset — sshd/config/system issue, not only domain user.' -ForegroundColor Red
    }
    else {
        Write-Host ('Administrator test output: ' + $adminOk.Text)
    }

    Write-Host ''
    Write-Host 'IT checks for domain SSH:' -ForegroundColor Yellow
    Write-Host '  - "Allow log on locally" (or Remote Desktop Users) for CALIKSOA\adm43419 on this server'
    Write-Host '  - AD account not locked; password not expired'
    Write-Host '  - User profile C:\Users\adm43419 exists and is not corrupted'
}

function Show-RepairFailedGuidance {
    param(
        [string]$User,
        [string]$UserDomain = ''
    )

    $sshUser = if ($UserDomain) { $UserDomain + '\' + $User } else { $User }

    Write-Host ''
    Write-Host 'Repair did not fix Connection reset. Next steps:' -ForegroundColor Yellow
    Write-Host '  1. Interactive password-only test:'
    Write-Host ('     ssh -o PreferredAuthentications=password -o PubkeyAuthentication=no ' + $sshUser + '@127.0.0.1')
    Write-Host '  2. Debug sshd (stop service first):'
    Write-Host '     Stop-Service sshd'
    Write-Host '     cd C:\Windows\System32\OpenSSH'
    Write-Host '     .\sshd.exe -d -d -d'
    Write-Host ('     # other window: ssh -vvv ' + $sshUser + '@127.0.0.1')
    Write-Host '  3. Try built-in: ssh Administrator@127.0.0.1'
    Write-Host '  4. If only domain user fails: ask IT — Allow log on locally, AD account, group policy.'
}

# --- Main ---
Write-Step 'Repair OpenSSH Server (connection reset / admin login)'

$sshDir = Split-Path -Parent $SshConfigPath
if ([string]::IsNullOrWhiteSpace($TestUser)) {
    $TestUser = $env:USERNAME
}

if (-not (Get-Service -Name 'sshd' -ErrorAction SilentlyContinue)) {
    throw 'sshd service is not installed. Run .\Install-WindowsOpenSshServer.ps1 first.'
}

Show-OpenSshDiagnostics
Test-WindowsAccountActive -UserName $TestUser
$TestUserDomain = Get-WindowsAccountDomain -UserName $TestUser
if ($TestUserDomain) {
    Write-Host ('SSH login may require DOMAIN\user format: ' + $TestUserDomain + '\' + $TestUser)
}

$needsReinstall = $ReinstallWin32OpenSsh -or (Test-OpenSshNeedsReinstall)
if ($needsReinstall) {
    try {
        Invoke-Win32OpenSshReinstall -LocalZipPath $ZipPath
    }
    catch {
        Write-Warning ('Win32 binary sync failed: ' + $_.Exception.Message)
        Write-Host 'Attempting sshd port recovery...'
        Repair-EnsureSshdPortListening -LocalPort $Port -ConfigPath $SshConfigPath | Out-Null
    }
    Show-OpenSshDiagnostics
}

if (-not (Test-SshdListening -LocalPort $Port)) {
    Write-Step 'Recover sshd (port not listening yet)'
    $portOk = Repair-EnsureSshdPortListening -LocalPort $Port -ConfigPath $SshConfigPath
    if (-not $portOk) {
        Write-Warning 'sshd is not listening on port ' + $Port + '. Continuing repair; handshake test may fail.'
    }
}

Write-Step '1. administrators_authorized_keys'
Repair-AdministratorsAuthorizedKeys -SshDir $sshDir

Write-Step '2. ACLs on ProgramData\ssh and host private keys'
Repair-SshDataDirectoryAcl -SshDir $sshDir

Write-Step '3. User .ssh folder ACLs'
Repair-UserSshFolder -UserName $TestUser

Write-Step '3b. Quarantine user authorized_keys (if pubkey breaks AD login)'
Repair-QuarantineUserAuthorizedKeys -UserName $TestUser

Write-Step '4. sshd_config authentication'
Set-SshdConfigAuthOptions -ConfigPath $SshConfigPath

Write-Step '5. Validate sshd_config'
$configOk = Test-SshdConfigSyntax -ConfigPath $SshConfigPath
if (-not $configOk) {
    Write-Warning 'Restore sshd_config from the newest .bak-* file in C:\ProgramData\ssh if needed.'
}

Write-Step '6. DefaultShell'
Set-OpenSshDefaultShellOption -Mode $DefaultShell

Write-Step '7. Restart sshd and verify port listen'
$portListening = Repair-EnsureSshdPortListening -LocalPort $Port -ConfigPath $SshConfigPath

Show-RecentOpenSshEvents

$ok = $true
if (-not $SkipHandshakeTest) {
    Write-Step ('9. Handshake test (user: ' + $TestUser + ')')
    if (-not $portListening) {
        Write-Warning 'Skipping handshake test because port ' + $Port + ' is not listening.'
        $ok = $false
    }
    else {
        $ok = Test-SshHandshake -User $TestUser -LocalPort $Port -UserDomain $TestUserDomain
    }
}

if (-not $ok) {
    Show-ConnectionResetDiagnostics -User $TestUser -UserDomain $TestUserDomain -ConfigPath $SshConfigPath -LocalPort $Port
    Show-RepairFailedGuidance -User $TestUser -UserDomain $TestUserDomain
    exit 1
}

Write-Host ''
Write-Host 'Repair complete. Test interactive login:' -ForegroundColor Green
Write-Host ('  ssh ' + $TestUser + '@127.0.0.1')
Write-Host ('  ssh ' + $TestUser + '@<server-lan-ip>')
