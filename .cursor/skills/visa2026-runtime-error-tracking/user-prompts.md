# Runtime error tracking — user prompts

Copy-paste messages to invoke [visa2026-runtime-error-tracking](./SKILL.md) in Cursor. Prefer **`@visa2026-runtime-error-tracking`** (or `@.cursor/skills/visa2026-runtime-error-tracking`) so the agent loads this skill.

**Not this skill:** IIS deploy / recycle / sa login → [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md); GitHub Actions CI → [ci-failed-triage](../ci-failed-triage/SKILL.md); officer **UserFeedback** reports.

**Flow:** app `LogError` → **`ApplicationRuntimeLogs` (SQL)** → local **`.cursor/runtime-errors/inbox/{id}.json`** → Cursor hooks / Agent triage → `mark-fixed`.

**IIS slots on `10.100.128.25`:**

| URL | Pull `-Profile` | Database |
|-----|-----------------|----------|
| `http://10.100.128.25/` | `Production` | `Visa2026DbProd` |
| `http://10.100.128.25:8080/` | `Staging` | `Visa2026DbStaging` |
| `http://10.100.128.25:8081/` | `Demo` | `Visa2026DbDemo` |

**Event Viewer:** manual only — not auto-pulled (see [environments.md](./environments.md)).

---

## Quick start

| You want… | Copy this |
|-----------|-----------|
| **First-time walkthrough** | `@visa2026-runtime-error-tracking Walk me through the full loop: pull from staging, check inbox folder, open Agent on newest row. Explain what I should see at each step.` |
| **Pull all on-prem slots** | `@visa2026-runtime-error-tracking Run Pull-Visa2026RuntimeErrorsRemote.ps1 for Production, Staging, and Demo (last 24h). Summarize queried/written counts and list any new inbox JSON files.` |
| **Fix newest error** | `@visa2026-runtime-error-tracking fix runtime error from inbox (newest open row)` |
| **Healthy check** | `@visa2026-runtime-error-tracking Pull all IIS slots and confirm inbox is empty. If empty, say what that means and how to verify in Operations → Runtime errors on staging :8080.` |
| **Continuous heartbeat** | `/loop 3m @visa2026-runtime-error-tracking pull all IIS slots, then triage newest open inbox row if any` |

---

## Pull remote (IIS → local inbox)

Run from repo root on your **dev PC** (SSH to `visa2026-onprem`):

```powershell
.\scripts\windows-iis\Pull-Visa2026RuntimeErrorsRemote.ps1
.\scripts\windows-iis\Pull-Visa2026RuntimeErrorsRemote.ps1 -Profile Staging -SinceMinutes 180
```

| Goal | User prompt |
|------|-------------|
| **All slots** | `@visa2026-runtime-error-tracking Pull runtime errors from all IIS slots (Production :80, Staging :8080, Demo :8081) into the local inbox. Report written vs skipped.` |
| **Staging only** | `@visa2026-runtime-error-tracking Pull-Visa2026RuntimeErrorsRemote.ps1 -Profile Staging -SinceMinutes 1440. List new files under .cursor/runtime-errors/inbox/.` |
| **Production only** | `@visa2026-runtime-error-tracking Pull production slot errors from 10.100.128.25 (Visa2026DbProd). Do not auto-fix prod — triage and suggest only unless I say deploy.` |
| **Demo only** | `@visa2026-runtime-error-tracking Pull demo slot (:8081) runtime errors for the last 7 days.` |
| **After deploy** | `@visa2026-runtime-error-tracking We just deployed to staging. Pull staging errors (last 2h), smoke :8080/LoginPage, triage any open inbox rows.` |
| **Direct SQL** (LAN SQL open) | `@visa2026-runtime-error-tracking Pull-Visa2026RuntimeErrorsRemote.ps1 -Profile Staging -UseDirectSql -ServerHost 10.100.128.25` |

---

## Local PC (F5 / LocalDB)

Inbox bridge is **automatic** when `ApplicationRuntimeLog:CursorBridgeEnabled` is true (default in dev `appsettings.json`).

