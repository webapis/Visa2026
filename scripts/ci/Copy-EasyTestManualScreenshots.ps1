#Requires -Version 5.1
<#
.SYNOPSIS
  Copy EasyTest milestone PNGs into officer manual screenshot assets.

.DESCRIPTION
  Maps Visa2026.E2E.Tests/recordings/screenshots/{run}/ labels to
  user-manual/assets/screenshots/v{version}/{locale}/ files referenced by guides.
  English captures are replicated to tr/tk/ru until per-locale EasyTest runs (D12).

.PARAMETER ScreenshotRunDir
  Directory containing 00-logon-page.png, 01-after-login.png, etc.

.PARAMETER Version
  screenshotsVersion folder segment (default 2026.08).

.PARAMETER Locales
  Locale codes to receive files (default en,tr,tk,ru).

.EXAMPLE
  ./scripts/ci/Copy-EasyTestManualScreenshots.ps1 -ScreenshotRunDir Visa2026.E2E.Tests/recordings/screenshots/20260804-120000
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ScreenshotRunDir,

    [string]$Version = '2026.08',

    [string[]]$Locales = @('en', 'tr', 'tk', 'ru')
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$runDir = if ([System.IO.Path]::IsPathRooted($ScreenshotRunDir)) {
    $ScreenshotRunDir
} else {
    Join-Path $repoRoot $ScreenshotRunDir
}

if (-not (Test-Path -LiteralPath $runDir)) {
    throw "Screenshot run directory not found: $runDir"
}

$assetsRoot = Join-Path $repoRoot "user-manual\assets\screenshots\v$Version"

