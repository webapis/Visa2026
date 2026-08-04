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
| **Scan** | Code change detection report; doc-debt rows — [code-drift-scan.md](./code-drift-scan.md) |
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
| **0** | MkDocs scaffold, empty generator | **Complete** | |
| **1** | Catalog generator + CI validator | **Complete** | `bo-catalog.json` committed; 3 UserManualDocs tests |
| **2** | Tier 0–4 pilot guides + screenshots | **In progress** | Login, navigation, register, add-passport drafts |
| **3** | GitHub Pages + EasyTest screenshots + **static videos** | **In progress** | `Copy-EasyTestManualVideos.ps1`; storage **static** (D6) |
| **4** | Tiers 5–7 + **tr/tk/ru** for pilots | **Not started** | Packages, dossier, templates last |
| **5** | In-app Help links | **Not started** | |

---

## Infrastructure checklist

| Component | Path | Status |
|-----------|------|--------|
| MkDocs site | `user-manual/` | **Scaffold** + generated reference |
| Catalog generator | `tools/UserManualManifestGenerator/` | **Shipped** |
| Doc unit tests | `tools/UserManualManifestGenerator.Tests/` (`Category=UserManualDocs`) | **3 tests** |
| `[UserDocumentation]` attribute | `Visa2026.Module/Documentation/` | **Shipped** (4 pilot BOs) |
| Link validator | `scripts/ci/Validate-UserManualLinks.ps1` | **Phase 1** |
| Build script (orchestrator) | `scripts/ci/Build-UserManual.ps1` | **Phase 1** |
| `bo-catalog.json` | `user-manual/generated/` | **Committed** |
| Manual generation manifest | `user-manual/manual-generation-manifest.yaml` | Planned (Phase 2) |
| Publish script | `scripts/ci/Publish-UserManualPages.ps1` | Planned |
| CI workflow | `.github/workflows/user-manual.yml` | **Wired** |
| Cursor push notify (optional) | `.github/workflows/cursor-on-push-user-manual.yml` | Optional — wakes agent, not auto-generate |
| E2E screenshot copy | `scripts/ci/Copy-EasyTestManualScreenshots.ps1` | **Shipped** |
| E2E video copy (static) | `scripts/ci/Copy-EasyTestManualVideos.ps1` | **Shipped** |
| Code change detection (mandatory for skill) | [code-drift-scan.md](./code-drift-scan.md) | **Shipped** (script planned Phase 2–3) |
| `Scan-UserManualDrift.ps1` | `scripts/ci/` | Planned |
| E2E step capture helper | `Visa2026.E2E.Tests/UserManualMediaCapture.cs` | Planned (Phase 3) |
| Test results (separate) | `manual-test-reports/` — full report; manual shows **green tick** only | Planned (Phase 3) |
| Published URL | _TBD_ | — |

---

## Guide inventory (Layer B)

**Publish order:** sort by `tier` then `order`. Full curriculum: [curriculum.md](./curriculum.md).

| Order | Tier | Slug | Title | `bo` | Ops | `e2eScenarioId` | Status | Phase |
|------:|------|------|-------|------|-----|-----------------|--------|-------|
| 1 | 0 | `getting-started/login` | Sign in to Visa2026 | — | read | `person-officer-journey` | **Draft** | 2 |
| 2 | 0 | `getting-started/navigation` | Main navigation | — | read | `person-officer-journey` | **Draft** | 2 |
| 3 | 1 | `person/open-and-search` | Find and open a person | Person | read | _TBD_ | **Backlog** | 2 |
| 4 | 2 | `person/register` | Register a new employee | Person | create | `person-officer-journey` | **Draft** | 2 |
| 5 | 2 | `person/add-passport` | Add a passport | Person | create | `person-officer-journey` | **Draft** | 2 |
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
| Person | `person/overview` | 1–3 | No | **Yes** |
| Passport | `person/passport-overview` | 2 | No | No |
| Application | `applications/overview` | 4 | No | **Yes** |
| ApplicationItem | `applications/item-overview` | 4 | No | **Yes** |
| ApplicationProgress | `applications/progress` | 4 | No | **Yes** |
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
| `Validate-UserManualLinks` | 2026-08-04 | Phase 1 (`bo:`, slugs, code fences) |
| `UserManualDocs` tests | 2026-08-04 | 3 passing |
| `mkdocs build` | Pending CI | Local dev needs Python 3 |
| `user-manual.yml` on PR | Wired | Awaiting first green run |
| Pages deploy (main) | — | On-prem Docker (D1), Phase 3 |

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

| # | Topic | Status |
|---|--------|--------|
| 6 | **Video storage backend** | **Decided: static** — `user-manual/assets/videos/v{version}/{locale}/`; MP4 promoted via `Copy-EasyTestManualVideos.ps1` (**gitignored**; same as PNG) |

Reviewer names (D5) due before Phase 2 publish sign-off.

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
