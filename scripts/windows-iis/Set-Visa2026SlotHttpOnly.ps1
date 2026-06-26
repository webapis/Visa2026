#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Switch an IIS slot to HTTP-only: disable HTTPS binding/redirect, set HTTPS_ENABLED=false, regenerate appsettings.
#>
param(
    [ValidateSet("Production", "Staging", "Demo")]
    [string]$Profile = "Production",

    [string]$SqlServer = "localhost\SQLEXPRESS"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$slot = Get-Visa2026IisSlotDefinition -Profile $Profile
$envFile = $slot.EnvFile

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Env file not found: $envFile"
}

$lines = Get-Content -LiteralPath $envFile
$found = $false
$out = foreach ($line in $lines) {
    if ($line -match '^\s*HTTPS_ENABLED\s*=') {
        $found = $true
        "HTTPS_ENABLED=false"
    }
    else {
        $line
    }
}
if (-not $found) {
    $out += "HTTPS_ENABLED=false"
}
Set-Content -LiteralPath $envFile -Value $out -Encoding UTF8
Write-Host "Set HTTPS_ENABLED=false in $envFile" -ForegroundColor Green

& (Join-Path $PSScriptRoot "Disable-Visa2026IisHttps.ps1") -Profile $Profile
& (Join-Path $PSScriptRoot "Configure-Visa2026Production.ps1") -Profile $Profile -SqlServer $SqlServer

Import-Module WebAdministration -ErrorAction Stop
Restart-WebAppPool -Name $slot.AppPoolName
Write-Host "Recycled app pool $($slot.AppPoolName)." -ForegroundColor Green

$url = Get-Visa2026SlotSmokeLoginPageUrl -Profile $Profile
Write-Host "Login page: $url" -ForegroundColor Green
