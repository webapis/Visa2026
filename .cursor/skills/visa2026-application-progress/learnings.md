# Application progress — learnings (append-only)

Read **before** progress/approval work; **append** after verified fixes. Promotion rules: [MATURITY.md](./MATURITY.md).

---

## Entries

### 2026-08-18 — Submitted stays on the Office progress bar

- After later Approvals, the bar only showed each ministry’s latest result, so Submitted disappeared (Activity still had it). Office now keeps **Submitted** + that row’s date once `1_REVIEW_STARTED` / `PROCESS_STARTED` exists. No extra timeline node.
- Test: `Build_SubmittedThenApproved_OfficeKeepsSubmittedOnBar`. Stop F5, rebuild, F5. On **8/-010** Overview — Office preparation shows Submitted and date; Türkmenenergo stays Approved.
- Prevent: Do not leave Office done with an empty badge after Submit. Do not add a separate Submitted step between Office and the first ministry.
- Cross-skill: visa2026-application-profile

### 2026-08-18 — Submitted stays in progress History

- Submitted was stored as `1_REVIEW_STARTED` but Overview Activity used lookup NameTm (“Sent for agreement”) and only the last 3–4 rows, so it vanished after later Approvals.
- Activity / History now use catalog labels (**Submitted**, **Approved**) and list every progress row. Progress tab shows the same log.
- Test: `Build_Activities_KeepSubmittedAfterLaterApprovals`. Stop F5, rebuild, F5. After Submit then Approve — History still shows Submitted with its date.
- Prevent: Do not label History from `ApplicationState.NameTm`. Do not `Take(3)` the progress log.
- Cross-skill: visa2026-application-profile

### 2026-08-18 — Office Result lists Submitted (leave-office)

- Officers could not pick Submitted on Office; it was a hidden default Advance (`1_REVIEW_STARTED`). Option A: Office Result is Submitted (default) then Cancelled. Advance writes the first-ministry (or Migration `PROCESS_STARTED`) row with the officer Date. After Advance, current is that ministry · Submitted; Office is done without a “Completed” badge.
- Test: `IsResultForStep_Submitted_IsTrueOnOfficeOnly` + `PreferredAdvanceCode_Office_UsesFirstMinistryStarted` + empty-history ResultOptions. Stop F5, rebuild, F5. On **8/-010** Office — Result Submitted, set Date, Advance → first ministry Submitted with that date.
- Prevent: Do not add a separate Submitted timeline node. Do not put Submitted on a ministry Result. Do not write `IS_BEING_PREPARED`.
- Cross-skill: visa2026-application-profile

### 2026-08-18 — Office Cancelled officer-verified

- Officer confirmed: after Advance with Result Cancelled on Office preparation, the Office node shows Cancelled (not Office preparation / In process).
- Cross-skill: visa2026-application-profile

### 2026-08-18 — Office Cancelled stays on Office after Advance

- Result Cancelled on Office preparation wrote `PROCESS_CANCELLED` (History was correct) but the Office node always showed `OfficeLabel` / “In process”. `SlotAnchorForCurrent` has no previous row from implied office, so the cancelled overlay never ran (`BuildOfficeStep` had no latest overlay; chrome skipped office result labels).
- Office now overlays the cancelled row (label, outcome, date, Revert). Chrome is `Office preparation · Cancelled`. Header Cancelled badge follows `OutcomeKind`.
- Test: `Build_OfficeCancelled_ShowsCancelledOnOffice`. Stop F5, rebuild, F5. On **8/-010** after revert to office — Result Cancelled → Advance → Office badge Cancelled; ministries stay Pending; Revert still works.
- Prevent: Do not keep Office `CurrentStateLabel` as the step name when latest is `PROCESS_CANCELLED`. Do not drop office result labels in `FormatChromeCurrentStep`.
- Cross-skill: visa2026-application-profile

### 2026-08-18 — Cancelled Result stays on the current ministry

- Selecting Cancelled on Tarkusenergo (Submitted) left the badge on Submitted. After Advance, `PROCESS_CANCELLED` was treated as Migration (`SlotKeyFor` / ministry-catalog match) so the wrong node changed.
- Cancelled is a Result of the current slot (preview + Advance). Timeline uses the previous row to keep Cancelled on that ministry, not Migration.
- Test: `IsResultForStep_Cancelled_IsTrueOnCurrentSlot` + `Build_FirstLegStartedThenCancelled_KeepsCancelledOnThatMinistry` + `Build_FirstLegApprovedThenCancelled_KeepsCancelledOnNextMinistry`. Stop F5, rebuild, F5. On **8/-006** Tarkusenergo — Result Cancelled updates that badge; Advance Cancelled stays on Tarkusenergo.
- Prevent: Do not map `PROCESS_CANCELLED` to Migration or always to leg 1. Do not preview the first Result option when Cancelled is selected.
- Cross-skill: visa2026-application-profile

### 2026-08-17 — Result includes Cancelled last on every current step

