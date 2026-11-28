#Requires -Version 5.1
<#
.SYNOPSIS
  Install Visa2026 template editing on this PC.

.DESCRIPTION
  Runs the Office trust script and the Word/Excel Sync add-in installer.
  Also creates %LOCALAPPDATA%\Visa2026\TemplateEdit and trusts that folder in Word and Excel.
  server-host.txt is written by Visa2026 when the zip is downloaded.
#>
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$hostFile = Join-Path $here "server-host.txt"
if (-not (Test-Path -LiteralPath $hostFile)) {
    throw "server-host.txt is missing. Download the setup again from Visa2026."
}

$serverHost = (Get-Content -LiteralPath $hostFile -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($serverHost)) {
    throw "server-host.txt is empty. Download the setup again from Visa2026."
}

$trust = Join-Path $here "Set-Visa2026TemplateEditOfficeTrust.ps1"
if (-not (Test-Path -LiteralPath $trust)) {
    throw "Set-Visa2026TemplateEditOfficeTrust.ps1 was not found next to this installer."
}

Write-Host "Installing Visa2026 template edit for $serverHost" -ForegroundColor Cyan
if ($serverHost -match '^(localhost|127\.0\.0\.1)$') {
    & $trust -ServerHost $serverHost -IncludeLocalhost
}
else {
    & $trust -ServerHost $serverHost
}
