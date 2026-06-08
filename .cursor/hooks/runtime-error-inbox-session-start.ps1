# Cursor sessionStart hook: inject pending runtime-error inbox context into new Agent chats.
# stdin: { "session_id": "...", "is_background_agent": bool, "composer_mode": "agent"|... }

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\runtime-error-inbox-common.ps1"

if (Test-RuntimeErrorHookDisabled) {
    Write-RuntimeErrorHookJson -Payload @{}
    exit 0
}

$pending = @(Get-RuntimeErrorInboxDocuments -ExcludePrompted)
if ($pending.Count -eq 0) {
    Write-RuntimeErrorHookJson -Payload @{}
    exit 0
}

$lines = @(
    "Visa2026 runtime error inbox: $($pending.Count) open item(s) under .cursor/runtime-errors/inbox/."
    "Newest (not yet hook-prompted in this session's stop loop):"
)

foreach ($item in ($pending | Select-Object -First 3)) {
    $code = if ([string]::IsNullOrWhiteSpace($item.ErrorCode)) { "(none)" } else { $item.ErrorCode }
    $msg = if ([string]::IsNullOrWhiteSpace($item.Message)) { "(no message)" } else { $item.Message }
    $lines += "- $($item.Id): [$code] $msg"
}

$lines += ""
$lines += "If the user wants triage, use @visa2026-runtime-error-tracking and agent-fix-loop.md."
$lines += "The stop hook auto-submits a fix prompt after each agent turn when new inbox JSON exists."

Write-RuntimeErrorHookJson -Payload @{ additional_context = ($lines -join [Environment]::NewLine) }
exit 0
