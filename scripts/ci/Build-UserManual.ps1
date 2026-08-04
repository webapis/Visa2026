#Requires -Version 5.1
<#
.SYNOPSIS
  Build the officer user manual site (MkDocs). Phase 1: catalog generator, UserManualDocs tests, validation.

.DESCRIPTION
  Unified pipeline entry point. Phase 3+ adds UserManual E2E and screenshot copy.
  See docs/USER_MANUAL_PIPELINE.md and .cursor/skills/visa2026-user-manual/reference.md.

.PARAMETER SkipE2E
  Skip EasyTest UserManual journeys (default until Phase 3).

.PARAMETER SkipGenerator
  Skip UserManualManifestGenerator (bo-catalog.json).

.PARAMETER SkipValidate
  Skip Validate-UserManualLinks.ps1.

.PARAMETER SkipUnitTests
  Skip UserManualDocs unit tests (local prose-only edits only).

.PARAMETER RequireMedia
  Fail validation when screenshot/video files are missing under user-manual/assets/
  (default: warn only — PNG/MP4 are gitignored; generate via Record-EasyTest.ps1).

.PARAMETER ManualMediaBaseUrl
  Remote HTTPS base for screenshots/videos (e.g. https://10.100.128.25:8081/manual-media).
  Overrides MANUAL_MEDIA_BASE_URL env. When set, skips copying assets into docs/assets/
  and MkDocs rewrites ../../assets/... links to absolute URLs at build time.

.EXAMPLE
  ./scripts/ci/Build-UserManual.ps1 -SkipE2E

.EXAMPLE
  $env:MANUAL_MEDIA_BASE_URL = 'https://10.100.128.25:8081/manual-media'
  ./scripts/ci/Build-UserManual.ps1 -SkipE2E
#>
[CmdletBinding()]
param(
    [switch]$SkipE2E = $true,
    [switch]$SkipGenerator,
    [switch]$SkipValidate,
    [switch]$SkipUnitTests,
    [switch]$RequireMedia,
    [string]$ManualMediaBaseUrl
)

$ErrorActionPreference = 'Stop'

function Get-ManualMediaBaseUrl {
    param([string]$Override)

    if (-not [string]::IsNullOrWhiteSpace($Override)) {
        return $Override.Trim().TrimEnd('/')
    }

    if (-not [string]::IsNullOrWhiteSpace($env:MANUAL_MEDIA_BASE_URL)) {
        return $env:MANUAL_MEDIA_BASE_URL.Trim().TrimEnd('/')
    }

    return ''
}

$mediaBaseUrl = Get-ManualMediaBaseUrl -Override $ManualMediaBaseUrl
if ($mediaBaseUrl) {
    $env:MANUAL_MEDIA_BASE_URL = $mediaBaseUrl
    Write-Host "MANUAL_MEDIA_BASE_URL=$mediaBaseUrl (remote media; skipping docs/assets sync)"
}
else {
    Remove-Item Env:\MANUAL_MEDIA_BASE_URL -ErrorAction SilentlyContinue
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manualRoot = Join-Path $repoRoot 'user-manual'
$mkdocsConfig = Join-Path $manualRoot 'mkdocs.yml'
$requirements = Join-Path $manualRoot 'requirements.txt'
$siteDir = Join-Path $manualRoot 'site'
$generatedDir = Join-Path $manualRoot 'generated'
$docsRoot = Join-Path $manualRoot 'docs'
$generatorProject = Join-Path $repoRoot 'tools\UserManualManifestGenerator\UserManualManifestGenerator.csproj'
$generatorTestsProject = Join-Path $repoRoot 'tools\UserManualManifestGenerator.Tests\UserManualManifestGenerator.Tests.csproj'
$validateScript = Join-Path $PSScriptRoot 'Validate-UserManualLinks.ps1'

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [string[]]$ArgumentList
    )

    Write-Host ">> $FilePath $($ArgumentList -join ' ')"
    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed ($LASTEXITCODE): $FilePath"
    }
}

function Sync-ManualAssets {
    param(
        [string]$ManualRoot,
        [string]$DocsRoot
    )

    $source = Join-Path $ManualRoot 'assets'
    if (-not (Test-Path -LiteralPath $source)) {
        Write-Warning "user-manual/assets/ is missing. Guide screenshots will not appear until EasyTest PNGs are copied (Copy-EasyTestManualScreenshots.ps1)."
        return
    }

    $screenshotsRoot = Join-Path $source 'screenshots'
    if (-not (Test-Path -LiteralPath $screenshotsRoot)) {
        Write-Warning "user-manual/assets/screenshots/ is empty. Run Record-EasyTest.ps1 then Copy-EasyTestManualScreenshots.ps1."
    }

    $videosRoot = Join-Path $source 'videos'
    if (-not (Test-Path -LiteralPath $videosRoot)) {
        Write-Warning "user-manual/assets/videos/ is empty. Run Record-EasyTest.ps1 (with ffmpeg) then Copy-EasyTestManualVideos.ps1."
    }

    $target = Join-Path $DocsRoot 'assets'
    if (Test-Path -LiteralPath $target) {
        Copy-Item -LiteralPath (Join-Path $source '*') -Destination $target -Recurse -Force
    }
    else {
        Copy-Item -LiteralPath $source -Destination $target -Recurse -Force
    }
}