- Officers needed **Cancelled** (`PROCESS_CANCELLED`) next to Approved / Disapproved (Unapproved). It was already a legal next state but hidden from Result.
- Result lists same-slot decisions, then Cancelled last. Default Advance stays Approved (or Submitted on Office / Migration enter). Picking Cancelled writes that row.
- Test: `PreferredAdvanceCode_Ministry_DefaultsToApprovedNotCancelled` + timeline ResultOptions last code. Stop F5, rebuild, F5. On **B/-008** Energetika Result — Approved, Unapproved, Cancelled.
- Prevent: Do not default Advance to Cancelled when it is the last Result option. Do not put first-ministry Submitted on Office Result.
- Cross-skill: visa2026-application-profile

### 2026-08-17 — Letter upload only on the current ministry edit form

- Completed ministries each showed a dashed Upload file box (Issued 8/-005). Officers only need that control on the step they are recording.
- Upload stays on the current ministry Result form. Done nodes keep **View letter** only.
- Test: `Build_FirstLegApproved_NextMinistryIsCurrent` — done leg `ShowMinistryLetterUpload` false; current true. Stop F5, rebuild, F5. On **8/-005** Progress — no upload on Approved ministries; View letter still opens the slot.
- Prevent: Do not set `ShowMinistryLetterUpload` from `decisionRow != null` on done legs.
- Cross-skill: visa2026-application-profile

### 2026-08-17 — Ministry letter Preview still iframed compressed XFA

- View letter on a filled Application form showed Please wait + Spire evaluation (Chrome PDF viewer). `/XFA` was only inside a Flate stream, so the occupant iframed the bytes.
- Progress letter PDFs always go through pdf.js now; detection also inflates streams. Scans still canvas-render. Download unchanged.
- Test: `PdfXfaDocumentTests`. Stop F5, rebuild, F5, Ctrl+F5. **B/-010** Progress → View letter.
- Prevent: Do not iframe ministry-letter PDFs. Do not Spire-flatten for Chrome.
- Cross-skill: visa2026-preview-slot | visa2026-document-copies

### 2026-08-17 — Approval letter upload on the ministry Result step

- After Result belonged to the current ministry, upload hid because that node often has no decision row yet (`ShowMinistryLetterUpload` required `IsMinistryDecisionStep` on latest). Officers need to attach the approval/disapproval letter on Energetika (and other ministries) before Advance.
- Upload shows when the current ministry has a Result (Approved/Disapproved). The file is held until Advance and stored on the new decision row. Completed ministries with a decision row can still upload onto that row (`DecisionProgressId`).
- Test: `Build_FirstLegApproved_NextMinistryIsCurrent` expects `ShowMinistryLetterUpload`. Stop F5, rebuild, F5. On **8/-010** Energetika — upload a letter, Advance Approved — letter sits on Energetika; View letter still works.
- Prevent: Do not require a saved decision row before showing upload on a ministry Result step. Do not attach a pending letter to the previous ministry’s latest row.
- Cross-skill: visa2026-application-profile | visa2026-preview-slot

### 2026-08-17 — Progress Result belongs to the current node

- **Next step** sat on Office / an approved ministry but the values were the following node (Submitted, or the next ministry’s Approved/Disapproved). Officers should set **this** step’s result.
- **Result** dropdown = same-slot decisions only (Approved / Disapproved / Issued / Rejected). Office has no Result — Advance starts the first ministry as Submitted. After a ministry is Approved, that node is done and the next ministry (or Migration) becomes current. Started/Cancelled are not Result values.
- Test: `ApplicationWorkspaceProgressAdvancePreviewTests` + `Build_FirstLegApproved_NextMinistryIsCurrent` + `Build_LastMinistryApproved_MigrationIsCurrent`. Stop F5, rebuild, F5. On **8/-010** Office — no Result dropdown; Advance → Türkmenenergo Submitted. On a ministry — Result Approved/Disapproved updates that ministry’s badge.
- Prevent: Do not label a following ministry’s state as Next step on the current node. Do not keep current on an already-Approved ministry while the dropdown sets the next one.
- Cross-skill: visa2026-application-profile

### 2026-08-17 — Next step dropdown previews the current ministry badge

- Changing Next step to Disapproved left the Energetika badge and header on Approved (recorded state). For the current slot, the badge, header, and Current state now follow the selected Next step when that choice is a result of the same ministry/migration (not the following step).
- Test: `ApplicationWorkspaceProgressAdvancePreviewTests`. Stop F5, rebuild, F5. On **8/-010** Progress → Energetika → Next step Disapproved — badge and “current step: Energetika · Disapproved” update before Advance.
- Prevent: Do not keep CurrentStateLabel on the current node when the officer has picked a same-slot result. Do not retarget the badge when Next step is the following ministry.
- Cross-skill: visa2026-application-profile

### 2026-08-17 — Revert to here stays hidden until Revert progress

- Revert to here on every completed node made a rare jump look like a daily action. **Revert progress** stays visible (current step + Progress rail). **Revert to here** appears only after a successful Revert, for this case visit. Advance, Back to list, or opening another case hides it again.
- Verify: stop F5, rebuild, F5. On **8/-006** Progress — only Revert progress at first; after one revert, Revert to here on completed steps; Advance hides them again.
- Prevent: Do not show Revert to here on first paint. First click of Revert progress must still delete the latest row.
- Cross-skill: visa2026-application-profile

### 2026-08-17 — Ministry letter Preview paints XFA via pdf.js

