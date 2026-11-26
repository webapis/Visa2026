#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true)]
<#
.SYNOPSIS
  Stop wedged MSBuild / Roslyn / `dotnet build` processes that make Visa2026 builds look hung.

.DESCRIPTION
  Visual Studio F5 and agent/CLI builds often leave VBCSCompiler or MSBuild nodes alive after a
  cancel. Those keep CPU high and block the next compile. This script kills build tooling only —
  not devenv.exe and not a running Visa2026.Blazor.Server host.

.EXAMPLE
  ./scripts/local/Stop-HungBuilds.ps1
#>
$ErrorActionPreference = 'Continue'

function Test-IsBuildToolCommand([string]$cmd) {
    if ([string]::IsNullOrWhiteSpace($cmd)) { return $false }
    if ($cmd -match 'Visa2026\.Blazor\.Server\.dll') { return $false }
    if ($cmd -match 'exec\s+".*Visa2026\.DataImporter') { return $false }
    if ($cmd -match 'MSBuild|msbuild|VBCSCompiler|build-server') { return $true }
    if ($cmd -match 'dotnet(\.exe)?(\s+")?.*\b(build|msbuild|restore)\b') { return $true }
    return $false
}

$killed = 0
Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -in @('MSBuild.exe', 'VBCSCompiler.exe') -or
        ($_.Name -eq 'dotnet.exe' -and (Test-IsBuildToolCommand $_.CommandLine))
    } |
    ForEach-Object {
        $label = "$($_.Name) pid=$($_.ProcessId)"
        if ($PSCmdlet.ShouldProcess($label, 'Stop-Process')) {
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
            Write-Host "Stopped $label"
            $killed++
        }
    }

try {
    $env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
    $env:MSBUILDDISABLENODEREUSE = '1'
    # Non-blocking-ish: if this hangs, user can Ctrl+C; we already killed processes above.
    $p = Start-Process -FilePath 'dotnet' -ArgumentList @('build-server','shutdown') -NoNewWindow -PassThru
    if (-not $p.WaitForExit(15000)) {
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        Write-Host 'build-server shutdown timed out (15s); processes already stopped.'
    }
} catch {
    Write-Host "build-server shutdown skipped: $($_.Exception.Message)"
}

Write-Host "Done. Stopped $killed build process(es)."
