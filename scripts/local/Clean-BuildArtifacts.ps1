#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
<#
.SYNOPSIS
  Remove bloated XAF trace logs and optionally bin/obj folders to speed up builds.

.DESCRIPTION
  DevExpress XAF appends to eXpressAppFramework.log under project bin folders during F5
  runs. That file can grow to gigabytes and slows MSBuild, Visual Studio, and antivirus.

  Default: delete eXpressAppFramework.log only (safe; regenerated on next run).
  -Full: also remove bin/ and obj/ next to each *.csproj (MSBuild output only; not node_modules).

.EXAMPLE
  ./scripts/local/Clean-BuildArtifacts.ps1

.EXAMPLE
  ./scripts/local/Clean-BuildArtifacts.ps1 -Full
#>
param(
    [switch]$Full
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

function Get-DotNetBuildArtifactDirs {
    $dirs = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    Get-ChildItem -LiteralPath $RepoRoot -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\node_modules\\' } |
        ForEach-Object {
            $projectDir = $_.Directory.FullName
            foreach ($name in @('bin', 'obj')) {
                $path = Join-Path $projectDir $name
                if (Test-Path -LiteralPath $path) {
                    [void]$dirs.Add($path)
                }
            }
        }
  return @($dirs) | Sort-Object
}

function Get-TreeBytes([string[]]$roots) {
    $total = [long]0
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $sum = (Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue |
            Measure-Object -Property Length -Sum).Sum
        if ($null -ne $sum) { $total += [long]$sum }
    }
    return $total
}

function Format-Megabytes([long]$bytes) {
    if ($bytes -lt 1MB) { return "{0:N0} KB" -f ($bytes / 1KB) }
    if ($bytes -lt 1GB) { return "{0:N1} MB" -f ($bytes / 1MB) }
    return "{0:N2} GB" -f ($bytes / 1GB)
}

$artifactDirs = @(Get-DotNetBuildArtifactDirs)
$beforeBytes = Get-TreeBytes $artifactDirs
Write-Host "MSBuild artifact footprint before: $(Format-Megabytes $beforeBytes)" -ForegroundColor Gray

$logFiles = @(
    foreach ($binDir in $artifactDirs) {
        if (-not $binDir.EndsWith('\bin', [StringComparison]::OrdinalIgnoreCase)) { continue }
        Get-ChildItem -LiteralPath $binDir -Recurse -File -Filter eXpressAppFramework.log -ErrorAction SilentlyContinue
    }
)
$logBytes = [long]0
foreach ($log in $logFiles) { $logBytes += $log.Length }

if ($logFiles.Count -gt 0) {
    Write-Host "Found $($logFiles.Count) eXpressAppFramework.log file(s) totaling $(Format-Megabytes $logBytes)."
    foreach ($log in $logFiles) {
        $rel = $log.FullName.Substring($RepoRoot.Length).TrimStart('\')
        if ($PSCmdlet.ShouldProcess($rel, "Delete")) {
            Remove-Item -LiteralPath $log.FullName -Force
            Write-Host "  Removed $rel ($(Format-Megabytes $log.Length))" -ForegroundColor Green
        }
    }
} else {
    Write-Host "No eXpressAppFramework.log files found." -ForegroundColor Gray
}

if ($Full) {
    if ($artifactDirs.Count -eq 0) {
        Write-Host "No bin/ or obj/ folders next to .csproj files." -ForegroundColor Gray
    } else {
        Write-Host "Removing $($artifactDirs.Count) MSBuild bin/obj folder(s)..."
        foreach ($dir in $artifactDirs) {
            $rel = $dir.Substring($RepoRoot.Length).TrimStart('\')
            if ($PSCmdlet.ShouldProcess($rel, "Remove directory")) {
                Remove-Item -LiteralPath $dir -Recurse -Force
                Write-Host "  Removed $rel" -ForegroundColor Green
            }
        }
    }
} else {
    Write-Host "Skipped bin/obj removal (pass -Full to delete all build output)." -ForegroundColor Gray
}

$artifactDirsAfter = @(Get-DotNetBuildArtifactDirs)
$afterBytes = Get-TreeBytes $artifactDirsAfter
$freed = $beforeBytes - $afterBytes
if ($freed -lt 0) { $freed = [long]0 }

Write-Host ""
Write-Host "Freed approximately $(Format-Megabytes $freed)." -ForegroundColor Cyan
Write-Host "MSBuild artifact footprint after: $(Format-Megabytes $afterBytes)" -ForegroundColor Cyan
