# Application Profile — implementation plan (status tracker)

**Skill:** [SKILL.md](./SKILL.md) · **Canonical:** [`docs/APPLICATION_PROFILE_PLAN.md`](../../../docs/APPLICATION_PROFILE_PLAN.md) §12

Update this file when a slice starts (**In progress**) or ships (**Done**). Mirror summary in plan §12 on merge-worthy changes.

**Status values:** `Pending` · `In progress` · `Done` · `Deferred`

---

## Slice overview

| # | Slice | Status | Notes |
|---|--------|--------|-------|
| 0 | Plan + UX prototypes | **Done** | `docs/prototypes/*.png` (22 mockups, 2026-08-10) |
| 1 | Deprecate `ApplicationType` (registry, UI, dual-read) | **Done** | `docs/DEPRECATED.md` — Application Profile cutover section (2026-08-07) |
| 2 | `ApplicationProfile` BO + legs + nested templates | **Done** | `ApplicationProfile.cs` v1 scalars/collections |
| 3 | `Application.ApplicationProfile` FK + default seeding | **Done** | Optional during dual-read; `ApplyDefaultsForApplicationProfile` |
| 4 | Permissions (Users read / VisaOffice manage) | **Done** | `Updater.cs` |
| 5 | Seed profiles from `ApplicationType` catalog | **Done** | `ApplicationProfileSeedSync` + mapper + updater + startup gate |
| 6 | Switch Appearance / progress to profile | **Done** | `ApplicationProfileConfigurationResolver`, `Cfg*` criteria, progress route/SLA |
| 7 | Config lock enforcement on profile edit UI | **Done** | DetailView read-only, save guard, clone duplicate |
| 8 | Configuration wizard UX | **Done** | 6-step Blazor wizard; **Configure profile** on Application Profiles |
| 8e | Wizard Company, Signatories | **Done** | Live read of Configuration singletons (not copied onto the profile); Edit in Configuration opens the real BOs |
| 8f | Wizard Results default lookup dropdowns | **Done** | Catalog snapshots; default-value selects enabled only when Use is checked |
| 8g | Wizard May produce / cancel with Related to | **Done** | Issuance → May produce; Cancellation → May cancel; moved off Results & fields |
| 8h | Wizard Approval legs with Directed to | **Done** | Via ministry → legs on Identity; Direct migration hides and clears legs |
| 8d | Wizard step 4 real template catalog + persist scope | **Done** | Live `UserReportTemplate` Category/Global; `CatalogScope`/`DataScope`/`CategoryKey` on nested template |
| 8a | Application Profile overview (live) | **Done** | Live config/defaults/legs/templates + linked `ApplicationProfileInstance` rows; overview shows wizard identity, company/signatories, required fields, process states, template scope; mock only if profile id unresolved |
| 8c | Custom catalog home (replace native List/Detail UI) | **Done** | List first; row opens overview; **Back to list**; New/Configure → wizard (new tab); **Save profile** reloads catalog; **Delete** when Linked = 0; toolbar **Total: N**; table-body scroll, sticky header |
| 9 | Profile picker at Application create | **Done** | Intercepts **New** on Application ListViews; Blazor picker UI |
| 10 | Person M2M DetailView; hard-remove `ApplicationItem` | **In progress** | Skip-navigation `People` + child BO M2M (includes **MedicalRecord**, **WorkDuty**). Output headers Invitation / WorkPermit / BorderZone / Rejection / IssuedVisas are **1:N** (May produce), not skip-nav. Wizard **May produce** includes Rejection. Person issued tab **Applications (linked)** verified. Rebuild DataImporter + resume Wave 2b (`-StartAt ApplicationProfileInstancePerson`); then People-tab / copies / Resminamalar smoke. |
| 10n | §10 auto-link gate + sticky ResolvedLinks | **Done** | `RequirePerson*` gate; sticky `LinkedObjectId`; toggle-off keeps existing; unit tests |
| 10o | Workspace Linked records tiles from ResolvedLinks | **Done** | Catalog + overview tiles; People tab focus; gated by person-config |
| 10p | Process-complete lock on resolved links | **Done** | `PROCESS_ISSUED` / `REJECTED` / `CANCELLED`; roster + ResolvedLinks immutable; UI lock badge |
| 10q | Overview Issued records (1:N headers) | **Done** | May produce tiles + inline Add/New; `IssuedHeaderNestedCreateController` still sets FK on native nested New |
| 10a | Application workspace UX shell (mock) | **Done** | `ApplicationWorkspaceHost`, Blazor component, Open workspace action |
| 10b | Wire real M2M + SQL views + resolver | **Done** | `ApplicationPerson` M2M, `ApplicationWorkspaceQueryService`, link/unlink toolbar; SQL views deferred (C# tab builder) |
| 10c | Workspace in-tab actions + person SQL view | **Done** | Link/Unlink/Open detail wired in component; `vw_application_workspace_person`; row selection on Person tab |
| 10d | ListView row opens workspace (default drill-in) | **Done** | `ApplicationListViewWorkspaceNavigationController` — row activate → workspace instead of legacy DetailView |
| 10e | Document copies on workspace (roster line) | **Done** | `ApplicationPerson` keyed catalog + ZIP/preview; `DocumentCopiesLineScope`; legacy `ApplicationItem` ListView path retained |
| 10s | Workspace Document copies person filter + person catalog | **Done** | Header chips toggle roster; catalog grouped by person; Preview/package use filtered `Person.ID`s; slot stays viewer-only |
| 10t | Document copies from linked records (ID labels) | **Done** | No Current/Previous/Next ApplicationItem slots; rows are ResolvedLinks labeled Passport/Visa/… number |
| 10u | §10.2 valid/not-expired auto-link gate | **Done** | Officer-only: Passport/Visa/WP/Invitation/BorderZone/Medical must be valid not-expired. VISA2014 import (`IsDataImport`) uses PersonCurrentItems so historical expired rows still link |
| 10f | Profiles rail actions wired | **Done** | Row → profile wizard; `+` → new Application from profile (inherits route from current Application) |
| 10g | Officer UI cutoff (`ApplicationItem` nav/tab/actions) | **Done** | Nav child removed; Person `ApplicationPeople` tab; dossier M2M-only; ListView doc copies disabled |
| 10h | Runtime roster reads → `ApplicationPeople` | **Done** | `ApplicationRosterHelper`; merge/Resminamalar hydration; header AvailablePeople; cancel counts |
| 10i | `Visa.IssuingApplication` dual-read | **Done** | FK + backfill; Path A M2M-first; legacy `IssuingApplicationItem` hidden when app set |
| 10j | Report Dashboard roster SQL + loaders (phase B start) | **Done** | `vw_rd_registration`, `vw_rd_passport`, to-be-checked-in/out; `ReportDashboardRosterQueryHelper`; Travel/Registration on process |
| 10k | Report Dashboard child-link C# filters + `vw_rd_application` | **Done** | Education/Address/Position/Medical Last-N via resolved links + legacy fallback; `vw_rd_application` first person from M2M |
| 10l | Report Dashboard visa extension / work permit SQL | **Done** | `View_VisaExtensionStatus`, `vw_rd_visa_app_progress`, `vw_rd_work_permit_app_progress`, `vw_rd_visa_state`, extension-required CTE; invitation first-person M2M |
| 10m | Report Dashboard ministry + direct-migration SQL | **Done** | `ministry_roster_lines` CTE in 8 embedded views; `ReportDashboardSqlViewResource` placeholder; legacy EF loaders dual-read |
| 11 | Person / Dossier **Start application** | **Done** | 2-step picker from Person + Dossier; M2M link; dossier Applications section |
| 12 | Resminamalar / merge reads profile nested templates | **Done** | Profile nested catalog + `profile:` entry keys; merge via matching `UserReportTemplate` name |
| 13a | Profile-first runtime + cutover prep | **Done** | Capability resolver; nav route criteria; profile-or-type validation; hide Type when profile set |
| 13b | Remove `Application.ApplicationType` FK (schema) | **Deferred** | After import cutover; Report Dashboard SQL, sync rules, PDF mapping remain on Type |
| H0 | HTML officer shell — tokens, router, mock store | **Done** | `wwwroot/officer-shell/` — plan §7 |
| H1 | HTML staged list/grid + Start process merge | **Done** | Mock `startProcess()` → in-process case |
| H2 | HTML in-process list/grid | **Done** | Row → `#/case/:id/overview` |
| H3 | HTML case workspace (6 tabs) | **Done** | overview, people, progress, documents, resminamalar, sla |
| H4 | HTML templates catalog + overview | **Done** | Left rail + Configure |
| H5 | HTML template wizard (5 steps) | **Done** | `#/templates/wizard/{0-4}` → Publish |
| H6 | HTML PNG gallery + README + parity checklist | **Done** | `parity/CHECKLIST.md` — sign-off pending |
| H7 | HTML Person DetailView staging | **Deferred** | Post–v1; People nav = stub only |
| B0 | Blazor officer shell — layout + nav + live queues | **Done** | Native XAF **Application Profiles** folder (staged / in-process / templates / via / direct). Custom left rail removed 2026-08-13 |
| B1 | Blazor shell PNG parity polish | **Done** | Chips, legend, pagination, grouped staged, rich grid, toolbar search |
| B2 | Start process domain merge | **Done** | Merge staged rows, `YYYY-NNNN` process number, first progress step |
| B3 | Immersive shell chrome | **Done** | Custom left rail retired; native accordion is the nav. `:has(.officer-shell-host)` hide unused unless leftover shell opens |
| B4 | Profile templates list/grid + detail | **Done** | PNG catalog, chips, pagination, rail overview drill-in |
| B5 | Case workspace 6-tab shell | **Done** | PNG parity pass: overview, people matrix, progress, inline doc copies + Resminamalar, SLA |
| B6 | Immersive tab-bar hide | **Done** | `OfficerShellImmersiveTabBarController` (`TabsModel.CssClass`) + CSS fallback |
| B7 | Case progress tab wiring | **Done** | Template Approval legs + Process & SLA names; first history row fills first ministry as current (not `1_REVIEW_STARTED` Sequence match) |
| B8 | Custom person link picker | **Done** | Inline picker on People tab (`IApplicationPersonLinkQueryService` + `OfficerShellPersonLinkPickerComponent`; officer shell only) |
| B9 | Native Application Profiles navigation | **Done** | Folder caption; staged/in-process ListViews + Start process; templates in folder; drop custom sidebar |
| R0 | Instance rename — spec freeze (§13) | **Done** | Plan locked; docs + slice tracker |
| R1 | Instance rename — new BOs + empty tables + permissions | **Done** | `ApplicationProfileInstance*` |
| R2 | Instance rename — same-Guid copy updater + FK repoint | **Done** | From `Applications*` |
| R3 | Instance rename — code/OData/import/SQL hard switch | **Done** | No Application OData alias |
| R4 | Instance rename — drop old tables + delete old BOs | **Done** | |
| R5 | Instance rename — officer copy purge | **Done** | Keep “Application Profile” for templates |
| R6 | Instance rename — Demo/local verify + learnings | **Done** | Solution Debug 0 errors; 209 Module.Tests passed; Demo F5/import still operator-run |

---

## Slice 8a — Profile overview (live)

**Delivered (2026-08-14):**

- `ApplicationProfileOverviewQueryService` maps the selected `ApplicationProfile` (identity, company/signatories, required fields + defaults, process states, approval legs, person toggles, nested templates with scope/data) without mock fillers.
- Linked applications are real `ApplicationProfileInstance` rows (newest 25, full count in the heading). Click a number to open case workspace.
- Prototype banner only when the profile id cannot be resolved (designer / missing object space).

**Verify:** Application Profiles → Application Profile Templates → select a profile. No Prototype banner; linked table matches instances or shows empty.

---

## Slice 5 — Seed from ApplicationType (detail)

**Goal:** Every active `ApplicationType` has a matching `ApplicationProfile`; Applications with Type get Profile FK.

**Tasks:**

- [x] `ApplicationProfileSeedUpdater` + `ApplicationProfileSeedSync` — match profile by `Code` (from Type `Code` or name slug)
- [x] Copy: `ProgressRoute`, action family, produce/cancel flags, SLA, person toggles, Require* from Type configuration (`ApplicationProfileFromApplicationTypeMapper`)
- [x] Backfill `Application.ApplicationProfile` from `ApplicationType` on existing rows
- [x] Startup gate when ModuleUpdater skipped (`ApplicationProfileSeedGate` in Blazor `Startup.Configure`)
- [ ] Officer verify after restart: Configuration → Application Profiles populated; Applications list **Application Profile** column filled

**Out of scope for slice 5:** wizard UX, M2M, removing Type FK.

---

## Slice 6 — Appearance / progress (detail)

**Goal:** Runtime behavior reads `Application.ApplicationProfile` first; Type is fallback only until slice 13.

**Tasks:**

- [x] Audit grep: `ApplicationType`, `ShowRegistration`, `ShowTravel`, `ApplicationProgressRoute`, etc.
- [x] Central helper: `ApplicationProfileConfigurationResolver` — profile-first, type fallback
- [x] Update `[Appearance]` criteria on `Application` / `ApplicationItem` via `Cfg*` computed properties
- [x] `ApplicationProgressProfileResolver` + route helper — profile route, embedded legs, migration SLA
- [x] Unit tests for resolver precedence (`ApplicationProfileConfigurationResolverTests`)

---

## Slice 8c — Custom catalog home (detail) — **Done**

**Goal:** Officers never land on native `ApplicationProfile_ListView` / `ApplicationProfile_DetailView` from Configuration nav, New, or row activate.

**Delivered:**

- `ApplicationProfileCatalogHost` + Blazor catalog (search, **Total: N** via `Grid.TotalCount`, badges, New / Configure / **list first**, row → overview, **Back to list**)
- Nav: `ApplicationProfileCatalogModelUpdater` + `ApplicationProfileCatalogNavigationController` (Configuration → catalog DetailView)
- `[NavigationItem(false)]` on `ApplicationProfile`; strip stale list nav
- ListView intercepts: row → overview; New → create + wizard
- Overview **Configure profile** CTA → wizard

**Verify:** Configuration → Application Profile → catalog → open row → overview → Configure → wizard; New profile → wizard.

---

## Slice 10a — Workspace mock UI (detail) — **Done**

**Goal:** Officer can open custom Application workspace DetailView with layout from `process-started-application-profile-workspace-mockup.png` and hard-coded mock rows.

**Delivered:**

- `ApplicationWorkspaceHost` + `ApplicationWorkspaceHost_DetailView`
- `IApplicationWorkspaceQueryService` + `ApplicationWorkspaceMockQueryService`
- `ApplicationWorkspaceComponent.razor` + `application-workspace.css`
- **Open workspace** action on Application ListView / DetailView
- Pending-open gate for Blazor URL sync (`IApplicationWorkspacePendingOpen`)

**Verify:** Applications list → select row → **Open workspace** (View category).

---

## Slice 8 — Configuration wizard UX (detail) — **Done**

**Goal:** Officer configures an `ApplicationProfile` via a guided wizard instead of scattered nested tabs.

**Delivered:**

- `ApplicationProfileWizardHost` + `ApplicationProfileWizardHost_DetailView`
- `IApplicationProfileWizardSession` + `IApplicationProfileWizardPendingOpen` (Blazor DI)
- `ApplicationProfileWizardComponent.razor` + step partials + `application-profile-wizard.css`
- **Configure profile** action on Application Profiles ListView (saved rows only)
- Respects `ApplicationProfileLockHelper` — read-only banner + disabled save when locked
- Steps: Identity · **Company, Signatories** · Results & fields · Process & SLA (embedded legs) · Templates & person · Review & save
- **May produce** / **May cancel** live under Identity **Related to** (`ActionFamily`): Issuance → produce; Cancellation → cancel
- **Approval legs** live under Identity **Directed to**; visible only for Via ministry
- Results default-value lookups load as ID/name snapshots (`ApplicationProfileWizardLookupData`); Default value is enabled only when Use is checked

**Deferred (later slices):** template file upload in wizard (attach binary on standard profile detail nested templates ListView); progress resolver reading `ProgressStateSettings` (stored in wizard, wire in application-progress slice).

**Slice 8b — Wizard prototype parity (2026-08-07):** Step 1 scope cards · Step 2 defaults/signatory table · Step 3 ministry/migration state checklists (`ApplicationProfileProgressStateSetting`) · Step 4 template add/edit/remove in wizard.

**Verify:** Configuration → Application Profiles → select row → **Configure profile**; edit and **Save profile**; locked profile → read-only + **Clone** escape hatch.

---

## Slice 9 — Profile picker at Application create (detail) — **Done**

**Goal:** New Application starts with profile selection (live FK + defaults), not a blank form.

**Delivered:**

- `ApplicationProfilePickerHost` + `ApplicationProfilePickerHost_DetailView`
- `IApplicationProfilePickerQueryService` — active profiles, route filter, MRU sort, applicability criteria
- `ApplicationProfilePickerNewController` — intercepts **New** on Application ListViews (skipped during data import)
- **Use profile (live link)** creates Application, sets `ApplicationProfile` + dual-read `ApplicationType`, applies defaults, opens DetailView
- Locked profiles remain selectable (config lock badge only)

**Verify:** Applications list → **New** → picker → select profile → **Use profile** → DetailView with profile read-only and defaults filled.

**Next:** Slice 13b — drop `Applications.ApplicationTypeID` after import cutover (Report Dashboard, sync rules, PDF mapping).

---

## Slice 12 — Resminamalar profile templates (detail)

**Delivered (2026-08-07):**

- When `Application.ApplicationProfile` has nested templates → Resminamalar catalog lists **only** those rows (sorted by `SortOrder`)
- Entry keys `profile:{ApplicationProfileTemplate.Id}`; merge resolves matching `UserReportTemplate` by **TemplateName** (same Word/Excel pipeline)
- Readiness: unlinked profile template → `ProfileTemplateUnlinked`; otherwise reuses user-template evaluator + dry-run hints
- Legacy `UserReportTemplate` visibility path unchanged when profile has **no** nested templates (dual-read)
- PdfForm profile templates excluded from Resminamalar catalog

**Verify:** Configure profile nested templates (names match User Report Templates) → Application Resminamalar shows profile list → preview/ZIP works.

---

## Slice 11 — Person / Dossier Start application (detail)

**Delivered (2026-08-07):**

- **Start application…** on Person DetailView and Person Dossier toolbar
- Extended profile picker: 2-step flow (profile → multi-select people) when `SeedPersonId` is set
- `ApplicationStartFromPersonHelper` — candidates, validation (via-ministry ProjectContract gate, audience, duplicate-open warn, incomplete flag)
- MRU profile sort + per-person usage badges in picker
- Create links people via `ApplicationPersonService`; Person start opens Application DetailView; Dossier start returns to dossier + toast
- Dossier **Applications** section uses `ApplicationPeople` M2M (legacy `ApplicationItem` fallback removed in slice 10g)

**Verify:** Person → **Start application…** → profile → people → create. Dossier → same → stays on dossier with success toast.

**Next:** Slice 10 close-out phase B — BO/schema removal (or 13b after import).

---

## Slice 10g — Officer UI cutoff (detail)

**Delivered (2026-08-08):**

- Removed **ApplicationItem** sub-nav under Applications (via ministry / direct migration).
- Person issued-documents tab: **Applications (linked)** via `ApplicationPeople` M2M (replaces `ApplicationItems` tab).
- Dossier Applications section: `ApplicationPeople` only (no `ApplicationItem` fallback).
- Disabled legacy **Document copies** on `ApplicationItem` ListView.

**Still retained (phase B):** `ApplicationItem` BO/table, Report Dashboard Registration/Travel SQL, Resminamalar item merge, VISA2014 import, sync rules.

**Verify:** Application nav has no Application items child; Person detail → Applications (linked); workspace document copies unchanged.

**Next:** Slice 10 close-out phase B — hard-remove BO after import/report migration.

---

## Slice 10h — Runtime roster reads (detail)

**Delivered (2026-08-08):**

- `ApplicationRosterHelper` — M2M-first roster reads with legacy `ApplicationItem` fallback.
- Resminamalar / Word merge via hydrated `ApplicationPerson` projections.
- Header BO `AvailablePeople`, person validation, passport defaults on item BOs.
- Application cancel counts + ListView person-count preload use roster helper.

**Verify:** M2M-only application — Resminamalar rows populate; invitation person picker lists linked people.

**Next:** Phase B continues — remaining Report Dashboard views, sync rules, then `ApplicationItem` BO removal after import.

---

## Slice 10j — Report Dashboard roster SQL (detail)

**Delivered (2026-08-08):**

- `ReportDashboardPostgresRosterSql` — shared M2M + legacy `ApplicationItems` SQL fragments.
- PostgreSQL views: `vw_rd_registration`, `vw_rd_passport`, `vw_rd_to_be_checked_in`, `vw_rd_to_be_checked_out`.
- `ReportDashboardRosterQueryHelper` — Travel, Registration on process, overview passport/address/travel counts.

**Verify:** Restart app (DB updater recreates views). Registration / Passport / Travel panels on mixed M2M + legacy DB.

**Next:** Remaining `vw_rd_*` on `ApplicationItems` (visa extension, work permit app progress); `SyncRulesUpdater`; hard-remove `ApplicationItem` BO.

---

## Slice 10k — Report Dashboard child-link C# filters (detail)

**Delivered (2026-08-08):**

- `ReportDashboardRosterQueryHelper.GetLinkedChildIdsInApplicationDateRange` — M2M `ApplicationPersonResolvedLink` + legacy `ApplicationItem` fallback for Education, Address, Position, Medical.
- `ReportDashboardQueryService` — Last-N filters for Education (view + legacy), Position history, Address of residence, Medical record.
- `vw_rd_application` — first person from `ApplicationPeople` (legacy `ApplicationItems` only when no M2M roster); fixed corrupted `ProgressStateCode` SQL line.

**Verify:** Report Dashboard Education / Address / Position / Medical panels on apps with M2M roster only; `vw_rd_application` preview shows correct person name.

**Next:** Visa extension / work permit progress SQL views; sync rules; hard-remove `ApplicationItem`.

---

## Slice 10l — Report Dashboard visa extension SQL (detail)

**Delivered (2026-08-08):**

- `ReportDashboardPostgresRosterSql` — visa/work-permit extension roster CTEs, `View_VisaExtensionStatus`, `vw_rd_visa_app_progress`, `vw_rd_work_permit_app_progress`, `vw_rd_visa_state`, `unfinished_extension_people`, first-person lateral join.
- `IssuedVisaID` dual-read: `IssuingApplicationItemID` or `IssuingApplicationID` + passport match (slice 10i).
- `vw_rd_visa_extension_required` — unfinished-extension people from M2M roster.
- `vw_rd_invitation_in_process` / `vw_rd_invitation_rejected` — first person from `ApplicationPeople`.
- `ReportDashboardRosterQueryHelper.ApplicationIdsWithPersonRole` — invitation in-process role filter.

**Verify:** Restart app (DB updater). Visa Extension status list, On Extension / Extension Required panels, Work Permit extension progress on M2M-only apps.

**Next:** `SyncRulesUpdater`; hard-remove `ApplicationItem` BO (post-import).

---

## Slice 10m — Report Dashboard ministry SQL (detail)

**Delivered (2026-08-08):**

- `CteMinistryRosterLines` + `{{MINISTRY_ROSTER_CTE}}` placeholder expanded in `ReportDashboardSqlViewResource.Load`.
- Embedded PostgreSQL views: invitation/visa-extension/other on-process + completed bases, direct-migration on-process + complete (8 files).
- `ReportDashboardQueryService` — ministry invitation legacy loader uses `ApplicationRosterHelper.GetMergeLineItems`; Application role filters include `ApplicationPeople`.

**Verify:** Report Dashboard → Application (via ministry) sub-reports on M2M-only applications; Open ListView row counts match preview.

**Next:** `SyncRulesUpdater`; `ApplicationItem` BO removal.

---

## Slice 10d — ListView opens workspace (detail)

**Delivered (2026-08-07):**

- `ApplicationListViewWorkspaceNavigationController` intercepts ListView row open (all Application lists) and shows **Application workspace** instead of `Application_DetailView`.
- **New** → profile picker → workspace (slice 9); **row open** → workspace (10d); **Open workspace** toolbar action remains for legacy DetailView tabs.

**Verify:** Applications (via ministry) → double-click row → workspace opens with live data.

---

## Slice 10b — Wire real M2M (detail)

**Delivered (2026-08-07):**

- `ApplicationPerson` + `ApplicationPersonResolvedLink` BOs, EF + `ApplicationWorkspaceSchemaSql`
- `ApplicationPersonRoster` services (`LinkPerson`, resolver, valid-item rules)
- `ApplicationWorkspaceQueryService` + `ApplicationWorkspaceTabBuilder` (live tabs from resolved links + profile toggles)
- DI: real query service registered in `Startup.cs`; mock retained for fallback
- `ApplicationWorkspacePersonController` — **Link person** / **Unlink person** on workspace DetailView
- Permissions for `ApplicationPerson` / resolved links in `Updater.cs`
- Prototype banner hidden when `IsPrototypeMock == false`

**Deferred:** additional `vw_application_workspace_*` views for child tabs (C# tab builder remains canonical for v1); hard-remove `ApplicationItem`.
**Prototype gates:**

| Gate | Artifact |
|------|----------|
| Wizard steps match plan §6 E–H groups | `application-profile-template-wizard*.png` |
| Staged → in-process lifecycle | `staged-profiles-*.png`, `process-started-profiles-*.png` |
| No “clone profile” language | Refresh `images/ap-04-lifecycle.png` when UX ships |

---

## VISA2014 migration waves (Application Profile catalog)

| Wave | Status | Doc |
|------|--------|-----|
| 0b | **Done** | [APPLICATION_PROFILE_CATALOG_WAVE0.md](../../../docs/VISA2014_MIGRATION/APPLICATION_PROFILE_CATALOG_WAVE0.md) |
| 1 | **Done** | Tenant `application-profile.calik-energi.json` |
| 2 | **Done** (local) | `Application-Profile.ps1` patch |
| 3 | **Done** (local) | [APPLICATION_PROFILE_CATALOG_WAVE3.md](../../../docs/VISA2014_MIGRATION/APPLICATION_PROFILE_CATALOG_WAVE3.md) — 637 nested templates patched |

---

## Open questions (carry from plan §2.6, §10.5)

| ID | Topic | Status |
|----|--------|--------|
| A | Unlock profile when no apps ≥ lock A | Open — recommend auto-unlock |
| B | Required-to-save vs visible | Open |
| C | Placeholder derive vs constrain | Open |
| D | Temporary visitor v1 | Open |
| E | TravelHistory valid rows | Open — current/latest vs broader |
| F | Re-sync Excel draft in repo | Open — attach updated workbook |
| G | Wide roster mandatory columns | Open |

Resolve in plan §2 before implementing dependent slices; log decisions in learnings.md.

---

## Verification checklist (any slice)

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual (officer path):

1. Configuration → Application Profiles — create/edit
2. New Application — profile pick + defaults
3. Progress past office prep — profile config lock
4. Existing Application — per-App fields still editable; profile read-only on detail
