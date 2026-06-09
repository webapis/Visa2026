---
name: visa2026-runtime-error-tracking
description: >-
  Developer-only: track, triage, and fix Visa2026.Blazor.Server runtime errors (local F5 + IIS slots).
  ApplicationRuntimeLog SQL, pull-remote inbox, Cursor hooks, and autonomous Agent fix loop with guardrails.
  Use for LogError, runtime error inbox, pull from staging/prod/demo, bug fix from inbox, 500.30 triage,
  or when Agent should autonomously diagnose and patch after pull/hooks (not for officers or UserFeedback).
disable-model-invocation: false
---

# Visa2026 runtime error tracking

## Audience

| Who | Role |
|-----|------|
| **Developers** (primary) | Pull errors, triage, fix code, mark resolution — via Cursor Agent + [user-prompts.md](./user-prompts.md) |
| **Cursor Agent** (autonomous helper) | When inbox JSON exists, hooks fire, or `/loop` runs: pull → triage → fix (local) or suggest fix (remote) per [agent-fix-loop.md](./agent-fix-loop.md) |
| **Not this skill** | End users / immigration officers, in-app **UserFeedback**, generic IT without repo access |

In-app **Operations → Runtime errors** and the SignalR badge are for **admins on the server**; this skill is the **developer workstation + Agent** workflow on the git repo.

## Goal

Help developers and Agent **identify**, **classify**, **collect**, and **resolve** errors from `Visa2026.Blazor.Server`: **when**, **where**, **why**, and **how critical**.

### In-scope deployments (current)

| Environment | How it runs | Log triage today |
|-------------|-------------|------------------|
| **Visual Studio local** | F5 / launch profile (`Visa2026 - LocalDB`, etc.) | VS **Output** window |
| **IIS on-prem production** | [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) → `C:\inetpub\visa2026` | Optional **stdout** under `logs\`; SSMS on SQLEXPRESS |

Docker / droplet: use [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) for logs only — **not** the primary tracking target unless user expands scope.

**Environment details:** [environments.md](./environments.md)

**Today:** errors persist to `ApplicationRuntimeLog` (SQL), push to admins via SignalR badge/toast, and (dev) write Cursor agent inbox JSON — [docs/RUNTIME_ERROR_TRACKING_PLAN.md](../../../docs/RUNTIME_ERROR_TRACKING_PLAN.md). Stdout/Output remain **fallback**.  
**Agent loop:** [agent-fix-loop.md](./agent-fix-loop.md) + `tools/RuntimeLogResolution` (`pull-remote`) + `scripts/windows-iis/Pull-Visa2026RuntimeErrorsRemote.ps1` + project hooks (`.cursor/hooks.json`).  
**Full emitter catalog:** [reference.md](./reference.md).

**Copy-paste prompts:** [user-prompts.md](./user-prompts.md)

**Related skills:** [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) (IIS prod deploy/recycle), [ci-failed-triage](../ci-failed-triage/SKILL.md) (CI only).

**Not the same as:** `UserFeedback` (officer-reported bugs), XAF Audit Trail (data changes), State notifications (business validity).

### Log channels (policy)

| Channel | Agent / automated pull? | When to use |
|---------|-------------------------|-------------|
| **`ApplicationRuntimeLog` (SQL)** | **Yes** — `Pull-Visa2026RuntimeErrorsRemote.ps1`, `pull-remote` | Primary heartbeat; user/runtime errors during normal operation |
| **SignalR badge / Operations ListView** | In-app (admins) | Real-time on server |
| **stdout** (`logs\stdout_*`) | No (manual / SSH tail) | Deploy, startup, when site returns 500.30 |
| **Windows Event Viewer (Application log)** | **No — manual only** | Deploy/crash triage when SQL logging never started; **not** Cursor inbox |

Event Viewer includes noisy handled Blazor disconnects (`JSDisconnectedException` via `XafErrorBoundaryComponent`) that are **not** production defects — do not bridge to Agent. Manual command: `Get-Visa2026RecentIisErrors.ps1` on the server (see [environments.md](./environments.md)).

### Chat openers

- `@visa2026-runtime-error-tracking` — pull IIS errors, triage inbox, or fix newest runtime error ([user-prompts.md](./user-prompts.md)).
- **Pull staging/prod/demo** / **fix runtime error from inbox** / **500.30 after deploy** / **classify this log line**.

### Autonomous Agent (when skill is active)

Agent may run the fix loop **without** the developer pasting a full prompt when:

1. **`.cursor/hooks.json` `stop` hook** — new unprompted inbox `{id}.json` after a turn → triage + fix per [agent-fix-loop.md](./agent-fix-loop.md).
2. **`sessionStart` hook** — pending inbox items → summarize and offer to fix newest Open row.
3. **`/loop`** — developer started periodic pull + triage (see [user-prompts.md](./user-prompts.md)).
4. **Developer says** “fix from inbox”, “autonomous fix”, or `@visa2026-runtime-error-tracking fix …`.

**Autonomous defaults:**

| Inbox source | Agent may patch code? | Agent may deploy/commit? |
|--------------|----------------------|---------------------------|
| **LocalVisualStudio** (F5) | **Yes** — minimal fix + `dotnet build` | Commit/push **only if developer asked** |
| **Staging / Demo** (pulled) | **Suggest** fix; patch if developer opted in | **Never** deploy without explicit ask |
| **Production** (pulled) | **Triage + suggest only** unless developer explicitly allows prod fix | **Never** deploy without explicit ask |

Skip noise: `BLAZOR-JS-DISC-001`, `E2E-JS-DISC-001`, already `Fixed`/`Ignored` rows. P0 DB down → ops steps, not blind schema patches.

---

## Quick triage (incident)

Copy checklist — pick **VS local** or **IIS prod** branch:

```
- [ ] Symptom: user report / toast / silent failure / site 500 / worker crash?
- [ ] Environment: Visual Studio local  OR  IIS on-prem prod
- [ ] Time window (UTC)

