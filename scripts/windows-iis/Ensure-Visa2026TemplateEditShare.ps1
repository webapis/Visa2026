#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Create or refresh the Resminamalar template-edit SMB share for one IIS slot.

.DESCRIPTION
  Resminamalar desktop Word/Excel editing uses a UNC path (TemplateEditStaging:StagingRootUnc).
  This script creates the on-disk folder, NTFS ACLs, and Windows SMB share per slot so the app
  pool and officers can read/write staged .docx / .xlsx files.

  Idempotent: safe to run on every deploy (Install-Visa2026IisSlots / Deploy-Visa2026IisSlotRemote).

.PARAMETER Profile
  Production, Staging, Demo, Legacy, or All (default Production).

.PARAMETER UncHost
  Host name used in the UNC path (\\HOST\share). Default: TEMPLATE_EDIT_UNC_HOST from slot env,
  else short DNS host name, else COMPUTERNAME.

.PARAMETER OfficersPrincipal
  Optional NTFS grant for officers (Modify). Default: TEMPLATE_EDIT_OFFICERS_PRINCIPAL from slot
  env file when set. Example: DOMAIN\VisaOfficers or "NT AUTHORITY\Authenticated Users".

.PARAMETER SkipOfficersAcl
  Do not grant any officer principal (app pool only).

.EXAMPLE
  .\Ensure-Visa2026TemplateEditShare.ps1 -Profile Production

.EXAMPLE
  .\Ensure-Visa2026TemplateEditShare.ps1 -Profile All -OfficersPrincipal "COMPANY\Visa Users"

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
  Feature: docs/TEMPLATE_STAGING_EDIT.md
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Production", "Staging", "Demo", "Legacy", "All")]
    [string]$Profile = "Production",

    [string]$UncHost = "",
    [string]$OfficersPrincipal = "",
    [switch]$SkipOfficersAcl
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

function Ensure-Visa2026TemplateEditShareForSlot {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Production", "Staging", "Demo", "Legacy")]
        [string]$SlotProfile,

        [string]$UncHost = "",
        [string]$OfficersPrincipal = "",
        [switch]$SkipOfficersAcl
    )

    $slot = Get-Visa2026IisSlotDefinition -Profile $SlotProfile
    $localPath = $slot.TemplateEditLocalPath
    $shareName = $slot.TemplateEditShareName
    $appPoolGrantee = "IIS AppPool\$($slot.AppPoolName):(OI)(CI)M"

    $envMap = Read-Visa2026DotEnvMap -Path $slot.EnvFile
    if ([string]::IsNullOrWhiteSpace($OfficersPrincipal) -and -not $SkipOfficersAcl) {
        if ($envMap.ContainsKey('TEMPLATE_EDIT_OFFICERS_PRINCIPAL') -and $envMap['TEMPLATE_EDIT_OFFICERS_PRINCIPAL']) {
            $OfficersPrincipal = $envMap['TEMPLATE_EDIT_OFFICERS_PRINCIPAL'].Trim()
        }
    }

    $stagingUnc = Get-Visa2026TemplateEditStagingUnc -Profile $SlotProfile -UncHost $UncHost -EnvFile $slot.EnvFile

    Write-Host ""
    Write-Host "Template edit share — $($slot.Profile)" -ForegroundColor Cyan
    Write-Host "  Local path : $localPath"
    Write-Host "  SMB share  : $shareName"
    Write-Host "  UNC        : $stagingUnc"

    if ($PSCmdlet.ShouldProcess($localPath, "Ensure template edit folder and SMB share")) {
        New-Item -ItemType Directory -Force -Path $localPath | Out-Null

        $existingShare = Get-SmbShare -Name $shareName -ErrorAction SilentlyContinue
        if ($existingShare) {
            if ($existingShare.Path -ne $localPath) {
                Write-Host "  Recreating SMB share (path changed)." -ForegroundColor Yellow
                Remove-SmbShare -Name $shareName -Force
                New-SmbShare -Name $shareName -Path $localPath -FullAccess "Administrators" | Out-Null
            }
            else {
                Write-Host "  SMB share already exists." -ForegroundColor DarkGray
            }
        }
        else {
            New-SmbShare -Name $shareName -Path $localPath -FullAccess "Administrators" | Out-Null
            Write-Host "  Created SMB share." -ForegroundColor Green
        }

        icacls $localPath /inheritance:e | Out-Null
        icacls $localPath /grant $appPoolGrantee | Out-Null

        if (-not $SkipOfficersAcl -and -not [string]::IsNullOrWhiteSpace($OfficersPrincipal)) {
            icacls $localPath /grant "${OfficersPrincipal}:(OI)(CI)M" | Out-Null
            Write-Host "  Officers ACL: $OfficersPrincipal" -ForegroundColor DarkGray
        }
        elseif (-not $SkipOfficersAcl) {
            Write-Warning "No TEMPLATE_EDIT_OFFICERS_PRINCIPAL in $($slot.EnvFile). Officers may not have Modify on the share until you grant ACLs or re-run with -OfficersPrincipal."
        }

        if (-not (Test-Path -LiteralPath $stagingUnc)) {
            Write-Warning "UNC not reachable from this session: $stagingUnc. Share created; verify from officer PC and app pool identity."
        }
        else {
            Write-Host "  UNC reachable from this account." -ForegroundColor Green
        }
    }

    [PSCustomObject]@{
        Profile       = $slot.Profile
        LocalPath     = $localPath
        ShareName     = $shareName
        StagingRootUnc = $stagingUnc
        AppPoolName   = $slot.AppPoolName
    }
}

$profiles = if ($Profile -eq "All") { Get-Visa2026IisSlotProfiles } else { @($Profile) }
$results = @()

foreach ($name in $profiles) {
    $results += Ensure-Visa2026TemplateEditShareForSlot `
        -SlotProfile $name `
        -UncHost $UncHost `
        -OfficersPrincipal $OfficersPrincipal `
        -SkipOfficersAcl:$SkipOfficersAcl
}

return $results