- Officers often attach a filled Application form (XFA) as the approval letter. Slot iframe showed Please wait. Same pdf.js path as Document copies Application form Preview (`PdfXfaDocument.ContainsXfa`).
- Test: `PdfXfaDocumentTests`. Stop F5, rebuild, F5, Ctrl+F5. Progress letter filename → slot shows the form; Download still XFA for Foxit.
- Prevent: Do not iframe XFA letters. Scanned non-XFA PDFs keep the iframe.
- Cross-skill: visa2026-preview-slot | visa2026-document-copies

### 2026-08-17 — Workspace Advance records officer-entered step date

- Advance always wrote `DateTime.Today`. Officers need the real ministry/migration date on each new `ApplicationProfileInstanceProgress` row. Progress tab Advance now has a Date field (default today); rail Advance from Overview opens Progress first so the date can be set. Still blocked if the date is before the previous row (`DateCannotBeBeforePrevious`).
- Verify: stop F5, rebuild, F5. On **8/-009** Progress — set Date to a past day, Advance; timeline shows that date. Date before the previous step is rejected.
- Prevent: Do not silently overwrite officer Date with today after the picker is set.
- Cross-skill: visa2026-application-profile

### 2026-08-17 — Workspace Progress can revert backward to office

- Officers needed to undo a mistaken Advance (wrong step or wrong ministry letter). History stays append-only: revert **deletes** later rows, it does not rewrite them. **Revert progress** removes the latest row (repeatable through Issued/Rejected/Cancelled). **Revert to here** on a completed slot (including Office preparation) drops every row after that slot. Empty history is implied office again; People lock follows the new latest row. Nested ListView still allows delete-last only.
- Test: `ApplicationProgressRevertHelperTests` + timeline CanRevert flags. Stop F5, rebuild, F5, Ctrl+F5. On **8/-009** Progress: Revert from Migration → previous ministry; Revert to here on Office → implied office; Advance still works.
- Prevent: Do not insert compensating “reverted” rows. Do not iframe/flatten for this. Repeat Revert until office; do not skip-delete older rows except via Revert to here.
- Cross-skill: visa2026-application-profile

### 2026-08-14 — Approval letters stay visible after the ministry step is done

- Upload still attaches `MinistryLetterFile` to the decision row. Case workspace now lists that filename on completed ministry nodes (Progress + Overview) and opens the preview slot. Previously the name was only copied onto the current step.
- Prevent: Do not treat letter preview as current-step-only UI.
- Cross-skill: visa2026-application-profile | visa2026-preview-slot

### 2026-08-14 — Workspace Progress letter preview uses preview slot, not download

- Officer clicks the uploaded ministry letter on the case Progress tab → `#visa-preview-slot` `ProgressLetters` occupant (`OpenPreviewOnly` + `FocusProgressId`). File bytes still come from `ApplicationProfileInstanceProgressMinistryLetterFileAccess` / `ProgressLettersInlinePreview`.
- Prevent: Do not open `target="_blank"` for workspace letter preview. ListView grid `.app-progress-letter-link` still opens the slot catalog.
- Cross-skill: visa2026-preview-slot | visa2026-application-profile

### 2026-08-14 — Officer workspace steps come from Application Profile, not 1_REVIEW_STARTED labels

- Workspace Progress nodes and badges use Application Profile Approval legs + Process & SLA display names (Submitted / Approved). `ApplicationState` codes such as `1_REVIEW_STARTED` remain on history rows for storage/import only. Slot matching uses template display order (1..N), not snapshot Sequence vs parsed `N_REVIEW_*`.
- Prevent: Do not build officer Advance options from ApplicationProgress transition lists when the instance has an Application Profile.
- Cross-skill: visa2026-application-profile

### 2026-08-14 — Workspace Advance ignored embedded profile legs

- First Advance from implied office failed (often silently) when the instance used `ApplicationProfile.ApprovalLegs` instead of `ApprovalLegProfile`. `TryValidateApprovalLegProfileForProgress` required the lookup BO; ministry SLA required `MinistryReviewSlaSettings` even when the profile had `MinistrySlaDays`. Rail Advance with 2+ next steps was a tab switch only.
- Validation now accepts embedded legs/snapshots; profile ministry SLA days satisfy the first `1_REVIEW_STARTED` check. New rows in `ProgressHistory` use latest existing history as previous when the current row is unsaved (same-day advance).
- Prevent: Do not require `ApprovalLegProfile` when the live Application Profile already has approval legs.
- Cross-skill: visa2026-application-profile

### 2026-08-14 — Workspace Progress tab: implied office notes + real timeline

- Empty `ProgressHistory` is implied office (no `IS_BEING_PREPARED` row). The case workspace Progress tab now lists that office step plus real history rows, not four PNG buckets. Officer notes at office persist on `ApplicationProfileInstance.OfficePreparationNotes` and copy onto the first explicit progress row on advance.
- Schema: `OfficePreparationNotes` text/nvarchar(max) via host-start `ApplicationProfileSchemaSql` (`ADD COLUMN IF NOT EXISTS`).
- Prevent: Do not seed a prepare row to make notes work.
- Cross-skill: visa2026-application-profile

### 2026-07-25 — Hide Approval/Migration deadline on Direct migration ListView

