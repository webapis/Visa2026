#Requires -Version 5.1
<#
.SYNOPSIS
  Validate officer manual content, guide frontmatter, and generated catalog parity.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ManualRoot,

    [switch]$RequireMedia
)

function Get-MarkdownFrontmatter {
    param([string]$Content)

    if ($Content -notmatch '(?s)^---\s*\r?\n(.*?)\r?\n---') {
        return $null
    }

    $yaml = $Matches[1]
    $map = @{}
    foreach ($line in $yaml -split '\r?\n') {
        if ($line -notmatch '^\s*([A-Za-z0-9_]+)\s*:\s*(.+?)\s*$') {
            continue
        }
        $key = $Matches[1]
        $value = $Matches[2].Trim()
        if ($value.StartsWith('"') -and $value.EndsWith('"') -and $value.Length -ge 2) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $map[$key] = $value
    }
    return $map
}

function Test-IsNoBoFrontmatterValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $true
    }

    $trimmed = $Value.Trim()
    if ($trimmed -eq '-') {
        return $true
    }

    if ($trimmed.Length -eq 1) {
        $code = [int][char]$trimmed
        if ($code -eq 0x2014 -or $code -eq 0x2013) {
            return $true
        }
    }

    return $false
}

function Test-OfficerContentHasCodeFence {
    param([string]$Content)

    $body = $Content
    if ($body -match '(?s)^---\s*\r?\n.*?\r?\n---\s*') {
        $body = $body.Substring($Matches[0].Length)
    }

    $body = [regex]::Replace($body, '(?ms)^```mermaid\s*\r?\n.*?^```\s*\r?\n?', '')
    return $body -match '(?m)^```'
}

function Test-DeveloperDocsLink {
    param([string]$Content)

    return $Content -match '\]\(\.\./\.\./docs/|\]\(docs/[A-Z_]+\.md\)'
}

function Get-ManualScreenshotLinks {
    param([string]$Content)

    $links = New-Object System.Collections.Generic.List[string]
    $pattern = '!\[[^\]]*\]\(([^)]+)\)'
    foreach ($match in [regex]::Matches($Content, $pattern)) {
        $target = $match.Groups[1].Value.Trim()
        if ($target -match '^(https?:)?//') {
            continue
        }

        if ($target.StartsWith('/')) {
            continue
        }

        $links.Add($target)
    }

    return $links
}

function Test-ManualScreenshotLink {
    param(
        [string]$MarkdownFile,
        [string]$RelativeLink,
        [string]$DocsRoot,
        [string]$ManualRoot
    )

    $markdownDir = Split-Path -Parent $MarkdownFile
    $resolved = [System.IO.Path]::GetFullPath((Join-Path $markdownDir $RelativeLink))
    $docsAssetsRoot = [System.IO.Path]::GetFullPath((Join-Path $DocsRoot 'assets'))
    $sourceAssetsRoot = Join-Path $ManualRoot 'assets'

    if (-not $resolved.StartsWith($docsAssetsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return "Screenshot link does not resolve under docs/assets: $RelativeLink in $MarkdownFile"
    }

    $relativeToDocsAssets = $resolved.Substring($docsAssetsRoot.Length).TrimStart('\', '/')
    $sourcePath = Join-Path $sourceAssetsRoot $relativeToDocsAssets
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        return "Missing screenshot file for $RelativeLink in $MarkdownFile (expected $sourcePath)"
    }

    return $null
}

function Get-ManualVideoSrcLinks {
    param([string]$Content)

    $links = New-Object System.Collections.Generic.List[string]
    $pattern = '<video\b[^>]*\bsrc="([^"]+)"'
    foreach ($match in [regex]::Matches($Content, $pattern)) {
        $links.Add($match.Groups[1].Value.Trim())
    }

    return $links
}

function Test-ManualVideoAsset {
    param(
        [string]$ManualRoot,
        [string]$Locale,
        [string]$VideosVersion,
        [string]$VideoFile
    )

    if ([string]::IsNullOrWhiteSpace($VideoFile)) {
        return $null
    }

    $sourcePath = Join-Path $ManualRoot "assets\videos\v$VideosVersion\$Locale\$VideoFile"
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        return "Missing video file for locale '$Locale': $sourcePath"
    }

    return $null
}

$ErrorActionPreference = 'Stop'

$manualRoot = (Resolve-Path -LiteralPath $ManualRoot).Path
$locales = @('en', 'tr', 'tk', 'ru')
$requiredPages = @(
    'index.md',
    'getting-started/login.md',
    'getting-started/navigation.md',
    'about/roadmap.md',
    'reference/index.md'
)
$catalogPath = Join-Path $manualRoot 'generated\bo-catalog.json'
$docsRoot = Join-Path $manualRoot 'docs'
$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

Write-Host "Validate-UserManualLinks.ps1 - Phase 1 checks under $manualRoot"

foreach ($locale in $locales) {
    $localeDir = Join-Path $docsRoot $locale
    if (-not (Test-Path -LiteralPath $localeDir)) {
        $failures.Add("Missing locale folder: $localeDir")
        continue
    }

    foreach ($page in $requiredPages) {
        $path = Join-Path $localeDir $page
        if (-not (Test-Path -LiteralPath $path)) {
            $failures.Add("Missing required page for locale '$locale': $path")
        }
    }
}

