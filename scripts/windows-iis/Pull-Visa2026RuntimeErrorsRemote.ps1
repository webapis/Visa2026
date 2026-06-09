#Requires -Version 5.1
<#
.SYNOPSIS
  Pull ApplicationRuntimeLog rows from IIS slots into the local Cursor runtime-error inbox.

.PARAMETER Profile
  Production, Staging, Demo, or All (default All).

.PARAMETER SshHost
  SSH config host (default visa2026-onprem).

.PARAMETER SinceMinutes
  Look-back window per slot (default 60). Overridden when pull-state has a newer watermark.

.PARAMETER Limit
  Max rows per slot (default 50).

.PARAMETER UseDirectSql
  Use tools/RuntimeLogResolution pull-remote with a LAN connection string instead of SSH SQL relay.

.PARAMETER ServerHost
  Replace localhost in remote connection string when -UseDirectSql (default 10.100.128.25).

.NOTES
  Skill: .cursor/skills/visa2026-runtime-error-tracking/agent-fix-loop.md
#>
param(
    [ValidateSet("Production", "Staging", "Demo", "All")]
    [string]$Profile = "All",

    [string]$SshHost = "visa2026-onprem",
    [int]$SinceMinutes = 60,
    [int]$Limit = 50,

    [ValidateSet("Error", "Warning", "Critical")]
    [string]$MinLevel = "Error",

    [switch]$UseDirectSql,
    [string]$ServerHost = "10.100.128.25"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot "Visa2026-IisSlots.ps1")

$profiles = if ($Profile -eq "All") { Get-Visa2026IisSlotProfiles } else { @($Profile) }
$inboxDirectory = Join-Path $RepoRoot ".cursor\runtime-errors\inbox"
$statePath = Join-Path $RepoRoot ".cursor\runtime-errors\pull-state.json"
$remoteDeploy = "C:/visa2026-deploy/iis"
$remoteGetScript = "$remoteDeploy/Get-Visa2026RuntimeErrorsForPull.ps1"
$localGetScript = Join-Path $PSScriptRoot "Get-Visa2026RuntimeErrorsForPull.ps1"

New-Item -ItemType Directory -Force -Path $inboxDirectory | Out-Null

function Get-PullState {
    if (-not (Test-Path -LiteralPath $statePath)) {
        return @{}
    }

    try {
        $raw = Get-Content -LiteralPath $statePath -Raw -Encoding UTF8 | ConvertFrom-Json
        $map = @{}
        foreach ($prop in $raw.PSObject.Properties) {
            $map[$prop.Name] = $prop.Value
        }
        return $map
    }
    catch {
        return @{}
    }
}

function Save-PullState {
    param([hashtable]$State)

    $directory = Split-Path -Parent $statePath
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    ($State | ConvertTo-Json -Depth 4) | Set-Content -LiteralPath $statePath -Encoding UTF8
}

function Get-SinceMinutesForProfile {
    param(
        [string]$SlotName,
        [hashtable]$State
    )

    if ($State.ContainsKey($SlotName) -and $State[$SlotName].lastPullUtc) {
        $lastPull = [datetime]::MinValue
        if ([datetime]::TryParse("$($State[$SlotName].lastPullUtc)", [ref]$lastPull)) {
            $minutes = [math]::Ceiling(((Get-Date).ToUniversalTime() - $lastPull.ToUniversalTime()).TotalMinutes) + 1
            if ($minutes -gt 0 -and $minutes -lt $SinceMinutes) {
                return [math]::Max(5, $minutes)
            }
        }
    }

    return $SinceMinutes
}

