# User Manual — status, roadmap & next steps

Status: **Planning complete — Phase 0 not started**  
Last updated: 2026-08-04  
Owner: Product + Visa officers + Tech lead

**Read this first** for where the manual program stands, what changed recently, and what to implement next.

| Document | Role |
|----------|------|
| **This file** | Snapshot · roadmap · changelog · **next inline** queue |
| [`USER_MANUAL_IMPLEMENTATION_PLAN.md`](USER_MANUAL_IMPLEMENTATION_PLAN.md) | Architecture, layout, CI design |
| [`USER_MANUAL_ROADMAP.md`](USER_MANUAL_ROADMAP.md) | Full phased timeline and milestones |
| [`USER_MANUAL_PIPELINE.md`](USER_MANUAL_PIPELINE.md) | Unified build — E2E embedded in doc generation |
| [`USER_MANUAL_E2E_MEDIA.md`](USER_MANUAL_E2E_MEDIA.md) | Screenshots & video contract |
| [`.cursor/skills/visa2026-user-manual/localization.md`](../.cursor/skills/visa2026-user-manual/localization.md) | **en, tr, tk, ru** i18n |
| [`.cursor/skills/visa2026-user-manual/testing-evidence.md`](../.cursor/skills/visa2026-user-manual/testing-evidence.md) | Green tick; separate `manual-test-reports/` |
| [`.cursor/skills/visa2026-user-manual/tracking.md`](../.cursor/skills/visa2026-user-manual/tracking.md) | Guide inventory, infra checklist, doc debt |
| [`.cursor/skills/visa2026-user-manual/curriculum.md`](../.cursor/skills/visa2026-user-manual/curriculum.md) | Publish order (tiers 0–7) |

**Agent skill:** [visa2026-user-manual](../.cursor/skills/visa2026-user-manual/SKILL.md)

---

## 1. At a glance

| Item | Value |
|------|--------|
| **Phase 0** | **Unblocked** (D1–D4, D11 recorded 2026-08-04) |
| **Officer site** | Not deployed — `user-manual/` does not exist yet |
| **Published guides** | 0 / 16 planned |
| **Pipeline** | Designed — `Build-UserManual.ps1` not implemented |
| **E2E UserManual traits** | Not implemented |
| **CI** | `user-manual.yml` not wired |

**Shipment principle:** documentation generation orchestrates EasyTest ([`USER_MANUAL_PIPELINE.md`](USER_MANUAL_PIPELINE.md)). Publish is **fail closed** if UserManual E2E or required media fails.

---

## 2. Roadmap summary

Full detail: [`USER_MANUAL_ROADMAP.md`](USER_MANUAL_ROADMAP.md). Calendar target: **Q3 2026** kickoff.

| Phase | Focus | Target window | Status |
|-------|--------|---------------|--------|
| **0** | MkDocs scaffold, empty catalog generator | Aug 2026 W1–2 | **Not started** |
| **1** | `bo-catalog.json`, link validator, CI build | Aug–Sep 2026 | Not started |
| **2** | Pilot guides **tiers 0–4** (CRUD-first curriculum) | Sep 2026 | Not started |
| **3** | Unified pipeline: E2E → screenshots → publish | Oct 2026 | Not started |
| **4** | Tiers 5–7 + **tr/tk/ru** for tier 0–4 pilots | Nov 2026 – Q1 2027 | Not started |
| **5** | In-app Help links (optional) | Q1 2027 | Not started |

### Phase 0 deliverables

| # | Deliverable | Done when |
|---|-------------|-----------|
| 0.1 | `user-manual/` MkDocs Material + **mkdocs-static-i18n** (en/tr/tk/ru) | `mkdocs build` green |
| 0.2 | `tools/UserManualManifestGenerator` (empty, compiles) | In solution |
| 0.3 | Placeholder pages + `mkdocs.yml` nav skeleton | `mkdocs serve` works |
| 0.4 | `scripts/ci/Build-UserManual.ps1` skeleton (`-SkipE2E`) | Runs mkdocs step |
| 0.5 | `.github/workflows/user-manual.yml` (build only) | PR check green |

