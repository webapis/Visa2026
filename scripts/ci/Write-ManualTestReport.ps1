#Requires -Version 5.1
<#
.SYNOPSIS
  Generate manual-test-reports/latest summary (JSON + HTML) from TRX files or dotnet test runs.

.DESCRIPTION
  Separate from the officer user manual (user-manual/). Tracks passed, failed, not_run, and skipped
  per test suite and per guide. See manual-test-reports/README.md and docs/MANUAL_TEST_REPORTS.md.

.PARAMETER RunTests
  Execute suites from manual-test-reports/manifest.yaml and capture TRX under TestResults/.

.PARAMETER RunE2E
  Include EasyTest UserManual E2E when -RunTests is set (slow; Windows + Postgres).

.PARAMETER TrxPath
  Existing TRX files to merge into the report (can be used without -RunTests).

.PARAMETER OutputDir
  Report output directory (default manual-test-reports/latest).

.PARAMETER RunId
  Optional run label (default UTC timestamp).

.EXAMPLE
  ./scripts/ci/Write-ManualTestReport.ps1 -RunTests

.EXAMPLE
  ./scripts/ci/Write-ManualTestReport.ps1 -TrxPath TestResults/user-manual-docs.trx
#>
[CmdletBinding()]
param(
    [switch]$RunTests,
    [switch]$RunE2E,
    [string[]]$TrxPath = @(),
    [string]$OutputDir,
    [string]$RunId
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manifestPath = Join-Path $repoRoot 'manual-test-reports\manifest.yaml'
$docsRoot = Join-Path $repoRoot 'user-manual\docs'
$testResultsRoot = Join-Path $repoRoot 'TestResults'

if (-not $OutputDir) {
    $OutputDir = Join-Path $repoRoot 'manual-test-reports\latest'
}

if (-not $RunId) {
    $RunId = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd_HHmmss')
}

function Invoke-External {
    param(
        [string]$FilePath,
        [string[]]$ArgumentList
    )

    Write-Host ">> $FilePath $($ArgumentList -join ' ')"
    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed ($LASTEXITCODE): $FilePath"
    }
}

function Get-GitShortCommit {
    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $sha = git -C $repoRoot rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $sha) { return $sha.Trim() }
    }
    finally {
        $ErrorActionPreference = $previousEap
    }

    return ''
}

function Read-SimpleYamlSuites {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Manifest not found: $Path"
    }

    $lines = Get-Content -LiteralPath $Path -Encoding UTF8
    $suites = @()
    $current = $null
    $inSuites = $false

    foreach ($line in $lines) {
        if ($line -match '^\s*suites:\s*$') {
            $inSuites = $true
            continue
        }

        if (-not $inSuites) { continue }

        if ($line -match '^\s{2}-\s+id:\s*(.+)$') {
            if ($current) { $suites += $current }
            $current = [ordered]@{ id = $Matches[1].Trim() }
            continue
        }

        if ($null -eq $current) { continue }

        if ($line -match '^\s{4}(\w+):\s*(.+)$') {
            $key = $Matches[1]
            $value = $Matches[2].Trim().Trim('"')
            if ($value -eq 'true') { $current[$key] = $true }
            elseif ($value -eq 'false') { $current[$key] = $false }
            else { $current[$key] = $value }
        }
    }

    if ($current) { $suites += $current }
    return $suites
}

