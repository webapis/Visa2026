---
name: visa2026-user-manual
description: >-
  Sole Agent skill for Visa2026 officer user manual: create, update, plan, fix, track.
  View locally: run scripts/local/Serve-UserManual.ps1 only — reuse user-manual/.tools/ Python;
  do not pip install or download Python unless that script fails. Deploy: docs/USER_MANUAL_RELEASE.md
  (static /manual/ + /manual-media/, not inside Blazor). Code change detection: code-drift-scan.md.
  MkDocs, catalog generator, CI. Officer site: UI language only (content-policy.md).
  Read advisory.md + curriculum.md first. Interlocked with visa2026-easytest-e2e.
---

# Visa2026 — User manual (documentation generation)

**This skill owns the full documentation-generation lifecycle:** **create** · **update** · **plan** · **fix** · **track** · **scan** (code change detection).

**Code change detection:** whenever this skill must determine whether app code affects officer guides, run **[code-drift-scan.md](./code-drift-scan.md)** — do not improvise ad-hoc diffs or edit guides without a scan report and approval (except when the user already scoped exact files/slugs).

---

## Unified pipeline (mandatory for publish)

**Canonical:** [`docs/USER_MANUAL_PIPELINE.md`](../../../docs/USER_MANUAL_PIPELINE.md)

Documentation generation **orchestrates** Playwright E2E — officers get fresh **doc-anchored screenshots** from the same run that proves guides still work. **Video is deferred** (D21 — screenshots-only).

```powershell
./scripts/ci/Build-UserManual.ps1   # EasyTest → assets → catalog → validate → mkdocs
```

| Rule | Detail |
|------|--------|
| **No separate media run** | Do not tell users to run `dotnet test` and manually copy PNGs for publish |
| **Three test layers** | `UserManualDocs` (xUnit, fast) → validator scripts → `UserManual` (E2E + media) — all inside `Build-UserManual.ps1` |
| **Fail closed** | Publish blocked if UserManual E2E or required screenshots fail |
| **Manifest** | `manual-generation-manifest.yaml` lists slugs + `testFilter` + expected PNGs |
| **Trait** | E2E tests: `[Trait("Category", "UserManual")]` + `[Trait("GuideSlug", "...")]` |

### Doc-anchored media (mandatory for published guides)

**Canonical:** [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) § Doc-anchored capture.

The **guide section** owns what each image/video must show. E2E proves that state at capture time.

| Step | Owner | Action |
|------|-------|--------|
| 1 | user-manual | Write step prose; add `<!-- media-capture: {key} -->` above each screenshot |
| 2 | user-manual | Register key in `user-manual/media-capture-registry.yaml` (`guideSlugs`, `description`, `assertBeforeCapture`) |
| 3 | easytest-e2e | Add key to `UserManualMediaCaptureKeys.cs`; `CaptureAsync` after assertions |
| 4 | pipeline | `Copy-EasyTestManualScreenshots.ps1` — 1:1 copy; **no new legacy fan-out** |
| 5 | CI | `Validate-UserManualMediaCaptures.ps1` — anchor, basename match, `guideSlugs` contains guide `slug` |

**Pinpoint (action shots):** registry `pinpoint:` + `CaptureAsync(page, key, locator)` — orange highlight burned into PNG (`UserManualScreenshotPinpoint`). Skip overview frames. Opt out: `VISA2026_E2E_PINPOINTS=false`.

**Rules:** capture key = PNG stem = E2E label. Shared keys across guides are allowed when the same UI state is correct. **No video** in published guides (D21). Optional video infra remains in repo (`-EnableVideo` on `Record-PlaywrightE2e.ps1`) — not used for officer manual publish.

**Pilots 1–5** are doc-anchored. Other guides still on **legacy fan-out** until migrated — see tracking **Media** column.

Local prose-only edit: `-SkipE2E` allowed with warning — **never** on CI/main publish.

