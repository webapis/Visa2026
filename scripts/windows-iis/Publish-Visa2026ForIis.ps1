#Requires -Version 5.1
<#
.SYNOPSIS
  [WORKSTATION OR BUILD AGENT] Publish Visa2026.Blazor.Server for IIS on Windows Server (no Docker).

.DESCRIPTION
  Runs dotnet publish -c Release and writes a folder ready to copy to e.g. C:\inetpub\visa2026.
  Requires DevExpress.Key\DevExpress_License.txt (same as CI / docker build).

  On the server: configure appsettings.Production.json + app pool env vars, then run
  Update-Visa2026Database.ps1 before first user traffic.

.EXAMPLE
  .\scripts\windows-iis\Publish-Visa2026ForIis.ps1

.EXAMPLE
  .\scripts\windows-iis\Publish-Visa2026ForIis.ps1 -OutputPath D:\artifacts\visa2026 -Zip

.NOTES
  Runbook: docs/ON_PREM_WINDOWS_IIS.md
#>
param(
    [string]$OutputPath = "",
    [ValidateSet("Debug", "Release", "EasyTest")]
    [string]$Configuration = "Release",
    [switch]$Zip,
    [switch]$OpenOutputFolder
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$licensePath = Join-Path $RepoRoot "DevExpress.Key\DevExpress_License.txt"
if (-not (Test-Path -LiteralPath $licensePath)) {
    throw "Missing DevExpress.Key\DevExpress_License.txt (required for publish, same as CI)."
}

function Get-AssemblyVersion {
    $csprojPath = Join-Path $RepoRoot "Visa2026.Module\Visa2026.Module.csproj"
    $xml = [xml](Get-Content -Raw $csprojPath)
    $version = $xml.Project.PropertyGroup.AssemblyVersion
    if (-not $version) {
        throw "Could not read AssemblyVersion from Visa2026.Module\Visa2026.Module.csproj"
    }
    return [string]$version
}

function Get-GitShaShort {
    $sha = git -C $RepoRoot rev-parse --short=7 HEAD 2>$null
    if (-not $sha) { return "local" }
    return $sha
}

$version = Get-AssemblyVersion
$gitSha = Get-GitShaShort

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot "dist\visa2026-iis-$version"
}

$publishRoot = [System.IO.Path]::GetFullPath($OutputPath)
$projectPath = Join-Path $RepoRoot "Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj"

Write-Host "==> Visa2026 IIS publish" -ForegroundColor Cyan
Write-Host "    Version: $version ($gitSha)"
Write-Host "    Config:  $Configuration"
Write-Host "    Output:  $publishRoot"

if (Test-Path -LiteralPath $publishRoot) {
    Write-Host "==> Removing existing output folder" -ForegroundColor Yellow
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

$publishArgs = @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-o", $publishRoot,
    "/p:UseAppHost=true",
    "/p:InformationalVersion=$version+$gitSha"
)

Write-Host "==> dotnet publish ..." -ForegroundColor Cyan
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exampleSource = Join-Path $PSScriptRoot "appsettings.Production.json.example"
$exampleDest = Join-Path $publishRoot "appsettings.Production.json.example"
if (Test-Path -LiteralPath $exampleSource) {
    Copy-Item -LiteralPath $exampleSource -Destination $exampleDest -Force
}

$readmeSource = Join-Path $PSScriptRoot "README.md"
$readmeDest = Join-Path $publishRoot "DEPLOY_README.txt"
if (Test-Path -LiteralPath $readmeSource) {
    Copy-Item -LiteralPath $readmeSource -Destination $readmeDest -Force
}

$versionFile = Join-Path $publishRoot "publish-version.txt"
@(
    "AssemblyVersion=$version"
    "GitSha=$gitSha"
    "Configuration=$Configuration"
    "PublishedUtc=$(Get-Date -Format o)"
) | Set-Content -LiteralPath $versionFile -Encoding UTF8

$zipPath = $null
if ($Zip) {
    $zipPath = "$publishRoot.zip"
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Write-Host "==> Creating $zipPath" -ForegroundColor Cyan
    Compress-Archive -LiteralPath $publishRoot -DestinationPath $zipPath -CompressionLevel Optimal
}

Write-Host ""
Write-Host "Publish complete." -ForegroundColor Green
Write-Host "  Folder: $publishRoot"
if ($zipPath) { Write-Host "  Zip:    $zipPath" }
Write-Host ""
Write-Host "Next (server):" -ForegroundColor DarkGray
Write-Host "  1. Copy folder to C:\inetpub\visa2026 (keep appsettings.Production.json + DataProtection keys on update)"
Write-Host "  2. Set app pool env: ASPNETCORE_ENVIRONMENT=Production, DEVEXPRESS_LICENSEKEY=..."
Write-Host "  3. .\Update-Visa2026Database.ps1 -PublishPath C:\inetpub\visa2026"
Write-Host "  4. Start IIS site - see docs/ON_PREM_WINDOWS_IIS.md"

if ($OpenOutputFolder) {
    explorer.exe $publishRoot
}
