<#
.SYNOPSIS
  Downloads Microsoft Edge WebDriver (msedgedriver.exe) to match your installed Edge browser.

.DESCRIPTION
  Places msedgedriver.exe in $env:USERPROFILE\.local\bin and prepends that directory to your
  user PATH if needed. Required for Visa2026.E2E.Tests (EasyTest Blazor / Edge).

.PARAMETER InstallDir
  Directory for msedgedriver.exe (default: $env:USERPROFILE\.local\bin)

.PARAMETER EdgeVersion
  Optional full version string (e.g. 147.0.3912.98). If omitted, reads Edge from disk and uses
  Microsoft's LATEST_RELEASE_<major>_WINDOWS endpoint.
#>
[CmdletBinding()]
param(
    [string] $InstallDir = (Join-Path $env:USERPROFILE '.local\bin'),
    [string] $EdgeVersion = ''
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Get-InstalledEdgeVersion {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft\Edge\Application\msedge.exe'),
        (Join-Path $env:ProgramFiles 'Microsoft\Edge\Application\msedge.exe')
    )
    foreach ($p in $candidates) {
        if (Test-Path -LiteralPath $p) {
            return (Get-Item -LiteralPath $p).VersionInfo.FileVersion
        }
    }
    throw 'Microsoft Edge (msedge.exe) not found under Program Files. Install Edge first.'
}

if (-not $EdgeVersion) {
    $EdgeVersion = Get-InstalledEdgeVersion
    Write-Host "Detected Microsoft Edge version: $EdgeVersion"
}

$major = ($EdgeVersion -split '\.')[0]
if (-not $major -or $major -notmatch '^\d+$') {
    throw "Could not parse major version from '$EdgeVersion'"
}

# Resolve driver build from Microsoft CDN (matches major branch)
$metaUrl = "https://msedgedriver.azureedge.net/LATEST_RELEASE_${major}_WINDOWS"
Write-Host "Querying: $metaUrl"
$driverVer = (Invoke-WebRequest -Uri $metaUrl -UseBasicParsing -TimeoutSec 60).Content.Trim()
Write-Host "Using Edge WebDriver build: $driverVer"

$zipUrl = "https://msedgedriver.azureedge.net/$driverVer/edgedriver_win64.zip"
$zipPath = Join-Path $env:TEMP "edgedriver_win64_$driverVer.zip"

Write-Host "Downloading: $zipUrl"
Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing -TimeoutSec 120

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
$extractRoot = Join-Path $env:TEMP "edgedriver_extract_$driverVer"
if (Test-Path $extractRoot) { Remove-Item $extractRoot -Recurse -Force }
Expand-Archive -LiteralPath $zipPath -DestinationPath $extractRoot -Force
Remove-Item $zipPath -Force

$exe = Get-ChildItem -Path $extractRoot -Recurse -Filter 'msedgedriver.exe' | Select-Object -First 1
if (-not $exe) { throw 'msedgedriver.exe not found inside the downloaded zip.' }

$target = Join-Path $InstallDir 'msedgedriver.exe'
Copy-Item -LiteralPath $exe.FullName -Destination $target -Force
Remove-Item $extractRoot -Recurse -Force

Write-Host "Installed: $target"
& $target --version

$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
if ($userPath -notlike "*$InstallDir*") {
    [Environment]::SetEnvironmentVariable('Path', "$InstallDir;$userPath", 'User')
    Write-Host "Prepended to user PATH: $InstallDir"
    Write-Host "Open a new terminal (or restart Cursor) so PATH picks up msedgedriver."
} else {
    Write-Host "PATH already contains $InstallDir"
}
