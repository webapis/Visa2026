#Requires -Version 5.1
<#
.SYNOPSIS
  Prepare versioned + latest folders for GitHub Pages (gh-pages branch).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReportDir,

    [Parameter(Mandatory = $true)]
    [string]$PublishRoot,

    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ReportDir)) {
    throw "Report directory not found: $ReportDir"
}

$reportsRoot = Join-Path $PublishRoot 'test-reports'
$versionDir = Join-Path $reportsRoot $Version
$latestDir = Join-Path $reportsRoot 'latest'

if (Test-Path -LiteralPath $versionDir) {
    Remove-Item -LiteralPath $versionDir -Recurse -Force
}

if (Test-Path -LiteralPath $latestDir) {
    Remove-Item -LiteralPath $latestDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $reportsRoot | Out-Null
Copy-Item -LiteralPath $ReportDir -Destination $versionDir -Recurse -Force
Copy-Item -LiteralPath $ReportDir -Destination $latestDir -Recurse -Force

$indexHtml = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta http-equiv="refresh" content="0; url=latest/index.html" />
  <title>Visa2026 UI scenario reports</title>
</head>
<body>
  <p><a href="latest/index.html">Latest UI scenario report</a></p>
  <p><a href="$Version/index.html">Report for version $Version</a></p>
</body>
</html>
"@

Set-Content -LiteralPath (Join-Path $reportsRoot 'index.html') -Value $indexHtml -Encoding utf8

$siteRootIndex = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta http-equiv="refresh" content="0; url=test-reports/latest/index.html" />
  <title>Visa2026 GitHub Pages</title>
</head>
<body>
  <p><a href="test-reports/latest/index.html">Latest UI scenario report</a></p>
  <p><a href="test-reports/">All scenario reports</a></p>
</body>
</html>
"@

Set-Content -LiteralPath (Join-Path $PublishRoot 'index.html') -Value $siteRootIndex -Encoding utf8
Write-Host "Prepared Pages publish root:"
Write-Host "  $versionDir"
Write-Host "  $latestDir"
Write-Host "  $(Join-Path $PublishRoot 'index.html') (site root redirect)"
