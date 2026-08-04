# User manual — tracking

**Living status board** for documentation generation. Update when **planning**, **shipping**, or **fixing** manual work.

**Canonical plan:** [docs/USER_MANUAL_IMPLEMENTATION_PLAN.md](../../../docs/USER_MANUAL_IMPLEMENTATION_PLAN.md)

**Status hub (roadmap · changelog · next inline):** [docs/USER_MANUAL_STATUS.md](../../../docs/USER_MANUAL_STATUS.md) — **update §4 when completing queue items**

**Advisory (read first):** [advisory.md](./advisory.md)

**Curriculum (publish order):** [curriculum.md](./curriculum.md) — CRUD on BOs → workflows → packages → template generation

**Roadmap:** [docs/USER_MANUAL_ROADMAP.md](../../../docs/USER_MANUAL_ROADMAP.md)

**E2E media contract:** [docs/USER_MANUAL_E2E_MEDIA.md](../../../docs/USER_MANUAL_E2E_MEDIA.md)

**Experience log (append-only):** [learnings.md](./learnings.md)

---

## How to use this file

| Activity | Update here |
|----------|-------------|
| **Plan** | Phase progress, backlog, open decisions |
| **Create** | New guide row → `draft`; respect **tier** order in [curriculum.md](./curriculum.md) |
| **Update** | Guide `status`, `lastReviewed`, `screenshotsVersion` |
| **Fix** | Move item from **Doc debt** → resolved; note in learnings |
| **Track** | Coverage %, CI health, publish URL |

After any verified change, also append [learnings.md](./learnings.md).

---

## Phase progress

