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
| 8g | Wizard May produce / cancel / change with Related to | **Done** | Issuance → May produce; Cancellation → May cancel; Change → May change; moved off Results & fields |
| 8o | Wizard Region and City lookups | **Done** | Split Region (city); instance Region + City; defaults + case summary |
| 8n | Wizard Registration Check in / Check out / Info change / Reg extension | **Done** | `RegistrationKind` on profile; Identity radios when Related to = Registration; dashboard SQL predicates ready, views not switched yet |
| 8h | Wizard Approval legs with Directed to | **Done** | Via ministry → legs on Identity; Direct migration hides and clears legs |
| 8i | Wizard Project contract with Directed to | **Done** | Via ministry → Project contract on Identity; gone from Results; instance copy is read-only |
| 8j | Wizard Process & SLA is duration only | **Done** | Removed ministry/migration state Include/SLA-track tables; instance process is Directed to + legs |
| 8k | Profile-specific template applicability | **Done** | Per-row Project contract (Via ministry) or Migration service (Direct) dropdown; instance catalog filters; Project back on Results |
| 8l | Approval leg versions (shared catalog + snapshot) | **Done** | Shared `ApprovalLegProfile`; profile Default only; create picker snapshots. **Phase A seed:** Calik Defaults from VISA2015. **Phase B:** instance FK / Default → snapshots + version name |
| 8m | Locked profile: still set Default approval legs | **Done** | Config lock A keeps other fields read-only; **Default** for this template stays editable; chains edited in Configuration; cannot change Name / May produce |
| 8p | Approval-leg catalog in preview slot | **Done** | Wizard **Edit in Configuration** opens `#visa-preview-slot` catalog CRUD (no XAF ListView). Default stays on wizard radios. **+ New ministry** creates `ApprovingMinistry` in the slot. No Duplicate. |
| 8d | Wizard step 4 real template catalog + persist scope | **Done** | Two officer scopes: Profile-specific and Shared. Shared Include/Exclude; GT-15 names excluded from Shared (upload under Profile-specific). **Preview** uses `#visa-preview-slot` File occupant (master PDF, placeholders). Internal Category/Global type-links unchanged. |
| 8a | Application Profile overview (live) | **Done** | Live config/defaults/legs/templates + linked `ApplicationProfileInstance` rows; overview shows wizard identity, company/signatories, required fields, SLA days, template scope; mock only if profile id unresolved |
| 8c | Custom catalog home (replace native List/Detail UI) | **Done** | List first; row opens overview; **Back to list**; New/Configure → wizard (new tab); **Save profile** reloads catalog; **Delete** when Linked = 0; toolbar **Total: N**; table-body scroll, sticky header |
| 9 | Profile picker at Application create | **Done** | **New** on Application Profile Instances lists only. Via ministry: profile then Approval legs (always, even if one version). Direct: one step. |
| 10 | Person M2M DetailView; hard-remove `ApplicationItem` | **In progress** | Skip-navigation `People` + child BO M2M (includes **MedicalRecord**, **WorkDuty**). Output headers Invitation / WorkPermit / BorderZone / Rejection / IssuedVisas are **1:N** (May produce), not skip-nav. Wizard **May produce** includes Rejection. Person issued tab **Applications (linked)** verified. Rebuild DataImporter + resume Wave 2b (`-StartAt ApplicationProfileInstancePerson`); then People-tab / copies / Resminamalar smoke. |
| 10n | §10 auto-link gate + sticky ResolvedLinks | **Done** | `RequirePerson*` gate; sticky `LinkedObjectId`; toggle-off keeps existing; unit tests |
| 10o | Workspace Linked records tiles from ResolvedLinks | **Done** | Catalog + overview tiles; People tab uses same `cw-link-tile` cards; click shows that person's records; gated by person-config |
| 10x | Case summary: edit instance Use fields | **Done** | Overview tiles + **Edit**; form + **Done**. Application number + date always shown (not profile-gated). Same Use fields. Persist on change; does not edit the profile template. Project is editable here (accepted prototype; do not re-lock via `IsProjectContractLocked`). Host-start adds `EntryCheckPointID`. |
| 10x-fill | Case summary fill-state colors | **Done** | Empty/`—` red; still matches profile default (or auto number/date) blue; officer-changed green. Tiles + Edit form; border + light tint. |
| 10v | People & links New missing person-owned BO | **Deferred** | In-tab **New {type}** removed — officers add person-owned data from **Open person detail**. Issued items stay on Overview → Issued records. `EnsureResolvedLink` kept for Relink. |
| 10w | People & links Relink / Unlink columns | **Done** | Per-person **Relink** and **Unlink** next to Open person detail; Relink pins missing ResolvedLinks; Unlink removes that person + links; both disabled when process-complete locked; toolbar Unlink removed |
| 10p | Process-complete lock on resolved links | **Done** | `PROCESS_ISSUED` / `REJECTED` / `CANCELLED`; roster + ResolvedLinks immutable; UI lock badge |
| 10q | Overview Issued records (1:N headers) | **Done** | May produce tiles + inline Add/New; `IssuedHeaderNestedCreateController` still sets FK on native nested New |
| 10q-iv | Issued visa compose — roster source (extension/direct) | **Done** | Same `#visa-preview-slot` as Path A; people from case roster; `IssuingInvitationItem` null; one visa per person |
| 10q-cue | Issued compose field border cues | **Done** | Orange empty required; blue system defaults; green after blur. Visa + Invitation + WP + Rejection + Border zone compose, plus new native DetailViews for those BOs and items |
| 10a | Application workspace UX shell (mock) | **Done** | `ApplicationWorkspaceHost`, Blazor component, Open workspace action |
| 10b | Wire real M2M + SQL views + resolver | **Done** | `ApplicationPerson` M2M, `ApplicationWorkspaceQueryService`, link/unlink toolbar; SQL views deferred (C# tab builder) |
| 10c | Workspace in-tab actions + person SQL view | **Done** | Link/Unlink/Open detail wired in component; `vw_application_workspace_person`; row selection on Person tab |
| 10d | ListView row opens workspace (default drill-in) | **Done** | `ApplicationListViewWorkspaceNavigationController` — row activate → workspace instead of legacy DetailView |
| 10e | Document copies on workspace (roster line) | **Done** | `ApplicationPerson` keyed catalog + ZIP/preview; `DocumentCopiesLineScope`; legacy `ApplicationItem` ListView path retained |
| 10s | Workspace Document copies person filter + person catalog | **Done** | Header chips toggle roster; catalog grouped by person; Preview/package use filtered `Person.ID`s; slot stays viewer-only |
| 10t | Document copies from linked records (ID labels) | **Done** | No Current/Previous/Next ApplicationItem slots; rows are ResolvedLinks labeled Passport/Visa/… number |
| 10u | §10.2 valid/not-expired auto-link gate | **Done** | Officer-only: Visa/WP/Invitation/BorderZone/Medical must be valid not-expired. **Passport expiration is not checked** (slice 10y). Import uses PersonCurrentItems for Last 1. |
| 10y | Person Last-N auto-link (Passport/Visa/Invitation/WP/Border zone) | **Done** | Profile `Person*LastCount` 1–3. Unique ResolvedLinks index includes `LinkedObjectId`. Missing expected rows flagged, create not blocked. Calik: `pasport_change` Last 2 passports; `cancel_invitation` Last 2 invitation; `cancel_invitation_wp` Last 2 invitation + Last 2 WP; `cancel_visa_wp` Last 2 visa + Last 2 WP; `cancel_workpermit` Last 2 WP. |
| 10f | Profiles rail actions wired | **Done** | Row → profile wizard; `+` → new Application from profile (inherits route from current Application) |
| 10g | Officer UI cutoff (`ApplicationItem` nav/tab/actions) | **Done** | Nav child removed; Person `ApplicationPeople` tab; dossier M2M-only; ListView doc copies disabled |
| 10h | Runtime roster reads → `ApplicationPeople` | **Done** | `ApplicationRosterHelper`; merge/Resminamalar hydration; header AvailablePeople; cancel counts |
| 10i | `Visa.IssuingApplication` dual-read | **Done** | FK + backfill; Path A M2M-first; legacy `IssuingApplicationItem` hidden when app set |
| 10j | Report Dashboard roster SQL + loaders (phase B start) | **Done** | `vw_rd_registration`, `vw_rd_passport`, to-be-checked-in/out; `ReportDashboardRosterQueryHelper`; Travel/Registration on process |
| 10k | Report Dashboard child-link C# filters + `vw_rd_application` | **Done** | Education/Address/Position/Medical Last-N via resolved links + legacy fallback; `vw_rd_application` first person from M2M |
| 10l | Report Dashboard visa extension / work permit SQL | **Done** | `View_VisaExtensionStatus`, `vw_rd_visa_app_progress`, `vw_rd_work_permit_app_progress`, `vw_rd_visa_state`, extension-required CTE; invitation first-person M2M |
| 10m | Report Dashboard ministry + direct-migration SQL | **Done** | `ministry_roster_lines` CTE in 8 embedded views; `ReportDashboardSqlViewResource` placeholder; legacy EF loaders dual-read |
| 11 | Person / Dossier **Start application** | **Removed** | Officers create instances only from Application Profile Instances (picker). Dossier/Person Start process hidden. |
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
| B0 | Blazor officer shell — layout + nav + live queues | **Done** | Native XAF **Application Profiles** folder (templates / via / direct). **Staged profiles** and **In process** hidden from left nav. Custom left rail removed 2026-08-13 |
| B1 | Blazor shell PNG parity polish | **Done** | Chips, legend, pagination, grouped staged, rich grid, toolbar search |
| B2 | Start process domain merge | **Done** | Merge staged rows, `YYYY-NNNN` process number, first progress step |
| B3 | Immersive shell chrome | **Done** | Custom left rail retired; native accordion is the nav. `:has(.officer-shell-host)` hide unused unless leftover shell opens |
| B4 | Profile templates list/grid + detail | **Done** | PNG catalog, chips, pagination, rail overview drill-in |
| B5 | Case workspace 6-tab shell | **Done** | PNG parity pass: overview, people matrix, progress, inline doc copies + Resminamalar, SLA. Tab switch shows top + panel progress bar (same pattern as Report Dashboard) |
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
| E0 | Template AI convert — decisions locked | **Done** | `E-D1`–`E-D8` locked 2026-08-20 in [`TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md`](../../../docs/TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md) §6.1 + §8. Golden set (3 Word + 3 Excel Çalık docs) still outstanding — gates Q5 / pilot exit only |
| E1 | Profile-scoped placeholder set (`IApplicationProfilePlaceholderSetService`) | **Done** | `packKey` added to all **66** entries of `Resources/UserReportPlaceholderCatalog.json` + `UserReportPlaceholderPack` enum; query takes the `ApplicationProfile` BO (no `IObjectSpace`); unknown pack **excludes** (never defaults to allowed); 23 tests, 445 Module.Tests passed. Closes L10 / Q1 / Q13. **Found:** `rootBoTypes` uses `"Application"`, not a `UserReportBoType` member — E1 does not filter on it; see spec §2 note |
| E2 | Instance value map (`IApplicationProfileInstanceValueMapService`) | **Done** | Wraps `UserReportMergeDataHelper.GetPropertyValue` + `GetActiveApplicationItems`, reuses `TemplateTextNormalizer`; sync + takes the instance BO and an injectable `Rows` list, so it is testable with no database; requires the **E1 set**, so a disallowed token can never reach the matcher; `TemplateValueMatchKeys` emits multi-form keys (date renderings incl. Turkmen long form, swapped name order, separator-stripped identifiers, both `1,500` readings); rejects `TooShort` / `SmallNumber` / `Ambiguous` and treats unset dates (`01.01.0001`) as absent. 37 tests, 482 Module.Tests passed |
| E3 | Token writer + diff gate + residual value scan | **Done** | `Visa2026.Module/Services/TemplateConvert/` (15 files) + 37 tests in `Visa2026.Module.Tests/TemplateConvert/`; Solution Debug 0 errors, 422 Module.Tests passed. Token written into the first `w:t` of the span, so **no run splitting was needed**; gate compares structural invariants (Q10); residual scan (Q4) takes probes, not E2 types. Registered via `AddTemplateConvert()` in `Startup.cs` |
| E4 | `TemplateConversionDraft` BO + EF + permissions + expiry sweep | **Pending** | **Adds a table — blocked on slice 10 heal; do not interleave with Wave 2b.** 24 h retention; commit via `ApplicationProfileTemplateUserReportBridge` |
| E5 | Candidate check — suitability score + highlight regions | **Done** | `ITemplateCandidateAnalyzer` + `TemplateTextIndex` (maps normalized match offsets back to original spans — without it every collapsed space or folded `ý` highlights the wrong text). Highlights reuse E3 `DocumentRegion`, so a Match converts straight to a `TokenSubstitution`; tokens come only from the E2 map, so Q8 holds by construction. Longest-match-wins overlaps; Excel = whole cell; roster loop needs 2+ distinct `RowIndex`; gaps limited to unmatched date/6+ digit literals; already-tokenized demotes Pass→Warn; unreadable upload = Fail, not an exception. Thresholds in `TemplateSuitabilityOptions` bound to `TemplateAiConvert:Suitability` (`E-D6`). Contract written up in spec **§4.4**. 18 tests, 500 Module.Tests passed |
| E6 | Ephemeral extract/validate + warning severity tier | **Done** | `IEphemeralTemplateValidationService` wraps the existing stream extractors/validators on in-memory bytes and adds the L10 set check plus the `Error`/`Warning` split (`E-D2`); `HasWarnings` drives the acknowledge checkbox. Registered **scoped** — the wrapped services are scoped. Issues carry a `TemplateValidationIssueCode`, so UI copy and tests never match on message text. `ApplicationProfilePlaceholderSet` now echoes `DataScope`/`TemplateKind` so the merge root (`PeopleM2M` → `ApplicationItem`, else `ApplicationProfileInstance`) comes from the set alone; Excel always validates as `ExcelMergeMode.ItemList`. Loops are balance-checked only — extractors de-duplicate, so document order is gone, and collection names are authoring-defined. The §6.1 "leftover literal" warning stays with the E3 residual scanner; **E7 merges both issue lists**. Contract in spec **§7**. 15 tests, 515 Module.Tests passed |
| E7a | Convert modal — HTML prototype in the officer shell | **Done** (pending officer sign-off) | `wwwroot/officer-shell/` (`template-convert-ui.js`, `template-convert-data.js`, `template-convert.css`). Views **V1–V13** — **all 16 prototype PNGs covered**: happy path (01–05, 13), gap packet (12), confirm (14), edge states (06, 07, 10, 11), fill-preview fallback (16), manual add / AI off (08). PNGs 09 (chat refusal) and 15 (instance picker) are behavior inside V4 and V1, verified by render. Guard regression: `node parity/smoke-edge.mjs` (32 assertions). Mock store mirrors the shipped DTOs field-for-field (`TemplateCandidateReport`, `TemplateValidationReport`, `HighlightRegion` with `DocumentRegion`) so E7b is a fetch swap, not a rewrite. Host = **modal**, not preview slot (spec §4). Entry: templates catalog always; case workspace behind the **L13** per-user switch. Interaction contract (views V0–V11, guards, transitions): [`TEMPLATE_AI_CONVERT_UI_FLOW.md`](../../../docs/TEMPLATE_AI_CONVERT_UI_FLOW.md) — **E7b builds against that doc, not the PNGs** |
| E7b+ | Roster loop derivation (unblock people tables) | **Done** (2026-08-21) | `TemplateRosterLoopPlanner` + `DeterministicPlan`; Convert unblocked when loop markers can be placed; first roster row only tokenized; AI keeps local loops. 4 tests; **175** TemplateConvert green |
| E7b | Convert modal — Blazor lift + deterministic path end to end | **Done, both entries (2026-08-21)** | **Shipped:** `TemplateConvertDialog.razor` + `TemplateConvertOutlineView.razor` (`Editors/`), `wwwroot/css/template-convert.css` (ported from E7a), entry buttons **Convert existing document** / **Add prepared template** in the top action row of `ApplicationReportPackageComponent.razor`, so `ResminamalarSlotPanel` and `OfficerShellCaseResminamalarTab` both inherit them. Sequencing lives in the Module — `ITemplateConvertOrchestrator` (`Analyze` → `ConvertAsync` → `Save`) chains E1→E2→E5→E3→gate→residual→E6 and **merges E6 issues with residual-scan and write-skip warnings** into `Errors`/`Warnings`; Approve is disabled on any error and needs the acknowledge checkbox on warnings. `ITemplateDocumentOutlineReader` is the new display projection (paragraph addresses identical to `DocumentRegion`), so highlights land on the real span. Gate = `TemplateConvertAccess.CanConvertTemplates()` (write on `UserReportTemplate` **and** `ApplicationProfileTemplate`) plus `TemplateAiConvert:Enabled` + `ShowInstanceEntry`. Config-locked profiles can check a document but not Approve. 8 orchestrator tests, 144 TemplateConvert tests pass. **Profile side (wizard step 5, beside `+ Add template`):** same dialog via `OpenForProfileAsync(profile, objectSpace)` — there is no case in context, so V1 gains a **Case to match against** picker (25 newest instances of that profile; empty list tells the officer to use `+ Add template` instead). It reuses the **wizard's** object space and **does not commit**, matching the existing add path where the officer presses **Save profile**; the Done screen says so. No `ShowInstanceEntry` gate here — editing a profile already implies template authoring. **Roster loops:** derived by `TemplateRosterLoopPlanner` (E7b+, 2026-08-21). **Not yet:** merged fill preview (needs the generate pipeline or E4 draft persistence, so Preview lands in the V12 fallback by design); localized strings (see learnings — the message catalog generator currently **reverts** hand-made renames, so the dialog ships English literals) |
| E8 | AI provider abstraction + `None` adapter + plan sanitizer | **Done** (2026-08-21) | `ITemplateConvertAiProvider` + `NoneTemplateConvertAiProvider` (`IsEnabled = false`, returns deterministic `PreMatched`, chat refuses); `ITemplateMappingPlanSanitizer` drops out-of-set tokens / unknown regions / overlaps before the writer; `TemplateMappingRequestBuilder` masks identifier previews (E-D1); options `Provider` / `RequestTimeoutSeconds` / `MaxDocumentCharacters` / `RedactIdentifiersInExtract`; DI falls back to None on unknown keys. Q7 (reflection: no BO / no raw id), Q13 (sanitizer), Q14 (None + stub adapter, no vendor types in Module) — 7 new tests, **151** TemplateConvert green. **Not wired into ConvertAsync yet** — deterministic path unchanged; E9 chat / E10 real adapter call the seam |
| E9 | Preview chat panel (mapping-only, reject out-of-scope) | **Done** (2026-08-21) | Local `TemplateConvertChatIntentClassifier` (L8) short-circuits rewrites to `OutOfScopeContentEdit` before any provider; `ITemplateConvertChatService` calls provider only for mapping intents, then sanitizes; host applies via `ITemplateConvertOrchestrator.ApplyPlanAsync` (shared with Convert). Preview rail shows Adjust mapping chat (hidden on V10 errors). Against `None`: mapping asks get AI-off; rewrites get PNG-09 copy. Q11 / Q12 + classifier theories — 14 new tests, **165** TemplateConvert green |
| E10 | First real AI adapter + per-slot flag + Demo pilot | **Done** (2026-08-21) | `Adapters/AzureOpenAiTemplateConvertAiProvider` — Azure OpenAI Chat Completions over **HttpClient** (no vendor SDK; Q14). Options `TemplateAiConvert:AzureOpenAI` + env `TEMPLATE_AI_CONVERT_AZURE_OPENAI_API_KEY`. `ConvertAsync` calls `ProposeMappingAsync` when `IsEnabled`, sanitizes, falls back to deterministic with a warning on failure. Chat already uses the same provider. Default remains `Provider=None`. Demo: set Provider=AzureOpenAI + endpoint/deployment + API key env on the Demo slot. 6 new tests, **171** TemplateConvert green |

**Template AI convert sequencing:** only **E4** waits for slice 10 (Person M2M / Wave 2b F5 heal) — it is the slice that adds a table. E1, E2, and E3 are schema-free and may run alongside; E3 already did. E0–E9 need no AI vendor. **E7 is split**: E7a is HTML-only in the officer-shell prototype (no Blazor, no services), E7b lifts it. Officer sign-off happens on E7a, so UX churn never costs Razor rewrites. Canonical docs: [`TEMPLATE_AI_CONVERT_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_CONVERT_PRODUCT_SPEC.md) (officer UX, L1–L12) · [`TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md`](../../../docs/TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md) (contracts, E-D1–E-D8).

### Template AI scan (separate feature)

| ID | Slice | Status | Notes |
|----|-------|--------|-------|
| S0 | Product spec + UI flow + prototypes 01–12 | **Done** (2026-08-28) | [`TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md) · [`TEMPLATE_AI_SCAN_UI_FLOW.md`](../../../docs/TEMPLATE_AI_SCAN_UI_FLOW.md) |
| S0e | Engineering spec Phase 0 + playbook stub | **Done** (2026-08-28) | [`TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md) · `Resources/TemplateAuthoring/SCAN_AUTHORING_PLAYBOOK.md` · SD-D1–D10 locked |
| S1 | Playbook loader, scan normalizer, suitability, options, DI | **Done** (2026-08-28) | `Services/TemplateScan/` — ingest pipeline, Spire PDF text OCR, 11 tests |
| S2 | Vision provider + field plan merger | **Done** (2026-08-28) | `ITemplateScanAiProvider`, deterministic planner, merger, Azure vision adapter; 20 TemplateScan tests |
| S3 | Blazor modal — upload + field review (V1–V2, V7–V8, V10–V11) | **Done** (2026-08-28) | `TemplateScanDialog.razor`, `TemplateScanFieldReviewView.razor`; **entry is case Resminamalar** (wizard Templates no longer creates files, 2026-09-02) |
| S4 | Clarification chat (V3) | **Done** (2026-08-28) | `ITemplateScanClarificationService`, intent classifier, chat UI, Azure clarify |
| S5 | Docx builder + Generate + preview/validate (V4–V5) | **Done** (2026-08-28) | `TemplateScanOrchestrator`, `ScanDraftDocxBuilder`, `TemplateScanPreviewView.razor` |
| S6 | Save helper extract + case Resminamalar entry | **Done** (2026-08-28) | `ApplicationProfileTemplateSaveHelper`, V6 Done view, Resminamalar entry (wizard create removed 2026-09-02) |
| S7 | Gap packet export (V9) + `TemplateScan` audit category | **Done** (2026-08-28) | `ScanGapPacketExporter`, `TemplateScanGapHelpView`, runtime log category |

**Template AI scan sequencing:** independent of Convert E4 gate (no new BO in v1). S1 → S2 → S3/S4 → S5 → S6 → S7. S1–S3 can ship with `Provider=None` (entry disabled). Do **not** merge into `TemplateConvertDialog`.

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
- Respects `ApplicationProfileLockHelper` — read-only banner for locked config; **approval-leg versions** stay editable; **Save profile** remains for version changes
- Steps: Identity · **Company, Signatories** · Results & fields · Process & SLA (embedded legs) · Templates & person · Review & save
- **May produce** / **May cancel** / **May change** live under Identity **Related to** (`ActionFamily`): Issuance → produce; Cancellation → cancel; Change → change
- **Registration is** Check in / Check out / Info change / Reg extension (`RegistrationKind`) when Related to = Registration; cleared for other families
- **Approval legs** live under Identity **Directed to** as named **versions**; visible only for Via ministry; instances snapshot the chosen version at create
- **Project contract** lives under Identity **Directed to**; visible + required only for Via ministry; Direct migration hides and clears it. Results & fields no longer lists Project. Instances copy the contract at create and cannot edit it.
- Identity wizard edits **Name** only — Description, Code, and Selection/quick code stay on the BO (auto Code at create) and are not shown on that step. The **name stays in the wizard header** on every step.
- **Process & SLA** is Ministry/Migration **days** only. State Include/SLA-track checklists are not officer-configured and do not drive instance Advance.
- **Project** is back on Results & fields (instance default). Profile-specific templates can bind to Project contract (Via ministry) or Migration service (Direct); Resminamalar catalog hides non-matching rows. Empty binding = all instances.
- Results default-value lookups load as ID/name snapshots (`ApplicationProfileWizardLookupData`); Default value is enabled only when Use is checked

**Deferred (later slices):** template file upload in wizard (attach binary on standard profile detail nested templates ListView). `ApplicationProfileProgressStateSetting` table retained unused (do not wire as a process designer).

**Slice 8b — Wizard prototype parity (2026-08-07):** Step 2 defaults/signatory table · Step 3 ministry/migration state checklists (`ApplicationProfileProgressStateSetting`) · Step 4 template add/edit/remove in wizard. **Identity XAF applicability scope cards removed (2026-08-27)** — picker uses audience/route/`IsActive` only.

**Verify:** Configuration → Application Profiles → select row → **Configure profile**; edit and **Save profile**; locked profile → read-only + **Clone** escape hatch.

---

## Slice 8l — Approval leg versions — **Done**

**Goal:** Officers pick a ministry chain at instance create. Already-started instances keep a snapshot.

**Locked (2026-08-20 redesign):**

1. **Reuse:** tenant-shared `ApprovalLegProfile` (Configuration), like Company / Signatory — **not** per-profile copies.
2. **Per profile:** `DefaultApprovalLegProfile` only (which shared version is pre-selected at create).
3. **After create:** instances **keep the ministries they started with**.

**Delivered:**

- Wizard Identity: shared list + **Edit in Configuration** / Refresh; radio **Default for this template**
- Create picker: required shared version cards; ministries copied to `ApplicationProfileInstanceApprovalLegSnapshot`
- Progress timeline reads **snapshots first**
- Calik seed sets Defaults from VISA2015 frequency and **clears nested** `ApplicationProfileApprovalLegVersion` copies
- **Phase B:** imported via-ministry instances keep their inferred `ApprovalLegProfile`; empty FK uses template Default; missing snapshots + `ApprovalLegVersionName` are filled on F5 (`ApplicationProfileInstanceApprovalLegBackfill`) and `--backfill-application-approval-leg-snapshots`

**Verify:** stop F5, rebuild, F5. Configure profile → Identity shows shared versions. New application → pick a version. Open an imported via-ministry case — Ministrlik / snapshot legs match the instance chain (not necessarily the template Default).

---

## Slice 8m — Locked profile still sets Default approval legs — **Done**

**Goal:** Config lock A does not freeze this template's Default. Chains are edited in Configuration. Instances already keep snapshots.

**Delivered:**

- Wizard Identity: **Default** radio stays enabled when the rest of the profile is read-only
- **Save profile** remains on Review when Via ministry + locked
- **Edit in Configuration** / Refresh stay available
- Nested save guard still blocks templates / progress-state settings
- Started cases are not restamped

**Verify:** stop F5, rebuild, F5. Open a **Config locked** Via ministry profile → Identity → change Default → Review → **Save profile**. Name / May produce stay disabled. Open an in-process case — ministries unchanged.

---

## Slice 8p — Approval-leg catalog in preview slot — **Done**

**Goal:** **Edit in Configuration** on wizard Identity opens the shared `ApprovalLegProfile` catalog in `#visa-preview-slot` (prototypes 01–05). No XAF ListView / DetailView / OK-Cancel lookup.

**Delivered:**

- Catalog (search, **+ New**, **Open**)
- Empty catalog and New form (Cancel / Create)
- Unused chain: edit ministries, Save / Delete; **+ New ministry** creates `ApprovingMinistry` in the slot (Short name + Official name) then appends it to the chain
- In-use chain: ministries locked; Code / Active still saveable
- Slot does **not** set Default — wizard radios + Refresh remain

**Verify:** stop F5, rebuild, Ctrl+F5. Configure a via-ministry profile → Identity → **Edit in Configuration**. Slot catalog; Open a used chain → ministries locked, Code/Active still saveable; **+ New** → Create; Refresh radios; Default unchanged unless officer picks it.

---

## Slice 9 — Profile picker at Application create (detail) — **Done**

**Goal:** New Application starts with profile selection (live FK + defaults), not a blank form.

**Delivered:**

- `ApplicationProfilePickerHost` + `ApplicationProfilePickerHost_DetailView`
- `IApplicationProfilePickerQueryService` — active profiles, route filter, MRU sort, applicability criteria
- `ApplicationProfilePickerNewController` — intercepts **New** on Application ListViews (skipped during data import)
- **Use profile (live link)** creates Application, sets `ApplicationProfile` + dual-read `ApplicationType`, applies defaults, opens DetailView
- Via ministry: **Continue →** then **Choose Approval legs** (always, even when there is one version; Default pre-selected). Direct migration: **Use profile** on step 1.
- Locked profiles remain selectable (config lock badge only)

**Verify:** Applications via ministry → **New** → pick profile → **Continue** → Approval legs (profile name in header) → **Use profile**. Direct migration: pick → **Use profile**.

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

## Slice 11 — Person / Dossier Start application (removed)

**2026-08-27:** Officers create Application Profile Instances **only** from Application Profile Instances lists (Choose Application Profile picker). Person DetailView and Person Dossier **Start process…** are hidden.

Via-ministry picker is **two steps**: profile → Approval legs (always shown, even when there is one version). Direct migration stays one step. People are linked later on the case.

`ApplicationStartFromPersonHelper` remains for roster linking after create.

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

**Open speed (2026-08-26):** Row click still opens the case workspace (not native DetailView). First paint no longer heals person links or loads all lookup catalogs (cities/contracts). Catalogs load when the officer taps **Edit** on case summary. Linked records batch-load; issued “has copy” does not materialize `File.Size`.

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
| C | Placeholder derive vs constrain | **Closed for template AI convert** (`E-D5` — constrain; E1 is the single token source). Still open for other placeholder surfaces |
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
