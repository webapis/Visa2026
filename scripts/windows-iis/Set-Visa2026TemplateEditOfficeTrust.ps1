#Requires -Version 5.1
<#
.SYNOPSIS
  Trust the Visa2026 IIS server for Office template editing (officer workstation).

.DESCRIPTION
  Resminamalar "Edit template" opens desktop Word/Excel via ms-word:/ms-excel: after exporting
  to the officer PC local sandbox. Office may block with:

    "Unsafe Content — coming from a site in the Restricted Sites zone"

  This script maps the Visa2026 HTTPS origin to the Local intranet zone (Internet Explorer
  zone settings still used by Office protocol handlers).

  Run on each officer PC, not on the IIS server. Sign out/in or reboot after running if Word
  was already open.

.PARAMETER ServerHost
  Host name or IP officers use in the browser (HTTPS). Default: 10.100.128.25

.PARAMETER Profile
  IIS slot label for log output only.

.PARAMETER AllUsers
  Write HKLM instead of HKCU (requires Administrator). Use for shared PCs.

.PARAMETER IncludeLocalhost
  Also trust http://localhost and https://localhost (and 127.0.0.1) for F5 dev.

.EXAMPLE
  # Local dev (F5 on localhost:5001) + production server
  .\scripts\windows-iis\Set-Visa2026TemplateEditOfficeTrust.ps1 -IncludeLocalhost

.EXAMPLE
  .\scripts\windows-iis\Set-Visa2026TemplateEditOfficeTrust.ps1 -ServerHost ENJ18VWSPVIZE2

.EXAMPLE
  # Elevated — all profiles on a shared workstation
  .\scripts\windows-iis\Set-Visa2026TemplateEditOfficeTrust.ps1 -AllUsers

.NOTES
  Feature: docs/TEMPLATE_STAGING_EDIT.md
    User Configuration → Administrative Templates → Windows Components →
    Internet Explorer → Internet Control Panel → Security Page →
    Site to Zone Assignment List → http://10.100.128.25 = 1

  Registry (this script):
    HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings
      UNCAsIntranet = 1
      ZoneMap\Ranges\<key> → :Range = <ServerHost>, http/file/https = 1
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ServerHost = "10.100.128.25",

    [ValidateSet("Production", "Staging", "Demo", "Legacy")]
    [string]$Profile = "Production",

    [switch]$AllUsers,
    [switch]$IncludeLocalhost
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

if ($AllUsers) {
    $identity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not $identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "AllUsers requires Administrator. Re-run elevated or omit -AllUsers for HKCU only."
    }
}

$ServerHost = $ServerHost.Trim().TrimEnd('\', '/')
if ([string]::IsNullOrWhiteSpace($ServerHost)) {
    throw "ServerHost is required."
}

$slot = Get-Visa2026IisSlotDefinition -Profile $Profile

$hive = if ($AllUsers) { "HKLM" } else { "HKCU" }
$baseKey = "$hive`:\Software\Microsoft\Windows\CurrentVersion\Internet Settings"
$zoneMapKey = Join-Path $baseKey "ZoneMap"
$rangesKey = Join-Path $zoneMapKey "Ranges"

function Set-DwordRegistryValue {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][int]$Value
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        if ($PSCmdlet.ShouldProcess($Path, "Create registry key")) {
            New-Item -Path $Path -Force | Out-Null
        }
    }

    if ($PSCmdlet.ShouldProcess($Path, "Set $Name = $Value")) {
        Set-ItemProperty -LiteralPath $Path -Name $Name -Value $Value -Type DWord -Force
    }
}