**Triggers:** **Cursor Cloud Agent** runs manual generation (primary). Git push / PR only **verifies** committed manual artifacts via CI — see [cursor-integration.md](./cursor-integration.md).

---

## Officer content (no code)

**Canonical:** [content-policy.md](./content-policy.md)

Published pages under `user-manual/docs/` use **UI language only** — menus, field labels, screenshots. No C#, SQL, BO/property names, repo paths, links to developer `docs/`, or **video embeds** (screenshots-only, D21). **Testing:** green tick only when verified — full results are separate ([testing-evidence.md](./testing-evidence.md)).

---

## Before implementing anything (mandatory)

**Read [advisory.md](./advisory.md)** — when to document, phase readiness, options to offer the user, quality gates, anti-patterns.

```text
1. Read learnings.md + tracking.md + advisory.md + curriculum.md
2. Classify intent → right audience? (officer vs developer)
3. Code change detection needed? → [code-drift-scan.md](./code-drift-scan.md) (report only; no guide edits yet)
4. Check phase + **curriculum tier** — next guide should follow tier order unless user skips
5. Check phase readiness (advisory §4) — don't skip infra if guides need CI
6. Offer 2–4 paths from advisory §5 — recommend a default; wait for user unless they said "go ahead"
7. Then run the checklist below (Create / Update / Fix / …)
```

### When this skill requires code change detection

Use **[code-drift-scan.md](./code-drift-scan.md)** (mandatory — not optional) if **any** of:

- Updating guides after a UI, BO, nav, or workflow change
- User asks whether the manual is stale or needs updates
- Feature just merged; release or drift review
- Regenerating catalog and comparing impact on published/draft guides
- Create/Update task references changed Module, Blazor, or XAF model paths

**Skip scan** only when the user names **exact** slugs/files to edit with no discovery needed (e.g. "fix typo in `person/register` step 3").

| User says… | Start with |
|------------|------------|
| "Start the manual" / "Phase 0" | Advisory §5 **B** (infra options) |
| "Document feature X" | **Scan** (if code shipped) → advisory §5 **A** |
| "Update docs after UI change" | **[code-drift-scan.md](./code-drift-scan.md)** → advisory §5 **C** after approval |
| "Does the manual need updates?" / merged PR | **[code-drift-scan.md](./code-drift-scan.md)** — report only |
| "What's the status?" | **Track** only — no scan unless drift suspected |
| "Just implement" | **Scan** if code context unknown; else state path and proceed |
| "How do I see / view the manual?" / "preview docs" | **Preview locally** (§ below) — **do not** bootstrap Python/pip yourself |
| "How is the manual shipped with the app?" | **Deploy alongside Blazor** (§ below) — [`USER_MANUAL_RELEASE.md`](../../../docs/USER_MANUAL_RELEASE.md) |

---

## Doc map

