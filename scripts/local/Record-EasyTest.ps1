<#
.SYNOPSIS
  Runs a Visa2026 EasyTest E2E filter with ffmpeg video and browser screenshots (both ON by default for user-manual media).

.DESCRIPTION
  Video capture defaults to the Edge browser window (not the full desktop) once the test opens it.
  Milestone PNGs use Selenium viewport screenshots (same as before).

  Output lands under Visa2026.E2E.Tests/recordings/ (gitignored).

  Requires: Windows, local PostgreSQL, msedgedriver (see Install-MsEdgeDriver.ps1).
  Video: ffmpeg on PATH, or Visa2026.E2E.Tests\.tools\ffmpeg\ffmpeg.exe.
  Screenshots: sets VISA2026_E2E_SCREENSHOTS=true unless -NoScreenshots.

.PARAMETER Filter
  xUnit FullyQualifiedName filter fragment (default: passport-create-only Fact).

.PARAMETER OutputName
  MP4 file name without path (default: derived from Filter).

.PARAMETER SkipBuild
  Skip "dotnet build Visa2026.slnx -c EasyTest".

.PARAMETER NoRecord
  Skip ffmpeg video (useful when ffmpeg is missing or for a quick headed run).

.PARAMETER RecordTarget
  Browser = capture Edge window only (default). Desktop = full screen (legacy). None = no video.

.PARAMETER NoScreenshots
  Skip milestone browser PNGs (VISA2026_E2E_SCREENSHOTS=false). Default is screenshots ON.

.PARAMETER Screenshots
  Obsolete — screenshots are ON by default. Kept for back-compat (no-op unless -NoScreenshots).

.EXAMPLE
  .\scripts\local\Record-EasyTest.ps1

.EXAMPLE
  .\scripts\local\Record-EasyTest.ps1 -Filter PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud

.EXAMPLE
  .\scripts\local\Record-EasyTest.ps1 -RecordTarget Desktop

.EXAMPLE
  .\scripts\local\Record-EasyTest.ps1 -NoRecord -NoScreenshots -SkipBuild
#>
[CmdletBinding()]
param(
    [string] $Filter = 'PersonOfficerJourney_LoginCreateEmployeeAddPassport',
    [string] $OutputName = '',
    [switch] $SkipBuild,
    [switch] $NoRecord,
    [ValidateSet('Browser', 'Desktop', 'None')]
    [string] $RecordTarget = 'Browser',
    [switch] $NoScreenshots,
    [switch] $Screenshots
)

$ErrorActionPreference = 'Stop'

function Get-EdgeBrowserHwnd {
    param(
        [int[]] $ExcludePids = @()
    )

    $automationEdges = @(
        Get-CimInstance Win32_Process -Filter "Name='msedge.exe'" -ErrorAction SilentlyContinue |
            Where-Object {
                $_.ProcessId -notin $ExcludePids -and
                $_.CommandLine -match 'remote-debugging-port'
            }
    )

    foreach ($edge in $automationEdges) {
        $proc = Get-Process -Id $edge.ProcessId -ErrorAction SilentlyContinue
        if ($null -ne $proc -and $proc.MainWindowHandle -ne [IntPtr]::Zero) {
            return $proc.MainWindowHandle
        }
    }

    # Automation browser may not own MainWindowHandle; pick newest visible Edge not in exclude list.
    $candidates = @(Get-Process msedge -ErrorAction SilentlyContinue |
        Where-Object {
            $_.MainWindowHandle -ne [IntPtr]::Zero -and
            $_.Id -notin $ExcludePids -and
            -not [string]::IsNullOrWhiteSpace($_.MainWindowTitle)
        })

    if ($candidates.Count -eq 0) {
        return [IntPtr]::Zero
    }

    $best = $candidates |
        Sort-Object @{
            Expression = {
                $title = $_.MainWindowTitle
                $score = 0
                if ($title -match 'localhost:5050|127\.0\.0\.1:5050') { $score += 100 }
                if ($title -match 'Visa Management|Visa2026') { $score += 40 }
                if ($title -match 'Sign in|Log on|Report Dashboard|Employees|Passport') { $score += 20 }
                if ($title -match '^data:,') { $score -= 50 }
                $score
            }
            Descending = $true
        }, StartTime -Descending |
        Select-Object -First 1

    return $best.MainWindowHandle
}

function Wait-TestEdgeBrowserHwnd {
    param(
        [System.Diagnostics.Process] $TestProcess,
        [int[]] $ExcludeEdgePids,
        [TimeSpan] $Timeout
    )

    $deadline = (Get-Date).Add($Timeout)
    while ((Get-Date) -lt $deadline -and -not $TestProcess.HasExited) {
        if (-not (Get-Process msedgedriver -ErrorAction SilentlyContinue)) {
            Start-Sleep -Milliseconds 500
            continue
        }

        $hwnd = Get-EdgeBrowserHwnd -ExcludePids $ExcludeEdgePids
        if ($hwnd -ne [IntPtr]::Zero) {
            return $hwnd
        }

        Start-Sleep -Milliseconds 500
    }

    return [IntPtr]::Zero
}

