#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Point Windows port 80 at the current WSL IP (NAT mode) so LAN clients reach Docker.

.DESCRIPTION
  WSL2 NAT does not publish Docker ports on 0.0.0.0 by default on Windows Server.
  Run after reboot or when http://<server-ip> refuses connection.

.EXAMPLE
  .\Set-OnPremWslPortProxy.ps1
#>
[CmdletBinding()]
param(
    [int]$Port = 80,
    [string]$DistroName = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'

$wslIp = (& wsl.exe -d $DistroName -u root -- hostname -I).Trim().Split()[0]
if ([string]::IsNullOrWhiteSpace($wslIp)) {
    throw 'Could not resolve WSL IP. Start Ubuntu first: wsl -d Ubuntu -u root -e echo ok'
}

& netsh.exe interface portproxy delete v4tov4 listenport=$Port listenaddress=0.0.0.0 2>$null | Out-Null
& netsh.exe interface portproxy add v4tov4 listenport=$Port listenaddress=0.0.0.0 connectport=$Port connectaddress=$wslIp | Out-Null

Write-Host ('Port proxy: 0.0.0.0:{0} -> {1}:{0}' -f $Port, $wslIp)
& netsh.exe interface portproxy show v4tov4
