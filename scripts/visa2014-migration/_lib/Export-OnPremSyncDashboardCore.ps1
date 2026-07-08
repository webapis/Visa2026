# Build sync-dashboard.json + optional Chart.js HTML from reconcile snapshot + run status.

function Export-OnPremSyncDashboard {
    param(
        $Config,
        $EntityRows,
        [string]$OutputRoot,
        [switch]$IncludeHtml,
        $RunStatus = $null
    )

    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        throw 'OutputRoot required.'
    }

    New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

    if (-not $RunStatus) {
        . (Join-Path $PSScriptRoot 'OnPremSyncRunStatus.ps1')
        $statusPath = Get-OnPremSyncRunStatusPath -Root $OutputRoot
        $RunStatus = Read-OnPremSyncRunStatus -Path $statusPath
    }

    $watermark = Get-OnPremSyncWatermark -Config $Config
    $waveSummary = Get-OnPremSyncWaveSummary -RunStatus $RunStatus
    $waveSummaryOrdered = [ordered]@{
        Pending   = $waveSummary.Pending
        Running   = $waveSummary.Running
        Completed = $waveSummary.Completed
        Failed    = $waveSummary.Failed
    }

    $entities = @($EntityRows | ForEach-Object {
        [ordered]@{
            Kind         = $_.Kind
            BO           = $_.BO
            Legacy       = $_.Legacy
            Migrated     = $_.Migrated
            NotCompleted = $_.NotCompleted
            IdMap        = $_.IdMap
            DuplicateGroups    = $_.DuplicateGroups
            DuplicateExtraRows = $_.DuplicateExtraRows
            SyncState    = $_.SyncState
            Note         = $_.Note
        }
    })

    $dashboard = [ordered]@{
        Version        = 1
        GeneratedUtc   = (Get-Date).ToUniversalTime().ToString('o')
        LegacySource   = $Config.LegacySource
        LegacyServer   = $Config.LegacyServer
        LegacyDatabase = $Config.LegacyDatabase
        TargetServer   = $Config.TargetServer
        TargetDatabase = $Config.TargetDatabase
        WatermarkUtc   = $watermark
        RunStatus      = $RunStatus
        WaveSummary    = $waveSummaryOrdered
        Entities       = $entities
    }

    $jsonPath = Get-OnPremSyncDashboardJsonPath -Root $OutputRoot
    $dashboard | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

    $htmlPath = $null
    if ($IncludeHtml) {
        $htmlPath = Get-OnPremSyncDashboardHtmlPath -Root $OutputRoot
        $html = Build-OnPremSyncDashboardHtml -Dashboard $dashboard
        Set-Content -LiteralPath $htmlPath -Value $html -Encoding UTF8
    }

    [pscustomobject]@{
        JsonPath = $jsonPath
        HtmlPath = $htmlPath
    }
}

