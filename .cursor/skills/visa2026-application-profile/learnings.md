# Application Profile — learnings (append-only)

Read **before** Application Profile work; **append** after verified fixes and slice completions. Promotion rules: [MATURITY.md](./MATURITY.md).

---

### 2026-08-14 — Approval letter links hide raw filenames

- Overview/Progress showed the uploaded file name (`AI-SDLC-…pdf`). Officers only need a link. The control now shows **View letter**; the real name stays on `title` (hover) and in the preview-slot header.
- Verify: stop F5, rebuild, F5. Open 8/-005 Overview — ministry steps show “View letter”, not the file name. Click still opens the side preview.
- Cross-skill: visa2026-preview-slot

### 2026-08-14 — Issued / Rejected / Cancelled progress tones are not all green

- Overview and Progress treated every finished node as `done` (green check). Migration **Rejected** looked the same as **Approved**.
- Steps now carry `OutcomeKind` from the progress state code (`PROCESS_ISSUED` / `PROCESS_REJECTED` / `PROCESS_CANCELLED` / `*_REVIEW_REJECTED`). CSS uses `BO_STATE_COLORS.md` hex: issued forest green, rejected salmon, cancelled firebrick; approved stays mint. Header badge follows the terminal outcome instead of always “In process”.
- Verify: stop F5, rebuild, F5. Open 8/-005 Overview — Approved ministries stay green; Migration Rejected is red with ✕; Issued cases use a darker green Issued badge.
- Cross-skill: visa2026-bo-state-colors | visa2026-preview-slot

### 2026-08-14 — Completed Progress steps keep ministry letter preview links

- After Advance past a ministry (or when Migration is Issued), the Progress tab and Overview hid approval PDFs because `MinistryLetterFileName` was only filled for the **current** step, and completed nodes are collapsed.
- Timeline now keeps the uploaded filename on done ministry legs. Progress and Overview show a clickable name that opens `#visa-preview-slot` (`OpenPreviewOnly`). Chrome current-step text uses the last done step when nothing is current (Issued, not Office preparation).
- Verify: stop F5, rebuild, F5. Open 8/-006 Progress/Overview — each ministry with an uploaded letter shows its filename; click opens the side preview.
- Cross-skill: visa2026-preview-slot

### 2026-08-14 — Progress ministry letter filename opens side preview

- Workspace Progress filename was a new-tab download. It now opens `#visa-preview-slot` with `ProgressLettersSlotRequest.OpenPreviewOnly` (viewer only, same as Resminamalar / Document copies from their tabs). Close preview closes the slot.
- Prevent: Do not use `/api/application-progress/.../ministry-letter` as the officer preview path from the case workspace.
- Cross-skill: visa2026-preview-slot

### 2026-08-14 — Progress tab nodes follow the Application Profile template

- After Advance from office, history showed Submitted (`1_REVIEW_STARTED` / "Sent for agreement") but Türkmenenergo stayed Pending, so the second Advance had no current ministry step. Slots were matched to `N_REVIEW_*` against snapshot/DB Sequence, not the template's approval-leg order. Labels came from `ApplicationState`, not Process & SLA (Submitted / Approved).
- The case workspace line is now Office → profile **Approval legs** in display order → Migration. Template legs win over snapshots. First ministry-track history row fills the first leg as current with the Process & SLA name (Submitted). Advance options on that node are the next included template states (Approved, …).
- Officer-facing steps do not use the ApplicationProgress transition list. History rows may still store `ApplicationState.Code` for import/list compatibility.
- Verify: stop F5, rebuild, F5. Open 8/-004 after the office Advance. Türkmenenergo is current with Submitted + date; Next step offers Approved (and other included ministry states). Advance moves that ministry to Approved.

### 2026-08-14 — Advance progress from office with embedded profile legs

- Clicking Advance on 8/-004 (via-ministry template with three embedded approval legs, empty history) did nothing. Save notes worked. Validation still required the old `ApprovalLegProfile` lookup even when `ApplicationProfile.ApprovalLegs` were present, and the first `1_REVIEW_STARTED` row also required the tenant `MinistryReviewSlaSettings` singleton instead of profile `MinistrySlaDays`. The right-rail Advance with multiple next steps only switched to the Progress tab, so a second click on Progress was a no-op. Failures set a status message without reloading, so the banner often never appeared.
- Advance now treats embedded profile legs / snapshots as configured ministries, accepts profile ministry SLA days, loads those collections before validate, and writes the new history row. Rail Advance on the Progress tab actually advances. Errors reload the workspace so the banner shows.
- Verify: stop F5, rebuild, F5. Open 8/-004 (or any via-ministry instance at office). Next step Submitted → Advance. Office becomes completed; first ministry is current with state + date. If something is still blocked, a warning banner appears.

### 2026-08-14 — Progress line shows predetermined approval legs + migration

- Overview only showed Office preparation on a via-ministry instance (8/-004) because the timeline listed implied office plus real history rows — pending ministry legs were omitted.
- The line is now Office preparation → one node per profile/snapshot approval leg → Migration service. Empty legs stay Pending (no date). When a progress row exists for that leg, the node turns current/done, shows the current state name, and the change date. Migration stays pending until `PROCESS_STARTED`. Process & SLA included states filter Advance options, not extra nodes.
- Verify: stop F5, rebuild, F5. Open a via-ministry instance with three legs and empty history. Overview shows Office (in progress) plus three pending ministries plus pending Migration. After Advance to first ministry, that node is active with state + date; later legs stay pending.

### 2026-08-14 — Workspace Progress tab uses real history + implied office

- The Progress tab always drew four fake buckets (Office / Ministry / Migration / Complete) and mapped dates by index. Empty history looked like ministry review. Save notes failed with "No progress history". Rail Advance ignored the next-step dropdown.
- Timeline is now implied **Office preparation** plus one step per real `ApplicationProfileInstanceProgress` row. Office notes persist on `OfficePreparationNotes` (host-start `ADD COLUMN IF NOT EXISTS`) and copy onto the first real row on advance. SLA uses ministry/migration helpers when those steps are current; otherwise profile `MinistrySlaDays` / `MigrationSlaDays`. Chrome current step is Office preparation when history is empty. Rail Advance with multiple next steps opens the Progress tab.
- Verify: stop F5, rebuild, F5 (`FORCE_XAF_DB_UPDATE` not required). Open an instance with empty history (e.g. B/-002). Progress shows only Office preparation; Save notes works; SLA days come from the profile; Advance creates the first real step and Office becomes completed.

### 2026-08-14 — Template overview shows wizard configuration

- The read-only Application Profile Templates overview only listed produce/cancel, SLA days, a few lookup defaults, legs, and person chips. Wizard steps (selection code, applicability, Company/Signatories, required date/region fields, included progress states, template scope/data) were missing.
- Overview now maps those fields from the live profile (eager-load nested collections) and Configuration singletons. Nested templates show Type, Scope, and Data.
- Verify: stop F5, rebuild, F5. Templates → open a configured row. Overview matches Configure profile: identity, company/signatories, all Use fields, process states, templates.

### 2026-08-14 — Save profile refreshes the Templates catalog

- After **Save profile**, the Application Profile Templates table stayed stale in its MDI tab (wizard opened with `TargetWindow.Current` and replaced the catalog, or an already-open catalog did not reload).
- Wizard save now calls `IApplicationProfileCatalogReload`. The catalog editor reloads rows. New / Configure open the wizard in a **new tab** so the list stays mounted and can refresh.
- Verify: stop F5, rebuild, F5. Templates → New or Configure → change name → Save profile. The Templates tab Total and row list update without reopening.

### 2026-08-14 — Wizard Save profile did not write to the database

- **Save profile** showed "Profile saved." while `CommitChanges` was a no-op: Blazor `@bind` edits were not in XAF `ModifiedObjects`, so the ObjectSpace skipped SaveChanges. A reused session ObjectSpace from a previous profile could also load the wrong row. Nested templates were created with only the FK, so Review showed Templates: 0. Catalog collapse hid a new type-only row that reused a seed Code.
- Save now `DetectChanges` + `SetModified` then commit, and surfaces unique-Code errors. Wizard binds the live profile from the PropertyEditor (not a second DI session). New nested rows are added to `NestedTemplates` / `ApprovalLegs`. Catalog lists every type-only profile even when Code matches a seed.
- Verify: stop F5, rebuild, F5. New Application Profile → set a unique Code and name → Save profile. Close the wizard, reopen Application Profile Templates — the new row is there. Re-open Configure — name/code/templates match.

### 2026-08-14 — Templates catalog scrolls inside the table border

- Page scroll (outside the table) moved TEMPLATE/CODE headers with the rows. `height: 100%` on XAF layout groups never became a real height, so `.ap-catalog__table-wrap` grew with 33 rows and sticky `thead` had nothing to stick to (`overflow: hidden` on `.ap-catalog` was the sticky containing block).
- Cap `.ap-catalog-detail` to `calc(100svh - 7.5rem)` (header + MDI tabs). Table wrap `flex: 1 1 0%`, `overflow: auto`, fallback `max-height: calc(100svh - 13.5rem)`. Sticky `thead` + `th`.
- Verify: hard-refresh Application Profile Templates. Search / Total / New stay put. Scroll inside the table border; column headers stay. Page does not scroll.

### 2026-08-14 — Templates catalog shows Total like Person ListView

- Application Profile Templates is a custom Blazor catalog, not a DxGrid ListView, so `ListViewTotalCountController` never ran. Toolbar now shows `VisaUiMessages.Format("Grid.TotalCount", Rows.Count)` (`Total: N`) to the right of search, matching Employees. Count follows the search filter.
- Verify: stop F5, rebuild, F5. Configuration → Application Profile Templates — toolbar shows Total next to search; typing in search updates the number to the visible row count.

### 2026-08-14 — Unlinked templates can be deleted

- Application Profile Templates had no delete action (catalog chrome hides native XAF Delete). Officers can delete a template when **Linked** is 0 (any `ApplicationProfileInstance` FK, not only staged/in-process). Linked rows stay undeletable. Confirm on the list row or overview, then reload.
- Verify: stop F5, rebuild, F5. Templates with Linked 0 → Delete → Confirm. Templates with Linked ≥ 1 have no Delete.

### 2026-08-14 — Approval legs sit under Directed to

- Legs apply only when **Via ministry**. They now live on Identity & purpose under **Directed to**. **Direct migration** hides the list and deletes embedded legs. Process & SLA keeps ministry/migration states and SLA days.
- Verify: stop F5, rebuild, F5. Identity → Via ministry shows Approval legs; Direct migration hides them. Process & SLA has no legs section.

### 2026-08-14 — May produce / May cancel sit under Related to

- Those sections belong with **ApplicationProfileInstance related to** (`ActionFamily`), not on Results & fields. **Issuance** shows May produce; **Cancellation** shows May cancel existing; Registration / Business trip show a hint only. Switching family clears the hidden flags.
- Verify: stop F5, rebuild, F5. Identity & purpose → Issuance shows May produce; Cancellation shows May cancel; Results & fields is required properties only.

### 2026-08-14 — Results default value is gated by Use