| Goal | User prompt |
|------|-------------|
| **After F5 error** | `@visa2026-runtime-error-tracking I reproduced an error locally (F5). Read newest .cursor/runtime-errors/inbox/*.json and triage per reference.md.` |
| **List open (LocalDB)** | `@visa2026-runtime-error-tracking List open ApplicationRuntimeLog rows on LocalDB (tools/RuntimeLogResolution list-open). If CLI fails on EF, read inbox JSON instead.` |
| **Demo local pipeline** | `@visa2026-runtime-error-tracking Guide me to trigger a harmless local LogError (e.g. LocalDB stop → F5 startup), confirm inbox JSON appears, then mark-ignored after triage.` |
| **Fix local only** | `@visa2026-runtime-error-tracking Fix the newest local inbox runtime error (LocalVisualStudio). Build verify before suggesting commit.` |

---

## Agent fix loop

| Goal | User prompt |
|------|-------------|
| **Newest inbox row** | `@visa2026-runtime-error-tracking fix runtime error from inbox (newest open row)` |
| **Specific id** | `@visa2026-runtime-error-tracking fix runtime error <guid> — read inbox JSON, mark-in-progress, triage reference.md, minimal fix, mark-fixed with notes.` |
| **Triage only** | `@visa2026-runtime-error-tracking Triage newest inbox error: ErrorCode, severity, typical cause, repro steps. Do not change code yet.` |
| **Suggest fix (staging)** | `@visa2026-runtime-error-tracking Staging inbox error — root cause and patch suggestion only; no prod deploy.` |
| **Ignore noise** | `@visa2026-runtime-error-tracking Mark inbox error <guid> as ignored with notes if it matches BLAZOR-JS-DISC-001 or other P3 noise in reference.md.` |
| **After you fixed code** | `@visa2026-runtime-error-tracking mark-fixed --id <guid> --notes "summary" for the runtime error we just fixed.` |

Follow [agent-fix-loop.md](./agent-fix-loop.md): inbox → mark-in-progress → reference.md → fix → build → mark-fixed → learnings.md.

---

## Incidents (deploy / site down / 500)

| Goal | User prompt |
|------|-------------|
| **500.30 / site won't start** | `@visa2026-runtime-error-tracking Staging :8080 returns 500.30. Triage: stdout, Get-Visa2026IisStartupError.ps1, Event Viewer (manual). Hand off IIS infra to @visa2026-windows-iis-deploy if sa/port/SQL.` |
| **After redeploy all slots** | `@visa2026-runtime-error-tracking Post-deploy check: pull all slots, curl LoginPage on :80 :8080 :8081, triage any new inbox errors.` |
| **User report, no inbox row** | `@visa2026-runtime-error-tracking User saw error on production but inbox is empty. Check reference.md § UI-only, batch ErrorMessage columns, and whether category is DevExpress (not in SQL).` |
| **PDF batch failed** | `@visa2026-runtime-error-tracking Inbox or logs show PDF-BATCH-001. Correlate batch id, PdfGenerationBatches.ErrorMessage, template path — triage per reference.md.` |
| **Resminamalar batch failed** | `@visa2026-runtime-error-tracking WORD-BATCH-001 on staging — triage WordReportGenerationBatches and report definitions.` |
| **Invalid column name** | `@visa2026-runtime-error-tracking EF-SQL-001 / Invalid column name on IIS. Schema drift — coordinate Run-Visa2026DbUpdateOnServer.ps1 -Profile {slot} with @visa2026-windows-iis-deploy.` |

**Event Viewer (manual — do not auto-pull):**

```powershell
ssh visa2026-onprem "powershell -File C:\visa2026-deploy\iis\Get-Visa2026RecentIisErrors.ps1"
```

| Goal | User prompt |
|------|-------------|
| **Event Viewer triage** | `@visa2026-runtime-error-tracking I pasted Event Viewer .NET Runtime 1000 / JSDisconnectedException from XafErrorBoundaryComponent. Is this BLAZOR-JS-DISC-001 noise or a real bug?` |

---

## Classify unknown errors

