# Shared helpers for Visa2026 runtime-error Cursor hooks.
# Requires: PowerShell 5.1+ (Windows). Uses CURSOR_PROJECT_DIR when set.

function Get-RuntimeErrorProjectRoot {
    if (-not [string]::IsNullOrWhiteSpace($env:CURSOR_PROJECT_DIR)) {
        return $env:CURSOR_PROJECT_DIR
    }

    $hooksDir = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $hooksDir "..\..")).Path
}

function Get-RuntimeErrorInboxDirectory {
    Join-Path (Get-RuntimeErrorProjectRoot) ".cursor\runtime-errors\inbox"
}

function Get-RuntimeErrorHookStatePath {
    Join-Path (Get-RuntimeErrorProjectRoot) ".cursor\runtime-errors\hook-prompted.json"
}

function Test-RuntimeErrorHookDisabled {
    $flag = Join-Path (Get-RuntimeErrorProjectRoot) ".cursor\runtime-errors\hook-disabled"
    return (Test-Path -LiteralPath $flag)
}

function Get-RuntimeErrorHookPromptedState {
    $path = Get-RuntimeErrorHookStatePath
    if (-not (Test-Path -LiteralPath $path)) {
        return [pscustomobject]@{
            promptedIds = @()
        }
    }

    try {
        $raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $parsed = $raw | ConvertFrom-Json
        $ids = @()
        if ($null -ne $parsed.promptedIds) {
            $ids = @($parsed.promptedIds | ForEach-Object { "$_".ToLowerInvariant() })
        }

        return [pscustomobject]@{
            promptedIds = $ids
        }
    }
    catch {
        return [pscustomobject]@{
            promptedIds = @()
        }
    }
}

function Save-RuntimeErrorHookPromptedState {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$PromptedIds
    )

    $path = Get-RuntimeErrorHookStatePath
    $directory = Split-Path -Parent $path
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $unique = @($PromptedIds | ForEach-Object { "$_".ToLowerInvariant() } | Select-Object -Unique)
    $payload = [ordered]@{
        promptedIds = $unique
        updatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    }

    ($payload | ConvertTo-Json -Depth 4) | Set-Content -LiteralPath $path -Encoding UTF8
}

function Test-RuntimeErrorInboxFileName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName
    )

    return $FileName -match '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.json$'
}

function Get-RuntimeErrorInboxDocuments {
    param(
        [switch]$ExcludePrompted
    )

    $inbox = Get-RuntimeErrorInboxDirectory
    if (-not (Test-Path -LiteralPath $inbox)) {
        return @()
    }

    $prompted = @()
    if ($ExcludePrompted) {
        $prompted = @(Get-RuntimeErrorHookPromptedState).promptedIds
    }

    $documents = @()
    foreach ($file in Get-ChildItem -LiteralPath $inbox -File -Filter "*.json") {
        if (-not (Test-RuntimeErrorInboxFileName -FileName $file.Name)) {
            continue
        }

        $id = [guid]::Parse($file.BaseName).ToString("D").ToLowerInvariant()
        if ($ExcludePrompted -and $prompted -contains $id) {
            continue
        }

        try {
            $raw = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
            $doc = $raw | ConvertFrom-Json
        }
        catch {
            continue
        }

        $status = "$($doc.resolutionStatus)"
        if ($status -in @("Fixed", "Ignored", "InProgress")) {
            continue
        }

        $occurred = [datetime]::MinValue
        if ($doc.occurredAtUtc) {
            [void][datetime]::TryParse("$($doc.occurredAtUtc)", [ref]$occurred)
        }

        $documents += [pscustomobject]@{
            Id = $id
            Path = $file.FullName
            ErrorCode = "$($doc.errorCode)"
            Message = "$($doc.message)"
            ResolutionStatus = $status
            OccurredAtUtc = $occurred
            WrittenAtUtc = $file.LastWriteTimeUtc
        }
    }

    return @(
        $documents |
            Sort-Object -Property @{ Expression = "OccurredAtUtc"; Descending = $true }, @{ Expression = "WrittenAtUtc"; Descending = $true }
    )
}

function Add-RuntimeErrorHookPromptedId {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Id
    )

    $state = Get-RuntimeErrorHookPromptedState
    $ids = @($state.promptedIds)
    $normalized = $Id.ToLowerInvariant()
    if ($ids -notcontains $normalized) {
        $ids += $normalized
    }

    Save-RuntimeErrorHookPromptedState -PromptedIds $ids
}

function Write-RuntimeErrorHookJson {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Payload
    )

    if ($Payload.Count -eq 0) {
        Write-Output "{}"
        return
    }

    Write-Output ($Payload | ConvertTo-Json -Compress -Depth 6)
}