function Start-FfmpegGdigrabCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string] $InputSource,
        [Parameter(Mandatory = $true)]
        [string] $OutputFile
    )

    $ffArgs = @(
        '-y', '-f', 'gdigrab', '-framerate', '10', '-draw_mouse', '1', '-i', $InputSource,
        '-movflags', 'frag_keyframe+empty_moov', '-pix_fmt', 'yuv420p', $OutputFile
    )
    return Start-Process -FilePath 'ffmpeg' -ArgumentList $ffArgs -PassThru -WindowStyle Hidden
}

function Build-VideoFromMilestoneScreenshots {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ScreenshotDir,
        [Parameter(Mandatory = $true)]
        [string] $OutputFile,
        [double] $SecondsPerSlide = 4
    )

    $labels = @(
        '00-logon-page',
        '01-after-login',
        '02-employees-list',
        '03-employee-created',
        '04-employee-detail',
        '05-passport-detail-new',
        '06-passport-fields-filled',
        '07-passport-saved'
    )

    $images = @()
    foreach ($label in $labels) {
        $match = Get-ChildItem -LiteralPath $ScreenshotDir -Filter "$label*.png" -ErrorAction SilentlyContinue |
            Sort-Object Name |
            Select-Object -First 1
        if ($null -ne $match) {
            $images += $match.FullName
        }
    }

    if ($images.Count -eq 0) {
        return $false
    }

    $concatFile = Join-Path $env:TEMP ("visa-manual-video-$([guid]::NewGuid().ToString('N')).txt")
    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($img in $images) {
        $escaped = $img.Replace('\', '/').Replace("'", "'\''")
        $lines.Add("file '$escaped'")
        $lines.Add("duration $SecondsPerSlide")
    }
    $last = $images[-1].Replace('\', '/').Replace("'", "'\''")
    $lines.Add("file '$last'")
    [System.IO.File]::WriteAllLines($concatFile, $lines)

    $ffArgs = @(
        '-y', '-f', 'concat', '-safe', '0', '-i', $concatFile,
        '-vf', 'fps=10,format=yuv420p', '-movflags', '+faststart', '-pix_fmt', 'yuv420p', $OutputFile
    )
    & ffmpeg @ffArgs
    $ok = $LASTEXITCODE -eq 0
    Remove-Item -LiteralPath $concatFile -Force -ErrorAction SilentlyContinue
    return $ok
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location -LiteralPath $repoRoot

if ($NoRecord) {
    $RecordTarget = 'None'
}

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
if ($NoScreenshots) {
    $env:VISA2026_E2E_SCREENSHOTS = 'false'
    Remove-Item Env:\VISA2026_E2E_SCREENSHOT_RUN -ErrorAction SilentlyContinue
    Write-Host 'Screenshots disabled (-NoScreenshots).'
}
else {
    $env:VISA2026_E2E_SCREENSHOTS = 'true'
    $env:VISA2026_E2E_SCREENSHOT_RUN = $runStamp
    Write-Host "Screenshots enabled (run $runStamp)."
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

$ffProc = $null
$testExit = 1
try {
    if ($RecordTarget -ne 'None') {
        if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
            throw "ffmpeg not found on PATH (or Visa2026.E2E.Tests\.tools\ffmpeg). Pass -NoRecord or install ffmpeg."
        }
    }

    if (-not $SkipBuild) {
        Write-Host 'Building EasyTest configuration...'
        dotnet build Visa2026.slnx -c EasyTest
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed with exit code $LASTEXITCODE"
        }
    }

    $driverSrc = Join-Path $repoRoot 'Visa2026.E2E.Tests\.webdrivers\msedgedriver.exe'
    $driverDstDir = Join-Path $repoRoot 'Visa2026.E2E.Tests\bin\EasyTest\net8.0\.webdrivers'
    if (Test-Path -LiteralPath $driverSrc) {
        New-Item -ItemType Directory -Force -Path $driverDstDir | Out-Null
        Copy-Item -LiteralPath $driverSrc -Destination (Join-Path $driverDstDir 'msedgedriver.exe') -Force
    }

    $testArgs = @(
        'test', 'Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj',
        '-c', 'EasyTest', '--no-build',
        '--filter', "FullyQualifiedName~$Filter",
        '--logger', 'console;verbosity=normal'
    )

    if ($RecordTarget -eq 'Desktop') {
        $ffProc = Start-FfmpegGdigrabCapture -InputSource 'desktop' -OutputFile $outFile
        $env:VISA2026_E2E_VIDEO_RECORDING_START = [DateTimeOffset]::UtcNow.ToString('o')
        Write-Host "Recording desktop (ffmpeg pid $($ffProc.Id)) -> $outFile"
        Start-Sleep -Seconds 1
        Write-Host "Running EasyTest filter: $Filter"
        & dotnet @testArgs
        $testExit = $LASTEXITCODE
    }
    elseif ($RecordTarget -eq 'Browser') {
        $excludeEdgePids = @(Get-Process msedge -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })
        Write-Host "Running EasyTest filter (browser recording): $Filter"
        $testProc = Start-Process -FilePath 'dotnet' -ArgumentList $testArgs -PassThru -NoNewWindow -WorkingDirectory $repoRoot
        $hwnd = Wait-TestEdgeBrowserHwnd -TestProcess $testProc -ExcludeEdgePids $excludeEdgePids -Timeout ([TimeSpan]::FromMinutes(4))
        if ($hwnd -ne [IntPtr]::Zero) {
            $hwndHex = "0x$($hwnd.ToInt64().ToString('X'))"
            $title = (Get-Process msedge -ErrorAction SilentlyContinue |
                Where-Object { $_.MainWindowHandle -eq $hwnd } |
                Select-Object -First 1).MainWindowTitle
            $ffProc = Start-FfmpegGdigrabCapture -InputSource "hwnd=$hwndHex" -OutputFile $outFile
            $env:VISA2026_E2E_VIDEO_RECORDING_START = [DateTimeOffset]::UtcNow.ToString('o')
            Write-Host "Recording Edge window '$title' ($hwndHex, ffmpeg pid $($ffProc.Id)) -> $outFile"
        }
        else {
            Write-Warning 'Edge browser window not found before test finished; MP4 will be missing.'
        }

        $testProc.WaitForExit()
        $testExit = $testProc.ExitCode
    }
    else {
        Write-Host 'Recording disabled (-NoRecord / RecordTarget None).'
        Write-Host "Running EasyTest filter: $Filter"
        & dotnet @testArgs
        $testExit = $LASTEXITCODE
    }
}
finally {
    if ($null -ne $ffProc) {
        Write-Host "Stopping ffmpeg (pid $($ffProc.Id))..."
        try { Stop-Process -Id $ffProc.Id -Force -ErrorAction SilentlyContinue } catch {}
        Start-Sleep -Seconds 2
    }
}