- Use (`Require*`) is what shows the property on ApplicationProfileInstance. Default value (and Has default) are disabled unless Use is checked and the profile is editable. Catalog rows still load so the list is ready when Use is turned on.
- Verify: stop F5, rebuild, F5. Configure profile → Results & fields — with Use off, lookup Default value is disabled; check Use → dropdown is selectable.

### 2026-08-14 — Results default-value lookup dropdowns were empty

- Default-value `<select>`s were `disabled` until **Has default** was checked, so officers only saw `—` and could not open the list. Catalogs were also loaded lazily from the profile ObjectSpace (`GetObjectsQuery` + `VisaTypes.Count == 0`).
- Load catalogs in the PropertyEditor via a dedicated ObjectSpace (`GetObjects`) into ID + display-name snapshots (`ApplicationProfileWizardLookupItem`). Keep the dropdown enabled whenever the profile is editable; choosing a value sets the default FK via the profile ObjectSpace. **Has default** still clears or picks the first catalog row.
- Region (city), business trip address, and work permit location stay Use-only (no `Default*` FKs on `ApplicationProfile`).
- Verify: stop F5 (DLL lock), rebuild, F5. Configure profile → Results & fields → Visa type / category / period / migration service / project / urgency / entry check point Default value lists catalog rows. Pick one, Save profile.

### 2026-08-14 — Results & fields no longer lists signatory / representative

- Those belong on **Company, Signatories** (live Configuration). The Results step dropdowns duplicated them as profile defaults.
- Verify: Configure profile → Results & fields has no Authorized signatory / Visa representative row or section.

### 2026-08-14 — Company, Signatories is a live Configuration reference

- The first wizard step bound `GetOrCreateInstance` in the **profile** ObjectSpace and saved those rows with **Save profile**. That looked like a copy: opening the wizard again did not prove a live link, and profile save could dirty/create org rows.
- Step is now **read-only**. Values load via `TryGetInstance` in a **separate** ObjectSpace (`ApplicationProfileWizardOrganizationSnapshot`). **Edit in Configuration** opens the real Company / Signatory / Representative DetailView. **Refresh from Configuration** (and each step change) re-reads. **Save profile** no longer writes those BOs.
- Verify: stop F5, rebuild. Configure profile → Company, Signatories is display-only. Change Configuration → Company, Refresh (or change step) → wizard shows the new name. Resminamalar still merges `OrganizationReportHelper.TryGetInstance`.

### 2026-08-14 — Wizard step Company, Signatories is live tenant org

- Officers asked to include Company / Authorized Signatory / Authorized Representative on Application Profile Template configuration. These stay **organization singletons** (Configuration nav); do not add FKs on `ApplicationProfile`.
- New wizard step **Company, Signatories** (after Identity) edits `CompanyProfile.GetOrCreateInstance` / signatory / representative in the wizard ObjectSpace. **Save profile** commits them with the profile. Review shows the three names. Step 2 dropdowns remain instance-create defaults only.
- Verify: stop F5, rebuild, Configure profile → step 2 shows company/signatory/rep fields; Save; Configuration → Company reflects the same values.

### 2026-08-14 — Templates catalog: scroll only the table, not the page

- `calc(100dvh - 10rem)` on `.ap-catalog` was taller than the XAF content pane (header + MDI tabs), so the **page** still scrolled and the table had no inner scrollbar.
- Catalog host now uses the same fill chain as other host DetailViews (`ap-catalog-detail` + `xaf-fill-root` / `xaf-fill-available`). Overflow is hidden on the view/layout; only `.ap-catalog__table-wrap` scrolls. Overview scrolls inside `.ap-catalog__detail-page`.
- Verify: stop F5, rebuild, F5. Application Profile Templates — window does not scroll; search + New stay put; rows scroll in the table.

### 2026-08-14 — Templates catalog ListView scrolls inside the table

- `overflow: auto` on `.ap-catalog__table-wrap` did nothing because the wrap had no height cap; the table grew and the XAF page scrolled.
- List page now fills `calc(100dvh - 10rem)`; toolbar stays put; table wrap `flex: 1; min-height: 0; overflow: auto`; sticky `thead`. Overview is unconstrained.
- Verify: hard-refresh Application Profile Templates. Search + New stay visible; rows scroll in the table; column headers stick. Overview + Back to list still work.

### 2026-08-14 — Templates catalog is list then overview (not split)

- Officers asked for the same pattern as other ListViews: do not show the profile list and overview side by side. Catalog `LoadAsync` was auto-selecting the first profile, which always opened the split shell.
- List page is a full-width table; row click loads overview in place; **Back to list** returns. Contract-clone collapse is unchanged.
- Verify: stop F5, rebuild, Application Profile Templates → table only; click a row → overview; Back to list → table.

### 2026-08-14 — Templates rail looked like duplicate profiles

- Local DB: 159 `ApplicationProfiles`, 25 distinct `Code`s. Wave 0b via-ministry clones share Code + SelectionCode and differ by `DefaultProjectContract` (e.g. SelectionCode `201` × 8, `402` × 26). Catalog CSS ellipsis hides the `(contract)` suffix, so the rail looks duplicated.
- Do not delete those rows — import matching still uses Code + contract. Officer Templates catalog and create picker now collapse to one row per Code+SelectionCode (`ApplicationProfileOfficerCatalogSelector`), preferring the type-only profile.
- Verify: stop F5, rebuild, Application Profile Templates — each SelectionCode once (about 25 rows), not 8 copies of 20.1 / 26 copies of 402.

### 2026-08-14 — Application Profile Templates overview is live

- Catalog overview was still `ApplicationProfileOverviewMockQueryService`: `IsPrototypeMock = true` even after `MapFromProfile`, mock legs/templates/defaults/toggles, and fake linked numbers (`12/-7010`). That is what showed the Prototype banner.
- Live service is `ApplicationProfileOverviewQueryService`. Linked rows come from `ApplicationProfileInstance` (caption + `ApplicationDate` + latest progress). Empty sections stay empty. Banner only when the profile id cannot be resolved. Click a linked number to open case workspace (`ApplicationWorkspaceOpenHelper`).
- Verify: stop F5 (DLL lock), rebuild, Application Profiles → Application Profile Templates → pick a seeded profile. No Prototype banner; linked table matches real instances (or empty). Configure profile still opens the wizard.

### 2026-08-14 — Overview Issued records (1:N create)

- Linked records stay skip-nav person data. **Issued records** is a separate Overview card for 1:N headers (Invitation / WorkPermit / BorderZone / Rejection / IssuedVisas). Tiles follow May produce (`ShowInvitations` … `ShowIssuedVisas`). Empty tile expands an inline panel; **New** opens a modal DetailView with the issuing FK set (`ApplicationWorkspaceIssuedHeaderOpenHelper`). Clicking an existing row opens that header. Rail **Issue record…** focuses the first empty tile.
- Do not mix InvitationItem / WorkPermitItem tiles into this card — those remain Linked records / People & links.
- Verify: stop F5 (DLL lock), rebuild, open an in-process case whose template has May produce on → Overview shows Issued records; New invitation saves with this instance as FK.

### 2026-08-13 — ListView row opened the old workspace cards, not case Overview

- `ApplicationListViewWorkspaceNavigationController` correctly opens `ApplicationWorkspaceHost`. The host still rendered the prototype card layout (`ApplicationWorkspaceComponent`: progress table + "profile used by Application").
- That host now embeds `OfficerShellCaseWorkspaceComponent` (Overview / People & links / Progress / Document copies / Resminamalar / SLA). Caption **Case workspace**. Native XAF accordion stays.
- Verify: stop F5, rebuild, open Direct migration ListView, click `AI-001` → case summary / stepper / linked records, not the old three-card workspace.

### 2026-08-13 — Only Application Profile Templates showed under Application Profiles

- Cause: list clones still used source id `Application_ListView`. After the instance rename XAF generates `ApplicationProfileInstance_ListView`, so EnsureListView returned null and staged / in-process / via / direct nav items were never created. Templates uses a DetailView host, so it appeared alone.
- Fix: resolve source as `ApplicationProfileInstance_ListView` then fallback `Application_ListView`. Clone route ListViews before the Person_ListView early-return.
- Verify: stop F5, rebuild, accordion should list five children.

### 2026-08-13 — Native XAF Application Profiles nav; custom left rail removed

- Folder id stays `"Application"` (security paths). Caption is **Application Profiles** (`Model.DesignedDiffs.xafml` + `CustomNavigationUpdater`).
- Children (index order): **Staged profiles**, **In process**, **Application Profile Templates** (catalog moved off Configuration), **Application Profile Instances (via ministry)**, **Application Profile Instances (direct migration)**. Spelling is **ministry**.
- Staged / in-process are `Application_ListView_*` clones with `OfficerShellApplicationFilters` criteria. **Start process** is `ApplicationStagedStartProcessController` on the staged ListView (same `OfficerShellStartProcessService` merge). Row activate still opens `ApplicationWorkspaceHost`.
- Custom `<aside class="os-sidebar">` removed. `OfficerShell` nav item stripped; do not intercept caption **Application Profiles** (that is the folder). Users Allow staged/in-process/catalog; Deny leftover OfficerShell. VisaOffice Allow Application folder + catalog only.
- Verify: Module + Blazor compile (solution copy failed while F5 locked `Visa2026.Blazor.Server` DLLs). Stop F5, rebuild, confirm accordion children and no custom left bar.

### 2026-08-13 — Issued visas + Rejection headers are 1:N; May produce Rejection

- Input linked visas stay skip-nav `ApplicationProfileInstance.Visas`. **Issued** visas (new visa and visa extension) are 1:N `IssuedVisas` ↔ `Visa.IssuingApplicationProfileInstance` (same FK as before; `WithMany(IssuedVisas)` instead of empty). Tab visible when May produce visa **or** invitation (`ShowIssuedVisas`).
- Rejection header was already 1:N; visibility now follows new **`ProduceRejection`** (wizard May produce), not `RequirePersonRejectionItem`. Person RejectionItem auto-link still uses `RequirePersonRejectionItem`.
- Nested New sets issuing FK for Rejection and Visa. Schema heal `ADD COLUMN IF NOT EXISTS "ProduceRejection"`.
- Verify: `Visa2026DbContextModelTests` + `ApplicationProfileConfigurationResolverTests`. Rebuild + F5 for column heal.

### 2026-08-13 — Invitation / WorkPermit / BorderZone headers are 1:N, not skip-nav

- InvitationItem / WorkPermitItem / BorderZoneItem stay skip-nav M2M (existing issued items on the roster).
- Output headers Invitation / WorkPermit / BorderZone are **one-to-many**: instance has many; child FK `ApplicationProfileInstance`. `[Aggregated]` + `[InverseProperty]` on the instance collections. EF fluent `HasOne.WithMany` (Invitation/WorkPermit optional; BorderZone required). Visa issued stays `IssuingApplicationProfileInstance` `HasOne` + `WithMany()`.
- Visibility on the instance DetailView is **May produce** (`ProduceInvitation` / `ProduceWorkPermit` / `ProduceBorderZone` → `CfgShowInvitations` / `CfgShowWorkPermits` / `CfgShowBorderZones`). Lookup filters on the header BOs use the same flags.
- Cause of the bug: dropping `[Aggregated]`/`[InverseProperty]` made EF invent skip-nav join tables. Heal no longer creates `"ApplicationProfileInstanceInvitations|WorkPermits|BorderZones"`; it **DROP TABLE CASCADE** leftovers. Nested `{Header}_ApplicationProfileInstances_ListView` removed (headers have no skip-nav collection). English `Application_DetailView` now has a BorderZones tab (Appearance hides it when May produce is off). Nested New on those lists sets the issuing FK only (`IssuedHeaderNestedCreateController`); skip-nav dual-write `SyncIssuedHeader` removed.
- Verify: `Visa2026DbContextModelTests` passed (join types null; FK principal-to-dependent is the collection). Rebuild + F5 so heal drops mistaken joins.

