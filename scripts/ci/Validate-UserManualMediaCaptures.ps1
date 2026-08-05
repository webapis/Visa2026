#Requires -Version 5.1
<#
.SYNOPSIS
  Validate doc-anchored media-capture comments in officer manual guides.

.DESCRIPTION
  Screenshots: <!-- media-capture: {key} --> above each PNG; registry captures + guideSlugs.
  Officer manual is screenshots-only (D21) — no <video> in guides.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ManualRoot,

    [switch]$RequireRegistry
)

$ErrorActionPreference = 'Stop'

$docsRoot = Join-Path $ManualRoot 'docs'
$registryPath = Join-Path $ManualRoot 'media-capture-registry.yaml'
$registeredKeys = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$captureGuideSlugs = @{}

function Get-GuideSlugFromFrontmatter {
    param([string]$Content)

    if ($Content -match '(?ms)^---\s*\r?\n.*?\bslug:\s*([^\r\n#]+)') {
        return $Matches[1].Trim().Trim('"').Trim("'")
    }

    return $null
}

function Read-RegistryGuideSlugs {
    param([string]$RegistryText)

    if ($RegistryText -notmatch '(?ms)^captures:\s*\r?\n(.*)^videos:\s*\r?\n') {
        return
    }

    $capturesBlock = $Matches[1]
    $currentKey = $null
    $inGuideSlugs = $false

    foreach ($line in $capturesBlock -split '\r?\n') {
        if ($line -match '^\s{2}([a-z0-9][a-z0-9-]+):\s*$') {
            $currentKey = $Matches[1]
            $inGuideSlugs = $false
            if (-not $captureGuideSlugs.ContainsKey($currentKey)) {
                $captureGuideSlugs[$currentKey] = New-Object 'System.Collections.Generic.List[string]'
            }
            [void]$registeredKeys.Add($currentKey)
            continue
        }

        if ($null -eq $currentKey) {
            continue
        }

        if ($line -match '^\s{4}guideSlugs:\s*$') {
            $inGuideSlugs = $true
            continue
        }

        if ($inGuideSlugs -and $line -match '^\s{6}-\s+(.+)$') {
            [void]$captureGuideSlugs[$currentKey].Add($Matches[1].Trim().Trim('"').Trim("'"))
            continue
        }

        if ($line -match '^\s{4}[A-Za-z0-9_]+:') {
            $inGuideSlugs = $false
        }
    }
}

if (Test-Path -LiteralPath $registryPath) {
    $registryText = Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8
    Read-RegistryGuideSlugs -RegistryText $registryText
}

$errors = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]
$anchored = 0
$images = 0

$guideFiles = Get-ChildItem -LiteralPath $docsRoot -Recurse -Filter '*.md' -File
foreach ($file in $guideFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    $relative = $file.FullName.Substring($docsRoot.Length).TrimStart('\', '/')
    $guideSlug = Get-GuideSlugFromFrontmatter -Content $content
    $isPublished = $false
    if ($content -match '(?ms)^---\s*\r?\n.*?\bguideStatus:\s*published\b') {
        $isPublished = $true
    }

    $imagePattern = '(?m)^<!--\s*media-capture:\s*([a-zA-Z0-9][a-zA-Z0-9_-]*)\s*-->\s*\r?\n!\[[^\]]*\]\(([^)]+)\)'
    foreach ($match in [regex]::Matches($content, $imagePattern)) {
        $anchored++
        $captureKey = $match.Groups[1].Value.Trim()
        $imagePath = $match.Groups[2].Value.Trim()
        $basename = [System.IO.Path]::GetFileNameWithoutExtension($imagePath)

        if (-not $imagePath -match 'assets/screenshots/.+\.png$') {
            $warnings.Add("$relative - media-capture '$captureKey' does not reference assets/screenshots/*.png ($imagePath)")
        }

        if (-not [string]::Equals($captureKey, $basename, [StringComparison]::Ordinal)) {
            $errors.Add("$relative - media-capture key '$captureKey' must match image basename '$basename'")
        }

        if ($registeredKeys.Count -gt 0 -and -not $registeredKeys.Contains($captureKey)) {
            $msg = "$relative - capture key '$captureKey' is not listed in media-capture-registry.yaml captures"
            if ($RequireRegistry) {
                $errors.Add($msg)
            }
            else {
                $warnings.Add($msg)
            }
        }

        if ($RequireRegistry -and -not [string]::IsNullOrWhiteSpace($guideSlug) -and $captureGuideSlugs.ContainsKey($captureKey)) {
            $allowedSlugs = $captureGuideSlugs[$captureKey]
            if ($allowedSlugs.Count -gt 0 -and -not $allowedSlugs.Contains($guideSlug)) {
                $errors.Add("$relative - capture key '$captureKey' guideSlugs in registry do not include guide slug '$guideSlug'")
            }
        }
    }

    $allImages = [regex]::Matches($content, '(?m)^!\[[^\]]*\]\(([^)]+)\)')
    foreach ($img in $allImages) {
        $target = $img.Groups[1].Value.Trim()
        if ($target -notmatch 'assets/screenshots/.+\.png$') {
            continue
        }

        $images++
        $lineStart = $img.Index
        $prefix = $content.Substring(0, $lineStart)
        if ($prefix -notmatch '(?m)<!--\s*media-capture:\s*[a-zA-Z0-9][a-zA-Z0-9_-]*\s*-->\s*$') {
            $msg = "$relative - screenshot '$target' is missing <!-- media-capture: {basename} --> anchor on the line above"
            if ($isPublished) {
                $errors.Add($msg)
            }
            else {
                $warnings.Add($msg)
            }
        }
    }

    if ($content -match '<video\b') {
        $msg = "$relative - officer manual is screenshots-only (D21); remove <video> embed"
        if ($isPublished) {
            $errors.Add($msg)
        }
        else {
            $warnings.Add($msg)
        }
    }

    if ($content -match '(?ms)^---\s*\r?\n(.*?)\r?\n---') {
        $frontmatter = $Matches[1]
        if ($frontmatter -match '(?m)^video(Storage|File|CaptureKey|Source|sVersion|CapturedAt)?\s*:') {
            $msg = "$relative - remove video frontmatter (screenshots-only policy D21)"
            if ($isPublished) {
                $errors.Add($msg)
            }
            else {
                $warnings.Add($msg)
            }
        }
    }
}

foreach ($warning in $warnings) {
    Write-Warning $warning
}

foreach ($errorMessage in $errors) {
    Write-Error $errorMessage
}

Write-Host "Media capture validation: $anchored anchored screenshot(s), $images screenshot link(s) in guides."
if ($errors.Count -gt 0) {
    throw "Validate-UserManualMediaCaptures failed with $($errors.Count) error(s)."
}