- **Request**: Hide **Approval deadline** (`ProgressSlaStatement`) and **Migration deadline** (`MigrationSlaStatement`) on `Application_ListView_DirectMigration` only.
- **Approach**: `Index="-1"` in Blazor `Model.xafml`; `CustomViewClonerUpdater.SetColumnVisibility(..., false)` after DirectMigration clone so cloned defaults from `Application_ListView` do not re-show them. Left Via ministries / default Application lists unchanged.
- **Prevent**: Do not set `[VisibleInListView(false)]` on the BO properties — that would hide deadlines on Via ministries too.
- **Cross-skill**: —

### 2026-07-23 — Postgres ProcessNumber column missing (42703)

- **Symptom**: `Npgsql.PostgresException 42703: column a.ProcessNumber does not exist` on Application ListView (PostgreSQL Demo).
- **Root cause**: ModuleUpdater SQL used `DO $$` blocks; XAF may skip updaters when ModuleInfo is current, and Postgres host-start `ApplyIfMissing` helpers were SQL Server–only.
- **Fix**: Applied columns + backfill on local `visa2026`; switched ensure SQL to `ADD COLUMN IF NOT EXISTS`; added `ApplicationProgressProcessNumberSchemaSql.ApplyIfMissing` and call it from Blazor `Startup` for **both** providers.
- **Prevent**: New Postgres additive columns need host-start `ApplyIfMissing` (or `FORCE_XAF_DB_UPDATE` once), not ModuleUpdater alone; avoid DO-block SQL for simple ADD COLUMN.
- **Cross-skill**: visa2026-lifecycle-docker | —

### 2026-07-23 — Application DisplayCaption + ProcessNumber field

- **Request**: Application DefaultProperty should include migration process number when present (not from "last" progress state).
- **Decision**: Real `ApplicationProgress.ProcessNumber`; denormalized `Application.ProcessNumber` synced from `PROCESS_STARTED` (fallback: Description on that step for pre-field imports). `[DefaultProperty(DisplayCaption)]` → `FullApplicationNumber · ProcessNumber`.
- **Schema**: `ApplicationProgressProcessNumberSchemaUpdater` (SQL Server + Postgres) + Description→ProcessNumber backfill on PROCESS_STARTED.
- **Import**: synthesis writes ProcessNumber (not Description) for PROCESS_STARTED / direct-migration PROCESS_ISSUED.
- **Prevent**: Do not resolve process number from latest progress alone (issued/cancelled often lack it).
- **Cross-skill**: visa2014-to-visa2026-import

### 2026-07-21 — Import: ProcessDate/ProcessNumber on PROCESS_STARTED only

### 2026-07-21 — Import completion from Invitations / WorkPermits

- **Decision**: `PROCESS_ISSUED` when legacy has issued invitation (`ApplicationResult` + `PersonInInvitation`) or work permit (`PersonInApplication.WorkPermit`). Date/number from latest invitation or work-permit evidence; cancelled/rejected apps skip issued.
- **Cross-skill**: visa2014-to-visa2026-import (`Visa2014ApplicationProgressCompletionIndex`).


- **Decision**: Legacy `ProcessDate` + `ProcessNumber` = **Işlenilýär** start, not **Resmileşdirildi**. Import puts `ProcessNumber: …` on `PROCESS_STARTED`; no `PROCESS_ISSUED` from these fields until completion source is mapped.
- **Example**: App `12/-7010` — ministry steps then `PROCESS_STARTED` @ ProcessDate with `ProcessNumber: AS538188`.
- **Cross-skill**: visa2014-to-visa2026-import


### 2026-07-20 — Office implied (no IS_BEING_PREPARED seed)

- **Decision**: Do not write `IS_BEING_PREPARED` progress rows. Empty history = at office until first explicit step (`1_REVIEW_STARTED` / `PROCESS_STARTED`). Catalog code kept for ListView implied label (Ofisde) and legacy rows.
- **Transitions**: First step from empty history is `Ylalaşyga Iberildi` (via ministries) or `PROCESS_STARTED` (direct). Legacy prep rows can still advance.
- **Import**: No synthetic prepare step.
- **Prevent**: Do not re-enable `ApplicationProgressInitializer` seeding.
- **Cross-skill**: visa2014-to-visa2026-import

### 2026-07-20 — Legacy-aligned naming + remove Location from progress

- **Decision**: Restore first-leg-only `1_REVIEW_STARTED` ("Ministrlige iberilen"); legs 2–5 stay approval/rejection only. Drop `ApplicationProgress.Location` — labels are state (+ ministry short name). Keep `ApplicationLocation` catalog unused by progress.
- **Labels**: Ofisde / Sent to ministry / Received from ministry / Processing / Issued (tk+en in `application-state.json` + `LookupCatalogStrings.json`).
- **Transitions**: `prep → 1_REVIEW_STARTED → 1_REVIEW_APPROVED`; later legs from prior approved; suggested next after prep = `1_REVIEW_STARTED`.
- **Import**: synthesize `1_REVIEW_STARTED` then approvals; no Location payload.
- **Schema**: `ApplicationProgressLocationDropSchemaUpdater` drops `LocationID`.
- **Prevent**: Do not re-add `2..5_REVIEW_STARTED` for new progress; do not require Location on progress save.
- **Cross-skill**: visa2014-to-visa2026-import