# Source label (without .png) -> destination file names under each locale folder
$map = [ordered]@{
    '00-logon-page' = @('login-step-01-logon.png')
    '01-after-login' = @(
        'login-step-02-report-dashboard.png',
        'navigation-step-01-shell.png',
        'navigation-step-02-left-menu.png',
        'person-mark-incomplete-step-04-dashboard.png',
        'report-dashboard-step-01-overview.png',
        'report-dashboard-step-02-category.png',
        'state-notifications-step-01-inbox.png'
    )
    '02-employees-list' = @(
        'navigation-step-03-employees-list.png',
        'report-dashboard-step-03-listview.png',
        'user-report-templates-step-01-list.png',
        'person-register-step-01-employees-list.png',
        'person-document-copies-step-03-list-column.png',
        'person-register-family-member-step-01-family-members-list.png',
        'person-register-temporary-visitor-step-01-list.png',
        'application-create-step-01-applications-list.png',
        'application-document-copies-step-01-items-list.png',
        'application-resminamalar-step-01-app-detail.png'
    )
    '04-employee-detail' = @(
        'navigation-step-04-detail-form.png',
        'person-register-step-03-open-from-list.png',
        'person-dossier-step-01-entry.png',
        'person-register-family-member-step-03-open-from-list.png',
        'person-add-passport-step-01-employee-detail.png',
        'person-document-copies-step-01-detail-toolbar.png',
        'person-add-education-step-01-employee-detail.png',
        'person-add-medical-record-step-01-employee-detail.png',
        'person-add-address-step-01-employee-detail.png',
        'person-add-position-history-step-01-employee-detail.png',
        'person-add-work-duty-step-01-employee-detail.png',
        'person-add-salary-step-01-employee-detail.png',
        'person-add-travel-step-01-employee-detail.png',
        'person-add-cv-documents-step-01-employee-detail.png',
        'person-edit-employee-step-01-detail-form.png',
        'person-dossier-step-02-screen.png',
        'person-edit-family-member-step-01-detail-form.png',
        'person-add-family-relation-documents-step-01-detail.png',
        'person-mark-incomplete-step-01-detail-form.png',
        'person-mark-incomplete-step-02-popup.png',
        'person-mark-incomplete-step-03-incomplete-tab.png',
        'application-create-step-02-type-selected.png',
        'application-add-items-step-02-item-form-new.png',
        'application-progress-step-02-new-row-form.png',
        'user-report-templates-step-02-detail.png'
    )
    '03-employee-created' = @(
        'person-register-step-02-saved-detail.png',
        'person-register-family-member-step-02-saved-detail.png',
        'person-register-temporary-visitor-step-02-saved-detail.png',
        'person-edit-employee-step-02-after-save.png',
        'person-edit-family-member-step-02-after-save.png',
        'application-create-step-03-saved-header.png',
        'application-add-items-step-03-item-saved.png',
        'application-progress-step-03-row-saved.png',
        'application-document-copies-step-03-toast.png',
        'application-resminamalar-step-03-toast.png',
        'person-dossier-step-04-export-toast.png',
        'application-progress-step-01-progress-tab.png',
        'application-document-copies-step-02-panel.png',
        'person-document-copies-step-04-preview.png',
        'application-resminamalar-step-02-catalog.png',
        'template-staging-step-01-catalog-gear.png',
        'template-staging-step-02-edit-sync.png',
        'person-dossier-step-03-copies-slot.png',
        'person-document-copies-step-02-catalog.png'
    )
    '05-passport-detail-new' = @(
        'person-add-passport-step-02-passport-form-new.png',
        'person-add-visa-step-02-visa-form-new.png',
        'person-add-education-step-02-education-form-new.png',
        'person-add-medical-record-step-02-medical-form-new.png',
        'person-add-address-step-02-address-form-new.png',
        'person-add-position-history-step-02-position-form-new.png',
        'person-add-work-duty-step-02-work-duty-form-new.png',
        'person-add-salary-step-02-salary-form-new.png',
        'person-add-travel-step-02-travel-form-new.png',
        'person-add-cv-documents-step-02-document-form-new.png',
        'person-add-family-relation-documents-step-02-form-new.png'
    )
    '06-passport-fields-filled' = @(
        'person-add-passport-step-03-passport-fields-filled.png',
        'person-add-visa-step-03-visa-fields-filled.png',
        'person-add-education-step-03-education-fields-filled.png',
        'person-add-medical-record-step-03-medical-fields-filled.png',
        'person-add-address-step-03-address-fields-filled.png',
        'person-add-position-history-step-03-position-fields-filled.png',
        'person-add-work-duty-step-03-work-duty-fields-filled.png',
        'person-add-salary-step-03-salary-fields-filled.png',
        'person-add-travel-step-03-travel-fields-filled.png',
        'person-add-cv-documents-step-03-document-file-attached.png',
        'person-add-family-relation-documents-step-03-file-attached.png'
    )
    '07-passport-saved' = @(
        'person-add-passport-step-04-passport-saved.png',
        'person-add-visa-step-01-passport-detail.png',
        'person-add-visa-step-04-visa-saved.png',
        'person-add-education-step-04-education-saved.png',
        'person-add-medical-record-step-04-medical-saved.png',
        'person-add-address-step-04-address-saved.png',
        'person-add-position-history-step-04-position-saved.png',
        'person-add-work-duty-step-04-work-duty-saved.png',
        'person-add-salary-step-04-salary-saved.png',
        'person-add-travel-step-04-travel-saved.png',
        'person-add-cv-documents-step-04-document-saved.png',
        'person-add-family-relation-documents-step-04-saved.png'
    )
}

function Copy-MappedScreenshot {
    param(
        [string]$SourcePath,
        [string]$TargetPath
    )

    $targetParent = Split-Path -Parent $TargetPath
    New-Item -ItemType Directory -Force -Path $targetParent | Out-Null
    Copy-Item -LiteralPath $SourcePath -Destination $TargetPath -Force
    Write-Host "  -> $TargetPath"
}

$copied = 0
foreach ($entry in $map.GetEnumerator()) {
    $matches = @(Get-ChildItem -LiteralPath $runDir -Filter ($entry.Key + '*.png') -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending)
    if ($matches.Count -eq 0) {
        Write-Warning "Missing source screenshot for label '$($entry.Key)' in $runDir"
        continue
    }

    $sourceFile = $matches[0].FullName
    foreach ($locale in $Locales) {
        $localeDir = Join-Path $assetsRoot $locale
        foreach ($destName in $entry.Value) {
            $destPath = Join-Path $localeDir $destName
            Copy-MappedScreenshot -SourcePath $sourceFile -TargetPath $destPath
            $copied++
        }
    }
}

if ($copied -eq 0) {
    throw "No screenshots copied from $runDir"
}

Write-Host "Copied $copied manual screenshot file(s) to $assetsRoot"
