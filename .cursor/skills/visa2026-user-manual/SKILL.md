---
name: visa2026-user-manual
description: >-
  Sole Agent skill for Visa2026 officer user manual documentation generation:
  create, update, plan, fix, and track the MkDocs site (user-manual/), BO catalog
  generator, guides, screenshots, video embeds, CI validation, and publish pipeline.
  Officer site content: UI language only — no code (content-policy.md). Status hub:
  docs/USER_MANUAL_STATUS.md. Before implementing anything, read advisory.md and
  curriculum.md: publish order is CRUD on BusinessObjects (read/create/update)
  through workflows to template generation last. Unified pipeline: Build-UserManual.ps1
  runs EasyTest for screenshots/video (not a separate runner). Locales en/tr/tk/ru
  (localization.md). Interlocked with visa2026-easytest-e2e.
---

# Visa2026 — User manual (documentation generation)

**This skill owns the full documentation-generation lifecycle:** **create** · **update** · **plan** · **fix** · **track**.

---

## Unified pipeline (mandatory for publish)

**Canonical:** [`docs/USER_MANUAL_PIPELINE.md`](../../../docs/USER_MANUAL_PIPELINE.md)

Documentation generation **orchestrates** EasyTest — officers get fresh screenshots/video from the **same run** that proves guides still work.

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

Local prose-only edit: `-SkipE2E` allowed with warning — **never** on CI/main publish.

**Triggers:** **Cursor Cloud Agent** runs manual generation (primary). Git push / PR only **verifies** committed manual artifacts via CI — see [cursor-integration.md](./cursor-integration.md).

---

## Officer content (no code)

**Canonical:** [content-policy.md](./content-policy.md)

Published pages under `user-manual/docs/` use **UI language only** — menus, field labels, screenshots, video. No C#, SQL, BO/property names, repo paths, or links to developer `docs/`. **Testing:** green tick only when verified — full results are separate ([testing-evidence.md](./testing-evidence.md)).

---

## Before implementing anything (mandatory)

**Read [advisory.md](./advisory.md)** — when to document, phase readiness, options to offer the user, quality gates, anti-patterns.

```text
1. Read learnings.md + tracking.md + advisory.md + curriculum.md
2. Classify intent → right audience? (officer vs developer)
3. Check phase + **curriculum tier** — next guide should follow tier order unless user skips
4. Check phase readiness (advisory §4) — don't skip infra if guides need CI
5. Offer 2–4 paths from advisory §5 — recommend a default; wait for user unless they said "go ahead"
6. Then run the checklist below (Create / Update / Fix / …)
```

| User says… | Start with |
|------------|------------|
| "Start the manual" / "Phase 0" | Advisory §5 **B** (infra options) |
| "Document feature X" | Advisory §5 **A** + scenario path §6 |
| "Update docs after UI change" | Advisory §5 **C** |
| "What's the status?" | **Track** only — no implementation |
| "Just implement" | State recommended path, then proceed |

---

## Doc map

| Doc | Role |
|-----|------|
| **[testing-evidence.md](./testing-evidence.md)** | **Green tick** on manual; full results separate |
| **[localization.md](./localization.md)** | **en · tr · tk · ru** — structure, rollout, screenshots |
| **[content-policy.md](./content-policy.md)** | **Officer site: no code** — UI labels only |
| **[advisory.md](./advisory.md)** | **When / how / options before code** |
| **[curriculum.md](./curriculum.md)** | **Publish order:** CRUD on BOs → template generation |
| **[decisions.md](./decisions.md)** | **Pre-implementation checklist** — decide before coding |
| [`docs/USER_MANUAL_STATUS.md`](../../../docs/USER_MANUAL_STATUS.md) | **Roadmap · changelog · next inline** |
| [`docs/USER_MANUAL_IMPLEMENTATION_PLAN.md`](../../../docs/USER_MANUAL_IMPLEMENTATION_PLAN.md) | Architecture and phased rollout |
| [`docs/USER_MANUAL_ROADMAP.md`](../../../docs/USER_MANUAL_ROADMAP.md) | Timeline, milestones, E2E interlock |
| [`docs/USER_MANUAL_PIPELINE.md`](../../../docs/USER_MANUAL_PIPELINE.md) | **Unified build** — E2E embedded in doc generation |
| [cursor-integration.md](./cursor-integration.md) | **Git push** — GHA manual pipeline + optional Cursor agent |
| [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) | Screenshots & video contract |
| [learnings.md](./learnings.md) | Append-only experience |
| [tracking.md](./tracking.md) | Guide inventory, infra, doc debt |
| [reference.md](./reference.md) | Commands, frontmatter, CI rules |
| [prompts.md](./prompts.md) | Copy-paste prompts |
| [MATURITY.md](./MATURITY.md) | Promotion ladder |

---

## Agent workflow (after advisory)

1. **Read** [learnings.md](./learnings.md), [`USER_MANUAL_STATUS.md`](../../../docs/USER_MANUAL_STATUS.md), [tracking.md](./tracking.md), [advisory.md](./advisory.md), [content-policy.md](./content-policy.md).
2. **Advise** — phase, audience, options, recommended path ([advisory.md](./advisory.md) §5–§8).
3. **Classify** activity (table below) and open the matching checklist.
4. **Regenerate catalog** when BOs, `[UserDocumentation]`, or XAF nav changed ([reference.md](./reference.md)).
5. **Never invent** field labels, menu paths, or roles — use `bo-catalog.json` or E2E map §3.
6. **Validate** before finishing (`Build-UserManual.ps1` or [reference.md](./reference.md)).
7. **Track** — [tracking.md](./tracking.md) + append [learnings.md](./learnings.md).

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
| **Updating** | Refresh steps, screenshots, locale | `lastReviewed`, `stale` → `review` |
| **Planning** | Backlog, IA, open decisions | tracking + roadmap alignment |
| **Fixing** | CI, links, drift, mkdocs | doc debt → resolved |
| **Tracking** | Status report | [USER_MANUAL_STATUS.md](../../../docs/USER_MANUAL_STATUS.md) + tracking only |

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
4. Validator green (Phase 1+)
5. `tracking.md` inventory row
6. `learnings.md`

---

## Update

Triggers: [advisory.md §2](./advisory.md#2-when-to-create-or-update-documentation). Offer **C1–C4** before editing.

1. Diff `bo-catalog.json` vs guide
2. Edit body; bump `screenshotsVersion` if UI changed
3. `status: review` until officer sign-off
4. `tracking.md` + `learnings.md`

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
| E2E media interlock | Video in git; storage backend TBD Phase 3 |
| **Four locales** en/tr/tk/ru | Single language only |
| **Green tick verification** | Raw test logs on officer pages |
| Officer UI prose, screenshots, video | **Code, APIs, BO names, dev `docs/` on the site** |

**Phases 0–5:** [ROADMAP](../../../docs/USER_MANUAL_ROADMAP.md). **E2E producer:** [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md). **Adapt sources:** PERSON_DOSSIER, APPLICATION_ITEM_DOCUMENT_COPIES, REPORT_DASHBOARD, etc. (table in [reference.md](./reference.md) or implementation plan).

---

## Local verify

```powershell
dotnet build Visa2026.slnx -c Debug
./scripts/ci/Build-UserManual.ps1    # Phase 1+ — includes EasyTest UserManual subset
pip install -r user-manual/requirements.txt
mkdocs serve -f user-manual/mkdocs.yml   # after Build-UserManual for fresh assets
```
