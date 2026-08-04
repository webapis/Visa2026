#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Remove Visa2026 IIS sites and app pools after cutover to Docker (frees HTTP ports).

.DESCRIPTION
  Stops and removes Production, Staging, Demo, and Legacy Visa2026 IIS sites/app pools.
  Unregisters Visa2026-IisAfterBoot. Publish folders and env files are kept by default.

.EXAMPLE
  .\Remove-Visa2026IisDeployment.ps1
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$KeepPublishFolders = $true
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

Import-Module WebAdministration -ErrorAction Stop
$appcmd = "$env:windir\System32\inetsrv\appcmd.exe"

$profiles = (Get-Visa2026IisSlotProfiles) + @("Legacy")
$removedSites = @()
$removedPools = @()

foreach ($profile in $profiles) {
    $ctx = Resolve-Visa2026IisSlotContext -Profile $profile
    Write-Host "==> $profile ($($ctx.SiteName) :$($ctx.HttpPort))" -ForegroundColor Cyan

    if ($PSCmdlet.ShouldProcess($ctx.SiteName, "Stop and remove IIS site")) {
        & $appcmd stop site $ctx.SiteName 2>$null | Out-Null
        & $appcmd set site $ctx.SiteName /serverAutoStart:false 2>$null | Out-Null
        $siteListed = & $appcmd list site $ctx.SiteName 2>$null
        if ($LASTEXITCODE -eq 0 -and $siteListed) {
            & $appcmd delete site $ctx.SiteName | Out-Null
            $removedSites += $ctx.SiteName
            Write-Host "  Removed site $($ctx.SiteName)" -ForegroundColor Green
        }
    }

    if ($PSCmdlet.ShouldProcess($ctx.AppPoolName, "Stop and remove IIS app pool")) {
        & $appcmd stop apppool $ctx.AppPoolName 2>$null | Out-Null
        $poolListed = & $appcmd list apppool $ctx.AppPoolName 2>$null
        if ($LASTEXITCODE -eq 0 -and $poolListed) {
            & $appcmd delete apppool $ctx.AppPoolName | Out-Null
            $removedPools += $ctx.AppPoolName
            Write-Host "  Removed app pool $($ctx.AppPoolName)" -ForegroundColor Green
        }
    }
}

$bootTask = "Visa2026-IisAfterBoot"
if ($PSCmdlet.ShouldProcess($bootTask, "Unregister scheduled task")) {
    schtasks /Query /TN $bootTask 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        schtasks /Delete /TN $bootTask /F | Out-Null
        Write-Host "Unregistered scheduled task $bootTask" -ForegroundColor Green
    }
}

if (-not $KeepPublishFolders) {
    Write-Warning "KeepPublishFolders is false but folder deletion is not automated. Remove C:\inetpub\visa2026* manually if required."
}

Write-Host ""
Write-Host "IIS removal complete. Sites removed: $($removedSites -join ', ')" -ForegroundColor Green
Write-Host "App pools removed: $($removedPools -join ', ')" -ForegroundColor Green
Write-Host "Publish folders kept under C:\inetpub\visa2026* (backup)." -ForegroundColor DarkGray
