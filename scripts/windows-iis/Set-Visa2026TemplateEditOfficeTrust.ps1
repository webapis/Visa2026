#Requires -Version 5.1
<#
.SYNOPSIS
  Trust the Visa2026 IIS server for Office template editing (officer workstation).

.DESCRIPTION
  Resminamalar "Edit template" opens desktop Word via ms-word: while the .docx lives on an
  SMB share (\\server\Visa2026TemplateEdit-Prod). Office may block with:

    "Unsafe Content — coming from a site in the Restricted Sites zone"

  This script configures Internet Explorer security zones (used by Office) so the Visa2026
  server and UNC paths are treated as Local intranet:

    - UNCAsIntranet = 1  (UNC shares → intranet zone)
    - Zone map for http(s)://<server> and file://<server>
    - Optional removal of the host from Restricted Sites (zone 4) if present

  Run on each officer PC (e.g. 10.100.64.x), not on the IIS server. Sign out/in or reboot
  after running if Word was already open.

  Fleet deploy: mirror these registry values with Group Policy — see NOTES.

.PARAMETER ServerHost
  Host name or IP officers use in the browser and UNC paths. Default: 10.100.128.25

.PARAMETER Profile
  IIS slot — used only to print the expected SMB share name in verification output.

.PARAMETER AllUsers
  Write HKLM instead of HKCU (requires Administrator). Use for shared PCs.

.PARAMETER SkipShareTest
  Do not run Test-Path on the template-edit UNC share.

.EXAMPLE
  .\scripts\windows-iis\Set-Visa2026TemplateEditOfficeTrust.ps1

.EXAMPLE
  .\scripts\windows-iis\Set-Visa2026TemplateEditOfficeTrust.ps1 -ServerHost ENJ18VWSPVIZE2

.EXAMPLE
  # Elevated — all profiles on a shared workstation
  .\scripts\windows-iis\Set-Visa2026TemplateEditOfficeTrust.ps1 -AllUsers

.NOTES
  Feature: docs/TEMPLATE_STAGING_EDIT.md
  Server share setup: Ensure-Visa2026TemplateEditShare.ps1 (run on Windows Server)

  GPO equivalent (user scope):
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
    [switch]$SkipShareTest
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
$shareName = $slot.TemplateEditShareName
$uncShare = "\\$ServerHost\$shareName"
$httpOrigin = if ($ServerHost -match '^\d') { "http://$ServerHost" } else { "http://$ServerHost" }
$httpsOrigin = if ($ServerHost -match '^\d') { "https://$ServerHost" } else { "https://$ServerHost" }

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

$rangeKeyName = "Visa2026TemplateEdit_" + ($ServerHost -replace '[^a-zA-Z0-9]', '_')
$rangeKeyPath = Join-Path $rangesKey $rangeKeyName

Write-Host ""
Write-Host "Visa2026 template edit - Office / intranet trust ($hive)" -ForegroundColor Cyan
Write-Host "  Server host : $ServerHost"
Write-Host "  SMB share   : $uncShare"
Write-Host "  Web origin  : $httpOrigin"
Write-Host ""

if ($PSCmdlet.ShouldProcess($baseKey, "Configure UNCAsIntranet and intranet zone map")) {
    Set-DwordRegistryValue -Path $baseKey -Name "UNCAsIntranet" -Value 1
    Set-DwordRegistryValue -Path $baseKey -Name "WarnOnIntranet" -Value 0

    if (-not (Test-Path -LiteralPath $rangesKey)) {
        New-Item -Path $rangesKey -Force | Out-Null
    }

    if (-not (Test-Path -LiteralPath $rangeKeyPath)) {
        New-Item -Path $rangeKeyPath -Force | Out-Null
    }

    Set-ItemProperty -LiteralPath $rangeKeyPath -Name ":Range" -Value $ServerHost -Force
    Set-DwordRegistryValue -Path $rangeKeyPath -Name "http" -Value 1
    Set-DwordRegistryValue -Path $rangeKeyPath -Name "https" -Value 1
    Set-DwordRegistryValue -Path $rangeKeyPath -Name "file" -Value 1

    Remove-RestrictedSiteEntry -HostName $ServerHost

    Write-Host "Registry updated:" -ForegroundColor Green
    Write-Host "  $baseKey"
    Write-Host "    UNCAsIntranet = 1"
    Write-Host "    ZoneMap\Ranges\$rangeKeyName -> intranet (zone 1) for http/https/file"
    Write-Host ""
}

$uncAsIntranet = (Get-ItemProperty -LiteralPath $baseKey -Name "UNCAsIntranet" -ErrorAction SilentlyContinue).UNCAsIntranet
Write-Host "Verification ($hive):" -ForegroundColor Cyan
Write-Host "  UNCAsIntranet = $uncAsIntranet"

if (-not $SkipShareTest) {
    Write-Host ""
    Write-Host "SMB share reachability (current Windows user):" -ForegroundColor Cyan
    if (Test-Path -LiteralPath $uncShare) {
        Write-Host "  OK - $uncShare is reachable." -ForegroundColor Green
    }
    else {
        Write-Host "  NOT reachable - $uncShare" -ForegroundColor Yellow
        Write-Host "  Port 445 may be open but SMB auth/share ACL can still block access."
        Write-Host "  Try: net use $uncShare /user:DOMAIN\YourUsername"
        Write-Host "  Server script: Ensure-Visa2026TemplateEditShare.ps1 -Profile $Profile"
    }
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Close Word and Edge completely (or sign out / reboot)."
Write-Host "  2. Hard-refresh Visa2026 (Ctrl+F5), click Edit template again."
Write-Host "  3. If Office still blocks, open the file from Explorer:"
Write-Host ('       explorer.exe "' + $uncShare + '"')
Write-Host ""