$mkdocsConfig = Join-Path $manualRoot 'mkdocs.yml'
if (-not (Test-Path -LiteralPath $mkdocsConfig)) {
    $failures.Add("Missing mkdocs.yml: $mkdocsConfig")
}

if (-not (Test-Path -LiteralPath $catalogPath)) {
    $failures.Add("Missing generated catalog: $catalogPath (run Build-UserManual.ps1 without -SkipGenerator)")
}
else {
    $catalog = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $catalogTypeNames = @($catalog.types | ForEach-Object { $_.name })

    $slugSet = @{}
    $guideFiles = Get-ChildItem -LiteralPath $docsRoot -Recurse -Filter '*.md' -File |
        Where-Object { $_.FullName -match '[\\/]guides[\\/]' -and -not $_.Name.StartsWith('_') }

    foreach ($guideFile in $guideFiles) {
        $content = Get-Content -LiteralPath $guideFile.FullName -Raw -Encoding UTF8
        $frontmatter = Get-MarkdownFrontmatter -Content $content
        if ($null -eq $frontmatter) {
            continue
        }

        $slug = $frontmatter['slug']
        $bo = $frontmatter['bo']
        $guideLocale = $frontmatter['locale']
        if ([string]::IsNullOrWhiteSpace($guideLocale)) {
            $relative = $guideFile.FullName.Substring($docsRoot.Length).TrimStart('\', '/')
            $guideLocale = ($relative -split '[\\/]')[0]
        }

        if (-not [string]::IsNullOrWhiteSpace($slug)) {
            $slugKey = "$guideLocale/$slug"
            if ($slugSet.ContainsKey($slugKey)) {
                $failures.Add("Duplicate guide slug '$slugKey' in $($guideFile.FullName) and $($slugSet[$slugKey])")
            }
            else {
                $slugSet[$slugKey] = $guideFile.FullName
            }
        }

        if (-not (Test-IsNoBoFrontmatterValue $bo) -and $catalogTypeNames -notcontains $bo) {
            $failures.Add("Guide references unknown bo '$bo' in $($guideFile.FullName)")
        }

        if (Test-OfficerContentHasCodeFence -Content $content) {
            $failures.Add("Code fence found in officer guide: $($guideFile.FullName)")
        }

        if (Test-DeveloperDocsLink -Content $content) {
            $warnings.Add("Developer docs link in officer guide: $($guideFile.FullName)")
        }
    }

    foreach ($locale in $locales) {
        $referenceDir = Join-Path $docsRoot "$locale\reference"
        if (-not (Test-Path -LiteralPath $referenceDir)) {
            continue
        }

        Get-ChildItem -LiteralPath $referenceDir -Filter '*.md' -File | ForEach-Object {
            $content = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
            if (Test-OfficerContentHasCodeFence -Content $content) {
                $failures.Add("Code fence found in officer reference page: $($_.FullName)")
            }
        }
    }

    foreach ($type in $catalog.types) {
        if ([string]::IsNullOrWhiteSpace($type.userDocSlug)) {
            $failures.Add("Catalog type '$($type.name)' is missing userDocSlug")
        }
    }
}

$guideAndGettingStartedFiles = Get-ChildItem -LiteralPath $docsRoot -Recurse -Filter '*.md' -File |
    Where-Object {
        (-not $_.Name.StartsWith('_')) -and (
            $_.FullName -match '[\\/]guides[\\/]' -or
            $_.FullName -match '[\\/]getting-started[\\/]'
        )
    }

foreach ($pageFile in $guideAndGettingStartedFiles) {
    $content = Get-Content -LiteralPath $pageFile.FullName -Raw -Encoding UTF8
    $frontmatter = Get-MarkdownFrontmatter -Content $content
    $pageLocale = $null
    if ($null -ne $frontmatter) {
        $pageLocale = $frontmatter['locale']
        if ([string]::IsNullOrWhiteSpace($pageLocale)) {
            $relative = $pageFile.FullName.Substring($docsRoot.Length).TrimStart('\', '/')
            $pageLocale = ($relative -split '[\\/]')[0]
        }

        $videoStorage = $frontmatter['videoStorage']
        if ($videoStorage -eq 'static') {
            $warnings.Add("Guide still has videoStorage static (screenshots-only D21): $($pageFile.FullName)")
        }
    }

    foreach ($imageLink in Get-ManualScreenshotLinks -Content $content) {
        $imageError = Test-ManualScreenshotLink -MarkdownFile $pageFile.FullName -RelativeLink $imageLink -DocsRoot $docsRoot -ManualRoot $manualRoot
        if ($imageError) {
            if ($RequireMedia) {
                $failures.Add($imageError)
            }
            else {
                $warnings.Add("$imageError - run Record-EasyTest.ps1 + Copy-EasyTestManualScreenshots.ps1")
            }
        }
    }

    foreach ($videoLink in Get-ManualVideoSrcLinks -Content $content) {
        $warnings.Add("Guide contains <video> src (screenshots-only D21): $videoLink in $($pageFile.FullName)")
    }
}

foreach ($warning in $warnings) {
    Write-Warning $warning
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }
    exit 1
}

Write-Host 'Phase 1 validation passed.'
exit 0
