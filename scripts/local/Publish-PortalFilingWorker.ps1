<#
.SYNOPSIS
  Publishes Visa2026.PortalFiling.Worker as a sample win-x64 zip (installer + binaries).

.DESCRIPTION
  Output: artifacts/portal-filing-worker/Visa2026.PortalFiling.Worker-win-x64-*.zip
  Optional: copy zip into Blazor wwwroot/downloads/portal-filing for later admin download page.

  Does not implement portal RPA; packages the scaffold worker for filling-PC / RDP install.
#>
[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64",
    [switch] $FrameworkDependent,
    [switch] $CopyToBlazorDownloads,
    [string] $RepoRoot = ""
)

$ErrorActionPreference = "Stop"

if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

$project = Join-Path $RepoRoot "Visa2026.PortalFiling.Worker\Visa2026.PortalFiling.Worker.csproj"
if (-not (Test-Path $project)) {
    throw "Project not found: $project"
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outRoot = Join-Path $RepoRoot "artifacts\portal-filing-worker"
$publishDir = Join-Path $outRoot "publish-$Runtime"
$stageDir = Join-Path $outRoot "stage-$stamp"
$zipName = "Visa2026.PortalFiling.Worker-$Runtime-$stamp.zip"
$zipPath = Join-Path $outRoot $zipName

if (Test-Path $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
if (Test-Path $stageDir) { Remove-Item -LiteralPath $stageDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publishDir, $stageDir | Out-Null

$selfContained = -not $FrameworkDependent
Write-Host "dotnet publish ($Configuration, $Runtime, self-contained=$selfContained)..."
$publishArgs = @(
    "publish", $project,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", $(if ($selfContained) { "true" } else { "false" }),
    "-o", $publishDir,
    "/p:PublishSingleFile=false"
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

# Stage package layout: binaries at root + install\ scripts
Copy-Item -Path (Join-Path $publishDir "*") -Destination $stageDir -Recurse -Force
$installSrc = Join-Path $RepoRoot "Visa2026.PortalFiling.Worker\install"
$installDst = Join-Path $stageDir "install"
New-Item -ItemType Directory -Force -Path $installDst | Out-Null
Copy-Item -Path (Join-Path $installSrc "*") -Destination $installDst -Recurse -Force

$exampleSrc = Join-Path $RepoRoot "Visa2026.PortalFiling.Worker\appsettings.Local.json.example"
if (Test-Path $exampleSrc) {
    Copy-Item -LiteralPath $exampleSrc -Destination (Join-Path $stageDir "appsettings.Local.json.example") -Force
}

$versionText = @"
Product=Visa2026.PortalFiling.Worker
Runtime=$Runtime
Configuration=$Configuration
SelfContained=$selfContained
BuiltUtc=$((Get-Date).ToUniversalTime().ToString("o"))
ScaffoldOnly=true
"@
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Join-Path $stageDir "portal-filing-version.txt"), $versionText.Trim() + "`n", $utf8)

if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Write-Host "Zipping $zipPath ..."
Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -Force

# Convenience: also write latest.zip
$latestZip = Join-Path $outRoot "Visa2026.PortalFiling.Worker-$Runtime-latest.zip"
Copy-Item -LiteralPath $zipPath -Destination $latestZip -Force

if ($CopyToBlazorDownloads) {
    $dl = Join-Path $RepoRoot "Visa2026.Blazor.Server\wwwroot\downloads\portal-filing"
    New-Item -ItemType Directory -Force -Path $dl | Out-Null
    Copy-Item -LiteralPath $zipPath -Destination (Join-Path $dl $zipName) -Force
    Copy-Item -LiteralPath $latestZip -Destination (Join-Path $dl "Visa2026.PortalFiling.Worker-$Runtime-latest.zip") -Force
    Write-Host "Copied zip(s) to $dl"
}

Write-Host ""
Write-Host "Published sample installer package:"
Write-Host "  $zipPath"
Write-Host "  $latestZip"
Write-Host "Install on filling PC: extract zip, run install\Install-PortalFilingWorker.ps1"