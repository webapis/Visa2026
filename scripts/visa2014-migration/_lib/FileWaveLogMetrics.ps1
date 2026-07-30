# Parse DocumentCopies / --import-visa2014-files log lines into scalar-style watch metrics.

function Parse-FileWaveLogMetrics {
    param(
        [string]$LogPath,
        [int]$TailLines = 250
    )

    $result = [ordered]@{
        Inserted   = $null
        Updated    = $null
        Failed     = $null
        LegacyRows = $null
    }

    if ([string]::IsNullOrWhiteSpace($LogPath) -or -not (Test-Path -LiteralPath $LogPath)) {
        return $result
    }

    $lines = @(Get-Content -LiteralPath $LogPath -Tail $TailLines -ErrorAction SilentlyContinue)
    foreach ($line in @($lines)) {
        if ($line -match 'INF Legacy (?:copy|diploma copy|family-proof) rows:\s*(\d+)') {
            $result.LegacyRows = [int]$Matches[1]
        }
        elseif ($line -match 'INF Rows with blob processed:\s*(\d+)') {
            $result.LegacyRows = [int]$Matches[1]
        }
        elseif ($line -match 'INF Processed:\s*(\d+)\s+Patched:\s*(\d+)(?:\s+.*Failed:\s*(\d+))?') {
            $result.Updated = [int]$Matches[2]
            if ($Matches[3]) { $result.Failed = [int]$Matches[3] }
        }
        elseif ($line -match 'INF Posted:\s*(\d+)\s+Failed:\s*(\d+)') {
            $result.Inserted = [int]$Matches[1]
            $result.Failed = [int]$Matches[2]
        }
        elseif ($line -match 'INF Progress:\s*(\d+)\s+posted,\s*(\d+)\s+failed') {
            $result.Inserted = [int]$Matches[1]
            $result.Failed = [int]$Matches[2]
        }
    }

    return $result
}

function Get-FileWaveStepLogPath {
    param(
        [string]$SyncHostRoot,
        [string]$LogDir,
        [string]$StepKey
    )

    if ($LogDir) {
        $p = Join-Path $LogDir "$StepKey.log"
        if (Test-Path -LiteralPath $p) { return $p }
    }

    if ($SyncHostRoot) {
        $p = Join-Path $SyncHostRoot "data\import-logs\document-copies\$StepKey.log"
        if (Test-Path -LiteralPath $p) { return $p }
    }

    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
    $p = Join-Path $repoRoot "artifacts\document-copies-import\$StepKey.log"
    if (Test-Path -LiteralPath $p) { return $p }

    return $null
}