function Get-GuideInventory {
    param([string]$Root)

    $guides = @()
    if (-not (Test-Path -LiteralPath $Root)) {
        return $guides
    }

    $pattern = '---\s*\r?\n(?<body>.*?)\r?\n---'
    foreach ($file in Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.md' -File) {
        $relative = $file.FullName.Substring($Root.Length).TrimStart('\', '/')
        $segments = $relative -split '[\\/]'
        $locale = if ($segments.Count -gt 0) { $segments[0] } else { 'en' }

        $isGuide = $relative -match '[\\/]guides[\\/]' -or $relative -match '[\\/]getting-started[\\/]'
        if (-not $isGuide) { continue }
        if ($file.Name.StartsWith('_')) { continue }

        $text = [System.IO.File]::ReadAllText($file.FullName)
        if ($text -notmatch '(?s)^---\s*\r?\n(.*?)\r?\n---') { continue }

        $yaml = $Matches[1]

        function Read-YamlScalar([string]$Body, [string]$Key) {
            if ($Body -match "(?m)^$([regex]::Escape($Key)):\s*(.+)$") {
                return $Matches[1].Trim().Trim('"')
            }
            return ''
        }

        $slug = Read-YamlScalar $yaml 'slug'
        if (-not $slug) { continue }

        $guides += [pscustomobject]@{
            slug           = $slug
            locale         = $locale
            title          = (Read-YamlScalar $yaml 'title')
            status         = (Read-YamlScalar $yaml 'status')
            tier           = (Read-YamlScalar $yaml 'tier')
            e2eTestFilter  = (Read-YamlScalar $yaml 'e2eTestFilter')
            e2eScenarioId  = (Read-YamlScalar $yaml 'e2eScenarioId')
            verified       = (Read-YamlScalar $yaml 'verified')
        }
    }

    return $guides
}

function Import-TrxResults {
    param([string[]]$Paths)

    $byName = @{}
    foreach ($path in $Paths) {
        if (-not (Test-Path -LiteralPath $path)) {
            Write-Warning "TRX not found: $path"
            continue
        }

        $suiteId = [System.IO.Path]::GetFileNameWithoutExtension($path)
        [xml]$trx = Get-Content -LiteralPath $path -Encoding UTF8
        $ns = @{ t = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010' }

        $nodes = Select-Xml -Xml $trx -XPath '//t:UnitTestResult' -Namespace $ns
        foreach ($node in $nodes) {
            $name = $node.Node.testName
            if ([string]::IsNullOrWhiteSpace($name)) { continue }

            $outcome = $node.Node.outcome
            $status = switch ($outcome) {
                'Passed' { 'passed' }
                'Failed' { 'failed' }
                'NotExecuted' { 'not_run' }
                'Skipped' { 'skipped' }
                default { $outcome.ToLowerInvariant() }
            }

            $durationMs = 0
            if ($node.Node.'duration') {
                try {
                    $ts = [TimeSpan]::Parse($node.Node.'duration')
                    $durationMs = [int]$ts.TotalMilliseconds
                }
                catch { }
            }

            $key = "$suiteId::$name"
            $byName[$key] = [pscustomobject]@{
                suiteId    = $suiteId
                name       = $name
                status     = $status
                durationMs = $durationMs
                trxPath    = $path
            }
        }
    }

    return $byName
}

function Get-TestStatusFromTrx {
    param(
        $TrxResults,
        [string]$FilterHint
    )

    if ([string]::IsNullOrWhiteSpace($FilterHint)) {
        return 'not_run'
    }

    $matches = @($TrxResults.Values | Where-Object { $_.name -like "*$FilterHint*" })
    if ($matches.Count -eq 0) { return 'not_run' }
    if ($matches | Where-Object { $_.status -eq 'failed' }) { return 'failed' }
    if ($matches | Where-Object { $_.status -eq 'passed' }) { return 'passed' }
    return ($matches[0].status)
}

function Format-Html([string]$Text) {
    if ($null -eq $Text) { return '' }
    return [System.Net.WebUtility]::HtmlEncode([string]$Text)
}

function Format-Badge([string]$Status) {
    $class = switch ($Status) {
        'passed' { 'ok' }
        'failed' { 'bad' }
        'skipped' { 'skip' }
        default { 'pending' }
    }
    return "<span class=""badge $class"">$(Format-Html $Status)</span>"
}

function New-HtmlReport {
    param($Summary)

    $suiteRows = ($Summary.suites | ForEach-Object {
        $s = $_
        "<tr><td>$(Format-Html $s.name)</td><td>$(Format-Html $s.kind)</td><td>$(Format-Badge $s.status)</td><td>$($s.passed)</td><td>$($s.failed)</td><td>$($s.notRun)</td><td>$($s.skipped)</td></tr>"
    }) -join "`n"

    $testRows = ($Summary.tests | ForEach-Object {
        $t = $_
        "<tr><td>$(Format-Html $t.suiteName)</td><td><code>$(Format-Html $t.name)</code></td><td>$(Format-Badge $t.status)</td><td>$($t.durationMs)</td></tr>"
    }) -join "`n"

    $guideRows = ($Summary.guides | ForEach-Object {
        $g = $_
        "<tr><td>$(Format-Html $g.slug)</td><td>$(Format-Html $g.locale)</td><td>$(Format-Html $g.title)</td><td><code>$(Format-Html $g.e2eTestFilter)</code></td><td>$(Format-Badge $g.status)</td></tr>"
    }) -join "`n"

    $overallClass = if ($Summary.overall -eq 'passed') { 'ok' } else { 'bad' }

    return @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Visa2026 manual test report</title>
  <style>
    :root { --ok:#1b7f3a; --bad:#c62828; --pending:#6b6b6b; --skip:#9a6b00; --bg:#f6f8fb; --card:#fff; }
    body { font-family: Segoe UI, system-ui, sans-serif; margin: 0; background: var(--bg); color: #1a1a1a; }
    header { background: #1a237e; color: #fff; padding: 1.25rem 1.5rem; }
    header h1 { margin: 0 0 .25rem; font-size: 1.35rem; }
    header p { margin: .15rem 0; opacity: .9; font-size: .92rem; }
    main { max-width: 1100px; margin: 0 auto; padding: 1.25rem; }
    .card { background: var(--card); border-radius: 8px; padding: 1rem 1.1rem; margin-bottom: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,.08); }
    h2 { margin: 0 0 .75rem; font-size: 1.05rem; }
    table { width: 100%; border-collapse: collapse; font-size: .9rem; }
    th, td { text-align: left; padding: .45rem .5rem; border-bottom: 1px solid #e6e9ef; vertical-align: top; }
    th { font-weight: 600; color: #444; }
    .badge { display: inline-block; padding: .12rem .45rem; border-radius: 999px; font-size: .78rem; font-weight: 600; text-transform: uppercase; }
    .badge.ok { background: #e7f6ea; color: var(--ok); }
    .badge.bad { background: #fdecea; color: var(--bad); }
    .badge.pending { background: #eceff1; color: var(--pending); }
    .badge.skip { background: #fff6e5; color: var(--skip); }
    .overall { font-size: 1.1rem; font-weight: 700; }
    .overall.ok { color: var(--ok); }
    .overall.bad { color: var(--bad); }
    code { font-size: .82rem; }
    .note { color: #555; font-size: .88rem; }
  </style>
</head>
<body>
  <header>
    <h1>Visa2026 manual test report</h1>
    <p>Separate from the officer user manual. Run <code>$(Format-Html $Summary.runId)</code> · Commit <code>$(Format-Html $Summary.commit)</code></p>
    <p>Generated $(Format-Html $Summary.generatedAt)</p>
  </header>
  <main>
    <section class="card">
      <h2>Overall</h2>
      <p class="overall $overallClass">$(Format-Html $Summary.overall)</p>
      <p class="note">Guides use <strong>passed</strong> only when their linked E2E test passed in this run. Officer site shows a green tick only - not this page.</p>
    </section>
    <section class="card">
      <h2>Test suites</h2>
      <table>
        <thead><tr><th>Suite</th><th>Kind</th><th>Status</th><th>Passed</th><th>Failed</th><th>Not run</th><th>Skipped</th></tr></thead>
        <tbody>$suiteRows</tbody>
      </table>
    </section>
    <section class="card">
      <h2>Individual tests</h2>
      <table>
        <thead><tr><th>Suite</th><th>Test</th><th>Status</th><th>Duration (ms)</th></tr></thead>
        <tbody>$testRows</tbody>
      </table>
    </section>
    <section class="card">
      <h2>Guides (en locale matrix)</h2>
      <table>
        <thead><tr><th>Slug</th><th>Locale</th><th>Title</th><th>E2E filter</th><th>Status</th></tr></thead>
        <tbody>$guideRows</tbody>
      </table>
    </section>
  </main>
</body>
</html>
"@
}

function Write-GuidesMatrixMarkdown {
    param($Summary)

    $lines = @(
        '# Manual test report - guide matrix',
        '',
        "| Slug | Locale | Title | E2E filter | Status |",
        "|------|--------|-------|------------|--------|"
    )

    foreach ($g in $Summary.guides) {
        $lines += "| $($g.slug) | $($g.locale) | $($g.title) | ``$($g.e2eTestFilter)`` | **$($g.status)** |"
    }

    return ($lines -join "`n") + "`n"
}

# --- main ---

$suites = Read-SimpleYamlSuites -Path $manifestPath
$trxFiles = @($TrxPath)
$runTrxDir = Join-Path $repoRoot "manual-test-reports\runs\$RunId"
New-Item -ItemType Directory -Force -Path $runTrxDir | Out-Null
New-Item -ItemType Directory -Force -Path $testResultsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

if ($RunTests) {
    foreach ($suite in $suites) {
        if ($suite.kind -eq 'e2e' -and -not $RunE2E) {
            Write-Host "Skipping E2E suite $($suite.id) (use -RunE2E to include)."
            continue
        }

        $trxName = "$($suite.id).trx"
        $trxOut = Join-Path $testResultsRoot $trxName
        $project = Join-Path $repoRoot ($suite.project -replace '/', '\')
        $config = if ($suite.configuration) { $suite.configuration } else { 'Debug' }

        $args = @(
            'test', $project,
            '-c', $config,
            '--no-restore',
            '--logger', "trx;LogFileName=$trxName",
            '--results-directory', $testResultsRoot
        )

        if ($suite.filter) {
            $args += @('--filter', $suite.filter)
        }

        try {
            Invoke-External -FilePath 'dotnet' -ArgumentList $args
        }
        catch {
            Write-Warning "Suite $($suite.id) test run failed - TRX may still be written."
        }

        if (Test-Path -LiteralPath $trxOut) {
            Copy-Item -LiteralPath $trxOut -Destination (Join-Path $runTrxDir $trxName) -Force
            $trxFiles += $trxOut
        }
    }
}

$trxResults = Import-TrxResults -Paths $trxFiles
$allGuides = Get-GuideInventory -Root $docsRoot
$enGuides = @($allGuides | Where-Object { $_.locale -eq 'en' } | Sort-Object slug)

$summarySuites = @()
$summaryTests = @()
$anyFailed = $false
$anyRequiredNotRun = $false

foreach ($suite in $suites) {
    $suiteTests = @($trxResults.Values | Where-Object { $_.suiteId -eq $suite.id })

    if ($suite.kind -eq 'e2e' -and -not $RunE2E -and $RunTests -and $suiteTests.Count -eq 0) {
        $suiteStatus = 'skipped'
        $passed = 0; $failed = 0; $notRun = 0; $skipped = 1
    }
    elseif ($suite.optional -and $suiteTests.Count -eq 0) {
        $suiteStatus = 'skipped'
        $passed = 0; $failed = 0; $notRun = 0; $skipped = 1
    }
    else {
        $passed = @($suiteTests | Where-Object { $_.status -eq 'passed' }).Count
        $failed = @($suiteTests | Where-Object { $_.status -eq 'failed' }).Count
        $notRun = @($suiteTests | Where-Object { $_.status -eq 'not_run' }).Count
        $skipped = @($suiteTests | Where-Object { $_.status -eq 'skipped' }).Count

        if ($failed -gt 0) { $suiteStatus = 'failed'; $anyFailed = $true }
        elseif ($passed -gt 0) { $suiteStatus = 'passed' }
        elseif ($RunTests -or $trxFiles.Count -gt 0) {
            if ($suite.kind -eq 'e2e' -and -not $RunE2E) {
                $suiteStatus = 'skipped'
                $skipped = 1
            }
            else {
                $suiteStatus = 'not_run'
                $notRun = 1
                if (-not $suite.optional) { $anyRequiredNotRun = $true }
            }
        }
        else {
            $suiteStatus = 'not_run'
            $notRun = 1
            if (-not $suite.optional) { $anyRequiredNotRun = $true }
        }
    }

    $summarySuites += [pscustomobject]@{
        id      = $suite.id
        name    = $suite.name
        kind    = $suite.kind
        status  = $suiteStatus
        passed  = $passed
        failed  = $failed
        notRun  = $notRun
        skipped = $skipped
    }

    foreach ($t in $suiteTests) {
        $summaryTests += [pscustomobject]@{
            suiteId   = $suite.id
            suiteName = $suite.name
            name      = $t.name
            status    = $t.status
            durationMs = $t.durationMs
        }
    }
}

$summaryGuides = @()
foreach ($g in $enGuides) {
    $status = Get-TestStatusFromTrx -TrxResults $trxResults -FilterHint $g.e2eTestFilter
    if ($status -eq 'not_run' -and $g.e2eTestFilter) { $anyRequiredNotRun = $true }
    if ($status -eq 'failed') { $anyFailed = $true }

    $summaryGuides += [pscustomobject]@{
        slug          = $g.slug
        locale        = $g.locale
        title         = $g.title
        guideStatus   = $g.status
        e2eTestFilter = $g.e2eTestFilter
        e2eScenarioId = $g.e2eScenarioId
        status        = $status
    }
}

$overall = if ($anyFailed) { 'failed' } elseif ($anyRequiredNotRun) { 'incomplete' } else { 'passed' }

$summary = [ordered]@{
    version     = 1
    runId       = $RunId
    generatedAt = (Get-Date).ToUniversalTime().ToString('o')
    commit      = Get-GitShortCommit
    workflowRunId = $env:GITHUB_RUN_ID
    overall     = $overall
    suites      = $summarySuites
    tests       = $summaryTests
    guides      = $summaryGuides
}

$jsonPath = Join-Path $OutputDir 'summary.json'
$htmlPath = Join-Path $OutputDir 'summary.html'
$mdPath = Join-Path $OutputDir 'guides-matrix.md'

$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
New-HtmlReport -Summary $summary | Set-Content -LiteralPath $htmlPath -Encoding UTF8
Write-GuidesMatrixMarkdown -Summary $summary | Set-Content -LiteralPath $mdPath -Encoding UTF8

Write-Host "Manual test report written to $OutputDir"
Write-Host "  overall: $overall"
Write-Host "  open: $htmlPath"

if ($overall -eq 'failed') { exit 1 }