### 2026-08-13 — WorkDuty skip-nav M2M with ApplicationProfileInstance

- WorkDuty had no skip-nav join. `LinkKind.Position` remains **EmployeePositionHistory** (ShowCurrentWorkDuty gate). New `LinkKind.WorkDuty = 12` for Gelmeginiň Maksady.
- Same pattern as MedicalRecord: `WorkDuty.ApplicationProfileInstances` ↔ hidden `ApplicationProfileInstance.WorkDuties`. Join `"ApplicationProfileInstanceWorkDuties"`. Heal backfills kind 12. LinkPerson auto-link + dual-write; UnlinkPerson removes. Nested list browse-only. Pdf hydrator sets `CurrentWorkDuty` from sticky link.
- Verify: `Visa2026DbContextModelTests` passed. F5 heal creates the table on next start.

### 2026-08-13 — MedicalRecord skip-nav M2M with ApplicationProfileInstance

- MedicalRecord already had `LinkKind = 6` auto-link / sticky ResolvedLinks, but was omitted from the child skip-nav join set.
- Same pattern as Education: `MedicalRecord.ApplicationProfileInstances` (not aggregated) ↔ hidden `ApplicationProfileInstance.MedicalRecords`. Join `"ApplicationProfileInstanceMedicalRecords"` composite PK only. Heal backfills from ResolvedLinks kind 6. LinkPerson/UnlinkPerson dual-write. Nested `MedicalRecord_ApplicationProfileInstances_ListView` browse-only.
- Verify: `Visa2026DbContextModelTests` passed (join present, no ID/LinkedAt). F5 heal creates the table on next start.

### 2026-08-13 — F5 42P01 BorderZoneItems in child skip-nav heal

- `Configure()` ran `ApplicationProfileInstanceChildSkipNavSchemaSql` after `"People"` existed. Backfill `INNER JOIN "BorderZoneItems"` failed: EF had created `"BorderZoneItem"` because there was no `DbSet<BorderZoneItem>` (InvitationItem/WorkPermitItem already had plural DbSets).
- Postgres also **plans** static SQL in `DO $$` even when `IF to_regclass` is false (same lesson as Applications rename).
- Fix: add `DbSet<BorderZoneItem> BorderZoneItems`; heal `ALTER TABLE "BorderZoneItem" RENAME TO "BorderZoneItems"` via `EXECUTE`; CREATE/INSERT also via `EXECUTE` and skip backfill until the child table exists.
- Verify: stop F5, rebuild, F5 `Visa2026 - PostgreSQL` past login.

### 2026-08-13 — Greenfield login failed: Configure heals blocked EnsureCreated

- Login page appeared but `Admin` + empty password failed. Local `visa2026` had only 5 tables (`ApplicationProfiles*` + `PersonExportBatches`) — no `PermissionPolicyUsers`.
- Cause: `Configure()` `CREATE TABLE IF NOT EXISTS` for profiles/export batches ran **before** `CheckCompatibility`. EF EnsureCreated saw a non-empty DB and skipped the rest of the model.
- Fix: skip Configure-time schema heals until `"People"` exists. Recreated empty `visa2026`. AddBuildStep heals still run after schema update.
- Verify: stop F5, rebuild, F5, log in `Admin` / empty password.

### 2026-08-13 — Greenfield F5 42P01 ApplicationTypes in profile seed gate

- Same empty-DB ordering: `ApplicationProfileSeedGate` in `Configure()` queried `ApplicationType` before `CheckCompatibility` created `"ApplicationTypes"`.
- `ApplicationType` is **deprecated, not removed** (plan §2 / slice 13b deferred). Seed still maps Type catalog → `ApplicationProfile`. Officer UX is Application Profiles.
- Fix: `PostgresRelationExists.All` skip seed + template gate until `ApplicationTypes` exists. ModuleUpdater still seeds after schema create.
- Verify: stop F5, rebuild, F5 empty `visa2026` past login.

### 2026-08-13 — Greenfield F5 42P01 workspace view before skip-nav join

- Empty `visa2026` (drop+create): `Startup.Configure` ran `ApplicationWorkspacePostgresViewsSql` before `CheckCompatibility`, so `vw_application_workspace_person` referenced `"ApplicationProfileInstancePeople"` that EF had not created yet → 42P01.
- Fix: skip that heal (and Report Dashboard roster views) until the join table exists. `AddBuildStep` still creates the views after schema update.
- Verify: F5 `Visa2026 - PostgreSQL` on empty local PG past login (no import).

### 2026-08-13 — Person DetailView missing Applications (linked) tab

- Cause: typed Person layouts in `Model.xafml` still bound `ViewItem="ApplicationProfileInstancePeople"` after skip-nav renamed the collection to `Person.ApplicationProfileInstances`. XAF drops the tab when the ViewItem does not exist.
- Fix: retarget Employee / FamilyMember / TemporaryVisitor `IssuedDocumentsTabs` to `ApplicationProfileInstances` (first tab, Index 0). `InverseProperty` on `Person.ApplicationProfileInstances` ↔ `ApplicationProfileInstance.People`.
- Not a schema/heal issue — F5 succeeding does not restore the tab until model diffs match the property name.
- Verify: **Done** — Employee DetailView → Issued documents → **Applications (linked)** first tab.

### 2026-08-13 — F5 42601 ministry roster CTE extra `)`

- `CteMinistryRosterLines` closed `AS ( SELECT ... )` then another `)`. Via-ministry views are `WITH {{MINISTRY_ROSTER_CTE}} SELECT ...` so Postgres 42601 at that extra paren (heal after skip-nav CASCADE dropped the views).
- Removed the extra `)`. `ExecuteEmbeddedSql` now names the resource leaf in the wrap exception.
- Verify: F5 past `ReportDashboardPostgresViewsHealSql`.

### 2026-08-13 — F5 2BP01 drop ApplicationProfileInstancePersonId (views depend)


- Skip-nav heal dropped `ApplicationProfileInstancePersonId` without CASCADE after only the FK named like that column. Live `vw_rd_*` / workspace views (and the unique index/constraint) still referenced it → Postgres 2BP01.
- Fix: drop **all** constraints and indexes on that column, then `DROP COLUMN ... CASCADE`. Startup already recreates views after this heal.
- Verify: F5 past `ApplicationProfileInstancePeopleSkipNavSchemaSql`; views heal next.

### 2026-08-13 — Child BO skip-navigation M2M (same pattern as Person)


- Passport, Visa, Education, AddressOfResidence, EmployeePositionHistory, EmployeeSalary, InvitationItem, WorkPermitItem, BorderZoneItem, TravelHistory each have `ApplicationProfileInstances` (`IList`, not `[Aggregated]`). Instance side: hidden `Passports` / `Visas` / `Educations` / `AddressesOfResidence` / `PositionHistories` / `Salaries` / `InvitationItems` / `WorkPermitItems` / `BorderZoneItems` / `TravelHistories`.
- Visa M2M is **input** linked visas. Issued-from stays `Visa.IssuingApplicationProfileInstance` (`HasOne` + `WithMany()`, no collection). InvitationItem M2M is distinct from the NotMapped parent-header `ApplicationProfileInstance` helper.
- Join tables `ApplicationProfileInstance{Child}` — composite PK only. Heal `ApplicationProfileInstanceChildSkipNavSchemaSql` CREATE IF NULL + backfill from ResolvedLinks `LinkKind`. LinkPerson dual-writes M2M; UnlinkPerson removes that pair's children from the collections.
- Nested `{Type}_ApplicationProfileInstances_ListView` browse-only (officers still only link Person).
- Verify: Module.Tests + Blazor.Server 0 errors; Module.Tests 209 passed. F5 heal still after Wave 2b (do not F5 during import).

### 2026-08-13 — Direct Person ↔ ApplicationProfileInstance skip-navigation M2M

- Deleted persistent `ApplicationProfileInstancePerson` BO. Roster is EF skip-navigation `ApplicationProfileInstance.People` ↔ `Person.ApplicationProfileInstances`. Join table `"ApplicationProfileInstancePeople"` is composite PK `(ApplicationProfileInstanceId, PersonId)` only — no `ID` / `LinkedAt` / `GCRecord`. Do **not** `[Aggregated]` `People` (would delete Person rows). Sticky links stay on `ApplicationProfileInstancePersonResolvedLink` keyed by `(ApplicationProfileInstanceId, PersonId, LinkKind)`.
- Heal: `ApplicationProfileInstancePeopleSkipNavSchemaSql` backfills ResolvedLink instance+person from old join `ID`, then `DROP TABLE … CASCADE` and recreates the two-column join. **Do not F5 while Wave 2b is writing the old join** — CASCADE drops views; startup recreates them after the heal.
- Leftover compile: `IList<Person>` made `ap.Person` / `ThenInclude(p => p.Person)` CS1061. Roster identity for copies/Resminamalar/PDF is **Person id + instance id** (UI properties still named `ApplicationProfileInstancePersonIds`). Wave 2b id-map is PersonInApplication.Oid → Person.ID.
- Guard: `Visa2026DbContextModelTests` asserts no `ApplicationProfileInstancePerson` CLR type, join has no `ID`/`LinkedAt`, ResolvedLink has instance+person FKs.
- Verify: `dotnet build` Module.Tests + Blazor.Server 0 errors; Module.Tests 209 passed. **F5 heal + People-tab / copies / Resminamalar smoke still required** after the in-progress Wave 2b import finishes (or wipe roster + re-run `--entity ApplicationProfileInstancePerson`).

### 2026-08-13 — Phase B: F5 green; three columns the rename script missed

- Host starts and serves the login page (200) on the migrated local `visa2026`; profile seed sync `created=0, updated=36`, 22 user report templates.
- `scripts/local/Rename-ApplicationToProfileInstance.ps1` renamed C# properties but no heal renamed the **columns**, so each start crashed on one 42703 at a time in `ApplicationProfileSeedSync`. Missed columns: `ApplicationTypes.ApplicationProgressRoute`, `ApplicationProfiles.CancelApplications`, `Visas.LegacyPersonInApplicationOid`.
- Fixed by appending all three to `ApplicationProfileInstanceCutoverSchemaSql.RenameChildFkColumnsPostgres` (idempotent: renames only when old exists and new does not; runs unconditionally on every start). Rename over additive `ADD COLUMN IF NOT EXISTS` for the Visas one — the additive heal would have stranded imported legacy PIA ids in the old column.
- Faster than crash-by-crash: build the EF model against the live DB and diff `entity.GetProperties().GetColumnName(storeObject)` against `information_schema.columns`. A throwaway probe listed all remaining drift in one run (needs `UseChangeTrackingProxies()`, otherwise `FileData` fails change-tracking validation).
- Leftover, not a code bug: saved tab state still points at `ViewID=Application_ListView_ViaMinistries&ObjectClassName=…BusinessObjects.Application`, so XAF logs handled "requested page is not found" on login until that per-user state is cleared.

