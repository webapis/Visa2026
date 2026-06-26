#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Enable HTTPS (when configured in slot env) and rewrite appsettings for one IIS slot.

.PARAMETER Profile
  Production, Staging, or Demo.

.PARAMETER SqlServer
  SQL Server instance (default localhost\SQLEXPRESS).

.NOTES
  Called from Deploy-Visa2026IisSlotRemote.ps1 over SSH.
#>
[CmdletBinding()]
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy")]
    [string]$Profile = "Production",

    [string]$SqlServer = "localhost\SQLEXPRESS"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$slot = Get-Visa2026IisSlotDefinition -Profile $Profile
$envFile = $slot.EnvFile

if (Resolve-Visa2026HttpsEnabled -EnvFile $envFile) {
    $httpsPort = Resolve-Visa2026HttpsPort -EnvFile $envFile -DefaultPort (Resolve-Visa2026DefaultHttpsPortForProfile -Profile $Profile)
    $ipAddress = ""
    if (Test-Path -LiteralPath $envFile) {
        $envMap = Read-Visa2026DotEnvMap -Path $envFile
        if ($envMap.ContainsKey("TEMPLATE_EDIT_UNC_HOST") -and $envMap["TEMPLATE_EDIT_UNC_HOST"]) {
            $ipAddress = $envMap["TEMPLATE_EDIT_UNC_HOST"].Trim()
        }
    }

    $httpsScript = Join-Path $PSScriptRoot "Enable-Visa2026IisHttps.ps1"
    if ($ipAddress) {
        & $httpsScript -Profile $Profile -HttpsPort $httpsPort -IpAddress $ipAddress -RedirectHttpToHttps
    }
    else {
        & $httpsScript -Profile $Profile -HttpsPort $httpsPort -RedirectHttpToHttps
    }
}
else {
    Write-Warning "HTTPS_ENABLED is not true in $envFile - skipping Enable-Visa2026IisHttps.ps1"
}

$configureScript = Join-Path $PSScriptRoot "Configure-Visa2026Production.ps1"
& $configureScript -Profile $Profile -SqlServer $SqlServer
