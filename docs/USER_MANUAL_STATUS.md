# User Manual — status, roadmap & next steps

Status: **Phase 1 complete — catalog + validator + UserManualDocs tests**  
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
| [`USER_MANUAL_RELEASE.md`](USER_MANUAL_RELEASE.md) | On-prem release bundle (media + site, nginx compose) |
| [`.cursor/skills/visa2026-user-manual/localization.md`](../.cursor/skills/visa2026-user-manual/localization.md) | **en, tr, tk, ru** i18n |
| [`.cursor/skills/visa2026-user-manual/testing-evidence.md`](../.cursor/skills/visa2026-user-manual/testing-evidence.md) | Green tick; separate `manual-test-reports/` |
| [`.cursor/skills/visa2026-user-manual/tracking.md`](../.cursor/skills/visa2026-user-manual/tracking.md) | Guide inventory, infra checklist, doc debt |
| [`.cursor/skills/visa2026-user-manual/curriculum.md`](../.cursor/skills/visa2026-user-manual/curriculum.md) | Publish order (tiers 0–7) |

**Agent skill:** [visa2026-user-manual](../.cursor/skills/visa2026-user-manual/SKILL.md)

---

## 1. At a glance

| Item | Value |
|------|--------|
| **Phase 0** | **Complete** |
| **Phase 1** | **Complete** — `bo-catalog.json`, validator, `UserManualDocs` tests |
| **Officer site** | MkDocs scaffold + generated reference catalog page |
| **Published guides** | 0 / 16 planned (4 drafts) |
| **Pipeline** | `Build-UserManual.ps1` (generator → unit tests → validate → mkdocs) |
| **E2E UserManual traits** | Not implemented (Phase 3) |
| **CI** | `user-manual.yml` (build + validate + UserManualDocs; **GitHub Pages** on `master` push) |
| **Local preview** | `scripts/local/Serve-UserManual.ps1` → **http://127.0.0.1:8765/manual/** |

**Shipment principle:** documentation generation orchestrates EasyTest ([`USER_MANUAL_PIPELINE.md`](USER_MANUAL_PIPELINE.md)). Publish is **fail closed** if UserManual E2E or required media fails.

---

## 2. Roadmap summary

Full detail: [`USER_MANUAL_ROADMAP.md`](USER_MANUAL_ROADMAP.md). Calendar target: **Q3 2026** kickoff.

| Phase | Focus | Target window | Status |
|-------|--------|---------------|--------|
| **0** | MkDocs scaffold, empty catalog generator | Aug 2026 W1–2 | **Complete** |
| **1** | `bo-catalog.json`, link validator, CI build | Aug–Sep 2026 | **Complete** |
| **2** | Tier 0–4 pilot guides + screenshots | Sep 2026 | **In progress** — login guide draft |
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
| 2026-08-04 | **Phase 0 scaffold** — `user-manual/` MkDocs i18n, generator stub, `Build-UserManual.ps1`, `user-manual.yml` |
| 2026-08-04 | **Phase 1 catalog** — `[UserDocumentation]` on pilot BOs, `bo-catalog.json`, validator, `UserManualDocs` tests |
| 2026-08-04 | **Phase 2 login guide** — `getting-started/login` draft (en/tr/tk/ru) |
| 2026-08-04 | **Phase 2 navigation guide** — `getting-started/navigation` draft (en/tr/tk/ru) |

**Run locally:** `./scripts/ci/Build-UserManual.ps1 -SkipE2E` (requires Python 3 for mkdocs step).

---

## 4. Next inline for implementation

Ordered queue for **Phase 0**. Complete in sequence unless noted. Update this section when items ship (move to §3 changelog).

| # | Task | Path / artifact | Owner skill | Depends on | Status |
|---|------|-----------------|-------------|------------|--------|
| **1** | MkDocs Material scaffold | `user-manual/mkdocs.yml`, `requirements.txt` | user-manual | — | **Done** |
| **2** | Placeholder site content | `user-manual/docs/{en,tr,tk,ru}/` | user-manual | 1 | **Done** |
| **3** | Status & roadmap pages in site | `user-manual/docs/*/about/roadmap.md` | user-manual | 2 | **Done** |
| **4** | Empty manifest generator project | `tools/UserManualManifestGenerator/` | user-manual | — | **Done** |
| **5** | Build script skeleton | `scripts/ci/Build-UserManual.ps1` (`-SkipE2E`) | user-manual | 1, 4 | **Done** |
| **6** | CI workflow (build + validate stub) | `.github/workflows/user-manual.yml` | user-manual | 5 | **Done** |
| **7** | Phase 0 acceptance | `mkdocs build` + CI green on PR | user-manual | 1–6 | **Done** |

### Phase 2 (in progress)

| # | Task | Status |
|---|------|--------|
| **P2-1** | `getting-started/login` guide (en/tr/tk/ru) | **Draft** |
| **P2-2** | `getting-started/navigation` guide (en/tr/tk/ru) | **Draft** |
| **P2-3** | Login + navigation screenshots (`v2026.08/en/`) | **Done** (EasyTest `person-officer-journey`; en UI replicated to tr/tk/ru per D12) |
| **P2-4** | `person/register` guide (en/tr/tk/ru) | **Draft** |
| **P2-5** | `person/add-passport` guide (en/tr/tk/ru) | **Draft** |
| **P2-6** | `person/edit-employee` guide | Backlog |
| **P2-7** | Assign reviewer names (D5) | TBD |

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
| Draft | 4 |
| Published | 0 |

**First guide to implement (Phase 2):** `getting-started/login` (tier 0), then `person/register` (tier 2, E2E: `person-employee-create`).

---

## 6. Infrastructure snapshot

| Component | Status |
|-----------|--------|
| `user-manual/` MkDocs site | **Scaffold** + generated reference |
| `tools/UserManualManifestGenerator/` | **Shipped** (4 pilot types) |
| `user-manual/generated/bo-catalog.json` | **Committed** (D4) |
| `scripts/ci/Build-UserManual.ps1` | **Phase 1** pipeline |
| `scripts/ci/Validate-UserManualLinks.ps1` | **Phase 1** (`bo:` + slug checks) |
| `UserManualManifestGenerator.Tests` | **3 tests** (`Category=UserManualDocs`) |
| `manual-generation-manifest.yaml` | Planned (Phase 2) |
| `user-manual.yml` CI | **Wired** |
| `UserManualMediaCapture` (E2E) | Planned (Phase 3) |
| Published URL | TBD (on-prem Docker, D1) |

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
| 2026-08-04 | Phase 0 scaffold shipped — MkDocs i18n site, generator stub, build script, CI workflow |