### 2026-08-13 — Phase B: POCO leaked into EF model via view-BO navigations

- F5 crashed at `ProxyBindingRewriter`: "Property 'ApplicationRosterMergeLine.ID' is not virtual" — the POCO was pulled into the EF model as an entity through navigation properties on **view-mapped** BOs (the bulk `ApplicationItem` → `ApplicationRosterMergeLine` rename hit them too).
- Removed 12 `VwRdApplication*` navigations (`[ForeignKey(ApplicationItemOid)] ApplicationRosterMergeLine`) plus `VisaExtensionTracking` / `WorkPermitExtensionTracking` navigations and their `HasOne(...)` config in `Visa2026DbContext`. `ApplicationItemOid` / `ApplicationItemID` stay as bare key columns.
- EF discovers entities from navigations, not just `DbSet<>`: after deleting a BO, grep **BusinessObjects** for property declarations of the replacement POCO, not only the DbContext.
- Guard: `Visa2026.Module.Tests/BusinessObjects/Visa2026DbContextModelTests.cs` builds the model with `UseChangeTrackingProxies()` / `UseLazyLoadingProxies()` and asserts `FindEntityType(typeof(ApplicationRosterMergeLine)) == null` — fails at test time instead of host startup.
- Verify: `dotnet build Visa2026.slnx -c Debug` 0 errors; Module.Tests 209 passed.

### 2026-08-13 — Phase B: ApplicationItem BO deleted → ApplicationRosterMergeLine POCO

- Deleted persistent `ApplicationItem` BO; merge/PDF hydrate to plain `ApplicationRosterMergeLine` (not DomainComponent / BaseObject).
- Hydrator / Resminamalar / PdfMappingHelper retargeted; roster identity remains `ApplicationProfileInstancePerson` IDs.
- ApplicationItem ListView controllers disabled or removed; import corrections/OData for ApplicationItem fail-fast/retired.
- `DROP ApplicationItems` still via `ApplicationItemsDropSchemaSql`. Verify: `dotnet build Visa2026.slnx` 0 errors.

### 2026-08-13 — Phase B: no DomainComponent; ApplicationItem non-persistent projection

- User locked: do **not** use Domain Components for ApplicationItem hard-remove. Always-on rule: `.cursor/rules/visa2026-no-domain-components.mdc` (linked from `visa2026-core.mdc` + `AGENTS.md`).
- `ApplicationItem` kept as `[NonPersistent]` merge/PDF projection (not DbContext / not `[DomainComponent]`); DROP TABLE wired via `ApplicationItemsDropSchemaSql`.
- Resminamalar batch worker loads `People` M2M; IssuingApplicationItem correction CLI retired; invitation/type-route corrections retargeted to `IssuingApplicationProfileInstance` / `People`.
- Verify: `dotnet build Visa2026.slnx -c Debug` 0 errors; Module.Tests 209 passed.
### 2026-08-12 — EF lazy-load: ApplicationItem.ApplicationProfileInstance backing field

- Property renamed but field stayed `application` → EF "No backing field was found for property 'ApplicationItem.ApplicationProfileInstance'".
- Renamed field to `applicationProfileInstance` (convention match).

### 2026-08-12 — F5 42703 IssuingApplicationProfileInstanceProfileInstanceID

- Mechanical rename applied twice: `IssuingApplicationID` → `IssuingApplicationProfileInstanceID` → garbled `…ProfileInstanceProfileInstanceID`.
- Correct Visas FK column is `IssuingApplicationProfileInstanceID` (cutover + EF). Fixed in `vw_rd_application_via_ministry_visa_extension_completed_base.postgres.sql`.

### 2026-08-12 — F5 42703 CreationProgressRoute in dashboard SQL

- `CreationProgressRoute` is `[NotMapped]` on `ApplicationProfileInstance` (in-memory ListView picker only) — not a PG column.
- Via-ministry / direct-migration views used `COALESCE(a."CreationProgressRoute", apf."ProgressRoute")` → 42703.
- Fix: filter on `COALESCE(apf."ProgressRoute", 0)` only.

### 2026-08-12 — Remaining Report Dashboard SQL off ApplicationType

- Migrated heal-path leftovers: invitation in-process/rejected, work-permit app progress, visa extension required/state, View_VisaExtensionStatus, registration, to-be-checked-in/out, vw_rd_application, roster registration/checkout CTEs.
- Filters: `ProduceInvitation`; visa-ext ProduceVisa+RequirePersonVisa; registration `ActionFamily=2`; checkout `Code=check_out`.
- SQL Server `SqlViewsUpdater` still has Type joins (non-Postgres / historical) — Postgres F5 uses embedded `.postgres.sql` + RosterSql.

### 2026-08-12 — Report Dashboard via-ministry SQL uses ApplicationProfile (not ApplicationType)

- F5 heal failed on `at.ApplicationProfileInstanceProgressRoute` — mechanical rename of Type column; Type is deprecated.
- Via-ministry / direct-migration / visa_app_progress / roster CTEs now join `ApplicationProfiles` on `ApplicationProfileID`.
- Route: `COALESCE(CreationProgressRoute, ProgressRoute)`; invitation: `ProduceInvitation`; visa-ext: `ProduceVisa` + `RequirePersonVisa` + Issuance.
- Do not rename `ApplicationTypes.ApplicationProgressRoute` in cutover — leave Type schema alone during dual-read.

### 2026-08-12 — F5 42601 {{MINISTRY_ROSTER_CTE}} in Report Dashboard heal

- Startup `ReportDashboardPostgresViewsHealSql` executed via-ministry embedded SQL raw; scripts contain `{{MINISTRY_ROSTER_CTE}}` which Postgres rejects (`syntax error at or near "{"`).
- Fix: load via `ReportDashboardSqlViewResource.Load` (same substitution as ModuleUpdater) instead of a private stream read.

### 2026-08-12 — F5 42P01 Applications after cutover rename

- Startup `ApplicationProfileInstanceCutoverSchemaSql.EnsureSchemaPostgres` still had `SELECT COUNT(*) FROM "Applications"` in the ELSIF condition. PL/pgSQL plans that subquery even when `to_regclass` is null, so a second F5 after rename fails.
- Fix: `EXECUTE` for rename/count/copy/drop of old Applications* names. Same pattern as issuing-column backfill.

### 2026-08-12 — F5 42703 vw_rd_visa_on_extension.ApplicationProfileInstanceOid

- Startup `ReportDashboardPostgresViewsHealSql.NeedsVisaAppProgressPrimaryCodeHeal` joined `o."ApplicationProfileInstanceOid"` while the live view still exposed `ApplicationOid` (ModuleUpdater skipped).
- Fix: recreate when `ApplicationOid` is present; run the terminal-state probe only after `ApplicationProfileInstanceOid` exists. Same recreate for via-ministry / work-permit wrappers that still have the legacy column.
- Embedded SQL already `DROP VIEW` then `CREATE` with the new alias — do not rename view columns in cutover.

### 2026-08-12 — F5 42703 ApplicationItems.ApplicationProfileInstanceID

- Startup `VisaIssuingApplicationProfileInstanceSchemaSql.ApplyIfMissing` ran **before** §13 cutover, so `ApplicationItems` still had `ApplicationID`.
- Fix: run `ApplicationProfileInstanceCutoverSchemaSql.ApplyIfMissing` first in `Startup`; issuing backfill uses `EXECUTE` + `pg_attribute` so missing new column does not parse-fail.

### 2026-08-12 — §13 R6 verify (local)

- `dotnet build Visa2026.slnx -c Debug` — 0 errors.
- `Visa2026.Module.Tests` — 209 passed.
- Import fail-fast string present for `--entity Application`.
- Operator still needed: local F5 ModuleUpdater row-count check; Demo import chain; Report Dashboard cards; E2E staged→workspace smoke.

### 2026-08-12 — §13 R0–R5 Application → ApplicationProfileInstance cutover shipped (code)

- Mechanical rename: Application BO → ApplicationProfileInstance; Progress/Person/ResolvedLink/ApprovalLegSnapshot; Issuing*; DbSet ApplicationProfileInstances; [Table] attrs.
- Cutover updater: ApplicationProfileInstanceCutoverSchemaUpdater renames/copies PG tables (same Guids), renames child FK columns, drops old Applications* leftovers. AssemblyVersion 1.0.0.663.
- Import hard break: `--entity Application` fails; use ApplicationProfileInstance. OData registers ApplicationProfileInstance.
- Do not rename XafApplication / Controller.Application / IModel*.Application / wizard session Application / merge placeholders Application_*.
- Officer captions: Profile instance № / Start process; keep Application Profile for templates.
- Verify still needed: local F5 ModuleUpdater counts; Demo import chain; Report Dashboard cards; E2E smoke.


### 2026-08-12 — §13 Instance rename cutover locked (R0)

- **Replace** case BO `Application` with `ApplicationProfileInstance` (new tables + same-Guid copy + hard break).
- **Also rename** Progress / Person / ResolvedLink / ApprovalLegSnapshot / Issuing* / import entity.
- **Do not rename** ApplicationProfile template, ApplicationType, ApplicationState/Location, ApplicationUser*, runtime log.
- **UI**: officers see “Application Profile instance” / process number only.
- **Parallel** with Wave 2b; ApplicationItem stays delete-path (not rename).
- **Plan**: [`docs/APPLICATION_PROFILE_PLAN.md`](../../../docs/APPLICATION_PROFILE_PLAN.md) §10.1a + §13; slices R0–R6.

### 2026-08-12 — Wave 2b ApplicationPerson import (Calik)

- **Shipped**: `--entity ApplicationPerson` in-process importer; chains include ApplicationPerson before ApplicationItem.
- **Cross-skill**: visa2014-to-visa2026-import learnings (Wave 2b).
- **Still open**: ApplicationItem hard-remove + child FK remap to Application+Person.
- **Next**: remap Visa.IssuingApplication / permit lines off ApplicationItem, then drop ApplicationItem from import chains.

### 2026-08-12 — Process-complete lock on roster + ResolvedLinks (slice 10p)

- **Trigger**: `Application.IsWorkflowTerminal` — `PROCESS_ISSUED`, `PROCESS_REJECTED`, `PROCESS_CANCELLED` (not ministry `*_REVIEW_REJECTED`).
- **Helper**: `ApplicationPersonRosterLockHelper`; blocks link/unlink/refresh + commit validation on `ApplicationPerson` / `ApplicationPersonResolvedLink`.
- **UI**: `CaseChrome.ResolvedLinksLocked`; officer shell lock badge; disabled Link/Unlink; message `ApplicationPerson.RosterLockedWhenWorkflowTerminal`.
- **Unlock**: edit/delete last progress step (same as workflow-terminal reopen).
- **Plan**: §10.5 item 4 locked.
- **Next**: Calik ApplicationPerson import (Wave 2b) or ApplicationItem hard-remove.

### 2026-08-12 — Workspace Linked records tiles from ResolvedLinks (slice 10o)

