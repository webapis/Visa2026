# Cursor stop hook: auto-submit agent fix prompt when a new runtime-error inbox JSON exists.
# stdin: { "status": "completed"|"aborted"|"error", "loop_count": number }

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\runtime-error-inbox-common.ps1"

try {
    $inputRaw = [Console]::In.ReadToEnd()
    if (-not [string]::IsNullOrWhiteSpace($inputRaw)) {
        $hookInput = $inputRaw | ConvertFrom-Json
        if ($hookInput.status -and $hookInput.status -ne "completed") {
            Write-RuntimeErrorHookJson -Payload @{}
            exit 0
        }
    }
}
catch {
    Write-RuntimeErrorHookJson -Payload @{}
    exit 0
}

if (Test-RuntimeErrorHookDisabled) {
    Write-RuntimeErrorHookJson -Payload @{}
    exit 0
}

$pending = @(Get-RuntimeErrorInboxDocuments -ExcludePrompted)
if ($pending.Count -eq 0) {
    Write-RuntimeErrorHookJson -Payload @{}
    exit 0
}

$newest = $pending[0]
Add-RuntimeErrorHookPromptedId -Id $newest.Id

$errorCode = if ([string]::IsNullOrWhiteSpace($newest.ErrorCode)) { "(none)" } else { $newest.ErrorCode }
$message = if ([string]::IsNullOrWhiteSpace($newest.Message)) { "(no message)" } else { $newest.Message }
$inboxRelative = ".cursor/runtime-errors/inbox/$($newest.Id).json"

$followup = @"
@visa2026-runtime-error-tracking fix runtime error from inbox

New runtime error inbox item detected by Cursor hook.

- Id: $($newest.Id)
- ErrorCode: $errorCode
- Message: $message
- File: $inboxRelative

Follow .cursor/skills/visa2026-runtime-error-tracking/agent-fix-loop.md:
1. Read the inbox JSON (or `dotnet run --project tools/RuntimeLogResolution -- get --id $($newest.Id)`)
2. mark-in-progress
3. Triage, fix, verify build
4. mark-fixed with notes
"@.Trim()

Write-RuntimeErrorHookJson -Payload @{ followup_message = $followup }
exit 0