function Sync-GeneratedReferencePages {
    param(
        [string]$GeneratedRoot,
        [string]$DocsRoot
    )

    $locales = @('en', 'tr', 'tk', 'ru')
    foreach ($locale in $locales) {
        $source = Join-Path $GeneratedRoot "reference\$locale\business-objects.md"
        if (-not (Test-Path -LiteralPath $source)) {
            continue
        }

        $targetDir = Join-Path $DocsRoot "$locale\reference"
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
        Copy-Item -LiteralPath $source -Destination (Join-Path $targetDir 'business-objects.md') -Force
    }
}

if (-not (Test-Path -LiteralPath $mkdocsConfig)) {
    throw "MkDocs config not found: $mkdocsConfig"
}

if (-not $SkipGenerator) {
    if (-not (Test-Path -LiteralPath $generatorProject)) {
        throw "Generator project not found: $generatorProject"
    }

    Invoke-External -FilePath 'dotnet' -ArgumentList @('build', $generatorProject, '-c', 'Debug')
    Invoke-External -FilePath 'dotnet' -ArgumentList @(
        'run', '--project', $generatorProject, '--no-build', '--',
        '--output', $generatedDir,
        '--guides', $docsRoot
    )
    Sync-GeneratedReferencePages -GeneratedRoot $generatedDir -DocsRoot $docsRoot
}

if (-not $SkipUnitTests) {
    Invoke-External -FilePath 'dotnet' -ArgumentList @(
        'test', $generatorTestsProject, '-c', 'Debug', '--no-restore',
        '--filter', 'Category=UserManualDocs'
    )
}

if (-not $SkipValidate) {
    if (-not (Test-Path -LiteralPath $validateScript)) {
        throw "Validator script not found: $validateScript"
    }

    $validateArgs = @{ ManualRoot = $manualRoot }
    if ($RequireMedia) {
        $validateArgs['RequireMedia'] = $true
    }

    & $validateScript @validateArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Validate-UserManualLinks.ps1 failed."
    }
}

if (-not $SkipE2E) {
    Write-Warning 'UserManual E2E is not wired until Phase 3. Re-run with -SkipE2E.'
}

function Get-PythonCommand {
    if ($env:USER_MANUAL_PYTHON -and (Test-Path -LiteralPath $env:USER_MANUAL_PYTHON)) {
        $prefix = @()
        if ($env:USER_MANUAL_PYTHON_ARGS) {
            $prefix = $env:USER_MANUAL_PYTHON_ARGS.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
        }

        return @{ FilePath = $env:USER_MANUAL_PYTHON; Prefix = $prefix }
    }

    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        foreach ($candidate in @('python', 'python3', 'py')) {
            if (-not (Get-Command $candidate -ErrorAction SilentlyContinue)) {
                continue
            }

            if ($candidate -eq 'py') {
                $probe = & py -3 -c "import sys; print(sys.version_info.major)" 2>$null
                if ($LASTEXITCODE -eq 0 -and $probe) {
                    return @{ FilePath = 'py'; Prefix = @('-3') }
                }

                continue
            }

            $probe = & $candidate -c "import sys; print(sys.version_info.major)" 2>$null
            if ($LASTEXITCODE -eq 0 -and $probe) {
                return @{ FilePath = $candidate; Prefix = @() }
            }
        }
    }
    finally {
        $ErrorActionPreference = $previousEap
    }

    throw 'Python is required on PATH (python, python3, or py -3), or run scripts/local/Serve-UserManual.ps1 which bootstraps portable Python.'
}

$python = Get-PythonCommand
$pipArgs = $python.Prefix + @('-m', 'pip', 'install', '-r', $requirements)
Invoke-External -FilePath $python.FilePath -ArgumentList $pipArgs

if (-not $mediaBaseUrl) {
    Sync-ManualAssets -ManualRoot $manualRoot -DocsRoot $docsRoot
}
else {
    Write-Host 'Skipping docs/assets sync (remote MANUAL_MEDIA_BASE_URL).'
}

if (Test-Path -LiteralPath $siteDir) {
    Remove-Item -LiteralPath $siteDir -Recurse -Force
}

$mkdocsArgs = $python.Prefix + @('-m', 'mkdocs', 'build', '-f', $mkdocsConfig, '-d', $siteDir)
Invoke-External -FilePath $python.FilePath -ArgumentList $mkdocsArgs

Write-Host "User manual site built at $siteDir"