function Build-OnPremSyncDashboardHtml {
    param($Dashboard)

    $generated = $Dashboard.GeneratedUtc
    $legacySource = $Dashboard.LegacySource
    $overall = if ($Dashboard.RunStatus) { $Dashboard.RunStatus.OverallStatus } else { 'Unknown' }
    $currentWave = if ($Dashboard.RunStatus) { $Dashboard.RunStatus.CurrentWave } else { '' }
    $watermark = if ($Dashboard.WatermarkUtc) { $Dashboard.WatermarkUtc } else { '(none)' }

    $scalarRows = @($Dashboard.Entities | Where-Object { $_.Kind -eq 'Scalar' })
    $labels = ($scalarRows | ForEach-Object { $_.BO }) -join "','"
    $legacyData = ($scalarRows | ForEach-Object { if ($null -eq $_.Legacy) { 0 } else { $_.Legacy } }) -join ','
    $migratedData = ($scalarRows | ForEach-Object { if ($null -eq $_.Migrated) { 0 } else { $_.Migrated } }) -join ','

    $ws = $Dashboard.WaveSummary
    $wavePending = if ($ws) { $ws.Pending } else { 0 }
    $waveRunning = if ($ws) { $ws.Running } else { 0 }
    $waveCompleted = if ($ws) { $ws.Completed } else { 0 }
    $waveFailed = if ($ws) { $ws.Failed } else { 0 }

    @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta http-equiv="refresh" content="30" />
  <title>Visa2026 legacy sync dashboard</title>
  <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
  <style>
    body { font-family: system-ui, sans-serif; margin: 1.5rem; background: #0f172a; color: #e2e8f0; }
    h1 { margin: 0 0 0.25rem; font-size: 1.5rem; }
    .meta { color: #94a3b8; font-size: 0.9rem; margin-bottom: 1.25rem; }
    .status { display: inline-block; padding: 0.2rem 0.65rem; border-radius: 999px; font-size: 0.85rem; font-weight: 600; }
    .status-running { background: #1d4ed8; }
    .status-completed { background: #15803d; }
    .status-failed { background: #b91c1c; }
    .grid { display: grid; grid-template-columns: 2fr 1fr; gap: 1rem; }
    .card { background: #1e293b; border-radius: 0.65rem; padding: 1rem; border: 1px solid #334155; }
    table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
    th, td { text-align: left; padding: 0.35rem 0.5rem; border-bottom: 1px solid #334155; }
    th { color: #94a3b8; font-weight: 600; }
    .gap-warn { color: #fbbf24; }
    .gap-ok { color: #86efac; }
  </style>
</head>
<body>
  <h1>Legacy sync dashboard</h1>
  <p class="meta">
    Source: $legacySource &middot; Generated: $generated &middot; Watermark: $watermark<br />
    Overall: <span class="status status-$(($overall -replace ' ','').ToLower())">$overall</span>
    $(if ($currentWave) { " &middot; Current wave: <strong>$currentWave</strong>" })
  </p>
  <div class="grid">
    <div class="card"><canvas id="countsChart" height="120"></canvas></div>
    <div class="card"><canvas id="waveChart" height="120"></canvas></div>
  </div>
  <div class="card" style="margin-top:1rem;">
    <table>
      <thead><tr><th>BO</th><th>Legacy</th><th>Migrated</th><th>Gap</th><th>Id-map</th><th>Dup grp</th><th>Dup +</th><th>Status</th></tr></thead>
      <tbody>
$(($scalarRows | ForEach-Object {
    $gap = if ($null -ne $_.NotCompleted) { $_.NotCompleted } else { '' }
    $gapClass = if ($gap -gt 0) { 'gap-warn' } else { 'gap-ok' }
    "        <tr><td>$($_.BO)</td><td>$($_.Legacy)</td><td>$($_.Migrated)</td><td class='$gapClass'>$gap</td><td>$($_.IdMap)</td><td>$($_.DuplicateGroups)</td><td>$($_.DuplicateExtraRows)</td><td>$($_.SyncState)</td></tr>"
}) -join "`n")
      </tbody>
    </table>
  </div>
  <script>
    const countsCtx = document.getElementById('countsChart');
    new Chart(countsCtx, {
      type: 'bar',
      data: {
        labels: ['$labels'],
        datasets: [
          { label: 'Legacy', data: [$legacyData], backgroundColor: '#64748b' },
          { label: 'Migrated', data: [$migratedData], backgroundColor: '#38bdf8' }
        ]
      },
      options: { responsive: true, plugins: { title: { display: true, text: 'Legacy vs migrated (scalar)', color: '#e2e8f0' } }, scales: { x: { ticks: { color: '#94a3b8' } }, y: { ticks: { color: '#94a3b8' } } } }
    });
    const waveCtx = document.getElementById('waveChart');
    new Chart(waveCtx, {
      type: 'doughnut',
      data: {
        labels: ['Pending', 'Running', 'Completed', 'Failed'],
        datasets: [{ data: [$wavePending, $waveRunning, $waveCompleted, $waveFailed], backgroundColor: ['#475569','#2563eb','#16a34a','#dc2626'] }]
      },
      options: { responsive: true, plugins: { title: { display: true, text: 'Wave status', color: '#e2e8f0' }, legend: { labels: { color: '#e2e8f0' } } } }
    });
  </script>
</body>
</html>
"@
}