Sync checkboxes with [USER_MANUAL_IMPLEMENTATION_PLAN.md §11](../../../docs/USER_MANUAL_IMPLEMENTATION_PLAN.md). **Next tasks:** [USER_MANUAL_STATUS.md §4](../../../docs/USER_MANUAL_STATUS.md#4-next-inline-for-implementation).

| Phase | Focus | Status | Notes |
|-------|--------|--------|-------|
| **0** | MkDocs scaffold, empty generator | **Not started** | |
| **1** | Catalog generator + CI validator | **Not started** | |
| **2** | Tier 0–4 pilot guides + screenshots | **Not started** | CRUD-first per curriculum |
| **3** | GitHub Pages + EasyTest screenshots | **Not started** | |
| **4** | Tiers 5–7 + **tr/tk/ru** for pilots | **Not started** | Packages, dossier, templates last |
| **5** | In-app Help links | **Not started** | |

---

## Infrastructure checklist

| Component | Path | Status |
|-----------|------|--------|
| MkDocs site | `user-manual/` | Planned |
| Catalog generator | `tools/UserManualManifestGenerator/` | Planned |
| Doc unit tests | `tools/UserManualManifestGenerator.Tests/` (`Category=UserManualDocs`) | Planned (Phase 1) |
| `[UserDocumentation]` attribute | `Visa2026.Module/Documentation/` | Planned |
| Link validator | `scripts/ci/Validate-UserManualLinks.ps1` | Planned |
| Build script (orchestrator) | `scripts/ci/Build-UserManual.ps1` — **E2E + catalog + mkdocs** | Planned |
| Manual generation manifest | `user-manual/manual-generation-manifest.yaml` | Planned (Phase 2) |
| Publish script | `scripts/ci/Publish-UserManualPages.ps1` | Planned |
| CI workflow | `.github/workflows/user-manual.yml` | Planned |
| Cursor push notify (optional) | `.github/workflows/cursor-on-push-user-manual.yml` | Optional — wakes agent, not auto-generate |
| E2E screenshot copy | `scripts/ci/Copy-EasyTestManualScreenshots.ps1` | Planned (Phase 3) |
| E2E step capture helper | `Visa2026.E2E.Tests/UserManualMediaCapture.cs` | Planned (Phase 3) |
| Test results (separate) | `manual-test-reports/` — full report; manual shows **green tick** only | Planned (Phase 3) |
| Published URL | _TBD_ | — |

---

## Guide inventory (Layer B)

**Publish order:** sort by `tier` then `order`. Full curriculum: [curriculum.md](./curriculum.md).

| Order | Tier | Slug | Title | `bo` | Ops | `e2eScenarioId` | Status | Phase |
|------:|------|------|-------|------|-----|-----------------|--------|-------|
| 1 | 0 | `getting-started/login` | Login and roles | — | read | — | **Backlog** | 2 |
| 2 | 0 | `getting-started/navigation` | Main navigation | — | read | — | **Backlog** | 2 |
| 3 | 1 | `person/open-and-search` | Find and open a person | Person | read | _TBD_ | **Backlog** | 2 |
| 4 | 2 | `person/register` | Register a new employee | Person | create | `person-employee-create` | **Backlog** | 2 |
| 5 | 2 | `person/add-passport` | Add a passport | Passport | create | `person-employee-passport-create` | **Backlog** | 2 |
| 6 | 3 | `person/edit-employee` | Update employee details | Person | update | _TBD_ | **Backlog** | 2 |
| 7 | 3 | `person/mark-incomplete` | Mark incomplete / complete | Person | update | _TBD_ | **Backlog** | 2 |
| 8 | 4 | `applications/create` | Create an application | Application | create | _TBD_ | **Backlog** | 2 |
| 9 | 4 | `applications/add-items` | Add application items | ApplicationItem | create | _TBD_ | **Backlog** | 2 |
| 10 | 4 | `applications/progress` | Track application progress | ApplicationProgress | update | _TBD_ | **Backlog** | 4 |
| 11 | 5 | `applications/document-copies` | Ministry document copies | ApplicationItem | generate | _TBD_ | **Backlog** | 4 |
| 12 | 5 | `applications/resminamalar` | Resminamalar report package | Application | generate | _TBD_ | **Backlog** | 4 |
| 13 | 6 | `person/dossier` | Person dossier | Person | read | _TBD_ | **Backlog** | 4 |
| 14 | 6 | `tracking/report-dashboard` | Report Dashboard | — | read | _TBD_ | **Backlog** | 4 |
| 15 | 7 | `administration/user-report-templates` | User report templates | UserReportTemplate | generate | _TBD_ | **Backlog** | 4–5 |
| 16 | 7 | `administration/template-staging` | Edit and sync templates | UserReportTemplate | update | _TBD_ | **Backlog** | 4–5 |

**Ops:** `read` · `create` · `update` · `delete` · `generate`  
**Status:** `backlog` · `draft` · `review` · `published` · `stale`  
**Phase:** target roadmap phase for first publish

---

## `[UserDocumentation]` coverage (Layer A anchors)

| BO / type | Slug | Tier | Guide exists | Catalog in CI |
|-----------|------|------|--------------|---------------|
| Person | `person/overview` | 1–3 | No | No |
| Passport | `person/passport-overview` | 2 | No | No |
| Application | `applications/overview` | 4 | No | No |
| ApplicationItem | `applications/item-overview` | 4 | No | No |
| ApplicationProgress | `applications/progress` | 4 | No | No |
| UserReportTemplate | `administration/templates-overview` | 7 | No | No |

---

## Doc debt / fix queue

| ID | Issue | Severity | Owner | Resolution |
|----|-------|----------|-------|------------|
| — | _None yet_ | — | — | — |

**Severity:** `blocker` (CI red) · `high` (wrong officer steps) · `medium` (stale screenshots) · `low` (typo)

---

## CI & publish health

| Check | Last known | Notes |
|-------|------------|-------|
| `Validate-UserManualLinks` | — | Not wired |
| `mkdocs build` | — | Not wired |
| `user-manual.yml` on PR | — | Not wired |
| Pages deploy (main) | — | Not wired |

---

## Decisions (recorded 2026-08-04)

Full table: [decisions.md](./decisions.md). Summary:

| # | Decision |
|---|----------|
| D1 | On-prem **Docker** container |
| D2 | **Tech** publishes `status: published` |
| D3–D4 | EasyTest screenshots; **commit** `bo-catalog.json` |
| D5 | Reviewer names **before Phase 2** (_TBD_) |
| D6–D7 | CI-only reference; deprecate `LookupNavigationStructure.md` |
| D8–D9 | Agent + officer review; **en first** |
| D10–D17 | See decisions.md |
| D14–D15 | On-prem static/object video; top 3–5 pilots |

**Governance:** green tick ≠ human review; no test logs on manual. Reviewer names: _TBD_.

---

## Open decisions

_None blocking Phase 0._ Reviewer names (D5) due before Phase 2.

---

## Changelog (tracking file only)

Detail changelog: [USER_MANUAL_STATUS.md §3](../../../docs/USER_MANUAL_STATUS.md#3-what-has-changed).

| Date | Change |
|------|--------|
| 2026-08-04 | Initial tracking board; all phases not started |
| 2026-08-04 | Roadmap + E2E media contract |
| 2026-08-04 | Video storage open decision #6 |
| 2026-08-04 | Curriculum-ordered guide inventory (tiers 0–7) |
| 2026-08-04 | **D1–D17 recorded** — [decisions.md](./decisions.md) |
| 2026-08-04 | Status hub doc — consolidated roadmap/changelog/next inline |
