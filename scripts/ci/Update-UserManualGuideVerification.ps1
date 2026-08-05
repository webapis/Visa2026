#Requires -Version 5.1
<#
.SYNOPSIS
  Patch officer guide frontmatter after a green UserManual E2E run.

.DESCRIPTION
  Sets verified/verifiedAt/verifiedCommit on matching slugs. Optionally promotes
  guideStatus to published for selected locales (officer-reviewed en pilots).

.PARAMETER Slugs
  Guide slug values from frontmatter (e.g. getting-started/login).

.PARAMETER PublishLocales
  Locales that receive guideStatus: published (default: en).

.PARAMETER VerifiedLocales
  Locales that receive verified: true (default: en, tr, tk, ru).

.PARAMETER VerifiedCommit
  Short git sha recorded in frontmatter.

.EXAMPLE
  ./scripts/ci/Update-UserManualGuideVerification.ps1 `
    -Slugs getting-started/login,getting-started/navigation `
    -VerifiedCommit (git rev-parse --short HEAD)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Slugs,

    [string[]]$PublishLocales = @('en'),

    [string[]]$VerifiedLocales = @('en', 'tr', 'tk', 'ru'),

    [string]$VerifiedCommit = '',

    [string]$VerifiedAt = '',

    [string]$MediaE2eRunId = '',

    [string]$ScreenshotsCapturedAt = '',

    [string]$VideoCapturedAt = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$docsRoot = Join-Path $repoRoot 'user-manual\docs'
$utf8 = New-Object System.Text.UTF8Encoding $false

if (-not $VerifiedCommit) {
    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $sha = git -C $repoRoot rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $sha) { $VerifiedCommit = $sha.Trim() }
    }
    finally {
        $ErrorActionPreference = $previousEap
    }
}

if (-not $VerifiedAt) {
    $VerifiedAt = (Get-Date).ToUniversalTime().ToString('o')
}

if ($ScreenshotsCapturedAt -and -not $VideoCapturedAt) {
    $VideoCapturedAt = $ScreenshotsCapturedAt
}

$slugSet = @{}
foreach ($s in $Slugs) { $slugSet[$s.Trim()] = $true }

$publishSet = @{}
foreach ($l in $PublishLocales) { $publishSet[$l.Trim()] = $true }

$verifiedSet = @{}
foreach ($l in $VerifiedLocales) { $verifiedSet[$l.Trim()] = $true }

function Set-YamlScalar {
    param(
        [string]$Body,
        [string]$Key,
        [string]$Value
    )

    $escapedKey = [regex]::Escape($Key)
    if ($Body -match "(?m)^${escapedKey}:\s*.+$") {
        return [regex]::Replace($Body, "(?m)^${escapedKey}:\s*.+$", "${Key}: $Value")
    }

    return ($Body.TrimEnd() + "`n${Key}: $Value")
}

function Set-YamlScalarQuoted {
    param(
        [string]$Body,
        [string]$Key,
        [string]$Value
    )

    return Set-YamlScalar -Body $Body -Key $Key -Value "`"$Value`""
}

function Insert-YamlAfterKey {
    param(
        [string]$Body,
        [string]$AfterKey,
        [string]$Line
    )

    $escapedAfter = [regex]::Escape($AfterKey)
    if ($Body -match "(?m)^${escapedAfter}:\s*.+$") {
        return [regex]::Replace(
            $Body,
            "(?m)^(${escapedAfter}:\s*.+)$",
            "`$1`n$Line",
            1)
    }

    return ($Body.TrimEnd() + "`n$Line")
}

function Set-MediaCaptureYaml {
    param(
        [string]$Body
    )

    $result = $Body
    $anchor = if ($result -match '(?m)^videosVersion:\s*.+$') { 'videosVersion' } else { 'screenshotsVersion' }

    $lines = @()
    if ($ScreenshotsCapturedAt) { $lines += "screenshotsCapturedAt: `"$ScreenshotsCapturedAt`"" }
    if ($VideoCapturedAt) { $lines += "videoCapturedAt: `"$VideoCapturedAt`"" }
    if ($MediaE2eRunId) { $lines += "mediaE2eRunId: `"$MediaE2eRunId`"" }
    if ($lines.Count -eq 0) { return $result }

    foreach ($key in @('screenshotsCapturedAt', 'videoCapturedAt', 'mediaE2eRunId')) {
        $escaped = [regex]::Escape($key)
        $result = [regex]::Replace($result, "(?m)^${escaped}:\s*.+\r?\n?", '')
    }

    $block = ($lines -join "`n")
    $escapedAnchor = [regex]::Escape($anchor)
    if ($result -match "(?m)^${escapedAnchor}:\s*.+$") {
        return [regex]::Replace($result, "(?m)^(${escapedAnchor}:\s*.+)$", "`$1`n$block", 1)
    }

    return ($result.TrimEnd() + "`n$block")
}

$updated = 0
foreach ($localeDir in Get-ChildItem -LiteralPath $docsRoot -Directory) {
    $locale = $localeDir.Name
    $guideRoots = @(
        (Join-Path $localeDir.FullName 'getting-started'),
        (Join-Path $localeDir.FullName 'guides')
    )

    foreach ($root in $guideRoots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }

        foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -Filter '*.md' -File) {
            if ($file.Name.StartsWith('_')) { continue }

            $text = [System.IO.File]::ReadAllText($file.FullName)
            if ($text -notmatch '(?s)\A---\r?\n(.*?)\r?\n---\r?\n(.*)\z') { continue }

            $yaml = $Matches[1]
            $body = $Matches[2]

            if ($yaml -notmatch '(?m)^slug:\s*(.+)$') { continue }
            $slug = $Matches[1].Trim().Trim('"')
            if (-not $slugSet.ContainsKey($slug)) { continue }

            $newYaml = $yaml
            if ($verifiedSet.ContainsKey($locale)) {
                $newYaml = Set-YamlScalar -Body $newYaml -Key 'verified' -Value 'true'
                $newYaml = Set-YamlScalar -Body $newYaml -Key 'verifiedAt' -Value "`"$VerifiedAt`""
                if ($VerifiedCommit) {
                    $newYaml = Set-YamlScalar -Body $newYaml -Key 'verifiedCommit' -Value "`"$VerifiedCommit`""
                }
            }

            if ($publishSet.ContainsKey($locale)) {
                $newYaml = Set-YamlScalar -Body $newYaml -Key 'guideStatus' -Value 'published'
            }

            if ($MediaE2eRunId -or $ScreenshotsCapturedAt -or $VideoCapturedAt) {
                $newYaml = Set-MediaCaptureYaml -Body $newYaml
            }

            if ($newYaml -eq $yaml) { continue }

            $out = "---`n$newYaml`n---`n$body"
            [System.IO.File]::WriteAllText($file.FullName, $out, $utf8)
            $updated++
            Write-Host "Updated $($locale)/$slug ($($file.FullName))"
        }
    }
}

Write-Host "Patched $updated guide file(s)."
