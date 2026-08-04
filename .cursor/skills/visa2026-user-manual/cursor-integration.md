# User manual — Cursor Cloud Agent (primary) vs git push (verify only)

**Related:** [`USER_MANUAL_PIPELINE.md`](../../../docs/USER_MANUAL_PIPELINE.md) · [SKILL.md](./SKILL.md)

---

## 1. Honest constraint

**Git push alone does not generate officer manual content.**

A push only moves **already committed** Markdown, assets, and code. It cannot:

- Draft or rewrite guide prose from a UI change
- Decide which guides are stale
- Adapt developer `docs/` into officer language ([content-policy.md](./content-policy.md))

That work needs a **Cursor Cloud Agent** (or a human author) using the **visa2026-user-manual** skill.

**Therefore:** treat **manual generation** as **agent-triggered**; treat **git push / PR** as **verify, E2E, and publish** what the agent (or human) committed.

---

## 2. Recommended model (realistic)

```text
TRIGGER (you choose when)
  Cursor Cloud Agent  @visa2026-user-manual
  · issue / Automation / explicit chat / release checklist
        │
        ▼
GENERATE (agent session)
  · Read diff, tracking, content-policy, curriculum
  · Update guides (UI labels only — no code)
  · Run Build-UserManual.ps1 where the environment allows
  · Open PR with user-manual/ + asset changes
        │
        ▼
VERIFY (GitHub Actions on that PR — not on every unrelated push)
  user-manual.yml → Build-UserManual.ps1
    · UserManualDocs unit tests
    · catalog generator
    · link validator
    · UserManual E2E + screenshots   ← E2E still inside pipeline
    · mkdocs build
        │
        ▼
PUBLISH (merge to main)
  · Deploy static site
  · Officer review for status: published
```

| Phase | Who | What |
|-------|-----|------|
| **Generate** | **Cursor Cloud Agent** (primary) | Prose, guide updates, PR |
| **Prove** | **CI** on manual PR | E2E, validators, mkdocs |
| **Publish** | Merge + optional officer sign-off | Live site |

**Git push on unrelated app code** → does **not** auto-run full manual generation. At most: optional **notification** to start an agent (see §4).

---

## 3. What `Build-UserManual.ps1` does in each place

| Where | Role |
|-------|------|
| **Cursor Agent** (cloud/local) | Agent runs or requests run while authoring; refreshes catalog; may `-SkipE2E` locally if no Windows host — **CI must run E2E before merge** |
| **CI (`user-manual.yml`)** | Runs on PRs that touch `user-manual/` or manual tooling — **fail closed** |
| **Developer laptop** | Optional pre-PR check |

E2E stays **inside** `Build-UserManual.ps1` — not a separate “run E2E first” workflow officers depend on. But E2E runs when a **manual PR** is validated, not on every app push.

---

## 4. Optional: push → notify agent (not generate)

If you want a nudge after shipping UI code, a webhook can **start** a Cloud Agent — it does **not** replace the agent.

```text
push to master (Visa2026.Module/** or Blazor officer UI)
        │
        └─► cursor webhook (optional)
              Payload: sha, changed paths, link to compare
              Agent task: "Review whether manual guides need updates; open PR if yes."
              NOT: silently publish manual
```

Same pattern as [cursor-on-issue-opened.yml](../../../.github/workflows/cursor-on-issue-opened.yml). **Secrets:** `CURSOR_WEBHOOK_URL`, `CURSOR_WEBHOOK_AUTH`.

**This is optional.** Many teams will use:

- Release checklist: “Run `@visa2026-user-manual` for this release”
- Cursor Automation on schedule (weekly manual drift review)
- GitHub issue template: “Manual update needed”

---

## 5. Options (if user asks)

| Option | Realistic? | Notes |
|--------|------------|-------|
| **A — Agent-first (recommended)** | **Yes** | You trigger Cloud Agent; agent opens PR; CI proves + publishes |
| **B — Agent + optional push notify** | **Yes** | Push only wakes agent; still no auto-publish |
| **C — Git push runs full generation** | **No** | Push cannot author content; gives false confidence |
| **D — E2E-only on every push** | **Poor fit** | No prose, no site; not manual generation |

**Default for Visa2026:** **A** now; add **B** later if you want reminders after UI merges.

---

## 6. Practical triggers (copy-paste)

| When | Action |
|------|--------|
| Feature branch ready for review | `@visa2026-user-manual` — update guides for {feature}; open PR |
| Release week | Cloud Agent + skill — run curriculum tier checklist; `Build-UserManual.ps1` via CI on PR |
| Officer reports wrong steps | Agent fix guide → PR → CI E2E |
| UI-only merge to main | Optional webhook → agent triage only |

---

## 7. Relationship to `e2e-tests.yml`

| Workflow | When |
|----------|------|
| **`user-manual.yml`** | PR that changes manual artifacts — **UserManual** E2E + mkdocs |
| **`e2e-tests.yml`** | App regression; nightly full suite |

Do not expect every `git push` to run either workflow unless paths match.

---

## 8. Acceptance criteria

- [ ] Manual **content** changes only via Agent PR (or human), not silent push
- [ ] `user-manual.yml` green on manual PR before merge
- [ ] `Build-UserManual.ps1` includes E2E on CI (Windows runner)
- [ ] Push-notify webhook documented as **optional**, not required

---

## 9. Changelog

| Date | Change |
|------|--------|
| 2026-08-04 | Initial — git push as orchestrator |
| 2026-08-04 | **Revised** — Agent-first generation; git push = verify/publish only; push-notify optional |
