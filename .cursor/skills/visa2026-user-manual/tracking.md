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
| **2** | Tier 0–4 pilot guides + screenshots | **In progress** | **34 drafts**, **5 published** (pilots 1–5 en), **1 postponed** (#6) |
| **3** | GitHub Pages + EasyTest **screenshots** | **In progress** | Video **deferred** (D21) |
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
| Media capture registry | `user-manual/media-capture-registry.yaml` | **Shipped** (v2 — `guideSlugs`) |
| Media capture validator | `scripts/ci/Validate-UserManualMediaCaptures.ps1` | **Shipped** |
| E2E video copy (static) | `scripts/ci/Copy-EasyTestManualVideos.ps1` | **Deferred** (D21 — opt-in `-EnableVideo` only) |
| Code change detection (mandatory for skill) | [code-drift-scan.md](./code-drift-scan.md) | **Shipped** (script planned Phase 2–3) |
| `Scan-UserManualDrift.ps1` | `scripts/ci/` | Planned |
| E2E step capture helper | `Visa2026.E2E.Tests/UserManual/UserManualMediaCaptureKeys.cs` | **Shipped** (pilots 1–5) |
| Test results (separate) | `manual-test-reports/` — `Write-ManualTestReport.ps1` → `latest/summary.html`; manual shows **green tick** only | **Foundation** (Phase 3) |
| Published URL | _TBD_ | — |

---

## Guide inventory (Layer B)

**Publish order:** sort by **order** column (BO dependency). Full curriculum: [curriculum.md](./curriculum.md) §2.1–§3.

| Order | Tier | Slug | Title | `bo` | Parent | Ops | `e2eScenarioId` | E2E notes | Media | Status | Phase |
|------:|------|------|-------|------|--------|-----|-----------------|-----------|-------|--------|-------|
| 1 | 0 | `getting-started/login` | Sign in to Visa2026 | — | — | read | `person-officer-journey` | Officer review + E2E 2026-08-05 | **doc-anchored** | **Published** | 2 |
| 2 | 0 | `getting-started/navigation` | Main navigation | — | — | read | `person-officer-journey` | Officer review + E2E 2026-08-05 | **doc-anchored** | **Published** | 2 |
| 3 | 1 | `person/open-and-search` | Find and open a person | Person | — | read | `person-officer-journey` | Officer review + E2E 2026-08-05 | **doc-anchored** | **Published** | 2 |
| 4 | 2 | `employee/register` | Register a new employee | Person | — | create | `person-officer-journey` | Officer review + E2E 2026-08-05 | **doc-anchored** | **Published** | 2 |
| 4a | 3 | `employee/family-members-for-visa-manual` | Family members for visa (manual) | Person | — | update | `person-officer-journey` | Manual-only outputs rule; guide **draft** pending E2E | **doc-anchored** | **Draft** | 2 |
| 5a | 2 | `employee/add-passport` | Add a passport (employee) | Person | — | create | `person-officer-journey` | Officer review + E2E 2026-08-05 | **doc-anchored** | **Published** | 2 |
| 5b | 2 | `family-member/add-passport` | Add a passport (family member) | Person | — | create | `person-officer-journey` | Full EN | legacy fan-out | **Draft** | 2 |
| 4b | 2 | `family-member/register` | Register a family member | Person | — | create | `person-officer-journey` | Playwright E2E (doc-anchored) | **doc-anchored** | **Draft** | 2 |
| 6a | 2 | `employee/add-visa` | Add a visa (employee) | Person | Passport | create | `person-officer-journey` | Officer review + E2E 20260805-172303 | **doc-anchored** | **Published** | 2 |
| 6b | 2 | `family-member/add-visa` | Add a visa (family member) | Person | Passport | create | `person-officer-journey` | Prose-first | legacy fan-out | **Draft** | 2 |
| 5c | 2 | `temporary-visitor/add-passport` | Add a passport (TV) | Person | — | create | `person-officer-journey` | Full EN | legacy fan-out | **Draft** | 2 |
| 4c | 2 | `temporary-visitor/register` | Register a temporary visitor | Person | — | create | `person-officer-journey` | List New | legacy fan-out | **Draft** | 2 |
| 6c | 2 | `temporary-visitor/add-visa` | Add a visa (TV) | Person | Passport | create | `person-officer-journey` | Full EN | legacy fan-out | **Draft** | 2 |
| 7 | 2 | `employee/add-education` | Add an education record | Person | — | create | `person-officer-journey` | Full CRUD test | legacy fan-out | **Draft** | 2 |
| 8a | 2 | `employee/add-medical-record` | Add a medical record (employee) | Person | — | create | `person-officer-journey` | Full CRUD test | legacy fan-out | **Draft** | 2 |
| 8b | 2 | `family-member/add-medical-record` | Add a medical record (FM) | Person | — | create | `person-officer-journey` | Full EN | legacy fan-out | **Draft** | 2 |
| 8c | 2 | `temporary-visitor/add-medical-record` | Add a medical record (TV) | Person | — | create | `person-officer-journey` | Full EN | legacy fan-out | **Draft** | 2 |
| 9a | 2 | `employee/add-address` | Add an address (employee) | Person | — | create | `person-officer-journey` | E2E deferred | legacy fan-out | **Draft** | 2 |
| 9b | 2 | `family-member/add-address` | Add an address (FM) | Person | — | create | `person-officer-journey` | E2E deferred | legacy fan-out | **Draft** | 2 |
| 9c | 2 | `temporary-visitor/add-address` | Add an address (TV) | Person | — | create | `person-officer-journey` | E2E deferred | legacy fan-out | **Draft** | 2 |
| 10 | 2 | `employee/add-position-history` | Add position history | Person | — | create | `person-officer-journey` | E2E deferred | legacy fan-out | **Draft** | 2 |
| 11 | 2 | `employee/add-work-duty` | Add a work duty | Person | — | create | `person-officer-journey` | Full CRUD test | legacy fan-out | **Draft** | 2 |
| 12 | 2 | `employee/add-salary` | Add a salary record | Person | — | create | `person-officer-journey` | Full CRUD test | legacy fan-out | **Draft** | 2 |
| 13 | 2 | `employee/add-travel` | Add a travel history | Person | — | create | `person-officer-journey` | External Arrival | legacy fan-out | **Draft** | 2 |
| 13c | 2 | `temporary-visitor/add-travel` | Add a travel history (TV) | Person | — | create | `person-officer-journey` | Full EN | legacy fan-out | **Draft** | 2 |
| 14b | 2 | `family-member/add-family-relation-documents` | Add family relation documents | Person | — | create | `person-officer-journey` | FM-only tab | legacy fan-out | **Draft** | 2 |
| 15b | 3 | `family-member/edit-family-member` | Update family member details | Person | — | update | `person-officer-journey` | — | legacy fan-out | **Draft** | 2 |
| 14 | 2 | `employee/add-cv-documents` | Add CV and personal files | Person | — | create | `person-officer-journey` | No E2E yet | legacy fan-out | **Draft** | 2 |
| 15 | 3 | `employee/edit-employee` | Update employee details | Person | — | update | `person-officer-journey` | — | legacy fan-out | **Draft** | 2 |
| 16 | 3 | `person/mark-incomplete` | Mark incomplete / complete | Person | — | update | _TBD_ | No E2E yet | legacy fan-out | **Draft** | 2 |
| 16.5 | 4 | `applications/overview` | Applications — ministry and direct migration | Application | — | read | `person-officer-journey` | Four Applications menu lists | legacy fan-out | **Draft** | 2 |
| 16.6 | 4 | `applications/application-profiles` | Application profiles — how configuration works | Application | — | read | — | Profile picker + live FK model; prose/mermaid preview | **none (preview)** | **Draft** | 2 |
| 17 | 4 | `applications/create` | Create an application | Application | — | create | `person-officer-journey` | Profile picker flow; legacy PNGs flagged | legacy fan-out | **Draft** | 2 |
| 18 | 4 | `applications/add-items` | Add application items | ApplicationItem | Application | create | `person-officer-journey` | Nested tab + Current* from person | legacy fan-out | **Draft** | 2 |
| 19 | 4 | `applications/progress` | Track application progress | ApplicationProgress | Application | update | `person-officer-journey` | Progress tab; implied office; ministry letter | legacy fan-out | **Draft** | 2 |
| 20 | 5 | `applications/document-copies` | Ministry document copies | ApplicationItem | Application | generate | `person-officer-journey` | Preview slot + PDF toast | legacy fan-out | **Draft** | 2 |
| 21 | 5 | `applications/resminamalar` | Resminamalar report package | Application | — | generate | `person-officer-journey` | Application + item scope; Word toast | legacy fan-out | **Draft** | 2 |
| 22 | 6 | `person/dossier` | Person dossier | Person | — | read | `person-officer-journey` | Screen/Paper; copies slot; director export | legacy fan-out | **Draft** | 2 |
| 23 | 6 | `tracking/report-dashboard` | Report Dashboard | — | — | read | `person-officer-journey` | Overview, categories, ListView, Excel, Person search | legacy fan-out | **Draft** | 2 |
| 24 | 7 | `administration/user-report-templates` | User report templates | UserReportTemplate | — | generate | `person-officer-journey` | Reports nav; Extract/Validate; visibility | legacy fan-out | **Draft** | 2 |
| 25 | 7 | `administration/template-staging` | Edit and sync templates | UserReportTemplate | — | update | `person-officer-journey` | Templates footer; FSA folder; Sync | legacy fan-out | **Draft** | 2 |
| 26 | 6 | `person/document-copies` | Person document copies | Person | — | read | `person-officer-journey` | Detail toolbar; list • column; dossier | legacy fan-out | **Draft** | 2 |
| 27 | 6 | `tracking/state-notifications` | State notifications | — | — | read | _TBD_ | Product decision: not in officer rollout | — | **Postponed** | — |
| 28 | 8 | `administration/configuration/overview` | Configuration overview | — | — | read | `person-officer-journey` | Eleven Configuration menu items | legacy fan-out | **Draft** | 2 |
| 29 | 8 | `administration/configuration/organization` | Organization settings | CompanyProfile | — | update | `person-officer-journey` | Four singletons | legacy fan-out | **Draft** | 2 |
| 30 | 8 | `administration/configuration/contracts-and-approvals` | Contracts and approvals | ProjectContract | — | update | `person-officer-journey` | Ministries + legs + contracts | legacy fan-out | **Draft** | 2 |
| 31 | 8 | `administration/configuration/sla` | SLA settings | ApplicationMigrationSlaProfile | — | update | `person-officer-journey` | Migration profile + ministry singleton | legacy fan-out | **Draft** | 2 |
| 32 | 8 | `administration/configuration/alerts-and-upload-limits` | Alerts and upload limits | ExpirationAlertRule | — | update | `person-officer-journey` | Expiry rules + upload MB caps | legacy fan-out | **Draft** | 2 |
| 33 | 8 | `administration/configuration/application-profiles` | Configure application profiles | ApplicationProfile | — | update | — | Configure profile wizard; VisaOffice | **none (preview)** | **Draft** | 2 |

**Parent:** domain parent BO for nested-create guides (`Passport` child of `Person`, `Visa` child of `Passport`).  
**personRole:** `Employee` · `FamilyMember` · `TemporaryVisitor` — matches typed detail view (`Person_DetailView_*`). Omit for cross-role guides.  
**Ops:** `read` · `create` · `update` · `delete` · `generate`  
**Media:** `doc-anchored` (registry + `media-capture` anchors) · `legacy fan-out` (unmigrated — migrate before publish)  
**Status:** `backlog` · `draft` · `review` · `published` · `stale`  
**Phase:** target roadmap phase for first publish

**Next implementation (BO order):** migrate **legacy fan-out** guides to doc-anchored keys; review/publish passes; E2E media; locale depth; **tier 8 configuration** screenshots. **State notifications (#6) postponed** — not in manual scope.

---

## `[UserDocumentation]` coverage (Layer A anchors)

| BO / type | Slug | Tier | Guide exists | Catalog in CI |
|-----------|------|------|--------------|---------------|
| Person | `person/overview` | 1–3 | No | **Yes** |
| Passport | `person/passport-overview` | 2 | No | No |
| Visa | — | 2 | No | No |
| Education | — | 2 | No | No |
| MedicalRecord | — | 2 | No | No |
| AddressOfResidence | — | 2 | No | No |
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
| 6 | **Video in officer manual** | **Deferred (D21)** — screenshots-only; optional `-EnableVideo` for experiments; not in CI publish gate |

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
| 2026-08-05 | **Doc-anchored media** — registry v2 (`guideSlugs`), tracking **Media** column, pilots 1–5 doc-anchored |
| 2026-08-04 | Status hub doc — consolidated roadmap/changelog/next inline |
| 2026-08-05 | **person/open-and-search** draft (en/tr/tk/ru); tier 1 inventory row → Draft |
| 2026-08-05 | **person/edit-employee** draft (en/tr/tk/ru); screenshot map in Copy-EasyTestManualScreenshots.ps1 |
| 2026-08-05 | **person/mark-incomplete** draft (en/tr/tk/ru); no E2E yet — dashboard label **Persons with incomplete data** |
| 2026-08-05 | **Inventory v2** — 25 guides; BO dependency order (Person nested 6–14 before `applications/add-items`); curriculum §2.1 |
