#Requires -Version 5.1
<#
.SYNOPSIS
  Preview manual test reports locally (separate from officer user manual).

.EXAMPLE
  ./scripts/local/Serve-ManualTestReports.ps1
#>
[CmdletBinding()]
param(
    [int]$Port = 8766,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$reportDir = Join-Path $repoRoot 'manual-test-reports\latest'
$writeScript = Join-Path $repoRoot 'scripts\ci\Write-ManualTestReport.ps1'

if (-not (Test-Path -LiteralPath (Join-Path $reportDir 'summary.html'))) {
    if (-not (Test-Path -LiteralPath $writeScript)) {
        throw "No report at $reportDir. Run scripts/ci/Write-ManualTestReport.ps1 first."
    }

    Write-Host 'No summary.html yet - generating report scaffold...'
    & $writeScript
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) {
        throw 'Write-ManualTestReport.ps1 failed.'
    }
}

$root = Join-Path $repoRoot 'manual-test-reports'
$url = "http://127.0.0.1:$Port/latest/summary.html"

Write-Host "Manual test report preview: $url"
Write-Host 'Press Ctrl+C to stop.'

if (-not $NoBrowser) {
    Start-Process $url
}

function Get-ManualPythonCommand {
    $portable = Join-Path $repoRoot 'user-manual\.tools\python312\python.exe'
    if (Test-Path -LiteralPath $portable) {
        Write-Host "Using portable Python at $portable"
        return @{ FilePath = $portable; Prefix = @() }
    }

    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        foreach ($candidate in @('python', 'python3', 'py')) {
            if (-not (Get-Command $candidate -ErrorAction SilentlyContinue)) { continue }
            if ($candidate -eq 'py') {
                $probe = & py -3 -c "import sys; print(sys.version_info.major)" 2>$null
                if ($LASTEXITCODE -eq 0 -and $probe) {
                    Write-Host 'Using system Python: py -3'
                    return @{ FilePath = 'py'; Prefix = @('-3') }
                }
                continue
            }

            $probe = & $candidate -c "import sys; print(sys.version_info.major)" 2>$null
            if ($LASTEXITCODE -eq 0 -and $probe) {
                Write-Host "Using system Python: $candidate"
                return @{ FilePath = $candidate; Prefix = @() }
            }
        }
    }
    finally {
        $ErrorActionPreference = $previousEap
    }

    throw 'Python is required. Run Serve-UserManual.ps1 once to bootstrap user-manual/.tools/python312, or install Python 3.'
}

$python = Get-ManualPythonCommand
$serveArgs = $python.Prefix + @('-m', 'http.server', [string]$Port, '--bind', '127.0.0.1')

Push-Location $root
try {
    & $python.FilePath @serveArgs
}
finally {
    Pop-Location
}