function Remove-RestrictedSiteEntry {
    param([string]$HostName)

    $escaped = [regex]::Escape($HostName)
    $domainsKey = Join-Path $zoneMapKey "Domains"
    if (Test-Path -LiteralPath $domainsKey) {
        Get-ChildItem -LiteralPath $domainsKey -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.PSChildName -eq $HostName -or $_.Name -match $escaped } |
            ForEach-Object {
                $props = Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction SilentlyContinue
                $isRestricted = ($props.http -eq 4) -or ($props.https -eq 4) -or ($props.file -eq 4)
                if (-not $isRestricted) {
                    return
                }

                if ($PSCmdlet.ShouldProcess($_.PSPath, "Remove restricted-site zone entry")) {
                    Remove-Item -LiteralPath $_.PSPath -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
    }

    if (Test-Path -LiteralPath $rangesKey) {
        Get-ChildItem -LiteralPath $rangesKey -ErrorAction SilentlyContinue |
            ForEach-Object {
                $rangeValue = (Get-ItemProperty -LiteralPath $_.PSPath -Name ":Range" -ErrorAction SilentlyContinue).":Range"
                if ($rangeValue -eq $HostName) {
                    $httpZone = (Get-ItemProperty -LiteralPath $_.PSPath -Name "http" -ErrorAction SilentlyContinue).http
                    if ($httpZone -eq 4) {
                        if ($PSCmdlet.ShouldProcess($_.PSPath, "Remove restricted-site range entry")) {
                            Remove-Item -LiteralPath $_.PSPath -Recurse -Force
                        }
                    }
                }
            }
    }
}

function Set-ZoneRange {
    param(
        [Parameter(Mandatory = $true)][string]$KeyName,
        [Parameter(Mandatory = $true)][string]$RangeValue,
        [Parameter(Mandatory = $true)][ValidateSet(1, 2)][int]$Zone
    )

    $path = Join-Path $rangesKey $KeyName
    if (-not (Test-Path -LiteralPath $path)) {
        if ($PSCmdlet.ShouldProcess($path, "Create zone range")) {
            New-Item -Path $path -Force | Out-Null
        }
    }

    if ($PSCmdlet.ShouldProcess($path, "Map $RangeValue to zone $Zone")) {
        Set-ItemProperty -LiteralPath $path -Name ":Range" -Value $RangeValue -Force
        Set-DwordRegistryValue -Path $path -Name "http" -Value $Zone
        Set-DwordRegistryValue -Path $path -Name "https" -Value $Zone
        Set-DwordRegistryValue -Path $path -Name "file" -Value $Zone
    }
}

function Set-ZoneDomain {
    param(
        [Parameter(Mandatory = $true)][string]$DomainName,
        [Parameter(Mandatory = $true)][ValidateSet(1, 2)][int]$Zone
    )

    $path = Join-Path (Join-Path $zoneMapKey "Domains") $DomainName
    if (-not (Test-Path -LiteralPath $path)) {
        if ($PSCmdlet.ShouldProcess($path, "Create zone domain")) {
            New-Item -Path $path -Force | Out-Null
        }
    }

    if ($PSCmdlet.ShouldProcess($path, "Map $DomainName to zone $Zone")) {
        Set-DwordRegistryValue -Path $path -Name "http" -Value $Zone
        Set-DwordRegistryValue -Path $path -Name "https" -Value $Zone
        Set-DwordRegistryValue -Path $path -Name "file" -Value $Zone
    }

    Remove-RestrictedSiteEntry -HostName $DomainName
}

function Set-IntranetZoneRange {
    param(
        [Parameter(Mandatory = $true)][string]$KeyName,
        [Parameter(Mandatory = $true)][string]$RangeValue
    )

    Set-ZoneRange -KeyName $KeyName -RangeValue $RangeValue -Zone 1
}

function Set-IntranetZoneDomain {
    param([Parameter(Mandatory = $true)][string]$DomainName)

    Set-ZoneDomain -DomainName $DomainName -Zone 1
}

$rangeKeyName = "Visa2026TemplateEdit_" + ($ServerHost -replace '[^a-zA-Z0-9]', '_')

Write-Host ""
Write-Host "Visa2026 template edit - Office trust ($hive)" -ForegroundColor Cyan
Write-Host "  Server host : $ServerHost"
Write-Host "  Web origin  : https://$ServerHost"
Write-Host ""

if ($PSCmdlet.ShouldProcess($baseKey, "Configure UNCAsIntranet and intranet zone map")) {
    Set-DwordRegistryValue -Path $baseKey -Name "UNCAsIntranet" -Value 1
    Set-DwordRegistryValue -Path $baseKey -Name "WarnOnIntranet" -Value 0

    if (-not (Test-Path -LiteralPath $rangesKey)) {
        New-Item -Path $rangesKey -Force | Out-Null
    }

    Set-IntranetZoneRange -KeyName $rangeKeyName -RangeValue $ServerHost
    Remove-RestrictedSiteEntry -HostName $ServerHost

    if ($IncludeLocalhost) {
        # Office ms-word: from localhost often needs Trusted Sites (zone 2), not only intranet (zone 1).
        Set-ZoneDomain -DomainName "localhost" -Zone 2
        Set-ZoneRange -KeyName "Visa2026TemplateEdit_localhost" -RangeValue "127.0.0.1" -Zone 2
        Remove-RestrictedSiteEntry -HostName "127.0.0.1"
        Write-Host "  localhost + 127.0.0.1 -> trusted sites (zone 2) for F5 dev" -ForegroundColor Green
    }

    Write-Host "Registry updated:" -ForegroundColor Green
    Write-Host "  $baseKey"
    Write-Host "    UNCAsIntranet = 1"
    Write-Host "    ZoneMap\Ranges\$rangeKeyName -> intranet (zone 1) for http/https/file"
    Write-Host ""
}

$uncAsIntranet = (Get-ItemProperty -LiteralPath $baseKey -Name "UNCAsIntranet" -ErrorAction SilentlyContinue).UNCAsIntranet
Write-Host "Verification ($hive):" -ForegroundColor Cyan
Write-Host "  UNCAsIntranet = $uncAsIntranet"
if ($IncludeLocalhost) {
    $localhostZone = (Get-ItemProperty -LiteralPath (Join-Path $zoneMapKey "Domains\localhost") -Name "http" -ErrorAction SilentlyContinue).http
    Write-Host "  localhost http zone = $localhostZone (2 = trusted sites, 1 = intranet, 4 = restricted)"
    if ($localhostZone -ne 2) {
        Write-Warning "localhost is not in trusted sites (zone 2). Re-run without -WhatIf or check permissions."
    }
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Close Word and Edge completely (or sign out / reboot)."
Write-Host "  2. Hard-refresh Visa2026 (Ctrl+F5), click Edit template again."
Write-Host "  3. If Office still blocks, open the file from Explorer (your chosen sandbox folder),"
Write-Host "     or use Copy path from Resminamalar after Edit template."
Write-Host ""
