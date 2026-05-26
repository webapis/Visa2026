#Requires -Version 5.1
<#
.SYNOPSIS
  Install this PC's SSH public key on a Windows Server for local Administrator (pubkey login).

.DESCRIPTION
  Windows OpenSSH stores administrator keys in:
    C:\ProgramData\ssh\administrators_authorized_keys
  (not C:\Users\Administrator\.ssh\authorized_keys).

  Run once from your admin PC while password SSH still works. After setup, test:
    ssh visa2026-onprem

.PARAMETER ServerIp
  Server IP or hostname (default 10.100.128.25).

.PARAMETER User
  SSH user (default Administrator).

.PARAMETER PublicKeyPath
  Path to your .pub file (default: id_ed25519_visa_onprem in user .ssh).

.EXAMPLE
  .\Setup-OnPremSshAuthorizedKey.ps1
#>
[CmdletBinding()]
param(
    [string]$ServerIp = '10.100.128.25',
    [string]$User = 'Administrator',
    [string]$PublicKeyPath = (Join-Path $env:USERPROFILE '.ssh\id_ed25519_visa_onprem.pub')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PublicKeyPath)) {
    $priv = $PublicKeyPath -replace '\.pub$', ''
    throw "Public key not found: $PublicKeyPath. Run: ssh-keygen -t ed25519 -f `"$priv`" -C webap-visa-onprem"
}

$pubLine = (Get-Content -LiteralPath $PublicKeyPath -Raw).Trim()
if ($pubLine -notmatch '^ssh-(ed25519|rsa|ecdsa) ') {
    throw "Invalid public key line in $PublicKeyPath"
}

$remoteScript = @"
`$ProgressPreference = 'SilentlyContinue'
`$pub = @'
$pubLine
'@
`$adminKeys = 'C:\ProgramData\ssh\administrators_authorized_keys'
`$sshDir = 'C:\ProgramData\ssh'
if (-not (Test-Path -LiteralPath `$sshDir)) {
    New-Item -ItemType Directory -Path `$sshDir -Force | Out-Null
}
`$token = (`$pub -split '\s+', 3)[1]
`$exists = `$false
if (Test-Path -LiteralPath `$adminKeys) {
    `$exists = (Select-String -LiteralPath `$adminKeys -Pattern ([regex]::Escape(`$token)) -Quiet)
}
if (-not `$exists) {
    Add-Content -LiteralPath `$adminKeys -Value `$pub -Encoding ascii
    [Console]::WriteLine('Added public key to administrators_authorized_keys')
} else {
    [Console]::WriteLine('Public key already present in administrators_authorized_keys')
}
icacls `$adminKeys /inheritance:r | Out-Null
icacls `$adminKeys /grant 'SYSTEM:F' | Out-Null
icacls `$adminKeys /grant 'Administrators:F' | Out-Null
[Console]::WriteLine('ACLs set on administrators_authorized_keys')
"@

$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($remoteScript))
$target = "${User}@${ServerIp}"

Write-Host "Installing public key on $target (password prompt once)..."
Write-Host "Key: $PublicKeyPath"
Write-Host ""

ssh -o PreferredAuthentications=password $target "powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand $encoded"

Write-Host ""
Write-Host "Test (no password):"
Write-Host "  ssh -o PreferredAuthentications=publickey -o PasswordAuthentication=no visa2026-onprem hostname"