| Doc | Role |
|-----|------|
| **[testing-evidence.md](./testing-evidence.md)** | **Green tick** on manual; full results separate |
| **[localization.md](./localization.md)** | **en · tr · tk · ru** — structure, rollout, screenshots |
| **[content-policy.md](./content-policy.md)** | **Officer site: no code** — UI labels only |
| **[code-drift-scan.md](./code-drift-scan.md)** | **Mandatory code change detection** for this skill — report first, update after approval |
| **[advisory.md](./advisory.md)** | **When / how / options before code** |
| **[curriculum.md](./curriculum.md)** | **Publish order:** CRUD on BOs → template generation |
| **[decisions.md](./decisions.md)** | **Pre-implementation checklist** — decide before coding |
| [`docs/USER_MANUAL_STATUS.md`](../../../docs/USER_MANUAL_STATUS.md) | **Roadmap · changelog · next inline** |
| [`docs/USER_MANUAL_IMPLEMENTATION_PLAN.md`](../../../docs/USER_MANUAL_IMPLEMENTATION_PLAN.md) | Architecture and phased rollout |
| [`docs/USER_MANUAL_ROADMAP.md`](../../../docs/USER_MANUAL_ROADMAP.md) | Timeline, milestones, E2E interlock |
| [`docs/USER_MANUAL_PIPELINE.md`](../../../docs/USER_MANUAL_PIPELINE.md) | **Unified build** — E2E embedded in doc generation |
| [`docs/USER_MANUAL_RELEASE.md`](../../../docs/USER_MANUAL_RELEASE.md) | **Deploy** — static site + media **beside** Blazor (Docker nginx / IIS); not in app image |
| [`scripts/local/Serve-UserManual.ps1`](../../../scripts/local/Serve-UserManual.ps1) | **Local preview** — one script, reuses `user-manual/.tools/`, MkDocs hot reload |
| [cursor-integration.md](./cursor-integration.md) | **Git push** — GHA manual pipeline + optional Cursor agent |
| [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) | Screenshots & video contract |
| [`user-manual/media-capture-registry.yaml`](../../../user-manual/media-capture-registry.yaml) | Doc-anchored capture keys + `guideSlugs` |
| [learnings.md](./learnings.md) | Append-only experience |
| [tracking.md](./tracking.md) | Guide inventory, infra, doc debt |
| [reference.md](./reference.md) | Commands, frontmatter, CI rules, **gitignore contract** |
| [prompts.md](./prompts.md) | Copy-paste prompts |
| [MATURITY.md](./MATURITY.md) | Promotion ladder |

---

## Agent workflow (after advisory)

1. **Read** [learnings.md](./learnings.md), [`USER_MANUAL_STATUS.md`](../../../docs/USER_MANUAL_STATUS.md), [tracking.md](./tracking.md), [advisory.md](./advisory.md), [content-policy.md](./content-policy.md).
2. **Advise** — phase, audience, options, recommended path ([advisory.md](./advisory.md) §5–§8).
3. **Classify** activity (table below) and open the matching checklist.
4. **Code change detection** — if required (see § Before implementing), run [code-drift-scan.md](./code-drift-scan.md) before Update/Create that depends on app diffs.
5. **Regenerate catalog** when BOs, `[UserDocumentation]`, or XAF nav changed ([reference.md](./reference.md)) — part of scan, not a substitute for it.
6. **Never invent** field labels, menu paths, or roles — use `bo-catalog.json` or E2E map §3.
7. **Validate** before finishing (`Build-UserManual.ps1` or [reference.md](./reference.md)).
8. **Track** — [tracking.md](./tracking.md) + append [learnings.md](./learnings.md).

---

## When to document (summary)

