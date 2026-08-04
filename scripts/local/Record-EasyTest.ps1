<#
.SYNOPSIS
  Runs a Visa2026 EasyTest E2E filter with optional ffmpeg desktop video and browser screenshots.

.DESCRIPTION
  Starts fragmented MP4 capture (gdigrab), builds EasyTest if needed, runs dotnet test with the
  given filter, then stops ffmpeg. Output lands under Visa2026.E2E.Tests/recordings/ (gitignored).
  Same capture style as .github/workflows/e2e-tests.yml.

  Requires: Windows, local PostgreSQL, msedgedriver (see Install-MsEdgeDriver.ps1).
  Video: ffmpeg on PATH, or Visa2026.E2E.Tests\.tools\ffmpeg\ffmpeg.exe.
  Screenshots: -Screenshots sets VISA2026_E2E_SCREENSHOTS=true (milestone PNGs under recordings/screenshots/).

.PARAMETER Filter
  xUnit FullyQualifiedName filter fragment (default: passport-create-only Fact).

.PARAMETER OutputName
  MP4 file name without path (default: derived from Filter).

.PARAMETER SkipBuild
  Skip "dotnet build Visa2026.slnx -c EasyTest".

.PARAMETER NoRecord
  Run the test headed without ffmpeg (useful when ffmpeg is missing).

.PARAMETER Screenshots
  Capture browser PNGs at passport-journey milestones (login, employees, passport save, …).

.EXAMPLE
  .\scripts\local\Record-EasyTest.ps1 -Screenshots

.EXAMPLE
  .\scripts\local\Record-EasyTest.ps1 -Filter PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud

.EXAMPLE
  .\scripts\local\Record-EasyTest.ps1 -NoRecord -SkipBuild
#>
[CmdletBinding()]
param(
    [string] $Filter = 'PersonOfficerJourney_LoginCreateEmployeeAddPassport',
    [string] $OutputName = '',
    [switch] $SkipBuild,
    [switch] $NoRecord,
    [switch] $Screenshots
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location -LiteralPath $repoRoot

# Prefer portable ffmpeg shipped under the E2E project (downloaded for local recording).
$localFfmpegDir = Join-Path $repoRoot 'Visa2026.E2E.Tests\.tools\ffmpeg'
if (Test-Path (Join-Path $localFfmpegDir 'ffmpeg.exe')) {
    $env:Path = "$localFfmpegDir;$env:Path"
}

$env:VISA2026_E2E_HEADED = 'true'
if ($env:VISA2026_E2E_HEADLESS) {
    Remove-Item Env:\VISA2026_E2E_HEADLESS -ErrorAction SilentlyContinue
}

$runStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
if ($Screenshots) {
    $env:VISA2026_E2E_SCREENSHOTS = 'true'
    $env:VISA2026_E2E_SCREENSHOT_RUN = $runStamp
    Write-Host "Screenshots enabled (run $runStamp)."
}
else {
    Remove-Item Env:\VISA2026_E2E_SCREENSHOTS -ErrorAction SilentlyContinue
    Remove-Item Env:\VISA2026_E2E_SCREENSHOT_RUN -ErrorAction SilentlyContinue
}

if (-not $OutputName) {
    $safe = ($Filter -replace '[^\w\-]+', '-').Trim('-')
    if (-not $safe) { $safe = 'easytest' }
    $OutputName = "$safe.mp4"
}
if ($OutputName -notlike '*.mp4') {
    $OutputName = "$OutputName.mp4"
}

$recDir = Join-Path $repoRoot 'Visa2026.E2E.Tests\recordings'
New-Item -ItemType Directory -Force -Path $recDir | Out-Null
$outFile = Join-Path $recDir $OutputName

$ffPid = $null
$testExit = 1
try {
    if (-not $NoRecord) {
        if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
            throw "ffmpeg not found on PATH (or Visa2026.E2E.Tests\.tools\ffmpeg). Pass -NoRecord or install ffmpeg."
        }
        $ffArgs = @(
            '-y', '-f', 'gdigrab', '-framerate', '10', '-draw_mouse', '1', '-i', 'desktop',
            '-movflags', 'frag_keyframe+empty_moov', '-pix_fmt', 'yuv420p', $outFile
        )
        $proc = Start-Process -FilePath 'ffmpeg' -ArgumentList $ffArgs -PassThru -WindowStyle Hidden
        $ffPid = $proc.Id
        Write-Host "Recording desktop (ffmpeg pid $ffPid) -> $outFile"
        Start-Sleep -Seconds 1
    }
    else {
        Write-Host 'Recording disabled (-NoRecord).'
    }

    if (-not $SkipBuild) {
        Write-Host 'Building EasyTest configuration...'
        dotnet build Visa2026.slnx -c EasyTest
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed with exit code $LASTEXITCODE"
        }
    }

    # Keep msedgedriver in test output in sync with project .webdrivers (version must match Edge).
    $driverSrc = Join-Path $repoRoot 'Visa2026.E2E.Tests\.webdrivers\msedgedriver.exe'
    $driverDstDir = Join-Path $repoRoot 'Visa2026.E2E.Tests\bin\EasyTest\net8.0\.webdrivers'
    if (Test-Path -LiteralPath $driverSrc) {
        New-Item -ItemType Directory -Force -Path $driverDstDir | Out-Null
        Copy-Item -LiteralPath $driverSrc -Destination (Join-Path $driverDstDir 'msedgedriver.exe') -Force
    }

    Write-Host "Running EasyTest filter: $Filter"
    dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --no-build `
        --filter "FullyQualifiedName~$Filter" `
        --logger "console;verbosity=normal"
    $testExit = $LASTEXITCODE
}
finally {
    if ($null -ne $ffPid) {
        Write-Host "Stopping ffmpeg (pid $ffPid)..."
        try { Stop-Process -Id $ffPid -Force -ErrorAction SilentlyContinue } catch {}
        Start-Sleep -Seconds 2
    }
}

if (-not $NoRecord) {
    if (Test-Path -LiteralPath $outFile) {
        $len = (Get-Item -LiteralPath $outFile).Length
        Write-Host "Recording saved ($len bytes): $outFile"
    }
    else {
        Write-Warning "Expected recording missing: $outFile"
    }
}

if ($Screenshots) {
    $shotDir = Join-Path $recDir "screenshots\$runStamp"
    if (Test-Path -LiteralPath $shotDir) {
        $pngs = @(Get-ChildItem -LiteralPath $shotDir -Filter '*.png' -ErrorAction SilentlyContinue)
        Write-Host "Screenshots ($($pngs.Count)): $shotDir"
        $pngs | ForEach-Object { Write-Host "  $($_.Name) ($($_.Length) bytes)" }
    }
    else {
        Write-Warning "Screenshot directory missing: $shotDir"
    }
}

exit $testExit