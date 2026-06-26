#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Remove HTTPS binding and HTTP->HTTPS redirect for a Visa2026 IIS slot (HTTP-only).

.PARAMETER Profile
  Production, Staging, Demo, or Legacy.

.PARAMETER HttpsPort
  HTTPS port to remove (default: profile default).

.EXAMPLE
  .\Disable-Visa2026IisHttps.ps1 -Profile Production
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy")]
    [string]$Profile = "Production",

    [int]$HttpsPort = 0
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

Import-Module WebAdministration -ErrorAction Stop

$slot = Get-Visa2026IisSlotDefinition -Profile $Profile
$siteName = $slot.SiteName

if ($HttpsPort -le 0) {
    $HttpsPort = Resolve-Visa2026DefaultHttpsPortForProfile -Profile $Profile
}

Write-Host ""
Write-Host "Visa2026 HTTP-only - $($slot.Profile)" -ForegroundColor Cyan
Write-Host "  Site       : $siteName"
Write-Host "  HTTP port  : $($slot.HttpPort)"
Write-Host "  Remove HTTPS port : $HttpsPort"

if (-not (Test-Path "IIS:\Sites\$siteName")) {
    throw "IIS site not found: $siteName"
}

$ruleName = "Visa2026-$($slot.Profile)-HttpToHttps"
$rewriteRoot = "system.webServer/rewrite/rules"

if ($PSCmdlet.ShouldProcess($siteName, "Remove HTTP to HTTPS redirect rule")) {
    $rules = Get-WebConfiguration -PSPath "IIS:\Sites\$siteName" -Filter $rewriteRoot -ErrorAction SilentlyContinue
    if ($rules) {
        $index = 0
        foreach ($rule in @($rules.Collection)) {
            if ($rule.name -eq $ruleName) {
                Remove-WebConfigurationProperty -PSPath "IIS:\Sites\$siteName" -Filter $rewriteRoot -Name "." -AtElement @{ name = $ruleName } -ErrorAction SilentlyContinue
                Write-Host "  Removed redirect rule: $ruleName" -ForegroundColor Green
                break
            }
            $index++
        }
    }
}

if ($PSCmdlet.ShouldProcess($siteName, "Remove HTTPS binding on port $HttpsPort")) {
    $httpsBinding = Get-WebBinding -Name $siteName -Protocol "https" -ErrorAction SilentlyContinue |
        Where-Object { $_.bindingInformation -like "*:${HttpsPort}:*" }
    if ($httpsBinding) {
        Remove-WebBinding -Name $siteName -Protocol "https" -BindingInformation $httpsBinding.bindingInformation
        Write-Host "  Removed HTTPS binding on port $HttpsPort." -ForegroundColor Green
    }
    else {
        Write-Host "  No HTTPS binding on port $HttpsPort." -ForegroundColor DarkGray
    }
}

if ($PSCmdlet.ShouldProcess($siteName, "Ensure HTTP binding on port $($slot.HttpPort)")) {
    $httpBinding = Get-WebBinding -Name $siteName -Protocol "http" -ErrorAction SilentlyContinue |
        Where-Object { $_.bindingInformation -like "*:$($slot.HttpPort):" }
    if (-not $httpBinding) {
        New-WebBinding -Name $siteName -Protocol "http" -Port $slot.HttpPort -IPAddress "*" | Out-Null
        Write-Host "  Added HTTP :$($slot.HttpPort) binding." -ForegroundColor Green
    }
    else {
        Write-Host "  HTTP :$($slot.HttpPort) binding already present." -ForegroundColor DarkGray
    }
}

Write-Host "  Officer URL: http://localhost:$($slot.HttpPort)/LoginPage" -ForegroundColor Green
