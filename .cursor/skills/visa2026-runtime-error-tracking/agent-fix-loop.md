# Agent fix loop — runtime errors → Cursor

**Developer-only skill.** End users and in-app admins use Operations → Runtime errors / SignalR toast; **this loop is for developers + Cursor Agent** on the workstation repo.

Use when a new file appears under [`.cursor/runtime-errors/inbox/`](../runtime-errors/inbox/), hooks fire, `/loop` runs, or the developer `@visa2026-runtime-error-tracking`.

**Copy-paste prompts:** [user-prompts.md](./user-prompts.md)

## Autonomous behavior (Agent)

When this skill is loaded and inbox has **Open** rows, Agent should **proactively** (no extra permission needed for local):

1. Read newest inbox JSON (or run pull if developer asked / `/loop`).
2. `mark-in-progress` on that id.
3. Triage [reference.md](./reference.md) → severity, ErrorCode, emitter.
4. If **P3 noise** or **BLAZOR-JS-DISC-001** → `mark-ignored` with short note; stop.
5. If **LocalVisualStudio** → implement minimal fix, `dotnet build Visa2026.slnx -c Debug`, `mark-fixed` with notes.
6. If **IIS pulled** (staging/demo/prod) → report root cause + proposed diff; **do not** apply fix unless developer said so in the same thread.
7. Append [learnings.md](./learnings.md) when root cause is verified.

**Never autonomous:** `git commit`, `git push`, IIS deploy, prod DB schema mutate, ignoring P0/P1 without telling the developer.

## Trigger

| Source | How Cursor learns |
|--------|-------------------|
| **Inbox JSON** | F5 with `CursorBridgeEnabled: true` writes `{id}.json` after SQL persist |
| **Manual** | `@visa2026-runtime-error-tracking fix runtime error <guid>` |
| **Poll** | `dotnet run --project tools/RuntimeLogResolution -- list-open --limit 5` |
| **IIS remote pull** | `scripts/windows-iis/Pull-Visa2026RuntimeErrorsRemote.ps1` (prod / staging / demo → local inbox) |

## Loop (copy checklist)

```
- [ ] 1. INBOX — read `.cursor/runtime-errors/inbox/{id}.json` OR `list-open` / `get --id`
- [ ] 2. ACK — mark-in-progress (prevents duplicate agent work on same row)
- [ ] 3. TRIAGE — reference.md → ErrorCode, severity, typical cause
- [ ] 4. REPRO — locate emitter (Category), read source, optional repro
- [ ] 5. FIX — minimal diff; match Module/host patterns
- [ ] 6. VERIFY — dotnet build Visa2026.slnx -c Debug (+ targeted test if exists)
- [ ] 7. RESOLVE — mark-fixed with notes (+ commit hash if user committed)
- [ ] 8. LEARN — append learnings.md on verified root cause
```

## CLI commands

Default connection: LocalDB `Visa2026` (override with `--connection` or `ConnectionStrings__DefaultConnection`).

**IIS slots (SSH relay — default on on-prem):**

```powershell
# All three slots → .cursor/runtime-errors/inbox/
.\scripts\windows-iis\Pull-Visa2026RuntimeErrorsRemote.ps1

# One slot, longer window
.\scripts\windows-iis\Pull-Visa2026RuntimeErrorsRemote.ps1 -Profile Staging -SinceMinutes 180

# Direct SQL when LAN/VPN reaches SQLEXPRESS on the server
.\scripts\windows-iis\Pull-Visa2026RuntimeErrorsRemote.ps1 -Profile Production -UseDirectSql -ServerHost 10.100.128.25
```

**Local / tunneled SQL:**

```powershell
dotnet run --project tools/RuntimeLogResolution -- list-open --limit 5
dotnet run --project tools/RuntimeLogResolution -- pull-remote --connection "Server=..." --since 1h --source-slot Staging --source-database Visa2026DbStaging
dotnet run --project tools/RuntimeLogResolution -- get --id <guid>
dotnet run --project tools/RuntimeLogResolution -- mark-in-progress --id <guid> --by cursor-agent
dotnet run --project tools/RuntimeLogResolution -- mark-fixed --id <guid> --notes "Root cause + fix summary" --by cursor-agent
dotnet run --project tools/RuntimeLogResolution -- mark-ignored --id <guid> --notes "Expected / won't fix"
```

`mark-fixed` / `mark-ignored` automatically move `{id}.json` to `.cursor/runtime-errors/archive/`.

## Guardrails

- **Auto-fix only** when `deploymentEnvironment` is `LocalVisualStudio` unless the user explicitly opts into IIS prod fixes.
- **Do not** auto-commit or push unless the user asked (see user git rules).
- **Skip** duplicate work: if `resolutionStatus` is already `Fixed` or `Ignored`, stop.
- **P0 startup/DB down** — triage and suggest ops steps; do not loop-blindly patch schema on prod.

## Resolution statuses (`ApplicationRuntimeLog`)

| Status | Meaning |
|--------|---------|
| `Open` | New row (default) |
| `Acknowledged` | Seen (optional; UI can add later) |
| `InProgress` | Agent or admin working |
| `Fixed` | Verified fix applied |
| `Ignored` | Expected noise / won't fix |

## Config (dev)

```json
"ApplicationRuntimeLog": {
  "CursorBridgeEnabled": true,
  "CursorBridgeLocalDevOnly": true,
  "CursorBridgeMinLevel": "Error"
}
```

IIS prod: keep `CursorBridgeEnabled: false` (see `Configure-Visa2026Production.ps1`).

## Optional: near-real-time in Cursor

### Cursor hooks (project `.cursor/hooks.json`) ✅

| Event | Behavior |
|-------|----------|
| **`sessionStart`** | Injects pending inbox summary into new Agent chats |
| **`stop`** | After each agent turn, auto-submits fix prompt for newest unprompted `{id}.json` |

**Requires:** Agent chat activity (hooks do not watch the filesystem). After F5 writes inbox JSON, open Agent or finish an agent turn → stop hook prompts triage.

**Disable:** touch `.cursor/runtime-errors/hook-disabled`  
**Re-prompt same id:** delete `.cursor/runtime-errors/hook-prompted.json`

Details: [`.cursor/hooks/README.md`](../hooks/README.md)

### `/loop` while idle

If no Agent session is active when the error lands:

```
/loop 2m @visa2026-runtime-error-tracking check runtime error inbox — list-open and triage newest Open row if any
```
