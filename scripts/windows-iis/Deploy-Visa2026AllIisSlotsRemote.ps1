#Requires -Version 5.1
<#
.SYNOPSIS
  Publish once and deploy Visa2026 to Production, Staging, and Demo on visa2026-onprem.

.PARAMETER SshHost
  SSH config host (default visa2026-onprem).

.PARAMETER ForceUpdate
  Pass -ForceUpdate to each slot DB update.

.PARAMETER SkipPublish
  Skip local dotnet publish.

.PARAMETER SkipDbUpdate
  Skip database update on all slots.
#>
param(
    [string]$SshHost = "visa2026-onprem",
    [string]$PublishPath = "",
    [switch]$SkipPublish,
    [switch]$SkipDbUpdate,
    [switch]$ForceUpdate,
    [switch]$EnableForceXafDbUpdate
)

$ErrorActionPreference = "Stop"
$deploy = Join-Path $PSScriptRoot "Deploy-Visa2026IisSlotRemote.ps1"

foreach ($profile in @("Production", "Staging", "Demo")) {
    Write-Host ""
    Write-Host "########################################" -ForegroundColor Magenta
    Write-Host "# Slot: $profile" -ForegroundColor Magenta
    Write-Host "########################################" -ForegroundColor Magenta

    $slotSkipPublish = $SkipPublish -or ($profile -ne "Production")
    if (-not $SkipPublish -and $profile -eq "Production") {
        $slotSkipPublish = $false
    }
    if ($SkipPublish) {
        $slotSkipPublish = $true
    }

    $params = @{
        Profile     = $profile
        SshHost     = $SshHost
        SkipPublish = $slotSkipPublish
    }
    if ($PublishPath) { $params.PublishPath = $PublishPath }
    if ($SkipDbUpdate) { $params.SkipDbUpdate = $true }
    if ($ForceUpdate) { $params.ForceUpdate = $true }
    if ($EnableForceXafDbUpdate) { $params.EnableForceXafDbUpdate = $true }

    & $deploy @params
    if ($LASTEXITCODE -ne 0) {
        throw "Deploy failed for slot $profile (exit $LASTEXITCODE)."
    }
}

Write-Host ""
Write-Host "All three slots deployed." -ForegroundColor Green
Write-Host "  Production : http://<server>/LoginPage" -ForegroundColor Green
Write-Host "  Staging    : http://<server>:8080/LoginPage" -ForegroundColor Green
Write-Host "  Demo       : http://<server>:8081/LoginPage" -ForegroundColor Green
