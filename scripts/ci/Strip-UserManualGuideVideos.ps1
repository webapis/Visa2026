#Requires -Version 5.1
<#
.SYNOPSIS
  Remove video frontmatter and Video walkthrough sections from officer manual guides.

.DESCRIPTION
  Implements D21 (screenshots-only). Idempotent — safe to re-run on guides without video.
#>
[CmdletBinding()]
param(
    [string]$ManualRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ManualRoot)) {
    $ManualRoot = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'user-manual'
}

$docsRoot = Join-Path $ManualRoot 'docs'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    throw "Docs root not found: $docsRoot"
}

$utf8 = New-Object System.Text.UTF8Encoding $false
$changed = 0

Get-ChildItem -LiteralPath $docsRoot -Recurse -Filter '*.md' -File | ForEach-Object {
    $path = $_.FullName
    $content = [System.IO.File]::ReadAllText($path)
    $original = $content

    if ($content -match '(?ms)\A---\s*\r?\n(.*?)\r?\n---') {
        $lines = $Matches[1] -split '\r?\n'
        $filtered = $lines | Where-Object {
            $_ -notmatch '^(videosVersion|videoCapturedAt|videoStorage|videoCaptureKey|videoFile|videoSource|video)\s*:'
        }
        $newFrontmatter = ($filtered -join "`n").TrimEnd()
        $content = $content -replace '(?ms)\A---\s*\r?\n.*?\r?\n---', "---`n$newFrontmatter`n---"
    }

    $content = $content -replace '(?ms)\r?\n## Video walkthrough\r?\n\r?\n<video\b[\s\S]*?</video>\s*(?:\r?\n<p class="visa-manual-video-caption">[\s\S]*?</p>\s*)?', "`n"
    # Localized headings (tr/tk/ru) — same HTML block as English pilots
    $content = $content -replace '(?ms)\r?\n## [^\r\n]*[Vv]ideo[^\r\n]*\r?\n\r?\n<video\b[\s\S]*?</video>\s*(?:\r?\n<p class="visa-manual-video-caption">[\s\S]*?</p>\s*)?', "`n"
    $content = $content -replace '(?ms)\r?\n<video class="visa-manual-video"[\s\S]*?</video>\s*(?:\r?\n<p class="visa-manual-video-caption">[\s\S]*?</p>\s*)?', "`n"

    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($path, $content, $utf8)
        $changed++
        Write-Host "Stripped video: $($_.FullName.Substring($docsRoot.Length).TrimStart('\', '/'))"
    }
}

Write-Host "Strip-UserManualGuideVideos: updated $changed file(s)."