### 2026-07-17 — ApprovalLegProfile column on Applications (via ministry) ListView

- **Request**: Show **Approval leg profile** on `Application_ListView_ViaMinistries` only (not Direct migration / default Application list).
- **Approach**: Keep `[VisibleInListView(false)]` on `Application.ApprovalLegProfile`; enable the column via model `Index` on ViaMinistries + `CustomViewClonerUpdater.SetColumnVisibility`; captions in DesignedDiffs + tr/ru/tk localization; Blazor `Model.xafml` column width/index.
- **Preload**: `ApplicationListViewPreloadController` Includes `ApprovalLegProfile.MinistryLegs.ApprovingMinistry` so `DefaultProperty` `MinistriesLabel` (e.g. Türkmenenergo-Energetika) renders without N+1.
- **Prevent**: Do not flip `VisibleInListView(true)` globally — that would pollute Direct migration lists; mirror Person TemporaryVisitors `ProjectContract` pattern (view-specific Index).
- **Cross-skill**: —
### 2026-07-10 — Ministrlik empty on ApplicationProgress (missing snapshots)

- **Symptom**: Progress detail **Ministrlik** blank; Progress list status shows `1st ministry review approved` without `- Energetika` (or similar).
- **Root cause**: `MinistryStepLabel` / `StatusListLabel` only read `Application.ApprovalLegSnapshots`. Snapshots are created when the officer changes **ApprovalLegProfile** in the Application detail controller, or during data-import `OnSaving`. Imported / older apps often have a profile + ministry progress rows but **empty snapshots**.
- **Fix (runtime)**: `GetMinistryShortNameForLeg` falls back to live `ApprovalLegProfile.MinistryLegs` → `ApprovingMinistry.ShortNameTm`. `EnsureSnapshots` heals incomplete snapshots on every Application / Progress save.
- **Fix (migrated data)**: `patch/Application-ApprovalLegSnapshots.ps1` / `--backfill-application-approval-leg-snapshots` — do **not** use `ApplicationProgress-MinistryLegs.ps1` (deletes progress).
- **Prevent**: Do not resolve ministry labels from snapshots alone when `ApprovalLegProfile` is set; keep snapshot heal on save for SLA / persistence.
- **Cross-skill**: visa2014-to-visa2026-import | visa2026-onprem-legacy-sync

### 2026-06-15 — Migration service SLA (Phase 2)

- **Scope**: `ApplicationMigrationSlaProfile` tenant lookup + per-`ApplicationType` FK; runtime via `ApplicationMigrationSlaHelper`; ListView columns `WorkingDaysInMigrationStep`, `MigrationSlaStatement`; row tint merged into `ProgressSlaAppearanceCode` (ministry first, then migration).
- **Clock**: `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE` → terminal `PROCESS_ISSUED` / `PROCESS_REJECTED` / `PROCESS_CANCELLED`; Mon–Fri `WorkingDaysHelper`.
- **Validation**: Block progress save to migration step when type lacks profile or `MaxDaysInReview` ≤ 0; warn (non-blocking) on `ApplicationType` save without profile; block profile delete when referenced (`DeleteBehavior.Restrict` + controller).
- **Seed**: `tenant/application-migration-sla-profile.json` (manifest v13); `MigrationSlaProfileCode` on all rows in `ApplicationTypeConfigurationCatalog.json`; wired in `ApplicationTypeConfigurationUpdater` after `LookupCatalogSyncUpdater`.
- **Docs**: [`docs/APPLICATION_PROGRESS_MIGRATION_SLA.md`](../../../docs/APPLICATION_PROGRESS_MIGRATION_SLA.md). Office prep SLA not in scope.
- **Prevent**: Do not add new `ApplicationState` for migration SLA — computed signal only (same as ministry). Reuse `APP_PROGRESS_SLA_*` appearance keys.
- **Cross-skill**: visa2026-bo-state-colors (row CSS) | visa2026-lookup-data (tenant catalog)

### 2026-06-13 — MinistryLetterFileID schema drift (Invalid column name)

- **Symptom**: `SqlException: Invalid column name 'MinistryLetterFileID'` when opening Applications (via ministry) list — stack through lazy-loaded `ProgressHistory` and `PrimaryStateCode`.
- **Try**: Reproduce on DB that existed before `MinistryLetterFile` property; check `ApplicationProgresses` columns.
- **Test**: Restart app after updater; confirm column + FK exist; list loads without SQL error.
- **Root cause**: EF model mapped `MinistryLetterFile` before SQL column existed on upgraded databases.
- **Fix**: `ApplicationProgressMinistryLetterFileSchemaUpdater` + idempotent SQL in `ApplicationProgressMinistryLetterFileSchemaSql`; register in `Module.GetModuleUpdaters` **before** schema sync.
- **Prevent**: Any new scalar FK on `ApplicationProgress` → pre-schema SQL updater pattern; see [reference.md](./reference.md) § Ministry decision letter.
- **Cross-skill**: lifecycle-docker (deploy) | —

### 2026-06-13 — ApprovalLegSnapshots Clear() breaks EF tracking