- **Catalog**: `ApplicationWorkspaceLinkedRecordsCatalog` — 12 kinds, tab keys, glyphs, `IsConfigured` via `ApplicationProfileConfigurationResolver`.
- **Tiles**: Overview counts from sticky `ApplicationPersonResolvedLink` rows (not tab row scans); empty-state hint when none.
- **People grid**: per-person record cards include visa + rejection; counts from ResolvedLinks.
- **UX**: tile click → People tab + highlight matching record type (`PeopleLinkedRecordFocusKey`).
- **Tests**: `ApplicationWorkspaceLinkedRecordsCatalogTests` (3) green.
- **Next**: process-complete lock (10p / §10.5) or Calik ApplicationPerson import.

### 2026-08-12 — §10 auto-link gate + sticky ResolvedLinks (slice 10n)

- **Change**: `ApplicationPersonResolver.RefreshResolvedLinks` no longer wipe/re-resolve. Creates only **missing** kinds when `RequirePerson*` (via `ApplicationProfileConfigurationResolver`) is on and a valid candidate exists; **keeps** existing `LinkedObjectId` (sticky); toggle-off does not delete.
- **API**: `IsAutoLinkEnabled`, `CollectMissingAutoLinks`; profile-only `RequirePersonBorderZoneItem` / `RequirePersonTravelHistory` on configuration resolver.
- **Unlink**: still `ApplicationPersonService.UnlinkPerson` → cascade deletes `ResolvedLinks`.
- **Tests**: `ApplicationPersonResolverTests` (7) green.
- **Out of scope**: process-complete lock (10p / §10.5); Linked records tiles (10o); ApplicationPerson importer.
- **Next**: workspace Linked records tiles (10o), or Calik ApplicationPerson import.

### 2026-08-12 — Locked: instance M2M person-related BOs + naming (§10)

- **Naming**: Profile = template (`ApplicationProfile`); “Application Profile instance” / in process = `Application`; progress lines = append-only `ApplicationProgress` on instance.
- **Number/date**: `ApplicationNumber` / `ApplicationDate` on **instance** (`Application`), not on shared profile.
- **Linked records**: Application-scoped M2M to person-related BOs; auto-link only if `RequirePerson*` checked; sticky original links; toggle-off = hide + no new links (keep existing); lock links when process completes.
- **Import (Calik)**: legacy Application → instance; people via **ApplicationPerson** (not ApplicationItem); immediate auto-link; child items on Application+Person only — see visa2014-to-visa2026-import Wave 2b.
- **Plan**: [`docs/APPLICATION_PROFILE_PLAN.md`](../../../docs/APPLICATION_PROFILE_PLAN.md) §10.1 / §10.1a updated.
- **Next implement**: auto-link/unlink + workspace Linked records tiles + process-complete lock (exact state codes still open §10.5); ApplicationPerson importer.

### 2026-08-12 — CatalogScope column missing on existing PG (42703)

- **Symptom**: `PostgresException 42703: column a.CatalogScope does not exist` when loading `NestedTemplates` (overview / officer shell).
- **Cause**: BO fields shipped before DB heal; ModuleInfo already current skipped EF add.
- **Fix**: `ApplicationProfileSchemaSql` ADD COLUMN IF NOT EXISTS for `CatalogScope` / `DataScope` / `CategoryKey` (ApplyIfMissing + ModuleUpdater); AssemblyVersion **1.0.0.662**. Local `visa2026` altered via psql.
- **Verify**: Restart F5 (or just retry after ALTER) → open profile overview / Configure step 4.

### 2026-08-12 — Step 4 real UserReportTemplate catalog + persist CatalogScope/DataScope

- **Catalog**: `ApplicationProfileWizardTemplateCatalog` — Global = no type/group links; Category = typed/grouped templates tagged Invitation/Visa/WorkPermit/Registration/BorderZone from links + capabilities.
- **BO**: `ApplicationProfileTemplate.CatalogScope`, `DataScope`, `CategoryKey` (defaults ProfileSpecific / PeopleM2M).
- **UI**: Wizard step 4 lists live catalog; Include/Exclude + Add/Edit write scope fields; profile-specific list filters `CatalogScope == ProfileSpecific`.
- **Verify**: Stop F5 → rebuild → unlocked profile → step 4 → Category/Global show real names; Include → Save profile → reopen; DataScope survives.

### 2026-08-12 — Edit template modal matched Word prototype PNG

- **Target**: `docs/prototypes/application-profile-wizard-template-edit-word-prototype.png` (teal bar + W icon, letter SAMPLE preview, meta rows Name/Kind/Scope/Sort/Linked Active, Status pills, Open/Sync hints below buttons, footer Cancel | Save metadata / Save & close).
- **UI**: `ApplicationProfileWizardStepTemplatesPerson.razor` Edit modal + `application-profile-wizard.css` (`.ap-wizard-edit-head*`, `.ap-wizard-preview--letter`, `.ap-wizard-edit-meta`, pills). No GUID dump; data-scope cards omitted from Edit (inferred on open).
- **Verify**: Hard-refresh CSS; unlocked profile → step 4 → Edit → compare to prototype PNG.

### 2026-08-12 — Wizard Edit → UserReportTemplate staging (Open / Sync)

- **Bridge**: `ApplicationProfileTemplateUserReportBridge` — find/create `UserReportTemplate` by nested template name; copy nested `TemplateFile` onto master when master empty; `WriteMasterFile` on Add/Replace.
- **UI**: Step 4 Edit modal uses `UserReportTemplateStagingUiService.ExportForEditAsync` + `visaTemplateStagingLocal.downloadTemplate` (Open/Download); Sync uses same `syncFromFilePickerDirect` path as Resminamalar (`JSInvokable` on wizard step).
- **Requires**: `TemplateEditStaging:Enabled` + Write on `UserReportTemplate`; profile nested row must exist (Include first for Global/Category mocks).
- **Verify**: Unlocked profile → step 4 → Edit existing (or Add with file) → Open in Word → edit/save → Sync → choose file → Imported; Resminamalar sees updated template by name.

### 2026-08-12 — Wizard step 4 template scopes / upload / edit UI (mock)

- **Prototypes** saved under `docs/prototypes/application-profile-wizard-template-*-prototype.png` (+ three-scopes, initial-upload, data-scope).
- **UI**: `ApplicationProfileWizardStepTemplatesPerson.razor` — Profile-specific / Category / Global sections; Add modal (upload + data scope cards); Edit modal (Open/Sync stubs, replace file, data scope); mock category/global catalogs with Include/Exclude.
- **Persist**: Add/Include creates `ApplicationProfileTemplate` (+ optional `TemplateFile` bytes). Scope/data-family UI state is in-memory (not BO columns yet). Open in Word / Sync stubbed (Resminamalar staging next).
- **Verify**: Configure unlocked profile → step 4 → Add template / Edit / Include global; Save profile. Stop F5 if Blazor DLL locked during build.

### 2026-08-11 — Application Profile catalog Wave 3 (nested templates)

- **Delivered**: `ApplicationProfileNestedTemplateProposalBuilder`, tenant JSON sync (`application-profile-nested-templates.calik-energi.json`), `ApplicationProfileNestedTemplateTenantCatalogSeedUpdater` in `Module.cs`, DataImporter export/patch CLI + PS scripts; [APPLICATION_PROFILE_CATALOG_WAVE3.md](../../../docs/VISA2014_MIGRATION/APPLICATION_PROFILE_CATALOG_WAVE3.md).
- **Source**: Target DB `UserReportTemplate` visibility (not legacy SQL); synthetic `Application` probe per Wave 0b catalog row.
- **Local export** (`visa2026`): 176 profile keys · 691 nested rows · 22 templates · 0 profiles without templates.
- **Fix**: `ApplicationType` lookup in proposal builder must use `.AsEnumerable()` before `string.Equals` (EF translation).
- **Sign-off**: Tenant JSON rows ship with empty `SignOff`; set `"approved"` before patch/deploy sync.
- **Local patch** (`visa2026`): 691 approved JSON rows → **637** `ApplicationProfileTemplate` rows (54 skipped — `FindProfile` could not resolve `Code` + contract).
- **Verify**: Review Excel → approve JSON → `Application-Profile-NestedTemplates.ps1` → Resminamalar on case workspace uses profile nested catalog.

### 2026-08-11 — Application Profile catalog Wave 0 (legacy → tenant JSON proposal)

- **Delivered**: `--export-visa2014-preview --entity ApplicationProfileCatalog`; `ApplicationProfileCatalogPreviewHelper`; `ApplicationProfileCatalog-CalikEnergi.ps1`; [APPLICATION_PROFILE_CATALOG_WAVE0.md](../../../docs/VISA2014_MIGRATION/APPLICATION_PROFILE_CATALOG_WAVE0.md).
- **Rule**: 1 profile per translated `ApplicationType`; full history; profile FK per legacy `Oid` type (not manual number).
- **Verify**: Run against `.15` / `VISA2015` → review `ApplicationProfileCatalog-proposal.calik-energi.xlsx` → fill Decision/SignOff.

### 2026-08-11 — Application Profile catalog Wave 2 (Application import FK)

- **Delivered**: `ApplicationProfile` on OData; `ResolveApplicationProfile` in import resolver; Application POST includes profile FK; `--patch-visa2014-application-profile` headless backfill + profile histogram; `Application-Profile.ps1`.
- **Rule**: Profile follows each legacy row's translated `ApplicationType` (same code path as tenant JSON).
- **Verify**: Dry-run patch on local PG id-map → histogram matches 21 profiles; re-import Application sets both FKs.

### 2026-08-11 — Application Profile catalog Wave 1 (tenant JSON + deploy sync)

- **Sign-off**: Developer approved Wave 0 Excel (21 profiles).
- **Delivered**: `application-profile.calik-energi.json` (21 rows, `SignOff: approved`); `ApplicationProfileTenantCatalogSeedUpdater` (after `ApplicationProfileSeedUpdater`); `--export-visa2014-application-profile-tenant-json`; `ApplicationProfileTenant-CalikEnergi.ps1`; `order.yaml` tenantCatalogGeneration step.
- **Verify**: Regenerate JSON → F5/DB update → `ApplicationProfile` rows match catalog by `Code`; overlay overrides type-derived seed.

### 2026-08-11 — Officer shell Document copies preview → global slot (preview-only)

- **Symptom**: Preview on case Document copies tab opened inline in main content (or failed before roster PDF merge fix).
- **Rule**: Same as Resminamalar — tab owns catalog; slot Preview = viewer only (`DocumentCopiesSlotRequest.OpenPreviewOnly`).
- **Fix**: `FocusSlotKey` + `FocusDisplayName`; `OfficerShellCaseDocumentsTab` → `OpenDocumentCopiesAsync`; `DocumentCopiesSlotPanel` preview-only mode; roster merge via `TryBuildMergedPdfForRoster`.
- **Verify**: Tab → Preview — PDF in `#visa-preview-slot`; catalog stays in tab; Close dismisses slot.

### 2026-08-10 — B5b Case workspace PNG parity (Blazor)