Full tables: [advisory.md §2](./advisory.md#2-when-to-create-or-update-documentation). **Publish order:** [curriculum.md](./curriculum.md) (tier 0 → 7).

| Tier | Topic | When |
|------|--------|------|
| 0–1 | Login, navigate, **read** BOs | First manuals after infra |
| 2–3 | **Create / update** Person, Passport, children | Before application guides |
| 4 | Application workflow | After person CRUD pilots |
| 5 | Document copies, Resminamalar | After tier 4 |
| 6 | Dossier, dashboard | After tier 4–5 |
| 7 | **Template generation** (admin) | **Last** — after Resminamalar |

---

## Activity routing

| You are… | Do this | Update |
|----------|---------|--------|
| **Advising** | Options menu, phase check, no code until path chosen | tracking if planning only |
| **Creating** | Guide, scaffold, generator, CI, BO attribute | guide → `draft`; phase checklist |
| **Updating** | **Scan** if code changed → then refresh steps/screenshots | `lastReviewed`, `stale` → `review` |
| **Planning** | Backlog, IA, open decisions; **scan** for coverage gaps | tracking + roadmap alignment |
| **Fixing** | CI, links, mkdocs; **scan** if drift caused the break | doc debt → resolved |
| **Tracking** | Status report | [USER_MANUAL_STATUS.md](../../../docs/USER_MANUAL_STATUS.md) + tracking only |
| **Scanning** | **Code change detection** for this skill — [code-drift-scan.md](./code-drift-scan.md); no guide edits until approved | report → approval → **Update** |
| **Previewing** | Run **`Serve-UserManual.ps1`** only — see § View the manual locally | none |

---

## View the manual locally (default answer)

When the user asks to **see**, **view**, **open**, or **preview** the officer manual — answer with the repo script. **Do not** install Python, run `pip install`, download embeddable Python, or run bare `mkdocs serve` unless the user explicitly asked to debug the script after it failed.

**Canonical command** (from repo root):

```powershell
./scripts/local/Serve-UserManual.ps1
```

| Detail | Value |
|--------|--------|
| **URL** | **http://127.0.0.1:8765/manual/** (locales: `/tr/`, `/tk/`, `/ru/`) |
| **Hot reload** | Edit `user-manual/docs/` — `mkdocs serve` reloads the browser (avoid `--dirtyreload`; it breaks sidebar titles) |
| **Reuse** | Script reuses **system Python** when available, else portable **`user-manual/.tools/python312/`** (created once; **do not delete or re-download** on every ask) |
| **MkDocs deps** | Installed into that same Python via `pip install -r user-manual/requirements.txt` inside the script — **not** a separate manual step |
| **Build** | Runs `Build-UserManual.ps1 -SkipE2E` unless `-SkipBuild` |

**Faster reopen** (site already built under `user-manual/site/`):

```powershell
./scripts/local/Serve-UserManual.ps1 -SkipBuild
```

**Prose-only iteration** (no generator/tests): same script — it already skips E2E locally. For screenshot work, run full `Build-UserManual.ps1` or E2E separately per [reference.md](./reference.md).

**Separate from Blazor:** the manual dev server is **not** `dotnet run` / F5. Officers document the app; previewing docs does **not** require the Visa2026 Blazor host unless capturing new screenshots.

**Agent anti-patterns (avoid):**

- Running `pip install -r user-manual/requirements.txt` before checking whether `Serve-UserManual.ps1` was run
- Deleting `user-manual/.tools/` to “fix” preview
- Bootstrapping a new venv or Python install when `.tools/python312/python.exe` already exists
- Telling the user to use `mkdocs serve` on port 8000 (outdated — script uses **8765** and `/manual/` base path)

Full flags and remote media mode: [reference.md § Local preview](./reference.md#local-preview).

---

## Deploy alongside Blazor (not embedded)

The officer manual is **static HTML + static media**, served **next to** the Blazor app — **not** baked into `Visa2026.Blazor.Server` or the Docker app image.

| Surface | Manual URL | Physical / volume |
|---------|------------|-------------------|
| **Local dev preview** | `http://127.0.0.1:8765/manual/` | MkDocs serve (above) |
| **Docker compose** (`manual` profile) | `http://<host>:8082/manual/` | `MANUAL_SITE_ROOT` + `MANUAL_MEDIA_ROOT` → nginx |
| **IIS on-prem** | e.g. `https://10.100.128.25:8082/manual/` | `C:\visa2026\manual\site` + `\media` |

**Canonical release runbook:** [`docs/USER_MANUAL_RELEASE.md`](../../../docs/USER_MANUAL_RELEASE.md) — `Publish-ManualRelease.ps1`, `MANUAL_MEDIA_BASE_URL`, promote media/site trees staging → prod.

**CI / GitHub Pages:** `user-manual.yml` publishes committed/built site artifacts — separate from app deploy.

---

## Create

See [advisory.md §5A–B](./advisory.md#5-options-menu-offer-the-user) for path options before coding.

| Artifact | Location |
|----------|----------|
| How-to guide | `user-manual/docs/guides/<slug>.md` |
| BO doc link | `[UserDocumentation("slug")]` |
| Screenshots | `user-manual/assets/screenshots/v<version>/{en\|tr\|tk\|ru}/` |

**Create checklist**

1. Confirm phase + officer reviewer ([advisory.md](./advisory.md))
2. Frontmatter: `slug`, `bo`, `status: draft`, `sourceDocs`, `e2eScenarioId` if E2E exists
3. Draft from catalog display names + E2E map §3 — **no code in body** ([content-policy.md](./content-policy.md))
4. For each screenshot: `<!-- media-capture: {key} -->` + registry row (`guideSlugs`, `description`, `assertBeforeCapture`) — see [§ Doc-anchored media](#doc-anchored-media-mandatory-for-published-guides)
5. Validator green (Phase 1+)
6. `tracking.md` inventory row (`Media: doc-anchored` when keys are wired)
7. `learnings.md`

---

## Update

Triggers: [advisory.md §2](./advisory.md#2-when-to-create-or-update-documentation). Offer **C1–C4** before editing.

**Prerequisite:** Drift scan approved items, or user explicitly scoped the edit — see [code-drift-scan.md](./code-drift-scan.md).

1. Diff `bo-catalog.json` vs guide
2. Edit body; for UI changes add/update `media-capture` anchors + registry rows; bump `screenshotsVersion` if UI changed
3. `status: review` until officer sign-off
4. `tracking.md` + `learnings.md`

---

## Scan (code change detection)

**Canonical:** [code-drift-scan.md](./code-drift-scan.md)

**This is the skill's code change detection procedure** — use it whenever officer-visible app changes must be compared to guides. Not a separate workflow; **Updating** and **Planning** call into it when discovery is needed.

1. Diff officer-visible paths (BOs, controllers, Blazor editors, XAF model)
2. Regen or diff `bo-catalog.json`; map to guide slugs + `tracking.md` inventory
3. Emit scan report (template in code-drift-scan.md); add doc-debt rows
4. Offer C1–C4 / A2 options — **wait for user approval**
5. Only then switch to **Update** checklist; max `status: review`

---

## Plan · Fix · Track

Unchanged detail: see prior sections in git history or [reference.md](./reference.md). **Plan** uses ROADMAP + tracking. **Fix** logs doc debt. **Track** uses status template:

```text
Phase: <0–5> | Guides: <published>/<backlog> | Stale: <n> | Debt: <n> | CI: <green|red>
Next: <1–3 items> | Recommended path: <advisory option id>
```

---

## Architecture — two layers

| Layer | Location | Sync |
|-------|----------|------|
| **A — Catalog** | `bo-catalog.json` | CI / generator |
| **B — Guides** | `user-manual/docs/guides/` | `bo:` / `slug:` + officer review |

Layer B must not invent names — reference Layer A and E2E maps.

---

## Scope · phases · related skills

| In scope | Out of scope |
|----------|----------------|
| Advisory + full doc lifecycle | Rewriting all `docs/` for officers |
| E2E media interlock | Video in officer guides (D21 — screenshots-only) |
| **Four locales** en/tr/tk/ru | Single language only |
| **Green tick verification** | Raw test logs on officer pages |
| Officer UI prose, screenshots | **Code, APIs, BO names, dev `docs/` on the site** |

**Phases 0–5:** [ROADMAP](../../../docs/USER_MANUAL_ROADMAP.md). **E2E producer:** [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md). **Adapt sources:** PERSON_DOSSIER, APPLICATION_ITEM_DOCUMENT_COPIES, REPORT_DASHBOARD, etc. (table in [reference.md](./reference.md) or implementation plan).

---

## Local verify (maintainers)

**Officers / “show me the manual”:** use [§ View the manual locally](#view-the-manual-locally-default-answer) — `Serve-UserManual.ps1` only.

**Full publish gate** (CI parity, includes UserManual E2E when implemented):

```powershell
dotnet build Visa2026.slnx -c Debug
./scripts/ci/Build-UserManual.ps1
```

**Preview after a full build** without re-running generator:

```powershell
./scripts/local/Serve-UserManual.ps1 -SkipBuild
```