| Goal | User prompt |
|------|-------------|
| **Unknown log line** | `@visa2026-runtime-error-tracking Classify this log line: "{paste}". Match reference.md ErrorCode + severity + handled vs unhandled.` |
| **VS Output snippet** | `@visa2026-runtime-error-tracking Classify this Visual Studio Output excerpt from local F5: "{paste}"` |
| **Add catalog row** | `@visa2026-runtime-error-tracking New emitter verified in prod — add a row to reference.md (append-only) with ErrorCode, severity, when/where.` |

---

## Resolution CLI (agent runs these)

```powershell
dotnet run --project tools/RuntimeLogResolution -- list-open --limit 5
dotnet run --project tools/RuntimeLogResolution -- get --id <guid>
dotnet run --project tools/RuntimeLogResolution -- mark-in-progress --id <guid> --by cursor-agent
dotnet run --project tools/RuntimeLogResolution -- mark-fixed --id <guid> --notes "..." --by cursor-agent
dotnet run --project tools/RuntimeLogResolution -- mark-ignored --id <guid> --notes "..."
dotnet run --project tools/RuntimeLogResolution -- pull-remote --connection "Server=..." --since 1h --source-slot Staging --source-database Visa2026DbStaging
```

| Goal | User prompt |
|------|-------------|
| **Sync resolution to SQL** | `@visa2026-runtime-error-tracking mark-fixed for inbox <guid> with notes "{summary}" and archive the inbox JSON.` |

---

## Cursor hooks

Hooks in [`.cursor/hooks.json`](../../hooks.json) inject inbox context on **new Agent chat** (`sessionStart`) and auto-prompt after agent turns (`stop`). They do **not** watch the filesystem continuously — pull or F5 must run first.

| Goal | User prompt |
|------|-------------|
| **Hooks not firing** | `@visa2026-runtime-error-tracking Inbox has {id}.json but Agent did not auto-prompt. Check hook-disabled, hook-prompted.json, and tell me to open a new Agent chat.` |
| **Disable hooks** | `@visa2026-runtime-error-tracking How do I temporarily disable runtime-error Cursor hooks?` |

---

## Guardrails (say explicitly when needed)

| Situation | Add to your prompt |
|-----------|-------------------|
| **Prod — triage only** | `… suggest fix only; do not deploy to production.` |
| **Prod — may fix** | `… you may patch code; I will deploy separately.` |
| **Staging — full loop** | `… fix, build verify, mark-fixed; staging deploy is OK if I confirm.` |
| **Local — full loop** | `… fix locally, dotnet build, mark-fixed.` |

Default: auto-fix **LocalVisualStudio** only; prod/staging inbox → triage unless you opt in.

---

## Fill-in template

```text
@visa2026-runtime-error-tracking [{Pull | Triage | Fix | Classify | Post-deploy}]
Environment: {Local F5 | Production :80 | Staging :8080 | Demo :8081}
Symptom: {inbox file | user report | 500.30 | batch failed | paste log line}
Time window: {last 1h | since deploy | guid <...>}
Action: {pull only | triage only | fix code | mark-fixed | ignore}
Constraints: {no prod deploy | build verify | commit if I ask}
```

---

## Examples

**Pull + triage after staging test:**

```text
@visa2026-runtime-error-tracking Pull staging (:8080) errors for the last 3 hours.
If any inbox JSON was written, triage the newest Open row per reference.md.
Do not deploy fixes to IIS — suggest patch only.
```

**Local F5 session:**

```text
@visa2026-runtime-error-tracking I'm debugging locally. After my repro,
fix runtime error from inbox (newest open row). Build verify before commit.
```

**Post-deploy all slots:**

```text
@visa2026-runtime-error-tracking We deployed 1.0.0.393 to all IIS slots.
Pull all profiles, verify LoginPage 200 on :80 :8080 :8081, triage new errors.
```

**Ignore circuit disconnect noise:**

```text
@visa2026-runtime-error-tracking Event Viewer shows XafErrorBoundaryComponent +
JSDisconnectedException. Confirm BLAZOR-JS-DISC-001 — no code change, no inbox pull.
```

**Continuous monitoring while working:**

```text
/loop 5m @visa2026-runtime-error-tracking Pull-Visa2026RuntimeErrorsRemote.ps1 -Profile All;
if new inbox files exist, triage newest Open row briefly (severity + ErrorCode only).
```
