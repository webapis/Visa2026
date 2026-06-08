# Cursor runtime error agent inbox

When `ApplicationRuntimeLog:CursorBridgeEnabled` is true (default in dev `appsettings.json`), persisted runtime errors are written here as `{id}.json` plus a rolling `inbox.jsonl`.

**Do not commit** inbox JSON files — they are local triage artifacts.

## Agent workflow

1. Read newest file in this folder (or `inbox.jsonl` tail).
2. Follow `@visa2026-runtime-error-tracking` → **Agent fix loop**.
3. After verify, mark fixed:

```powershell
dotnet run --project tools/RuntimeLogResolution -- mark-fixed --id <guid> --notes "summary"
```

Processed files move to `.cursor/runtime-errors/archive/`.

## Cursor hooks (auto-prompt)

Project [`.cursor/hooks.json`](../../hooks.json) runs when you use Agent:

- **New Agent chat** — pending inbox items appear in session context.
- **After each agent turn** — if a new `{id}.json` exists, Cursor auto-submits a fix prompt.

Keep an Agent chat open while F5-debugging, or use `/loop` (see skill `agent-fix-loop.md`) when idle.

Disable hooks locally: create [`.cursor/runtime-errors/hook-disabled`](../hook-disabled).