Visual Studio local:
- [ ] VS Output window (Visa2026.Blazor.Server) — filter fail: | Error | batch failed
- [ ] Launch profile + DB: launchSettings.json (LocalDB vs Docker SQL)
- [ ] Optional: dotnet run with same profile from repo root

IIS on-prem prod:
- [ ] HTTP smoke: /LoginPage (200?)
- [ ] Pull-Visa2026RuntimeErrorsRemote.ps1 (SQL → local inbox) OR Operations → Runtime errors
- [ ] Get-Visa2026IisStartupError.ps1 or tail slot publish logs\stdout_* (deploy/startup only)
- [ ] Event Viewer Application log — **manual only** if 500.30/crash: Get-Visa2026RecentIisErrors.ps1
- [ ] If 502.5/500.30/sa login — visa2026-windows-iis-deploy §6 first (infra vs app)
- [ ] SSMS → localhost\SQLEXPRESS → batch ErrorMessage columns

Both:
- [ ] Match message to reference.md → ErrorCode + severity + typical cause
- [ ] If UI-only symptom: likely NOT in stdout — reference.md § UI-only
- [ ] Append verified fix to learnings.md (note VS vs IIS)
```

Log grep patterns: [BLAZOR_SERVER_LOGGING.md § grep](../../../docs/BLAZOR_SERVER_LOGGING.md).

---

## Severity ladder (use in responses)

| Level | Meaning | Ops action |
|-------|---------|------------|
| **P0 Critical** | App/worker cannot function; data loss risk | Immediate — restart, DB, rollback |
| **P1 Error** | Feature/job failed; user blocked for that action | Same day — fix or workaround |
| **P2 Warning** | Degraded/partial success; retries | Monitor; fix if recurring |
| **P3 UI-only** | User saw error; server log silent | Reproduce; add reporter if recurring |

---

## Recommended implementation (default)

When the user asks to **implement** central error storage, follow [RUNTIME_ERROR_TRACKING_PLAN.md](../../../docs/RUNTIME_ERROR_TRACKING_PLAN.md) — do **not** invent a parallel store.

**Default stack (in-repo):**

1. **`ApplicationRuntimeLog` BO** (Module) — dedicated SQL table, XAF ListView for admins
2. **Custom `ILoggerProvider`** — captures existing `LogError`/`LogCritical` (and optional `LogWarning`) without rewriting every call site
3. **`CorrelationIdMiddleware`** — ties request → log rows
4. **Queued writer** — never block the logging thread on SQL insert
5. **SignalR notifier** — after persist, push to admin badge/toast (reuse batch-toast / state-badge patterns)
6. **Optional later:** Sentry for cross-host grouping (parallel to SQL, not replacement)

**Avoid as primary store:** parsing stdout/IIS log files alone, Audit Trail, or only `UserFeedback`. Stdout/Output remain **fallback** until `ApplicationRuntimeLog` exists; on **IIS prod** the SQL table is the main ops view.

**Shortcut if user wants SQL fast without XAF BO:** `Serilog.Sinks.MSSqlServer` — document trade-off (no in-app inbox/security); prefer BO if user asked for “dedicated database table” in the **app** database.

---

## Implementation workflow (agent)

### Phase 1 — Persist

1. Read [RUNTIME_ERROR_TRACKING_PLAN.md](../../../docs/RUNTIME_ERROR_TRACKING_PLAN.md) §4 Phase 1
2. Add BO + updater + provider + middleware in Module/host per plan §5
3. Register provider in `Startup.cs` / `Program.cs`
4. Add Operations nav + admin permissions in `Updater.cs`
5. `dotnet build Visa2026.slnx -c Debug`
6. Manual test **VS local:** F5 → trigger `LogError` → row in LocalDB  
7. Manual test **IIS prod:** reproduce on server → row in SQLEXPRESS / `Visa2026DbProd`

### Phase 2 — Real-time ✅

1. Hub + notifier after successful insert (`SignalRApplicationRuntimeLogNotifier`, `ApplicationRuntimeLogHub`)
2. Admin-only Blazor badge/toast (`RuntimeErrorAlertHost.razor`)
3. Test: F5 as Admin → trigger `LogError` → badge count + toast; click → Operations → Runtime errors

### Phase 3 — Coverage

1. `IApplicationErrorReporter` for UI-only paths in reference.md § UI-only
2. Assign `ErrorCode` constants at top emitters

### Phase 5 — Cursor agent loop ✅

1. **5a** — `ApplicationRuntimeLogResolutionStatus` + resolution fields on BO; `IApplicationRuntimeLogResolution`; admin ListView actions (Mark in progress / fixed / ignored)
2. **5b** — `FileCursorRuntimeErrorInboxWriter` → `.cursor/runtime-errors/inbox/{id}.json` when `CursorBridgeEnabled`
3. **5c** — `tools/RuntimeLogResolution` CLI + [agent-fix-loop.md](./agent-fix-loop.md)

**Invoke agent fix:**

```
@visa2026-runtime-error-tracking fix runtime error from inbox (newest open row)
```

Or read `.cursor/runtime-errors/inbox/*.json` and follow [agent-fix-loop.md](./agent-fix-loop.md).

---

## Classify an unknown log line

1. Open [reference.md](./reference.md) — search message substring or service name
2. If no match: identify logger **category** (prefix before colon in log line)
3. Read source `LogError`/`LogWarning` call — determine **handled vs unhandled**
4. Add new row to reference.md if verified new emitter (append-only style)

---

## Key emitters (summary)

| Area | P0/P1 examples | Reference section |
|------|----------------|-------------------|
| Startup / SQL | DB down, seed failed | § Infrastructure |
| PDF batch worker | `PDF batch failed` | § Background workers |
| Word batch worker | `Resminamalar batch failed` | § Background workers |
| PDF pipeline | template missing, fill error | § Document pipelines |
| Framework | `fail:` unhandled | § Framework |
| UI | validation, preview failed | § UI-only |

---

## Configuration keys (planned)

`ApplicationRuntimeLog:Enabled`, `MinLevel`, `PersistWarnings`, `RetentionDays`, `RealtimeNotifyMinLevel` — see plan doc §6.

---

## learnings.md

After a **verified** production/dev incident resolution, append one entry to [learnings.md](./learnings.md): symptom, ErrorCode, root cause, fix, environment.

---

## Maturity

Promote repeated triage steps from learnings → this SKILL.md or reference.md after **2+** hits (see [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)).