### Phase 2 pilot guides (curriculum order)

Tiers 0–4 only in Phase 2 — document copies, dossier, templates deferred to Phase 4.

| Tier | Guides |
|------|--------|
| 0 | Login, navigation |
| 1 | Find / open person |
| 2 | Register employee, add passport |
| 3 | Edit employee, mark incomplete |
| 4 | Create application, add items |

### Success metrics (targets)

| Metric | Target | When |
|--------|--------|------|
| Pilot guides published | 5+ (tier 0–4) | End Phase 2 |
| Guides with E2E in `scenarios/ready/` | 2+ | End Phase 2 |
| Guides with auto screenshots | 5 | End Phase 3 |
| Manual site URL live | 1 | End Phase 3 |
| Daily officer tasks documented | ≥ 80% | End Phase 4 |

---

## 3. What has changed

Consolidated changelog across planning docs. Append here when architecture or phase scope changes.

| Date | Change |
|------|--------|
| 2026-08-04 | Initial implementation plan v0.1 — two-layer model (catalog + guides), MkDocs, CI |
| 2026-08-04 | Agent skill **visa2026-user-manual** — create · update · plan · fix · track |
| 2026-08-04 | Roadmap v0.1 — phased timeline, E2E media interlock |
| 2026-08-04 | **Curriculum** tiers 0–7 — CRUD on BOs first, **template generation last** |
| 2026-08-04 | **Unified pipeline** — `Build-UserManual.ps1` orchestrates E2E; not a separate runner ([`USER_MANUAL_PIPELINE.md`](USER_MANUAL_PIPELINE.md)) |
| 2026-08-04 | Video storage backend left **open** until Phase 3 (embed / static / object / Postgres) |
| 2026-08-04 | Guide inventory: 16 slugs ordered by tier in `tracking.md` |
| 2026-08-04 | **This status doc** — single entry for roadmap, changelog, next inline queue |
| 2026-08-04 | **UserManualDocs** xUnit layer — fast doc-generation tests in pipeline |
| 2026-08-04 | **Content policy** — officer site excludes code ([content-policy.md](../.cursor/skills/visa2026-user-manual/content-policy.md)) |
| 2026-08-04 | Git push → `user-manual.yml` orchestrates E2E; Cursor assist optional ([cursor-integration.md](../.cursor/skills/visa2026-user-manual/cursor-integration.md)) |
| 2026-08-04 | **Locales** en/tr/tk/ru — localization.md, mkdocs-static-i18n |
| 2026-08-04 | **Testing evidence** — green tick on manual; `manual-test-reports/` separate |
| 2026-08-04 | **D1–D17 decided** — on-prem Docker, tech publish, minimal pilot ([decisions.md](../.cursor/skills/visa2026-user-manual/decisions.md)) |

**Not changed yet (code):** no `user-manual/` folder, no generator, no CI workflow, no officer-facing content.

---

## 4. Next inline for implementation

Ordered queue for **Phase 0**. Complete in sequence unless noted. Update this section when items ship (move to §3 changelog).

| # | Task | Path / artifact | Owner skill | Depends on | Status |
|---|------|-----------------|-------------|------------|--------|
| **1** | MkDocs Material scaffold | `user-manual/mkdocs.yml`, `requirements.txt` | user-manual | — | **Next** |
| **2** | Placeholder site content | `user-manual/docs/index.md`, `getting-started/`, nav skeleton | user-manual | 1 | Pending |
| **3** | Status & roadmap pages in site | `user-manual/docs/about/roadmap.md` (sync from this doc) | user-manual | 2 | Pending |
| **4** | Empty manifest generator project | `tools/UserManualManifestGenerator/` | user-manual | — | Pending |
| **5** | Build script skeleton | `scripts/ci/Build-UserManual.ps1` (`-SkipE2E`, mkdocs only) | user-manual | 1, 4 | Pending |
| **6** | CI workflow (build + validate stub) | `.github/workflows/user-manual.yml` | user-manual | 5 | Pending |
| **7** | Phase 0 acceptance | `mkdocs build` + CI green on PR | user-manual | 1–6 | Pending |
| **8** | `cursor-on-push-user-manual.yml` (optional notify only) | Webhook wakes agent after UI merge — does not auto-generate | user-manual | 7 | Optional |