- **Symptom**: Exception or lost snapshots when replacing ministry legs on contract change.
- **Root cause**: `ObservableCollection.Clear()` sends Reset notification; EF Core change tracker rejects it on aggregated children.
- **Fix**: Delete existing snapshot rows via `ObjectSpace.Delete` in a loop (`ProjectContractMinistryHelper.ApplySnapshot`).
- **Prevent**: Never `Clear()` on XAF aggregated collections — delete entities individually.
- **Cross-skill**: —

### 2026-06-13 — Active contract SLA fields required (Option C)

- **Requirement**: Admin must set expected working days per ministry leg before a `ProjectContract` can be saved as active; officers see SLA warning/overdue on Application ListView.
- **Model**: `ProjectContractMinistryLeg.MaxDaysInReview` + optional `WarningDaysBeforeMax`; copied to `ApplicationApprovalLegSnapshot` on contract selection.
- **Validation**: `ProjectContractMinistryController` + `ProjectContractMinistryHelper.TryValidateLegSla` block commit when `IsActive` and any leg lacks `MaxDaysInReview > 0` or warning ≥ max. Class-level `RuleCriteria` for positive values and warning &lt; max; no `RuleRequiredField` on leg (allows draft leg setup).
- **SLA clock**: Triggers on `{n}_REVIEW_APPROVED` (elapsed days anchored on previous step's date); Mon–Fri via `WorkingDaysHelper`; `ApplicationProgressSlaHelper` + ListView fields `WorkingDaysInCurrentStep`, `ProgressSlaStatement`; row tint via `APP_PROGRESS_SLA_WARNING` / `APP_PROGRESS_SLA_OVERDUE` (overrides workflow color in `ApplicationProgressRowAppearanceController`). Legacy `_REVIEW_STARTED` rows handled for backward compat.
- **Seed/backfill**: Defaults max 10 / warn 8 in `ProjectContractMinistrySeedUpdater` + `ProjectContractMinistryLegSlaBackfillUpdater`.
- **Prevent**: Do not auto-advance progress for overdue; do not add new `ApplicationState` for SLA — computed signal only.
- **Cross-skill**: visa2026-bo-state-colors (row CSS) | —

### 2026-06-13 — RuleCriteria on scalar property breaks login

- **Symptom**: `InvalidOperationException: The 'RuleCriteria' rule can be applied only to class or collection property` on `ProjectContractMinistryLeg.MaxDaysInReview` — app fails at startup / login.
- **Root cause**: `[RuleCriteria]` was placed on scalar `int?` properties; XAF only allows it on the class or on collection properties.
- **Fix**: Move SLA leg rules to **class-level** `[RuleCriteria]` on `ProjectContractMinistryLeg` (same pattern as `WorkPermitItem`, `ApplicationProgress`). Active-contract `MaxDaysInReview` requirement stays in `ProjectContractMinistryController` / `TryValidateLegSla`.
- **Prevent**: Never put `[RuleCriteria]` on scalar members — use class-level criteria or `[RuleValueComparison]` for single-field checks.
- **Cross-skill**: —

### 2026-06-13 — ProjectContractMinistryLeg FK on new contract save

- **Symptom**: `SaveAndClose` on new **Configuration → Project contract** with ministry legs fails: `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId`.
- **Root cause**: Explicit `ProjectContractId` scalar stayed `Guid.Empty` while nested list only populated `MinistryLegs` collection (no back-reference). EF persisted the empty scalar. `OnSaving` alone was insufficient when navigation was null.
- **Fix**: Restore explicit `ProjectContractId`; sync in `WireMinistryLegs` / `PrepareLegsForCommit` / `OnSaving`. App-wide `ProjectContractMinistryLegSaveController` (`WindowController` + `ObjectSpaceCreated`) wires legs on every commit — shadow-only FK inserted NULL under XAF EF.
- **Prevent**: Child BOs with aggregated nested lists — navigation-only parent FK (like `ProjectContractImage`) or wire back-reference before commit.
- **Cross-skill**: —

## 2026-06-15 — ProjectContract ministry leg FK (navigation-only follow-up)

- **Symptom**: SaveAndClose on Project Contract still failed with `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId` after scalar sync attempts.
- **Root cause**: Explicit `ProjectContractId` on the leg let EF insert the child with a FK scalar before the parent relationship was tracked (wrong insert order on new contracts).
- **Fix**: Drop `ProjectContractId` / `ApprovingMinistryId` from `ProjectContractMinistryLeg` BO; EF shadow FK columns via fluent API (`HasForeignKey("ProjectContractId")`). Wire `leg.ProjectContract` only; `PrepareLegsForCommit` calls `SetModified(parent)`. Added `ProjectContractMinistryLegNestedController` for nested ListView `ObjectCreated`.
- **Prevent**: Match `ProjectContractImage` pattern for aggregated children — navigation FK only, no explicit parent scalar on BO.

## 2026-06-15 — ProjectContract ministry leg New (ObjectSpace 1021)

- **Symptom**: New ministry leg from nested list → error 1021 “object belongs to another ObjectSpace” in `CreateDetailView`.
- **Root cause**: `ProjectContractMinistryLegNestedController` `ObjectCreated` assigned `MasterObject` contract and `MinistryLegs.Add(leg)` across object spaces before popup detail opened.
- **Fix**: Removed nested ListView `ObjectCreated` handler. Added `ProjectContractMinistryLegDetailDefaultsController` on leg DetailView — wire parent via `ObjectSpace.GetObject(masterContract)` when opened from `Link` + `PropertyCollectionSource`.
- **Prevent**: Never assign `MasterObject` directly to a child in `ObjectCreated`; resolve with `GetObject` in the **same** ObjectSpace as the child detail view, or rely on commit-time `WireMinistryLegs`.

## 2026-06-15 — ProjectContract ministry leg save NULL ProjectContractId

- **Symptom**: Save on leg popup → `Cannot insert NULL into column 'ProjectContractId'`.
- **Root cause**: Shadow-only FK did not populate when `ProjectContract` navigation was unset; leg popup save did not always run parent wiring before commit.
- **Fix**: Restore explicit `ProjectContractId` / `ApprovingMinistryId` + `SyncForeignKeys()` / `OnSaving`. `TryAttachLegToParent` on commit + leg detail `Committing`. Keep navigation wiring + `SetModified(parent)` for insert order.

## 2026-06-16 — Manual UI create vs JSON seed (parent already exists)

- **Symptom**: Seeded `project-contract.json` legs sync fine; manual **New** contract + ministry leg from UI → `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId`.
- **Root cause**: Seed attaches legs to **persisted** contracts (`FindContract`); UI creates parent + legs together. Explicit `ProjectContractId` scalar on the BO let EF insert the leg before the parent; Blazor leg popup can commit in a separate session.
- **Fix**: Navigation-only parent FK (shadow column) failed under XAF Blazor — NULL `ProjectContractId` on insert. Restored mapped `ProjectContractId` with deferred sync while parent `IsNewObject`; `EnsureLegInObjectSpace` copies popup-session legs into the parent root session; `[RuleRequiredField]` on `ProjectContract` navigation.
- **Prevent**: No mapped parent FK scalar on aggregated children; leg popup on unsaved parent must commit root `ObjectSpace`, not popup session.

## 2026-06-16 — ProjectContract ministry leg FK on Save (scalar pre-fill)

- **Symptom**: Save / SaveAndClose on new **Project contract** with ministry legs → `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId`.
- **Root cause**: `ProjectContractId` scalar was set while the parent contract was still unsaved, so EF inserted the leg before the parent row. Commit-batch detection also missed the parent when legs lived only on `MinistryLegs` (not in `GetObjectsToSave(false)`).
- **Fix**: `SyncForeignKeys` clears `ProjectContractId` until the parent is persisted; `PrepareLegsForCommit` collects legs from nested collections + `GetObjectsToSave(true)` and always `SetModified` on contracts in batch; restore parent-in-batch detection via root object space.
- **Prevent**: Never pre-fill parent FK scalar on aggregated children while parent `IsNewObject`; include nested-collection legs in commit prep (not only `GetObjectsToSave(false)`).

## 2026-06-15 — Blazor NestedFrame parent resolution

- **Symptom**: Leg popup Save still NULL `ProjectContractId`; Link-only wiring missed Blazor nested list.
- **Root cause**: `ApplicationItemCreationContext` pattern — Blazor nested UI uses `NestedFrame` + `PropertyCollectionSource`, not `Link` owner chain.
- **Fix**: `ProjectContractMinistryLegCreationContext` (NestedFrame ViewItem / ListView master / Link). Leg detail controller uses `Frame` on ObjectSaving + Committing. Object-space hooks moved to `Module.Setup` (always subscribed). `FindParentContract` falls back to single `ModifiedObjects` contract.

## 2026-06-15 — Ministry leg popup Save on new contract (nested ObjectSpace FK)

- **Symptom**: Save on ministry leg popup from **new** `ProjectContract` → `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId` (leg commits with `ProjectContractId` set but parent row not inserted).
- **Root cause**: Blazor leg popup can commit in a **nested** `ObjectSpace` while the unsaved parent contract lives in the **root** session — leg-only batch inserts the child before the parent exists.
- **Fix**: `ObjectSpaceHelper.GetRootObjectSpace` (reflection parent chain when present) and `ObjectSpaceHelper.Get(parent)` for EF Core popup sessions. `PrepareLegsForCommit` / `TryFinalizeLegCommit` wire legs on the parent session, `SetModified(parent)`, and redirect leg-only commits to `parentObjectSpace.CommitChanges()`. `WouldOrphanLegForeignKey` detects new parent missing from batch. Last resort: `ProjectContract.SaveBeforeMinistryLeg` user message.
- **Prevent**: Aggregated child popup saves on unsaved parents — resolve parent in **root** OS (`ApplicationItemPersonLinkedDefaults` pattern); do not re-add `ProjectContractMinistryLegNestedController` (`ObjectSpace` 1021).

## 2026-06-16 — Save and Close on leg popup (friendly exception before wire)

- **Symptom**: Save and Close on ministry leg popup from new contract → `UserFriendlyException`: “Save the project contract before saving this ministry leg.”
- **Root cause**: Global `ProjectContractMinistryLegObjectSpaceHooks.OnCommitting` called `TryFinalizeLegCommit` **before** `ProjectContractMinistryLegDetailDefaultsController` could wire parent from `Frame` / main window. `SaveGuard` only hooked `SaveAction`, not `SaveAndCloseAction`.
- **Fix**: Hooks `OnCommitting` → `PrepareLegsForCommit` only; leg detail `Committing` wires parent then `TryCommitParentWithLeg` then `TryFinalizeLegCommit`. `SaveGuard` hooks both Save and Save and Close. `TryFinalizeLegCommit` retries `TryCommitParentWithLeg` per leg; `FindParentContract` searches root `CollectContractsInCommitBatch`.
- **Prevent**: Global object-space hooks must not throw on leg commit before frame-aware controllers run; hook **both** Save and SaveAndClose for popup guard controllers.

## 2026-06-16 — SaveGuard throws before parent resolved (cross-session new contract)

- **Symptom**: Save / Save and Close on leg popup → `UserFriendlyException` in `ProjectContractMinistryLegSaveGuardController.SaveAction_Executing` (not Committing).
- **Root cause**: `TryBringIntoObjectSpace` returned false for an **unsaved** parent contract in the main-window session when the leg popup used a different `ObjectSpace` (`IsNewObject(source)` on target space is false). Parent never wired → `CanCommitLeg` failed in Executing.
- **Fix**: `TryBringIntoObjectSpace` returns the source instance for new parents (callers resolve owning space via `ObjectSpaceHelper.ResolveObjectSpace`). Walk parent `Frame` chain (`Parent` / `TemplateFrame`). `AttachLegInSpace` always targets the parent's session. SaveGuard wires + `TryCommitParentWithLeg` only (no throw in Executing); finalize stays on leg detail `Committing`.
- **Prevent**: Cross-session parent resolution for aggregated children must not require `GetObject` on unsaved parents; defer hard guard to Committing after frame wiring.

## 2026-06-16 — StackOverflow on ministry leg Save (reentrant CommitChanges)

- **Symptom**: `System.StackOverflowException` when Save / Save and Close on ministry leg popup (no usable stack trace in VS).
- **Root cause**: Leg `Committing` redirected to `parentObjectSpace.CommitChanges()`, which re-entered the same leg `Committing` / finalize path (`TryCommitParentWithLeg` → `CommitChanges` loop). `SaveGuard` also called `TryCommitParentWithLeg` from `Executing`, doubling entry points.
- **Fix**: `LegCommitRedirectScope` (`AsyncLocal` depth) around parent `CommitChanges`; leg detail `Committing` skips redirect when scope active; `SaveGuard` only wires parent in `Executing` (redirect stays on `Committing`).
- **Follow-up (still overflowed)**: Nested leg `ObjectSpace` still ran `PrepareLegsForCommit` / `EnsureLegInObjectSpace` / `Delete` during parent redirect; `TryFinalizeLegCommit` duplicated redirect calls; `GetRootObjectSpace` had no cycle guard.
- **Follow-up fix**: `ShouldPrepareLegsOnCommit` skips nested popup OS during redirect; per-OS `PrepareLegsForCommitScope`; `TryFinalizeLegCommit` validates only (no second redirect); no `Delete` on leg copy during redirect; cycle-safe `GetRootObjectSpace` + frame walk.
- **Prevent**: Never call `CommitChanges` from ministry-leg save handlers without a reentrancy guard; one redirect entry point (`Committing`), not `Executing` + `Committing`.

## 2026-06-16 — Save contract without ministry legs

- **Symptom**: Officers cannot save active `ProjectContract` (e.g. GT-16) until ≥1 ministry leg is configured.
- **Root cause**: `ProjectContractMinistryController` blocked commit when `IsActive && !HasConfiguredLegs`.
- **Fix**: Removed contract-save legs-required check. SLA validation (`TryValidateLegSla`) still runs when legs exist. `ApplicationProgressProfileResolver.TryValidateProjectContractOnApplication` still blocks via-ministries applications without legs once progress moves past office preparation.
- **Prevent**: Do not require ministry legs on contract save; enforce at application progress / contract selection time instead.

## 2026-06-16 — Application header lock scope + gear StackOverflow

- **Symptom**: `StackOverflowException` when clicking optional-fields gear on `ApplicationItem_DetailView` after ministry progress; officers could not edit gear-hidden registration fields on in-progress applications.
- **Root cause (gear)**: `OptionalDetailFieldsController.RefreshAfterToggle` called `AppearanceController.Refresh()`, which recursively `ToggleReadOnly` on XAF synthetic layout member `AdditionalFields`.
- **Root cause (lock)**: `ApplicationReadOnlyAfterOfficePreparation` and `TryValidateApplicationUnchangedAfterProgress` locked visa/travel fields, child tabs, and all `ApplicationItem` saves once progress left office prep — too broad for fields filled later in the workflow.
- **Fix**: Gear toggle uses imperative `OptionalDetailFieldsVisibilityApplicator` only (+ reentrancy guard). Lock narrowed to `ApplicationProgressProfileResolver.LockedApplicationHeaderTargetItems` (numbering, date, type, contract); child collection commit blocking removed.
- **Docs**: [`docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md`](../../../docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md) §3.4; [`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md) — interaction + pitfall #6.
- **Prevent**: Do not add workflow/child fields to header lock without product sign-off; do not call `AppearanceController.Refresh()` from optional-fields gear toggle on Blazor.
