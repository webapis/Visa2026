# Shared path helper for scripts under scripts/visa2014-migration/.
function Get-Visa2026RepoRoot {
    param([string]$From = $PSScriptRoot)

    $dir = $From
    while ($dir) {
        if (Test-Path -LiteralPath (Join-Path $dir 'Visa2026.slnx')) {
            return (Resolve-Path -LiteralPath $dir).Path
        }

        $parent = Split-Path -Parent $dir
        if ([string]::IsNullOrEmpty($parent) -or $parent -eq $dir) {
            break
        }

        $dir = $parent
    }

    throw "Could not locate Visa2026 repo root (Visa2026.slnx) from '$From'."
}

function Get-Visa2014MigrationRoot {
    return (Resolve-Path -LiteralPath (Join-Path (Get-Visa2026RepoRoot -From $PSScriptRoot) 'scripts\visa2014-migration')).Path
}