- **Delivered**: Full case workspace lift from HTML prototype — `ApplicationWorkspaceCaseView` + `ApplicationWorkspaceCaseBuilder`; tab UIs for overview (summary tiles + stepper + linked records), people matrix + rail, progress vertical timeline + advance action, document copies + Resminamalar catalogs (preview later moved to global slot).
- **Files**: `OfficerShellCaseWorkspaceComponent.razor`, `OfficerShellCaseDocumentsTab.razor`, `OfficerShellCaseResminamalarTab.razor`, `ApplicationWorkspaceCaseModels.cs`, `ApplicationWorkspaceCaseBuilder.cs`.
- **Verify**: F5 → Application Profiles → In process → open row → all 6 tabs; documents/resminamalar render in-tab (wide layout, no preview-slot redirect).

### 2026-08-11 — Officer shell Resminamalar preview → global slot (preview-only)

- **Symptom**: Preview on case Resminamalar tab opened full Templates catalog in `#visa-preview-slot` (duplicate of tab catalog).
- **Rule**: Tab owns catalog; slot Preview = viewer only (`ResminamalarSlotRequest.OpenPreviewOnly`). Rail / Application DetailView still opens slot with catalog.
- **Fix**: `OpenPreviewOnly` + `FocusDisplayName`; `ResminamalarSlotPanel` skips catalog and closes slot on preview Close.
- **Verify**: Tab → Preview → PDF in slot only; Close returns to tab catalog.

### 2026-08-11 — Officer shell Resminamalar preview → global slot

- **Symptom**: Preview on case workspace Resminamalar tab opened inline in main content instead of `#visa-preview-slot`.
- **Fix**: `OfficerShellCaseResminamalarTab` routes preview to `IVisaPreviewSlotService.OpenResminamalarAsync` with `ResminamalarSlotRequest.FocusEntryKey`; `ApplicationReportPackageComponent` auto-previews focused entry (ProgressLetters pattern); slot panel shows `ReportPackageInlinePreview`.
- **Verify**: F5 → case → Resminamalar tab → Preview — PDF opens in right preview slot; catalog stays in tab.

### 2026-08-11 — Person detail ObjectDisposedException (officer shell / workspace)

- **Symptom**: `ObjectDisposedException` on `SecuredEFCoreObjectSpace` during `ProcessViewShortcut` / page refresh after **Open person detail** from case workspace.
- **Cause**: `OpenPersonDetailAsync` used `using var objectSpace` then `ShowView` — XAF kept the DetailView but the ObjectSpace was disposed when the method returned.
- **Fix**: `PersonDetailOpenHelper.TryShowDetailView` (typed detail via `PersonDetailViewModelHelper`; view-owned ObjectSpace not disposed). Used from `OfficerShellPropertyEditor` and `ApplicationWorkspacePropertyEditor`.
- **Verify**: Stop F5, rebuild, open case → People → Open person detail → refresh page — no error.

### 2026-08-11 — B8 Custom person link picker (Blazor)

- **Delivered**: `IApplicationPersonLinkQueryService` / `ApplicationPersonLinkQueryService` — search link candidates (exclude already linked; `PersonListViewFullTextSearchCriteriaBuilder` for name/personal number/passport). `OfficerShellPersonLinkPickerComponent` — inline panel on People tab; link via `ApplicationPersonService.LinkPerson`. Replaces XAF Person ListView modal in `OfficerShellPropertyEditor` only.
- **Verify**: F5 → case workspace → People & links → Link existing… → search → Link → person appears in roster; Cancel closes panel.

### 2026-08-11 — B7 Case progress tab wiring (Blazor)

- **Delivered**: `OfficerShellCaseProgressService` — save `ApplicationProgress.Description` (officer notes), upload `MinistryLetterFile` on decision steps, append next progress row via `ApplicationProgressTransitionHelper` (state picker when multiple legal next steps).
- **UI**: `OfficerShellCaseProgressTab.razor` — editable notes, ministry letter upload + download link, in-shell advance (no Application DetailView redirect).
- **Verify**: F5 → case workspace → Progress tab → save notes, upload letter on `*_REVIEW_APPROVED`/`REJECTED` step, advance with route validation messages.

### 2026-08-11 — B6 Immersive tab-bar hide

- **Delivered**: `OfficerShellImmersiveTabBarController` toggles `TabsModel.CssClass` (`visa-officer-shell-hide-mdi-tabs`) when `OfficerShellHost_DetailView` is active; `#visa-app-shell:has(.officer-shell-host)` CSS fallback hides TabbedMDI `.dxbl-tabs-header` (not form-layout tabs); shell min-height `calc(100vh - 48px)`.
- **Verify**: F5 → Application Profiles — no XAF document tab strip; open another view (e.g. Advance progress) — tab strip returns.

### 2026-08-10 — B5 Case workspace 6-tab shell

- **Delivered**: `OfficerShellCaseWorkspaceComponent` — PNG `cw-*` layout with tabs (overview, people, progress, documents, resminamalar, SLA); live `ApplicationWorkspaceSnapshot` + `CaseChrome` header; person link/unlink/detail.
- **Preview**: Resminamalar + Document copies catalogs in tab; **Preview** → `#visa-preview-slot` viewer only (`OpenPreviewOnly`).
- **Module**: `ApplicationWorkspaceCaseBuilder`, `ApplicationWorkspaceResminamalarOpenHelper`.
- **Cross-skill**: **visa2026-preview-slot**, **visa2026-document-copies**, **visa2026-resminamalar**.

### 2026-08-10 — B4 Profile templates catalog (list/grid + detail)

- **Delivered**: `OfficerShellTemplateCatalogComponent` — PNG-parity templates catalog (family chips, list/grid, pagination, status pills, staged/in-process usage counts); drill-in rail + `ApplicationProfileOverviewComponent`; `ApplicationProfileCatalogRow` extended with usage + family key.
- **Next**: PNG 6-tab case workspace shell; parity sign-off `parity/CHECKLIST.md`.
- **Cross-skill**: —

### 2026-08-10 — B2 Start process domain merge + B3 immersive chrome

- **Delivered**: `OfficerShellStartProcessService` — validates staged+ready rows, merges people into primary `Application`, deletes secondary staged shells, allocates `YYYY-NNNN` process number (`OfficerShellProcessNumberAllocator`), appends first progress step (`1_REVIEW_STARTED` or `PROCESS_STARTED` by route), syncs `Application.ProcessNumber` + latest progress.
- **Blazor**: `StartProcessAsync` commits via `ObjectSpace`, reloads queues, opens case workspace.
- **B3**: `#visa-app-shell:has(.officer-shell-host)` hides XAF `.sidebar` and strips DetailView padding (`officer-shell-host.css`).
- **Merge**: multi-select links roster via `ApplicationPersonService`; copies `ProjectContract` / profile when primary empty.
- **Next**: dedicated templates list/grid screen; PNG 6-tab case workspace; parity sign-off in `parity/CHECKLIST.md`.
- **Cross-skill**: progress transition rules — **visa2026-application-progress**.

### 2026-08-10 — B0 Blazor officer shell lift

- **Delivered**: `OfficerShellHost` + `OfficerShellComponent.razor` — PNG sidebar (staged / in-process / templates), live badge counts, list/grid queues from `IOfficerShellStagedQueryService` / `IOfficerShellInProcessQueryService`, embedded `ApplicationProfileCatalogComponent` + `ApplicationWorkspaceComponent`.
- **Nav**: Application → **Application Profiles** (`OfficerShellModelUpdater`).
- **CSS**: copied `wwwroot/officer-shell/styles/*` → `wwwroot/css/officer-shell/` + `officer-shell-host.css`.
- **Staged heuristic**: `ProcessNumber` empty + `LatestPrimaryStateCode` in `OFFICE_PREPARATION` / `DRAFT` / null.
- **Start process (v1)**: opens case workspace for first selected staged row — full merge/number assignment deferred to **B2**.
- **HTML prototype**: remains at `/officer-shell/` for parity QA; production path is XAF nav.
- **Next**: **B1** grouped staged + pagination + workspace tab chrome in shell; **B2** domain merge on Start process.
- **Cross-skill**: —

### 2026-08-10 — B1 Blazor shell PNG polish

- **Delivered**: Family filter chips + legend, pagination (10/25/50), grouped staged accordion (`OfficerShellStagedGroupedView`), rich grid cards with color stripe, toolbar search, SLA chips on in-process; `OfficerShellTemplateFamily` maps profile code/action family → reg/inv/ext/wp.
- **Components**: `OfficerShellPaginationBar`, `OfficerShellFamilyChips`, `OfficerShellStagedGroupedView`.
- **Still 🟡 vs PNG/HTML**: full XAF chrome, dedicated templates list/grid, case workspace 6-tab PNG layout, template catalog pagination.
- **Next**: **B2** Start process merge; optional immersive XAF chrome hide.
- **Cross-skill**: —

### 2026-08-08 — Slice 10m: Report Dashboard ministry SQL

- **Delivered**: `ministry_roster_lines` CTE (M2M + legacy) via `{{MINISTRY_ROSTER_CTE}}` in 8 embedded `.postgres.sql` views; visa-extension completed `IssuedVisa` dual-read; ministry invitation legacy EF loader + role filters use M2M roster.
- **Next**: `SyncRulesUpdater`; `ApplicationItem` BO removal post-import.

### 2026-08-08 — Slice 10l: Report Dashboard visa extension SQL

- **Delivered**: Shared visa/work-permit extension roster CTEs; migrated `View_VisaExtensionStatus`, `vw_rd_visa_app_progress`, `vw_rd_work_permit_app_progress`, `vw_rd_visa_state`, `vw_rd_visa_extension_required` unfinished-extension filter; invitation in-process/rejected first person from M2M; `IssuedVisaID` dual-read in status view.
- **Next**: Ministry `vw_rd_application_via_ministry_*` embedded SQL files; `SyncRulesUpdater`; `ApplicationItem` removal.

### 2026-08-08 — Slice 10k: Report Dashboard child-link C# filters

- **Delivered**: `GetLinkedChildIdsInApplicationDateRange` on `ReportDashboardRosterQueryHelper`; Education, Address, Position, Medical Last-N loaders in `ReportDashboardQueryService` use M2M resolved links + legacy fallback; `vw_rd_application` first person from `ApplicationPeople`.
- **Fix**: Corrupted `ProgressStateCode` line in `vw_rd_application` SQL (`\`r\`n` literal) repaired during view migration.
- **Next**: Visa extension / work permit progress SQL views; `SyncRulesUpdater`; `ApplicationItem` BO removal.

### 2026-08-08 — Slice 10j: Report Dashboard roster SQL (phase B start)

- **Delivered**: `ReportDashboardPostgresRosterSql`; migrated `vw_rd_registration`, `vw_rd_passport`, `vw_rd_to_be_checked_in`, `vw_rd_to_be_checked_out` to M2M resolved links + legacy `ApplicationItems` fallback; `ReportDashboardRosterQueryHelper` for Travel, Registration on process, overview passport/address/travel counts.
- **Pattern**: Same dual-read as `ApplicationRosterHelper` — apps with `ApplicationPeople` rows use resolved links only; legacy `ApplicationItems` only when parent app has no M2M roster.
- **Next**: Remaining Report Dashboard views (education, position, medical, ministry extension, …), `SyncRulesUpdater`, then `ApplicationItem` BO removal post-import.

### 2026-08-08 — Slice 10i: Visa.IssuingApplication dual-read

