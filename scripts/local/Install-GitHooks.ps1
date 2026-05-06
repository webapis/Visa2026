#Requires -Version 5.1
<#
.SYNOPSIS
  Install local git hooks for this repo (no git config changes).

.DESCRIPTION
  Copies hooks from scripts/git-hooks/ into .git/hooks/.
  This is intentionally local-only; hooks are not automatically run for other users unless they install.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts/local/Install-GitHooks.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$gitDir = Join-Path $RepoRoot ".git"
if (-not (Test-Path $gitDir)) {
    throw "Not a git repo (missing .git): $RepoRoot"
}

$srcDir = Join-Path $RepoRoot "scripts/git-hooks"
$dstDir = Join-Path $gitDir "hooks"

if (-not (Test-Path $srcDir)) { throw "Missing hooks source folder: $srcDir" }
if (-not (Test-Path $dstDir)) { throw "Missing git hooks folder: $dstDir" }

$hooks = @("pre-commit")
foreach ($h in $hooks) {
    $src = Join-Path $srcDir $h
    $dst = Join-Path $dstDir $h
    if (-not (Test-Path $src)) { throw "Missing hook file: $src" }

    Copy-Item -LiteralPath $src -Destination $dst -Force

    # Ensure executable bit for Git Bash environments (best-effort; safe on Windows)
    try { & git update-index --chmod=+x $src 2>$null | Out-Null } catch { }
}

Write-Host "Installed hooks to $dstDir" -ForegroundColor Green
Write-Host "Hook active: pre-commit auto-bumps build version." -ForegroundColor Cyan
