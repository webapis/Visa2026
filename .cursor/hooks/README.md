# Visa2026 Cursor hooks

Project hooks for the runtime-error agent loop (Phase 5c).

## Runtime error inbox

| Hook | When | Effect |
|------|------|--------|
| `sessionStart` | New Agent chat | Injects `additional_context` when open inbox JSON files exist |
| `stop` | Agent turn completes | Auto-submits `followup_message` to triage the newest unprompted inbox item |

Scripts: `runtime-error-inbox-session-start.ps1`, `runtime-error-inbox-stop.ps1`, shared `runtime-error-inbox-common.ps1`.

## Requirements

- **Cursor desktop** with Hooks enabled (Settings → Hooks)
- **PowerShell** (Windows dev — matches F5 / LocalDB workflow)
- Dev app with `ApplicationRuntimeLog:CursorBridgeEnabled: true` writing `.cursor/runtime-errors/inbox/{id}.json`

## Limitation

Cursor hooks do **not** watch the filesystem. The app writes inbox JSON outside the agent; the **`stop`** hook runs after an agent turn ends, and **`sessionStart`** runs when you open a new Agent chat. For polling while idle, use `/loop` (see skill `agent-fix-loop.md`).

## Disable locally

Create an empty file:

`.cursor/runtime-errors/hook-disabled`

## Reset “already prompted” state

Delete `.cursor/runtime-errors/hook-prompted.json` to allow the stop hook to re-prompt for the same inbox id.

## Debug

Cursor **Settings → Hooks** tab and **Hooks** output channel. Restart Cursor if `hooks.json` changes are not picked up.