- **Delivered**: `Visa.IssuingApplication` FK + deploy backfill; Path A matcher prefers `ApplicationPerson` M2M; validations/chronology use effective issuing application; legacy `IssuingApplicationItem` deprecated on detail when app FK set.
- **Verify**: Create visa for person on M2M-only application — Issuing Application field populated; F5/deploy backfills existing visas from item FK.
- **Prevent**: New visa linking code should set `IssuingApplication`, not only `IssuingApplicationItem`.
- **Next**: Report Dashboard SQL + sync rules + ApplicationItem BO removal.
- **Cross-skill**: visa2014 import post-pass still sets `IssuingApplicationItem` until import scripts updated.

### 2026-08-08 — Slice 10h: Runtime roster reads via ApplicationPeople

- **Delivered**: `ApplicationRosterHelper` centralizes M2M-first roster reads with legacy `ApplicationItem` fallback; Resminamalar merge hydrates from `ApplicationPerson`; header BO `AvailablePeople` + validation use M2M; Application cancel counts + ListView preload use roster helper.
- **Verify**: Workspace app with linked people only (no ApplicationItem rows) — Resminamalar rows populate; invitation/work-permit person pickers show roster.
- **Prevent**: New runtime reads should call `ApplicationRosterHelper`, not `Application.ApplicationItems` directly.
- **Next**: Phase B BO/schema drop after import + `vw_rd_*` migration.
- **Cross-skill**: visa2026-resminamalar (merge rows).

### 2026-08-08 — Slice 10g: ApplicationItem officer UI cutoff (phase A)

- **Delivered**: Removed ApplicationItem sub-nav under Applications; Person issued-documents tab uses `ApplicationPeople` (Applications linked); dossier Applications = M2M only; disabled `ApplicationItemDocumentCopiesController` on ListView.
- **Verify**: No Application items nav child; Person → Applications (linked); workspace document copies still work.
- **Prevent**: Officer paths must use Application workspace + `ApplicationPerson`; do not re-add ApplicationItem ListView actions without explicit legacy need.
- **Next**: Phase B — BO/schema removal (import, `vw_rd_*`, sync rules, Resminamalar merge) after VISA2014 cutover.
- **Cross-skill**: visa2026-document-copies (workspace path canonical).

### 2026-08-08 — Workspace: drop full profiles rail

- **Symptom**: Application Workspace left rail duplicated Configuration → Application Profile catalog and confused officers.
- **Fix**: Removed profile list/search rail from workspace; keep profile strip (title/chips) with Configure + New Application for the linked profile only.
- **Prevent**: Profile browsing/admin lives only under Configuration → Application Profile.
- **Cross-skill**: —

### 2026-08-08 — Catalog master-detail left rail

- **Symptom**: Opening a profile left the catalog ListView; officers lost the profile list on the left.
- **Fix**: Catalog shell is left rail (search + profiles + New) + inline overview on the right; Configure still opens wizard.
- **Prevent**: Do not navigate away to OverviewHost for the default select path from catalog.
- **Cross-skill**: —

### 2026-08-08 — Slice 8c: Custom catalog home (native List/Detail not officer UI)

- **Delivered**: `ApplicationProfileCatalogHost` + Blazor catalog (search, Active/locked badges, New / Configure / row open); Configuration nav via `ApplicationProfileCatalogModelUpdater` + `ApplicationProfileCatalogNavigationController`; `ApplicationProfile` `[NavigationItem(false)]`; ListView row → overview, New → create+wizard; overview **Configure profile** CTA.
- **Verify**: Configuration → Application Profile → catalog (no checkbox grid) → row → overview → Configure → wizard; New profile → wizard.
- **Prevent**: Non-persistent hosts need nav ModelUpdater + CustomShowNavigationItem (Report Dashboard pattern), not `[NavigationItem("Configuration")]` alone.
- **Next**: Slice 10 close-out (`ApplicationItem` hard-remove) or 13b after import.
- **Cross-skill**: —

### 2026-08-08 — Slice 10f: Profiles rail wired

- **Delivered**: `ApplicationWorkspaceProfileRailHelper` — profile row opens **Configure profile** wizard; **`+`** creates new Application with selected profile (same pipeline as picker; inherits `CreationProgressRoute` from current workspace Application) and opens new workspace.
- **Verify**: Workspace left rail → click profile name → wizard; click **`+`** on another profile → new Application workspace opens.
- **Next**: Slice 10 close-out (`ApplicationItem` hard-remove) or workspace Resminamalar / multi-select roster.

### 2026-08-07 — Slice 10e: Document copies on Application workspace (roster line)

- **Delivered**: `ApplicationPersonLinkedDocumentsResolver` + `ApplicationPersonPdfPackageLineHydrator`; `DocumentCopiesLineScope` on `DocumentCopiesSlotRequest`; workspace Person tab **Document copies** (selected `ApplicationPerson` row); `ApplicationPersonPdfBatchEnqueueService` (`ItemKeyType` = `ApplicationPerson`); worker hydrates roster lines for packer/PDF; resolver visibility uses `ApplicationProfileConfigurationResolver` (profile-first).
- **Verify**: Workspace → Person tab → select roster row → **Document copies** → slot catalog, scan preview, application form download, package → PDF toast; legacy `ApplicationItem` ListView **Document copies** still works.
- **Deferred**: Multi-select roster lines in workspace; previous passport/WP/invitation slots until resolver stores them on `ApplicationPerson`.
- **Next**: Slice 10 close-out (`ApplicationItem` hard-remove).

### 2026-08-07 — Slice 10d: Application ListView opens workspace

- **Delivered**: `ApplicationListViewWorkspaceNavigationController` — row activate on Application ListViews opens workspace (not legacy `Application_DetailView`).
- **Verify**: Applications list → open row → workspace; picker create still opens workspace; toolbar **Open workspace** unchanged.
- **Next**: `ApplicationItem` hard-remove (slice 10 close-out) or child-tab SQL views.

### 2026-08-07 — Slice 10c: Workspace in-tab actions + person SQL view

- **Delivered**: `ApplicationWorkspacePersonUiActions` bridge; Person tab **Link existing…** / **Unlink** / **Open detail** wired to XAF popup actions; row selection on Person tab; `vw_application_workspace_person` + `ApplicationWorkspacePostgresViewsSql` startup heal; picker/create opens workspace (prior session).
- **Verify**: Application workspace → Person tab → **Link existing…** → pick person → row appears; select row → **Open detail**; **Unlink** removes roster row.
- **Deferred**: SQL views for passport/visa/etc. tabs; `ApplicationItem` hard-remove (slice 10 close-out).

### 2026-08-07 — Slice 13a: Profile-first runtime + cutover prep

- **Delivered**: `ApplicationProfileConfigurationResolver` capability methods (`CanIssueVisa`, `CanIssueInvitation`, `CanIssueWorkPermit`, `CanBeIssuingApplicationForVisa`); `ApplicationTypeCapabilities` Application overloads; profile-aware queries in `Visa`, `VisaIssuingLinkPathAMatcher`, `Invitation`, `WorkPermit`, `ApplicationItemVisaDefaults`; `ApplicationProgressRouteNavigation` criteria use `CreationProgressRoute` + profile + type fallback; `Application` validation requires profile **or** type (Type hidden on detail when profile set).
- **Dual-write**: Picker still syncs matching `ApplicationType` on create for Report Dashboard / sync rules / PDF until slice **13b**.
- **Deferred (13b)**: Drop `Applications.ApplicationTypeID`; migrate Report Dashboard SQL, `SyncRulesUpdater`, `PdfMappingHelper`, import mappers.
- **Verify**: Create Application via profile picker (no manual Type) → appears in correct route nav list; link Visa/Invitation/WorkPermit to issuing Application; Type field hidden on detail when profile present.
- **Cross-skill**: visa2014-to-visa2026-import (import must set `ApplicationProfile` before 13b)

### 2026-08-07 — Slice 8b: Wizard prototype parity

- **Delivered**: Step 1 applicability scope cards (Always vs Scoped + criteria); Step 2 property table with Require + Has default + lookup defaults + signatory pickers; Step 3 ministry/migration state Include/SLA tables (`ApplicationProfileProgressStateSetting` child BO + catalog seeder); Step 4 template add/edit/remove (name, kind, sort) in wizard.
- **Schema**: `ApplicationProfileProgressStateSettings` table in `ApplicationProfileSchemaSql` + EF mapping; permissions on child type.
- **Not wired yet**: `ProgressStateSettings` → `ApplicationProgress` route/SLA engine (configuration stored; read in later slice with **visa2026-application-progress**).
- **Verify**: Configure profile → walk all 5 steps → Save profile; existing profiles get default state rows on first wizard open.
- **Cross-skill**: visa2026-application-progress (state checklist consumption)

### 2026-08-07 — Slice 9: Profile picker at Application create

- **Delivered**: `ApplicationProfilePickerHost`, Blazor picker component + CSS, `ApplicationProfilePickerNewController` (intercepts **New** on Application ListViews), `ApplicationProfilePickerQueryService` (active + route filter + applicability criteria + MRU by last `ApplicationDate`), `ApplicationProfilePickerApplyHelper` (profile FK + dual-read `ApplicationType` sync + defaults).
- **Flow**: List **New** → choose profile → **Use profile (live link)** → new Application DetailView with read-only profile and seeded per-Application defaults.
- **Route lists**: Via-ministries / direct-migration ListViews filter profiles by `ProgressRoute`; general Applications list shows all active profiles.
- **Locked profiles**: Still selectable for new Applications; picker shows **Config locked** badge (configuration edits blocked on profile, not on new app).
- **Verify**: Applications → **New** → pick profile → DetailView shows **Application Profile** + defaults (Visa Type, etc. when configured on profile).
- **Officer manual**: `user-manual/docs/en/guides/applications/application-profiles.md`, `administration/configuration/application-profiles.md` (preview prose/mermaid; no E2E screenshots yet).
- **Next**: Slice 10b — real M2M workspace data; Slice 11 — Person/Dossier start application.
- **Cross-skill**: visa2026-application-progress (route filter) | visa2026-user-manual | visa2026-person-dossier (slice 11 entry)

### 2026-08-07 — Slice 8: Configuration wizard UX

- **Delivered**: `ApplicationProfileWizardHost`, 5-step Blazor `ApplicationProfileWizardComponent` + step partials, `application-profile-wizard.css`, **Configure profile** action on Application Profiles ListView, `IApplicationProfileWizardSession` / pending-open gate (Blazor DI in `Startup.cs`).
- **Steps**: Identity (name/code/audience/related-to) · Results & fields (produce/cancel flags, Require*) · Process & SLA (route, SLA days, embedded approval legs add/remove) · Templates & person (nested templates hint + person toggles) · Review & save.
- **Lock**: Wizard honors `ApplicationProfileLockHelper` — read-only UI + save blocked when profile config locked (state A).
- **Deferred**: Ministry/migration state checklist tables from prototype; template file upload in wizard (officers use standard nested templates ListView).
- **Verify**: Configuration → Application Profiles → **Configure profile** → edit → **Save profile**; locked row → read-only banner.
- **Next**: Slice 9 — profile picker at Application create.
- **Cross-skill**: visa2026-application-progress (route/legs) | visa2026-resminamalar (nested templates, slice 12)

### 2026-08-07 — Slice 7: ApplicationProfile config lock (state A)

