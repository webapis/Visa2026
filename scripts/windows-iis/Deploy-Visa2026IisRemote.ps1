#Requires -Version 5.1
<#
.SYNOPSIS
  Deploy published Visa2026 to visa2026-onprem over SSH (IIS slot).

.DESCRIPTION
  Wrapper for Deploy-Visa2026IisSlotRemote.ps1. Default slot is Production (:80).

.PARAMETER Profile
  Production (:80), Staging (:8080), or Demo (:8081).

.PARAMETER SshHost
  SSH config host (default visa2026-onprem).

.PARAMETER SkipPublish
  Do not run Publish-Visa2026ForIis.ps1 locally.

.PARAMETER SkipDbUpdate
  Skip --updateDatabase on server.

.PARAMETER ForceUpdate
  Pass -ForceUpdate to Run-Visa2026DbUpdateOnServer.ps1.

.PARAMETER EnableForceXafDbUpdate
  Set FORCE_XAF_DB_UPDATE=true on the slot app pool for this deploy.
#>
param(
    [ValidateSet("Production", "Staging", "Demo")]
    [string]$Profile = "Production",

    [string]$SshHost = "visa2026-onprem",
    [string]$PublishPath = "",
    [switch]$SkipPublish,
    [switch]$SkipDbUpdate,
    [switch]$ForceUpdate,
    [switch]$EnableForceXafDbUpdate
)

$ErrorActionPreference = "Stop"
$params = @{
    Profile = $Profile
    SshHost = $SshHost
}
if ($PublishPath) { $params.PublishPath = $PublishPath }
if ($SkipPublish) { $params.SkipPublish = $true }
if ($SkipDbUpdate) { $params.SkipDbUpdate = $true }
if ($ForceUpdate) { $params.ForceUpdate = $true }
if ($EnableForceXafDbUpdate) { $params.EnableForceXafDbUpdate = $true }

& (Join-Path $PSScriptRoot "Deploy-Visa2026IisSlotRemote.ps1") @params