function Write-InboxDocuments {
    param(
        [array]$Documents,
        [ref]$WrittenCount,
        [ref]$SkippedCount
    )

    foreach ($doc in $Documents) {
        $id = "$($doc.id)".ToLowerInvariant()
        if ($id -notmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') {
            continue
        }

        $target = Join-Path $inboxDirectory "$id.json"
        if (Test-Path -LiteralPath $target) {
            $SkippedCount.Value++
            continue
        }

        ($doc | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $target -Encoding UTF8
        $jsonlPath = Join-Path $inboxDirectory "inbox.jsonl"
        Add-Content -LiteralPath $jsonlPath -Value (($doc | ConvertTo-Json -Depth 8 -Compress)) -Encoding UTF8
        $WrittenCount.Value++
    }
}

function Invoke-DirectSqlPull {
    param(
        [string]$SlotName,
        [int]$SlotSinceMinutes
    )

    $ctx = Resolve-Visa2026IisSlotContext -Profile $SlotName
    $remoteSettingsWin = Join-Path $ctx.PublishPath "appsettings.Production.json"

    $json = ssh $SshHost "powershell -NoProfile -Command \"(Get-Content -LiteralPath '$remoteSettingsWin' -Raw | ConvertFrom-Json | ConvertTo-Json -Compress)\""
    $cfg = $json | ConvertFrom-Json
    $connectionString = $cfg.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw "DefaultConnection missing for $SlotName on server."
    }

    $connectionString = $connectionString -replace '(?i)Server=localhost\\SQLEXPRESS', "Server=$ServerHost\SQLEXPRESS"
    $connectionString = $connectionString -replace '(?i)Server=localhost', "Server=$ServerHost"
    $connectionString = $connectionString -replace '(?i)Server=\(local\)', "Server=$ServerHost"
    $sinceArg = "${SlotSinceMinutes}m"

    $toolProject = Join-Path $RepoRoot "tools\RuntimeLogResolution\RuntimeLogResolution.csproj"
    & dotnet run --project $toolProject -c Release --no-build -- pull-remote `
        --connection $connectionString `
        --since $sinceArg `
        --limit $Limit `
        --min-level $MinLevel `
        --source-slot $SlotName `
        --source-database $ctx.DbName `
        --inbox $inboxDirectory

    if ($LASTEXITCODE -ne 0) {
        throw "pull-remote failed for $SlotName (exit $LASTEXITCODE)."
    }
}

function Invoke-SshRelayPull {
    param(
        [string]$SlotName,
        [int]$SlotSinceMinutes
    )

    scp -q $localGetScript "${SshHost}:${remoteGetScript}"
    $remoteWin = $remoteGetScript -replace '/', '\'
    $output = ssh $SshHost "powershell -NoProfile -ExecutionPolicy Bypass -File $remoteWin -Profile $SlotName -SinceMinutes $SlotSinceMinutes -Limit $Limit -MinLevel $MinLevel"
    if ($LASTEXITCODE -ne 0) {
        throw "Get-Visa2026RuntimeErrorsForPull.ps1 failed for $SlotName (exit $LASTEXITCODE)."
    }

    $payload = $output | ConvertFrom-Json
    return ,$payload
}

if ($UseDirectSql) {
    & dotnet build (Join-Path $RepoRoot "tools\RuntimeLogResolution\RuntimeLogResolution.csproj") -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build RuntimeLogResolution tool."
    }
}

$state = Get-PullState
$totalWritten = 0
$totalSkipped = 0
$totalQueried = 0

foreach ($slot in $profiles) {
    Write-Host ""
    Write-Host "==> Pull runtime errors: $slot" -ForegroundColor Cyan

    $slotSince = Get-SinceMinutesForProfile -SlotName $slot -State $state
    Write-Host "    Since: ${slotSince}m  Limit: $Limit  MinLevel: $MinLevel" -ForegroundColor DarkGray

    $written = 0
    $skipped = 0
    $queried = 0
    $newestOccurred = $null

    if ($UseDirectSql) {
        Invoke-DirectSqlPull -SlotName $slot -SlotSinceMinutes $slotSince
        Write-Host "    Direct SQL pull completed (see tool JSON output)." -ForegroundColor Green
    }
    else {
        $payload = Invoke-SshRelayPull -SlotName $slot -SlotSinceMinutes $slotSince
        $queried = [int]$payload.queriedCount
        Write-InboxDocuments -Documents @($payload.documents) -WrittenCount ([ref]$written) -SkippedCount ([ref]$skipped)

        if ($payload.documents -and $payload.documents.Count -gt 0) {
            $newestOccurred = ($payload.documents | Sort-Object { [datetime]$_.occurredAtUtc } -Descending | Select-Object -First 1).occurredAtUtc
        }

        Write-Host "    Queried: $queried  Written: $written  Skipped: $skipped" -ForegroundColor Green
        $totalWritten += $written
        $totalSkipped += $skipped
        $totalQueried += $queried
    }

    $state[$slot] = [ordered]@{
        lastPullUtc = (Get-Date).ToUniversalTime().ToString("o")
        lastSinceMinutes = $slotSince
        lastQueriedCount = $queried
        lastWrittenCount = $written
        newestOccurredAtUtc = $newestOccurred
    }
}

Save-PullState -State $state

Write-Host ""
Write-Host "Pull complete. Inbox: $inboxDirectory" -ForegroundColor Green
Write-Host "  Total queried: $totalQueried  written: $totalWritten  skipped: $totalSkipped" -ForegroundColor Green
Write-Host "  Cursor: @visa2026-runtime-error-tracking fix runtime error from inbox (newest open row)" -ForegroundColor DarkGray