- **Delivered**: `[Appearance]` read-only when `IsConfigLocked`; `ApplicationProfileDetailViewController` (`View.AllowEdit`); `ApplicationProfileConfigLockObjectSpaceHooks` (save guard on profile + nested legs/templates); `ApplicationProfileCloneController` (CloneObject suffix for locked-profile escape hatch).
- **Lock helper**: `IsPrimaryStateAtOrPastLockStateA` now treats `IS_BEING_PREPARED` / `OFFICE_PREPARATION` / `DRAFT` as unlocked; `IsProfileConfigLocked` queries linked Applications via ObjectSpace.
- **Officer path**: Configuration → Application Profiles → locked row is read-only; use **Clone** to duplicate and edit configuration.
- **Next**: Slice 8 — configuration wizard Blazor UX.
- **Cross-skill**: visa2026-application-progress

### 2026-08-07 — Slice 6: Appearance / progress reads ApplicationProfile first

- **Delivered**: `ApplicationProfileConfigurationResolver` (profile-first, `ApplicationType` fallback); `Application.ConfigurationVisibility` (`Cfg*` properties for XAF `[Appearance]`); updated `Application` + `ApplicationItem` criteria; `ApplicationProgressRouteHelper`, `ApplicationProgressProfileResolver` (embedded profile legs, `RequireProject` / approval-leg gates), `ApplicationMigrationSlaHelper`, migration SLA validation in `ApplicationProgressTransitionHelper`.
- **Pattern**: XAF criteria cannot call static helpers — expose `[NotMapped] CfgShow*` on `Application`; nested items use `Application.CfgShow*`.
- **Tests**: `ApplicationProfileConfigurationResolverTests` (8 facts) + existing `ApplicationProgressProfileResolverTests` still pass.
- **Not migrated**: Report Dashboard SQL, sync rules, PDF mapping, import tools — still read `ApplicationType` where appropriate until slice 13.
- **Next**: Slice 7/8 — config lock UX + wizard.
- **Cross-skill**: visa2026-application-progress | visa2026-bo-state-colors

### 2026-08-07 — Slice 5: seed ApplicationProfile from ApplicationType

- **Delivered**: `ApplicationProfileFromApplicationTypeMapper`, `ApplicationProfileSeedSync`, `ApplicationProfileSeedUpdater` (after SLA type links), `ApplicationProfileSeedGate` on host start.
- **Behavior**: One profile per `ApplicationType` (key = `Code` or slug from `Name`); idempotent re-sync updates profile scalars from Type; backfills `Application.ApplicationProfile` where `ApplicationType` is set.
- **Maps**: `ProgressRoute`, action family (registration/cancel/business trip/issuance), produce/cancel flags, audience (`Category`), Require* per-app and person toggles, ministry/migration SLA days.
- **Verify**: Restart app → Configuration → Application Profiles; Applications list **Application Profile** column; log `ApplicationProfileSeedSync: profiles created=…`.
- **Next**: Slice 6 — central resolver; switch `[Appearance]` / progress reads from `Show*` to profile.
- **Cross-skill**: visa2026-lookup-data (Type JSON seed) | visa2026-lifecycle-docker (schema heal + startup gates)

### 2026-08-07 — Postgres 42703 Applications.ApplicationProfileID missing

- **Symptom**: `column a.ApplicationProfileID does not exist` on Application ListView after ApplicationProfile BO shipped.
- **Root cause**: ModuleInfo current → XAF skipped EF schema sync; no startup heal for new profile tables/FK.
- **Fix**: `ApplicationProfileSchemaSql` + `ApplicationProfileSchemaUpdater` + `Startup` `ApplyIfMissing` (ApplicationProfiles tables + `Applications.ApplicationProfileID`).
- **Prevent**: New ApplicationProfile-related columns need idempotent SQL heal (not ModuleUpdater alone).
- **Cross-skill**: visa2026-lifecycle-docker | —

### 2026-08-07 — Slice 12: Resminamalar reads profile nested templates

- **Delivered**: `ApplicationProfileNestedTemplateCatalogHelper`; catalog prefers profile `NestedTemplates`; `profile:` entry keys in `ApplicationWordReportEntryGenerator`; name-match to `UserReportTemplate` for merge.
- **Dual-read**: empty nested list → legacy `UserReportVisibilityService` catalog unchanged.
- **Deferred**: profile `TemplateFile` bytes override at merge (uses User Report Template file); profile default FKs at empty merge fields (plan §2 decision 12).
- **Cross-skill**: visa2026-resminamalar

### 2026-08-07 — Slice 11: Person / Dossier Start application

- **Delivered**: **Start application…** on Person DetailView + Person Dossier; 2-step profile picker (MRU for seed person, usage badges, people multi-select); `ApplicationStartFromPersonHelper`; dossier Applications section from M2M.
- **Rules shipped**: via-ministry blocks without ProjectContract; registration suggests family; duplicate-open warn + acknowledge; incomplete data warned not blocked.
- **Cross-skill**: visa2026-person-dossier (Applications section columns)

### 2026-08-07 — Slice 10b: Application workspace live M2M

- **Delivered**: `ApplicationPerson` roster, auto-resolved child links, `ApplicationWorkspaceQueryService`, toolbar **Link person** / **Unlink person**, schema heal `ApplicationWorkspaceSchemaSql`.
- **Gotcha**: Service namespace must not be `Services.ApplicationPerson` — shadows BO type from sibling `Services.ApplicationWorkspace` (renamed to `ApplicationPersonRoster`).
- **Gotcha**: XAF0009 on `ApplicationPersonResolvedLink` — `LinkKind` and `LinkedObjectId` must be nullable.
- **Deferred**: SQL `vw_application_workspace_*` views; in-component toolbar buttons still decorative (use XAF actions).
- **Cross-skill**: —

### 2026-08-07 — Slice 10a: Application workspace mock UI shipped

- **Delivered**: `ApplicationWorkspaceHost`, mock `IApplicationWorkspaceQueryService`, Blazor `ApplicationWorkspaceComponent`, **Open workspace** on Application ListView/DetailView.
- **Pattern**: Same as Person dossier (non-persistent host + PropertyEditor) + Report Dashboard (mock query service).
- **Next**: Slice 10b — Person M2M domain, `ApplicationWorkspaceQueryService`, SQL `vw_app_*` tab grids; then hard-remove ApplicationItem.
- **Cross-skill**: —

### 2026-08-07 — Skill created; next slice is Type → Profile seed

- **Context**: Plan + prototypes done; `ApplicationProfile` BO and optional `Application.ApplicationProfile` FK shipped; dual-read with deprecated `ApplicationType` continues.
- **Decision**: **Slice 5** (seed profiles from ApplicationType + backfill FK) is the recommended next implementation step before wizard UX or M2M DetailView.
- **Prevent**: Do not build wizard/M2M on empty profile catalog in prod-like DBs — seed first.
- **Cross-skill**: —

### 2026-08-10 — P10 case workspace tabs (PNG parity)

- **Delivered**: `case-tabs-ui.js` + `case-tabs.css` — People & links (table + per-person record grid + summary rail), Progress (vertical timeline + ministry detail + rail), Resminamalar (grouped catalog + preview + ZIP), SLA & deadlines (metrics, timeline, deadlines table, alerts).
- **Routes**: `#/case/p1/people`, `/progress`, `/resminamalar`, `/sla`.
- **Cross-skill**: visa2026-resminamalar, visa2026-person-detail-tabs (production reference)

### 2026-08-10 — P9 nav badge counts (PNG parity)

- **Delivered**: `nav-ui.js` — live sidebar badges (orange **18** staged, blue **24** in-process); templates subcopy “Configuration · Visa office admin”; `os-nav-badge` styling in `shell.css`.
- **Mock seed**: `seedInProcessDemoCases()` pads in-process to 24 (ext 8 · inv 6 · reg 5 · wp 5); staged remains 18.
- **Note**: counts update live after Start process (by design).
- **Cross-skill**: —

### 2026-08-10 — P8 staged grouped workspace (PNG parity)

- **Delivered**: `staged-workspace-ui.js` + `staged-workspace.css` — accordion groups by template family (reg/inv/ext/wp), avatars, row meta badges, readiness dots, collapsible sections, bottom selection bar; **Grouped** toggle + `#/staged?group=template`.
- **Cross-skill**: —

### 2026-08-10 — P6 pagination (PNG parity)

- **Delivered**: `pagination-ui.js` + `pagination.css` — shared bar on staged, in-process, and templates (list + grid): “Showing X–Y of Z”, rows-per-page select (10/25/50), Bootstrap prev/next + numbered pages; filter/search resets to page 1.
- **Store**: `pagination.{staged,inProcess,templates}` in mock-data.
- **Cross-skill**: —

### 2026-08-10 — P4 template catalog + overview (PNG parity)

- **Delivered**: `template-catalog-ui.js` + `template-catalog.css` — list/grid catalog with status pills (Active/Locked/Draft), rich grid cards (stripe, icon, stats, Configure), toolbar search + filter/sort dropdowns, pagination stub; overview with left rail cards, 4 numbered summary columns, usage stats bar, lock hint footer.
- **Mock seed**: 12 templates (chip counts All 12 / Issuance 4 / Registration 3 / Cancellation 2 / Business trip 3).
- **Routes**: `#/templates` (list/grid toggle), `#/templates/t1` (overview).
- **Cross-skill**: —

### 2026-08-10 — P3 document copies tab (PNG parity)

- **Delivered**: `document-copies-ui.js` — readiness summary + progress bar, per-person accordion (6 slots), Ready/Missing badges, preview pane with metadata; `#/case/:id/documents`.
- **Cross-skill**: visa2026-document-copies (production dialog reference)

### 2026-08-10 — P2 case workspace overview (PNG parity)

- **Delivered**: `case-workspace-ui.js` + `case-workspace.css` — header with SLA badge, person avatars, summary icon grid, horizontal progress stepper, linked-record tiles, readiness + activity rail.
- **Route**: `#/case/p1/overview` (any in-process case id).
- **Cross-skill**: —

- **Delivered**: Template wizard rebuilt with **Bootstrap 5.3** + **Bootstrap Icons** (CDN); `js/wizard-ui.js` + `styles/wizard.css` for PNG stepper, badges, green section headers, all 5 steps.
- **Wizard mode**: `os-app--wizard` collapses sidebar to icon rail (matches wizard mockup).
- **Cross-skill**: —

### 2026-08-10 — HTML officer shell (H0–H6)

- **Delivered**: Interactive prototype at `Visa2026.Blazor.Server/wwwroot/officer-shell/` — hash router, mock store, staged merge → in-process workspace, templates + 5-step wizard, PNG gallery; 22 PNGs copied to `assets/png/`.
- **Plan**: [`docs/APPLICATION_PROFILE_HTML_PROTOTYPE_PLAN.md`](../../../docs/APPLICATION_PROFILE_HTML_PROTOTYPE_PLAN.md) — slices H0–H6 **Done**; H7 (Person staging) deferred.
- **Parity**: `parity/CHECKLIST.md` created — visual sign-off not yet run at 1440×900.
- **Wizard routing**: use `#/templates/wizard/{0-4}` (query string in hash unreliable).
- **Next**: Officer walkthrough + parity checkboxes; then Blazor lift (`OfficerShellLayout.razor`) when product locks template → staged → in-process pivot.
- **Cross-skill**: —
