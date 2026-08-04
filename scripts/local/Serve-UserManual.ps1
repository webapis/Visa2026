#Requires -Version 5.1
<#
.SYNOPSIS
  Build and preview the officer user manual locally (MkDocs Material + i18n).

.DESCRIPTION
  Bootstraps a portable Python under user-manual/.tools/ when system Python is missing,
  runs Build-UserManual.ps1, then mkdocs serve with live reload.

.PARAMETER Port
  Local HTTP port (default 8765).

.PARAMETER SkipBuild
  Skip generator/tests/validate/mkdocs build; only serve existing user-manual/site/.

.PARAMETER NoBrowser
  Do not open the default browser after the server starts.

.PARAMETER ManualMediaBaseUrl
  Remote HTTPS base for guide screenshots/videos (e.g. https://10.100.128.25:8081/manual-media).
  Overrides MANUAL_MEDIA_BASE_URL env. When set, media is loaded from that URL instead of
  user-manual/docs/assets/ during mkdocs serve.

.EXAMPLE
  ./scripts/local/Serve-UserManual.ps1

.EXAMPLE
  ./scripts/local/Serve-UserManual.ps1 -ManualMediaBaseUrl 'https://10.100.128.25:8081/manual-media'

.EXAMPLE
  ./scripts/local/Serve-UserManual.ps1 -Port 9000 -SkipBuild
#>
[CmdletBinding()]
param(
    [int]$Port = 8765,
    [switch]$SkipBuild,
    [switch]$NoBrowser,
    [string]$ManualMediaBaseUrl
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manualRoot = Join-Path $repoRoot 'user-manual'
$toolsRoot = Join-Path $manualRoot '.tools'
$pythonRoot = Join-Path $toolsRoot 'python312'
$pythonExe = Join-Path $pythonRoot 'python.exe'
$requirements = Join-Path $manualRoot 'requirements.txt'
$mkdocsConfig = Join-Path $manualRoot 'mkdocs.yml'
$siteDir = Join-Path $manualRoot 'site'
$buildScript = Join-Path $repoRoot 'scripts\ci\Build-UserManual.ps1'

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

function Test-SystemPython {
    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'

    try {
        foreach ($candidate in @('python', 'python3', 'py')) {
            if (-not (Get-Command $candidate -ErrorAction SilentlyContinue)) {
                continue
            }

            if ($candidate -eq 'py') {
                $version = & py -3 -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')" 2>$null
                if ($LASTEXITCODE -eq 0 -and $version) {
                    return @{ FilePath = 'py'; Prefix = @('-3') }
                }

                continue
            }

            $version = & $candidate -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')" 2>$null
            if ($LASTEXITCODE -eq 0 -and $version) {
                return @{ FilePath = $candidate; Prefix = @() }
            }
        }
    }
    finally {
        $ErrorActionPreference = $previousEap
    }

    return $null
}

function Ensure-PortablePython {
    if (Test-Path -LiteralPath $pythonExe) {
        $probe = & $pythonExe -c "import sys; print(sys.version)" 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Using portable Python at $pythonExe ($probe)"
            return
        }
    }

    $embedVersion = '3.12.7'
    $zipName = "python-$embedVersion-embed-amd64.zip"
    $zipUrl = "https://www.python.org/ftp/python/$embedVersion/$zipName"
    $zipPath = Join-Path $toolsRoot $zipName

    Write-Host "Bootstrapping portable Python $embedVersion under $pythonRoot"
    New-Item -ItemType Directory -Force -Path $toolsRoot | Out-Null
    if (Test-Path -LiteralPath $pythonRoot) {
        Remove-Item -LiteralPath $pythonRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $pythonRoot | Out-Null
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing
    Expand-Archive -LiteralPath $zipPath -DestinationPath $pythonRoot -Force

    $sitePackages = Join-Path $pythonRoot 'Lib\site-packages'
    New-Item -ItemType Directory -Force -Path $sitePackages | Out-Null

    $pthFile = Get-ChildItem -LiteralPath $pythonRoot -Filter 'python*._pth' | Select-Object -First 1
    if (-not $pthFile) {
        throw "Embeddable Python ._pth file not found in $pythonRoot"
    }

    $pthContent = @(
        'python312.zip'
        '.'
        'Lib\site-packages'
        'import site'
    )
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($pthFile.FullName, $pthContent, $utf8NoBom)

    $getPip = Join-Path $toolsRoot 'get-pip.py'
    Invoke-WebRequest -Uri 'https://bootstrap.pypa.io/get-pip.py' -OutFile $getPip -UseBasicParsing
    Invoke-External -FilePath $pythonExe -ArgumentList @($getPip, '--no-warn-script-location')
    Write-Host "Portable Python ready at $pythonExe"
}

function Get-ManualPython {
    $system = Test-SystemPython
    if ($system) {
        Write-Host "Using system Python: $($system.FilePath)"
        return $system
    }

    Ensure-PortablePython
    return @{ FilePath = $pythonExe; Prefix = @() }
}

function Install-MkDocsRequirements {
    param($Python)

    $pipArgs = $Python.Prefix + @('-m', 'pip', 'install', '--disable-pip-version-check', '-r', $requirements)
    Invoke-External -FilePath $Python.FilePath -ArgumentList $pipArgs
}

function Stop-ManualPreviewListener {
    param([int]$ListenPort)

    $listeners = @(Get-NetTCPConnection -LocalPort $ListenPort -State Listen -ErrorAction SilentlyContinue)
    foreach ($listener in $listeners) {
        if ($listener.OwningProcess -le 0) {
            continue
        }

        Write-Host "Stopping existing listener on port $ListenPort (PID $($listener.OwningProcess))"
        Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
    }

    if ($listeners.Count -gt 0) {
        Start-Sleep -Seconds 1
    }
}

function Wait-ManualPreviewReady {
    param(
        [string]$Url,
        [int]$TimeoutSec = 120
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSec)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200 -and $response.Content.Length -gt 0) {
                return
            }
        }
        catch {
            # mkdocs serve builds all locales before the first response is ready
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Manual preview did not become ready at $Url within ${TimeoutSec}s."
}

function Start-ManualPreview {
    param($Python)

    $mediaBaseUrl = Get-ManualMediaBaseUrl -Override $ManualMediaBaseUrl
    if ($mediaBaseUrl) {
        $env:MANUAL_MEDIA_BASE_URL = $mediaBaseUrl
        Write-Host "MANUAL_MEDIA_BASE_URL=$mediaBaseUrl"
    }
    else {
        Remove-Item Env:\MANUAL_MEDIA_BASE_URL -ErrorAction SilentlyContinue
    }

    if (-not $SkipBuild) {
        if (-not (Test-Path -LiteralPath $buildScript)) {
            throw "Build script not found: $buildScript"
        }

        $env:USER_MANUAL_PYTHON = $Python.FilePath
        if ($Python.Prefix.Count -gt 0) {
            $env:USER_MANUAL_PYTHON_ARGS = ($Python.Prefix -join ' ')
        } else {
            Remove-Item Env:USER_MANUAL_PYTHON_ARGS -ErrorAction SilentlyContinue
        }

        & $buildScript -SkipE2E -ManualMediaBaseUrl $ManualMediaBaseUrl
        if ($LASTEXITCODE -ne 0) {
            throw "Build-UserManual.ps1 failed."
        }
    } elseif (-not (Test-Path -LiteralPath (Join-Path $siteDir 'index.html'))) {
        throw "No built site at $siteDir. Run without -SkipBuild first."
    }

    $assetsSource = Join-Path $manualRoot 'assets'
    if (-not $mediaBaseUrl -and -not (Test-Path -LiteralPath (Join-Path $assetsSource 'screenshots'))) {
        Write-Warning "No screenshots under user-manual/assets/screenshots/. Run Record-EasyTest.ps1 then Copy-EasyTestManualScreenshots.ps1, or images will be missing in guides."
    }
    elseif ($mediaBaseUrl) {
        Write-Host "Remote media mode: screenshots/videos load from $mediaBaseUrl"
    }

    Install-MkDocsRequirements -Python $Python

    $url = "http://127.0.0.1:$Port/manual/"
    Write-Host ""
    Write-Host "Officer manual preview: $url"
    Write-Host "Guides: ${url}getting-started/login/  |  ${url}guides/person/register/"
    Write-Host "Locales: $url (en)  |  ${url}tr/  |  ${url}tk/  |  ${url}ru/"
    Write-Host "Edit user-manual/docs/ - the server reloads automatically (Ctrl+C to stop)"
    Write-Host ""

    Stop-ManualPreviewListener -ListenPort $Port

    $serveArgs = $Python.Prefix + @(
        '-m', 'mkdocs', 'serve',
        '-f', $mkdocsConfig,
        '-a', "127.0.0.1:$Port",
        '--dirtyreload'
    )

    Write-Host "Starting mkdocs serve..."
    $serveProcess = Start-Process -FilePath $Python.FilePath -ArgumentList $serveArgs -PassThru -NoNewWindow
    try {
        Wait-ManualPreviewReady -Url $url
        Write-Host "Preview ready."

        if (-not $NoBrowser) {
            Start-Process $url
        }

        Wait-Process -Id $serveProcess.Id
        if ($serveProcess.ExitCode -and $serveProcess.ExitCode -ne 0) {
            throw "mkdocs serve failed ($($serveProcess.ExitCode))."
        }
    }
    finally {
        if (-not $serveProcess.HasExited) {
            Stop-Process -Id $serveProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

if (-not (Test-Path -LiteralPath $mkdocsConfig)) {
    throw "MkDocs config not found: $mkdocsConfig"
}

$python = Get-ManualPython
Start-ManualPreview -Python $python