### Immediately after Phase 0 (Phase 1 preview)

| # | Task | Notes |
|---|------|-------|
| 8 | `UserDocumentationAttribute` on pilot BOs | Person, Application, ApplicationItem, ApplicationProgress |
| 9 | Generator → `bo-catalog.json` | Commit JSON on main |
| 10 | `UserManualManifestGenerator.Tests` — `Category=UserManualDocs` | Catalog + manifest parity |
| 11 | `Validate-UserManualLinks.ps1` | Fail on bad `bo:` references |
| 11 | Guide `_template.md` | Frontmatter: `tier`, `e2eScenarioId`, `status` |

### Parallel E2E (Phase 2 prep)

| # | Task | Notes |
|---|------|-------|
| E1 | Promote `person-employee-create` → `scenarios/ready/` | easytest-e2e |
| E2 | Add `[Trait("Category", "UserManual")]` on person register journey | Wired in Phase 3 pipeline |

**Rule:** when an item ships, check the box in [implementation plan §11](USER_MANUAL_IMPLEMENTATION_PLAN.md), update [tracking.md](../.cursor/skills/visa2026-user-manual/tracking.md), and append §3 above.

---

## 5. Guide inventory snapshot

Full table: [tracking.md § Guide inventory](../.cursor/skills/visa2026-user-manual/tracking.md#guide-inventory-layer-b).

| Status | Count |
|--------|------:|
| Backlog | 16 |
| Draft | 0 |
| Published | 0 |

**First guide to implement (Phase 2):** `getting-started/login` (tier 0), then `person/register` (tier 2, E2E: `person-employee-create`).

---

## 6. Infrastructure snapshot

| Component | Status |
|-----------|--------|
| `user-manual/` MkDocs site | Planned |
| `tools/UserManualManifestGenerator/` | Planned |
| `scripts/ci/Build-UserManual.ps1` | Planned |
| `manual-generation-manifest.yaml` | Planned (Phase 2) |
| `user-manual.yml` CI | Planned |
| `UserManualMediaCapture` (E2E) | Planned (Phase 3) |
| Published URL | TBD |

---

## 7. Decisions (recorded 2026-08-04)

**Full record:** [decisions.md](../.cursor/skills/visa2026-user-manual/decisions.md)

| Area | Choice |
|------|--------|
| Hosting (D1) | On-prem **Docker** container |
| Publish authority (D2) | **Tech** |
| Screenshots (D3) | EasyTest only |
| Catalog in git (D4) | Yes on `main` |
| Pilots (D11) | Login/navigation + `person/register` first |
| Deploy gate (D17) | Manual CI green for on-prem officer releases |
| Reviewers (D5) | Names **TBD** before Phase 2 |

**Phase 0:** unblocked.

---

## 8. How to keep this doc current

| Event | Update |
|-------|--------|
| Phase milestone completed | §1 at a glance, §2 status column, §4 queue |
| Architecture / scope change | §3 changelog + linked doc changelog |
| New guide or infra shipped | [tracking.md](../.cursor/skills/visa2026-user-manual/tracking.md) + §5 counts |
| Verified fix or pipeline lesson | [learnings.md](../.cursor/skills/visa2026-user-manual/learnings.md) |

**Agents:** read this file + `tracking.md` before any manual implementation work.

---

## 9. Changelog (this file)

| Date | Change |
|------|--------|
| 2026-08-04 | Initial status hub — roadmap summary, consolidated changelog, Phase 0 next-inline queue |
