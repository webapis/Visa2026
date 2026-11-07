<#
.SYNOPSIS
  Sample installer for Visa2026 Portal Filing Playwright worker (filling PC / RDP desktop).

.DESCRIPTION
  Copies the published worker next to this script into C:\PortalFiling (default),
  creates edge-profile and screenshots folders, and seeds appsettings.Local.json
  from the example when missing. Does not store migration.gov.tm passwords.

  Scaffold: form fill / Visa2026 job API not implemented yet.
#>
[CmdletBinding()]
param(
    [string] $InstallRoot = "C:\PortalFiling",
    [string] $Visa2026BaseUrl = "",
    [switch] $SkipPlaywrightBrowserInstall
)

$ErrorActionPreference = "Stop"

$packageRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $packageRoot "Visa2026.PortalFiling.Worker.exe"))) {
    # Layout when running from zip extract: install\ next to exe, or flat publish with install\
    $candidate = Join-Path $PSScriptRoot "..\Visa2026.PortalFiling.Worker.exe"
    if (Test-Path $candidate) {
        $packageRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    }
}

$exe = Join-Path $packageRoot "Visa2026.PortalFiling.Worker.exe"
if (-not (Test-Path $exe)) {
    throw "Visa2026.PortalFiling.Worker.exe not found beside install folder. Extract the full publish zip first."
}

$workerDir = Join-Path $InstallRoot "worker"
$edgeDir = Join-Path $InstallRoot "edge-profile"
$shotDir = Join-Path $InstallRoot "screenshots"

Write-Host "Installing Portal Filing worker to $InstallRoot"

New-Item -ItemType Directory -Force -Path $workerDir, $edgeDir, $shotDir | Out-Null

Write-Host "Copying worker files -> $workerDir"
Get-ChildItem -LiteralPath $packageRoot -Force | Where-Object {
    $_.Name -ne "install" -and $_.Name -notlike "Install-PortalFilingWorker.ps1"
} | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $workerDir -Recurse -Force
}

$localCfg = Join-Path $workerDir "appsettings.Local.json"
$exampleCfg = Join-Path $workerDir "appsettings.Local.json.example"
if (-not (Test-Path $localCfg) -and (Test-Path $exampleCfg)) {
    Copy-Item -LiteralPath $exampleCfg -Destination $localCfg -Force
    Write-Host "Created appsettings.Local.json from example — edit Visa2026 credentials."
}

# Patch paths in local config when possible (simple replace of default paths)
if (Test-Path $localCfg) {
    $json = Get-Content -LiteralPath $localCfg -Raw -Encoding UTF8
    $json = $json -replace "C:\\\\PortalFiling\\\\edge-profile", ($edgeDir -replace "\\", "\\")
    $json = $json -replace "C:\\\\PortalFiling\\\\screenshots", ($shotDir -replace "\\", "\\")
    if ($Visa2026BaseUrl) {
        $escaped = $Visa2026BaseUrl.TrimEnd("/") + "/"
        $json = $json -replace '"BaseUrl"\s*:\s*"[^"]*"', "`"BaseUrl`": `"$escaped`"", 1
    }
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($localCfg, $json, $utf8NoBom)
}

$versionSrc = Join-Path $packageRoot "portal-filing-version.txt"
if (Test-Path $versionSrc) {
    Copy-Item -LiteralPath $versionSrc -Destination (Join-Path $InstallRoot "portal-filing-version.txt") -Force
}

if (-not $SkipPlaywrightBrowserInstall) {
    $playwrightPs1 = Join-Path $workerDir "playwright.ps1"
    if (Test-Path $playwrightPs1) {
        Write-Host "Installing Playwright msedge browser bits (once)..."
        & pwsh -NoProfile -File $playwrightPs1 install msedge
    } else {
        Write-Warning "playwright.ps1 not found under $workerDir — run Playwright install manually after first build that includes it."
    }
}

Write-Host ""
Write-Host "Install complete."
Write-Host "  Worker:      $workerDir\Visa2026.PortalFiling.Worker.exe"
Write-Host "  Edge profile: $edgeDir  (sign into rgstr.migration.gov.tm once in Edge using this profile)"
Write-Host "  Screenshots:  $shotDir"
Write-Host ""
Write-Host "Attended / RDP: leave a Windows user session logged on; connect with Remote Desktop to watch runs."
Write-Host "Next: edit appsettings.Local.json, then run the exe (ScaffoldOnly=true exits after config check)."