if ($RecordTarget -ne 'None') {
    if (Test-Path -LiteralPath $outFile) {
        $len = (Get-Item -LiteralPath $outFile).Length
        Write-Host "Recording saved ($len bytes): $outFile"
    }
    else {
        Write-Warning "Expected recording missing: $outFile"
    }
}

$shotDir = $null
if (-not $NoScreenshots) {
    $shotDir = Join-Path $recDir "screenshots\$runStamp"
    if (Test-Path -LiteralPath $shotDir) {
        $pngs = @(Get-ChildItem -LiteralPath $shotDir -Filter '*.png' -ErrorAction SilentlyContinue)
        Write-Host "Screenshots ($($pngs.Count)): $shotDir"
        $pngs | ForEach-Object { Write-Host "  $($_.Name) ($($_.Length) bytes)" }

        if ($testExit -eq 0) {
            $copyScript = Join-Path $repoRoot 'scripts\ci\Copy-EasyTestManualScreenshots.ps1'
            if (Test-Path -LiteralPath $copyScript) {
                Write-Host 'Copying screenshots into user-manual/assets/...'
                & $copyScript -ScreenshotRunDir $shotDir
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning 'Copy-EasyTestManualScreenshots.ps1 failed; PNGs remain under recordings/screenshots/.'
                }
            }
        }
    }
    else {
        Write-Warning "Screenshot directory missing: $shotDir"
    }
}

if ($testExit -eq 0 -and $RecordTarget -eq 'Browser' -and $null -ne $shotDir -and (Test-Path -LiteralPath $shotDir)) {
    $videoBytes = if (Test-Path -LiteralPath $outFile) { (Get-Item -LiteralPath $outFile).Length } else { 0 }
    if ($videoBytes -lt 800000) {
        Write-Host 'Browser HWND capture was short; building MP4 from milestone screenshots (browser viewport)...'
        if (Build-VideoFromMilestoneScreenshots -ScreenshotDir $shotDir -OutputFile $outFile) {
            $len = (Get-Item -LiteralPath $outFile).Length
            Write-Host "Screenshot-based recording saved ($len bytes): $outFile"
        }
        else {
            Write-Warning 'Failed to build MP4 from milestone screenshots.'
        }
    }
}

if ($testExit -eq 0 -and $RecordTarget -ne 'None' -and (Test-Path -LiteralPath $outFile)) {
    $videoCopyScript = Join-Path $repoRoot 'scripts\ci\Copy-EasyTestManualVideos.ps1'
    if (Test-Path -LiteralPath $videoCopyScript) {
        $videoArgs = @{
            SourceVideo = $outFile
        }
        if ($Filter -match 'Passport' -and $Filter -notmatch 'PersonOfficerJourney') {
            $videoArgs['GuideVideo'] = @('person-add-passport.mp4')
        }
        Write-Host 'Copying video into user-manual/assets/videos/...'
        & $videoCopyScript @videoArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Warning 'Copy-EasyTestManualVideos.ps1 failed; MP4 remains under recordings/.'
        }
    }
}

exit $testExit
