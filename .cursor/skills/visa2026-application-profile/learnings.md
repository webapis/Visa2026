### 2026-09-24 — Application Result ignores Seretmezlik people

- **Need**: People on a Seretmezlik letter should not keep Invitation / Visa / Work permit / Border zone “Missing” on Application Result (or ListView Netije chips). Example: 4 on case, 1 excluded → 0/3 not 0/4. All excluded → expected 0 → Result complete.
- **Cause**: Result expected used the full roster; coverage roster ids did not drop exclusions. Seretmezlik is always before those issued docs.
- **Fix**: `RosterPersonIds` removes excluded ids (`GetExcludedPersonIds`); case builder expected count uses that active roster; ListView `ApplyTo` loads exclusion pairs and uses the same active set. Numbers only — no Seretmezlik note on Result overview.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Open a case with Seretmezlik — Application Result denominators match non-excluded people; instance list Netije chips match.
- **Prevent**: Do not count Seretmezlik people toward Result expected or coverage. Do not add a Seretmezlik caption on Result overview without an explicit UX ask.
- **Cross-skill**: —

### 2026-09-24 — Overview fits a 1280×800 window

- **Need**: Case summary tiles on a 14-inch officer PC crushed (border-zone text, Process number cut off). That PC is the minimum.
- **Cause**: Measured screen is **1280×800 at 100%** (working area 1280×752). XAF sidebar is 320px. Overview then keeps a 300px section nav and a 270px rail, so the summary pane is about 300px and the 3-column tile grid plus a side identity column cannot fit.
- **Fix**: `case-workspace.css` only, Overview tab. At a window of 1101–1280px the rail drops under the summary. Inside a summary pane under 760px, number / date / process sit in one row on top and the other tiles use two columns. Wider monitors keep the 3-column summary and the side rail.
- **Officer**: Stop F5, rebuild the Blazor host, hard-refresh. On this 1280×800 screen, Overview should show №, application date, and process number in one row, and the case tiles in two columns. Readiness / Quick actions sit under the summary.
- **Prevent**: Do not shrink the 300px section nav. Do not treat a phone width as the Overview minimum. Other workspace tabs are not on this breakpoint yet.
- **Cross-skill**: —

### 2026-09-24 — Overview tiles were stretching on both window sizes

- **Need**: After the first Overview pass, a narrower window still showed a giant Urgency tile, and a wider window stretched Case summary tiles to the height of the identity column.
- **Cause**: `.cw-summary-body` used `align-items: stretch`, so the shorter tile row grew to match the taller column. The rail-drop rule keyed off the browser window (`max-width: 1280px`), so a pane that was already narrow beside the 320px sidebar kept the rail.
- **Fix**: Tiles stay content height (`align-items: flex-start`, grid `align-content: start`). Rail drops under Overview when the case pane itself is under 1120px (the 1280×800 screen after the sidebar).
- **Officer**: Hard-refresh `localhost:5001`. Tiles should hug their text. On this PC, Readiness / Quick actions sit under the summary.
- **Prevent**: Do not set `align-items: stretch` on `.cw-summary-body`. Do not key the Overview rail drop off the full window width.
- **Cross-skill**: —

### 2026-09-21 — Report Dashboard residual C# sync (not a rewrite)

- **Need**: Dashboard SQL already used instances + profiles; C# still filtered `ApplicationType != null` and Open ListView still named `ApplicationItem_ListView`.
- **Cause**: Dual-read leftover after ApplicationItem hard-remove and instance rename.
- **Fix**: Profile-first EF filters (`ReportDashboardProfileInstanceQuery`); Registration `VwRd*` ListViews; Travel mock uses `Person_ListView`.
- **Officer**: Rebuild Blazor host. Open Report Dashboard Registration / Application (via ministry) — profile-only cases should appear; Registration Open ListView should not error.
- **Prevent**: Do not re-join ApplicationTypes in `vw_rd_*`. Do not restore ApplicationItem ListView targets.
- **Cross-skill**: visa2026-report-dashboard

### 2026-09-21 — Import adult FM Education as Orta / Orta mekdep / Orta bilim

- **Need**: Already-imported family members showed Tehniki howpsuzlyk we zähmeti goramak instead of the VISA2014 auto-create triple.
- **Cause**: Import copied live VISA2015 specialty 1:1. Audit shows those rows were created as Orta bilim then mutated in legacy.
- **Fix**: Education transform + `--correct-visa2014-family-member-education` force Orta / Orta mekdep / Orta bilim for age 18+ family members. Country and year stay. Children unchanged.
- **Officer**: Rebuild DataImporter, run `--correct-visa2014-family-member-education` on the target DB (local PG already patched: 114 updated, 2 created). Hard-refresh the FM Educations tab.
- **Prevent**: Do not leave adult FM Education as a 1:1 specialty copy. Do not turn RequirePersonEducation off.
- **Cross-skill**: visa2014-to-visa2026-import

### 2026-09-21 — Family member Educations tab is visible

- **Need**: Officers could not see the auto-created Orta education row because the FM detail hid Educations.
- **Cause**: `EmployeeOnly` / `EmployeeOnly_PersonRecordTabsLayout` hid Educations; FamilyMember layout had Educations `Removed="True"`.
- **Fix**: Educations stays on family-member Person record tabs (index 0). Position / salaries / work duties stay hidden. Temporary visitor Educations stays removed.
- **Officer**: Rebuild Blazor host, hard-refresh. Open Asiye ÖZÇUBUKOĞLU (or any adult FM) → **Educations** tab. New adult FM after DOB + save shows Orta / Orta mekdep / Orta bilim.
- **Prevent**: Do not hide Educations for family members. Do not un-hide it for temporary visitors.
- **Cross-skill**: —

### 2026-09-21 — Adult family member gets default Orta education on create


- **Need**: Legacy VISA2014 added Education Orta / Orta mekdep / Orta bilim for adult family members. Visa2026 only had those strings as report captions, so new adult FMs stayed Education 0 on People & links.
- **Cause**: No Education row was created on Person. Children correctly have none; adults need a real BO for completeness.
- **Fix**: `FamilyMemberDefaultEducation` on DateOfBirth (ImmediatePostData) and Person.OnSaving. Age 18+, family member, no existing Educations, CountryOfBirth set. Looks up catalog NameTm. Graduation year = birth year + 17. Skip children, employees, temporary visitors, and `MigrationImportContext.IsDataImport`.
- **Test**: `FamilyMemberDefaultEducationTests` + existing child caption tests passed.
- **Officer**: Stop F5, rebuild Blazor host. New family member with DOB 18+ should have Education 1 after save. Under 18 still Çaga with no Education row.
- **Prevent**: Do not turn RequirePersonEducation off. Do not invent Education during VISA2014 import. Do not use Education.OnCreated IsDefault (that is Ýörite Orta).
- **Cross-skill**: visa2014-to-visa2026-import (import still does not invent Education)

### 2026-09-21 — Child dependent Education tile is not a gap


- **Need**: Wiza Maksadyny Üýtgetmek FM People & links showed Education **0** (red) for a child (Elzem). Children usually have no Education BO; legacy import does not invent one.
- **Cause**: Profile `RequirePersonEducation` is on for adult dependents. Completeness used Last-N expected 1 for every roster person. Merge already prints **Çaga** via `ChildDependentEducationCaption`.
- **Fix**: People & links Education for `ChildDependentEducationCaption.Applies` (non-employee, age under 18 or marital Çaga) is caption-only **Çaga** when no Education is linked. Adult dependents still require Education. If a child has an Education row, the normal count tile stays. Start-from-person incomplete check skips children.
- **Test**: `ChildDependentEducationCaptionTests` (completeness caption not short; adult 0/1 still short). 25 related tests passed.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Open the FM case — child Education shows **Çaga**, not red 0. Adult Education stays 1 or red 0.
- **Prevent**: Do not turn `RequirePersonEducation` off on ForFamilyMember profiles. Do not create dummy Education rows in VISA2014 import.
- **Cross-skill**: visa2026-document-copies (skip Education.Current LinkMissing for the same child rule)

### 2026-09-19 — Progress sits before Netije; both cells stay white

- **Need**: Ýüztutmanyň ýagdaýy at the end of the row, immediately before Ýüztutmanyň netijesi. No green/blue row fill on those two cells.
- **Cause**: Stepper replaced Ýagdaý in the middle. Row-state CSS paints every td.
- **Fix**: Progress column last-but-one. `lv-plain-cell` forces white on both columns (selected/hover too).
- **Officer**: Rebuild Blazor host, hard-refresh the instance list.
- **Prevent**: Do not leave the stepper after Adam sany. Do not let visa-progress-row color those two cells.
- **Cross-skill**: —
### 2026-09-19 — ListView shows the workspace progress path

- **Need**: Officers want Ofis / ministry / Migrasiya on the instance list without opening each case, like Netije chips.
- **Cause**: ListView only had LatestProgressState text (one step).
- **Fix**: `ProgressStepsDisplay` replaces Ýagdaý. Same slots as workspace Progress (`BuildDisplay`). One batched history query in `ApplicationListViewPreloadController`. Direct = Ofis + Migrasiya. Display-only. Netije stays last.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Via / Direct / default lists show the mini stepper.
- **Prevent**: Do not call workspace Build / ProgressHistory per row. Do not add a second progress column.
- **Cross-skill**: visa2026-application-progress (slot codes unchanged)

### 2026-09-19 — Result chips overlap until AutoFit runs after preload

- **Need**: Netije chips in one row without covering Ýagdaý / dates.
- **Cause**: `ListViewGridColumnFitController` AutoFit runs on open when the cell is still `—`, so the column shrinks. Nowrap chips then overflow.
- **Fix**: `MinWidth` 560 on the column. AutoFit again after chip template + first preload `Refresh`.
- **Officer**: Rebuild Blazor host, hard-refresh the instance list.
- **Prevent**: Do not AutoFit the instance list only at first paint. Do not use overflow:visible on the chip cell (that overlaps neighbors).
- **Cross-skill**: —

### 2026-09-19 — ListView shows the same result coverage as Netije syny

- **Need**: Officers want Işlenen / Garaşylýan people counts on the instance ListView without opening each case.
- **Cause**: Coverage lived only on Ýüztutmanyň netijesi.
- **Fix**: `ResultCoverageDisplay` after Adam sany. Chips reuse `ApplicationWorkspaceIssuedResultOverview` (items, roster intersect, Rejection optional). Batch load in `ApplicationListViewPreloadController`. Types the profile does not produce are omitted. `—` when the profile produces nothing.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Via ministry / Direct / default lists show **Ýüztutmanyň netijesi** chips.
- **Prevent**: Do not count headers. Do not treat Ret 0 as missing. Do not add five separate type columns.
- **Cross-skill**: —

### 2026-09-19 — Result lists are person-level for all issued types

- **Need**: Work permit / invitation / rejection / border zone Hereketler lists should identify people, like visa.
- **Cause**: Those tiles listed headers (number + date) only.
- **Fix**: Table rows are InvitationItem / WorkPermitItem / RejectionItem / BorderZoneItem (person, passport, header fields). Button `Count` stays distinct headers. Click still opens the header.
- **Officer**: Rebuild + hard-refresh. Open Iş rugsady / Çakylyk / Ret / Serhet — each person is a row.
- **Prevent**: Do not show only the header number for issued types that have items.
- **Cross-skill**: —

### 2026-09-19 — Issued visa list shows person and passport

- **Need**: Visa number alone does not show who the visa belongs to.
- **Cause**: Issued list used Title = VisaNumber and Subtitle = issue date.
- **Fix**: Visa Hereketler table: person, passport, visa number, type, category, issued, expiration, process number.
- **Officer**: Rebuild + hard-refresh. Open Berlen wiza — each row names the person.
- **Prevent**: Do not list issued visas by number only.
- **Cross-skill**: —

### 2026-09-19 — Result status cards are not buttons

- **Need**: Netije syny bars are informational. Hereketler must look clickable.
- **Cause**: Both rows were pale cards; top cards were also `<button>`.
- **Fix**: Top type cards are `<article>` (no click, no hover lift). Hereketler uses filled `cw-issued-btn` with Aç / + Goş CTA.
- **Officer**: Hard-refresh CSS. Click only the blue Hereketler buttons.
- **Prevent**: Do not wire onclick on Netije syny status cards.
- **Cross-skill**: visa2026-preview-slot

### 2026-09-19 — Result % is per type; do not sum people

- **Need**: Işlenen 26 with 14 linked people looked wrong. Officers want a % on each BO type, not one ring for all types.
- **Cause**: Totals added WP people + visa people (15+11). Coverage also counted WP item people who are not on the roster (15/14).
- **Fix**: Coverage = distinct **roster** people only. `Sum` is required **types** complete/short (not people). Removed the overall ring. Each type card shows its own %.
- **Officer**: Rebuild + hard-refresh. WP at most **14 / 14**. Visa **11 / 14 · 79%**. No 26.
- **Prevent**: Do not add issued people across invitation / WP / visa. Do not count item people who are not linked to the case.
- **Cross-skill**: —

### 2026-09-19 — Result tab dashboard polish

- **Need**: Ýüztutmanyň netijesi looked sparse — empty white boxes, weak hierarchy.
- **Cause**: First overview was status cards only (ratio + pill).
- **Fix**: Completeness ring + tinted Işlenen / Ýetmezçilik / Garaşylýan, per-type progress bar, status badge. Counts unchanged. Hereketler still below.
- **Officer**: Hard-refresh after rebuild (CSS versioned). Result tab should read as one dashboard.
- **Prevent**: Do not put compose tiles back on Overview. Do not change item/person coverage or Rejection optional.
- **Cross-skill**: visa2026-preview-slot (keep officer-shell CSS copies aligned)

### 2026-09-19 — Result overview counts items; Rejection optional

- **Need**: One work permit / invitation / rejection header can cover many people. Visa is one per linked person. Rejection is not required.
- **Cause**: Netije syny used header `Count`. Shared-header Expected=1 was also wrong.
- **Fix**: `CoverageCount` = distinct people on InvitationItem / WorkPermitItem / RejectionItem / Visa.Passport.Person (issued-from header FK, not input M2M). Expected = roster except Rejection (`IsOptional`, Expected 0). `Sum` skips Rejection. Hereketler still uses header `Count`.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. 1 WP with 14 items shows **14 / 14**. Ret stays optional.
- **Prevent**: Do not count WorkPermit / Invitation / Rejection headers for overview. Do not treat zero Rejection as missing. Do not use instance.WorkPermitItems / InvitationItems input M2M.
- **Cross-skill**: —

### 2026-09-19 — Application result overview issued / missing

- **Need**: On Ýüztutmanyň netijesi, officers see what is issued, what is missing, and how many — without opening each tile.
- **Cause**: Result tab only had action tiles (count or + Add).
- **Fix**: `ApplicationWorkspaceIssuedResultOverview` + tile `ExpectedCount` / `IsOptional`. Expected = roster. Rejection optional (zero is not missing). **Netije syny** totals + per-type cards; **Hereketler** tiles stay below. Nav missing badge.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Result tab shows Işlenen / Ýetmezçilik / Garaşylýan then type cards.
- **Prevent**: Do not put compose tiles back on Overview. Do not count Rejection as missing.
- **Cross-skill**: visa2026-preview-slot

### 2026-09-19 — Application result is its own workspace nav

- **Need**: Invitation / work permit / visa / rejection / border zone issuance on its own section — the application result. Türkmençe nav **Ýüztutmanyň netijesi**.
- **Cause**: Issued tiles lived at the bottom of Overview.
- **Fix**: `CaseTabs` key `result` after Progress. Card lifted to `RenderApplicationResult`. Title `Workspace.ApplicationResult` (not Progress `Workspace.Result` / Netije). Office-prep gate does **not** block this tab.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Overview no longer shows issued tiles. After Ýüztutmanyň soň ýagdaýy: **Ýüztutmanyň netijesi**.
- **Prevent**: Do not put issued tiles back on Overview. Do not reuse `Workspace.Result` (progress Advance label).
- **Cross-skill**: visa2026-preview-slot

### 2026-09-19 — Organization is last workspace nav

- **Need**: Gurama (Organization) letterhead on its own section, last left-nav item — it changes less often than case summary.
- **Cause**: Company / Signatory / Representative lived on Overview under the case-summary tiles.
- **Fix**: Last `CaseTabs` key `organization`. Card lifted to `RenderOrganization`. Wide main (no rail). Office-prep gate does **not** block this tab (same as People).
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Overview no longer shows Gurama. Last nav is **Gurama**.
- **Prevent**: Do not put the letterhead card back on Overview. Do not add Organization to `IsProcessTab`.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Overview nav is Ýüztutma

- **Need**: Case workspace left-nav Overview in Türkmençe must be **Ýüztutma**, not Syn.
- **Cause**: `ApplicationProfileInstance.Workspace.Overview` tk-TM was Syn.
- **Fix**: tk-TM → **Ýüztutma** (JSON + generated catalog).
- **Officer**: Hard-refresh Türkmençe. First nav item reads **Ýüztutma**.
- **Prevent**: Do not use Syn for this workspace Overview tab.
- **Cross-skill**: —

### 2026-09-19 — Widen case workspace left nav

- **Need**: Instance workspace left nav wide enough for Türkmençe labels (Ýüztutmanyň soň ýagdaýy) without wrapping.
- **Cause**: `.cw-layout` first column was 210px.
- **Fix**: Nav column **300px** (overview, docs, wide, loading). `.cw-nav__item` `white-space: nowrap`.
- **Officer**: Hard-refresh (CSS). Nav labels stay on one line.
- **Prevent**: Do not shrink the first grid column back to 210px.
- **Cross-skill**: visa2026-preview-slot

### 2026-09-19 — ListView Issued stays English

- **Need**: Application Profile Instance ListView **Ýagdaý** cell **Issued** must be **Işlendi** in Türkmençe (and other outcomes via the same map).
- **Cause**: `LatestProgressState` returned stored `LatestProgressDisplay` (English snapshot from sync). Column caption was already Ýagdaý.
- **Fix**: Display-only `ProgressStepLabel` on `LatestProgressState` and `CurrentState`. Stored `LatestProgressDisplay` stays English.
- **Officer**: Stop F5, rebuild, hard-refresh Türkmençe. Status column reads **Işlendi**.
- **Prevent**: Do not write culture-specific text into `LatestProgressDisplay`. Localize at the getter.
- **Cross-skill**: —

### 2026-09-19 — Drop duplicate overview rail document / issue shortcuts

- **Need**: Overview right rail should not repeat Resminamalar, document copies, and Issue record — those already exist on left nav and Overview issued tiles.
- **Cause**: Quick actions copied the three shortcuts next to Advance / Revert.
- **Fix**: Removed the three rail buttons and unused `IssueRecordQuickAction`. Left **Ýagdaýy Ilerletmek** / **Ýagdaýy yzyna öwürmek**.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Rail Quick actions no longer shows the three shortcuts.
- **Prevent**: Do not add those three back to the overview rail.
- **Cross-skill**: visa2026-resminamalar, visa2026-document-copies, visa2026-preview-slot

### 2026-09-19 — Turkmen SLA tab stays English

- **Need**: Case workspace **SLA we möhletler** tab in Türkmençe (metrics, timeline, step table, source).
- **Cause**: `RenderSla` used English literals. Step names came from English catalog (`Office preparation`). Tone labels (`On track`) were English in the builder and UI.
- **Fix**: Added `Workspace.SlaSub` / `OverallCase` / `SlaByStep` / `Due` / `OnTrack` / … keys. Wired page chrome. Display-only `ProgressStepLabel` on step names. `ToneLabel` / `SlaToneLabel` use Layer A. Builder English fallbacks (`Profile template`, alert text) localized at display.
- **Officer**: Stop F5, rebuild Blazor host in Visual Studio, hard-refresh, Türkmençe. Title **SLA we möhletler**. Table: **Ädim / Möhlet senesi / Galan gün / Ýagdaý**. Office step **Ofisde taýýarlyk**. Status **Möhletinde**.
- **Prevent**: Do not change CompletenessGate Office preparation logic key. Localize SLA chrome at display.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Organization card stays English

- **Need**: Overview Organization (Gurama) card in Türkmençe: heading, letterhead hint, Company / Signatory / Representative, field labels.
- **Cause**: Card used English literals. Keys already existed (`Workspace.Organization`, `OrgSub`, `OrgCallout`, `Picker.Company`/`Signatory`/`Representative`, `Org.Name`…).
- **Fix**: Wired view + edit mode in `OfficerShellCaseWorkspaceComponent` (including + Täze, Bellenen, Bellenen et).
- **Officer**: Stop F5, rebuild Blazor host in Visual Studio, hard-refresh, Türkmençe. Card reads **Gurama**, **Kompaniýa**, **Ygtyýarly gol**, **Ygtyýarly wekil**, **At / Kod / Telefon / Salgysy / E-poçta / Salgyt**.
- **Prevent**: Do not leave Organization chrome as English literals when Layer A keys exist.
- **Cross-skill**: —

### 2026-09-19 — Turkmen overview rail Readiness / Quick actions stay English

- **Need**: Overview right rail Readiness and Quick actions in Türkmençe.
- **Cause**: Headings and buttons were English literals. Keys already existed (`Workspace.Readiness`, `RequiredDataComplete`, `BoStatesValid`, `QuickActions`, `OpenDocuments`, `GenerateResminamalar`, `IssueRecord`).
- **Fix**: Wired `OfficerShellCaseWorkspaceComponent` overview rail to those keys. Missing-count suffix stays `CaseSummary.MissingOne` (yetmezçilik).
- **Officer**: Stop F5, rebuild Blazor host in Visual Studio, hard-refresh, Türkmençe. Rail reads **Taýýarlyk**, **Hökmany maglumatlar taýýar**, **Ýazgy ýagdaýlary dogry**, **Çalt hereketler**, **Resminama nusgalaryny aç**, **Resminamalar paketini döret**, **Ýazgy ber…**.
- **Prevent**: Do not leave overview rail chrome as English literals when Layer A keys exist.
- **Cross-skill**: —

### 2026-09-19 — Turkmen issued compose and list columns stay English

- **Need**: Month = **Aý**. List Process number / visa columns, issued tiles, and New work permit / rejection / visa drawers in Türkmençe.
- **Cause**: `ProcessNumber` (XafDisplayName "Process number") had no list ColumnInfo. Issued tiles used English catalog `Definition.Label` / AddCaption / PanelTitle. Compose drawers (`IssueIssuedHeaderSlotPanel`, `IssueIssuedVisaSlotPanel`) hardcoded English field labels.
- **Fix**: ProcessNumber + VisaType/Period/Category/MigrationService on Application* and ApplicationProfileInstance list columns (Month already Aý). Display-only `IssuedRecordLabel` / `IssuedAddCaption` / `IssuedPanelTitle` / `IssuedComposeTitle` / `IssuedStatus`. Wired overview issued card and compose property labels. Catalog English kept for tests.
- **Officer**: Stop F5, rebuild Blazor host in Visual Studio, hard-refresh, Türkmençe. List Month = **Aý**, Process number = **Proses belgisi**. Tiles: Iş rugsady / Ret / Berlen wiza. Drawers: Iş rugsadynyň belgisi, Ret edilen resminamanyň belgisi, Wiza belgisi.
- **Prevent**: Do not change IssuedRecordsCatalog Definition strings. Localize at display. Do not map Office preparation as a logic key.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Resminamalar This profile is Şu Ýüztutma degişli

- **Need**: Resminamalar “This profile” tab in Türkmençe must be **Şu Ýüztutma degişli**.
- **Cause**: Tab used Bu profil ({0}).
- **Fix**: `ApplicationReportPackage.ThisProfileTab` tk-TM → `Şu Ýüztutma degişli ({0})`.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe.
- **Prevent**: Do not use Bu profil for this instance catalog tab.
- **Cross-skill**: visa2026-resminamalar

### 2026-09-19 — Turkmen Preview is Görmek

- **Need**: Preview action in Türkmençe must be **Görmek**, not Öňünden görmek.
- **Cause**: Resminamalar and Document copies used the longer invented verb.
- **Fix**: `ApplicationReportPackage.Preview` and `ApplicationItemDocumentCopies.Action.Preview` tk-TM → **Görmek**.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Preview buttons read **Görmek**.
- **Prevent**: Do not use Öňünden görmek for this action.
- **Cross-skill**: visa2026-resminamalar, visa2026-document-copies

### 2026-09-19 — Turkmen People tab is Ýüztutmadaky adamlar

- **Need**: Case workspace People & links nav in Türkmençe must be **Ýüztutmadaky adamlar**.
- **Cause**: Layer A used “Adamlar we baglanyşyklar”.
- **Fix**: `ApplicationProfileInstance.Workspace.PeopleLinks` tk-TM → that phrase. Help lines that named the tab updated to match.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Left nav reads **Ýüztutmadaky adamlar**.
- **Prevent**: Do not reuse “Adamlar we baglanyşyklar” for this tab.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Progress tab is Ýüztutmanyň soň ýagdaýy

- **Need**: Case workspace Progress nav in Türkmençe must be **Ýüztutmanyň soň ýagdaýy**, not the short Ýagdaýy.
- **Cause**: Officer later replaced the short Progress caption.
- **Fix**: `ApplicationProfileInstance.Workspace.Progress` tk-TM → that phrase. Native Progress group / CurrentState stay Ýagdaýy.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Left nav Progress reads the long caption.
- **Prevent**: Do not shorten this workspace tab back to Ýagdaýy. Still do not use Ösüş.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Issued records heading

- **Need**: Issued records heading in Türkmençe must be **Migrasiýa gullugy tarapyndan resmileşdirilen resminamalar**.
- **Cause**: Overview card title was still the English literal; tk-TM was “Berlen ýazgylar”.
- **Fix**: Wired `Workspace.IssuedRecords`. tk-TM → that phrase.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Issued card title uses the long heading.
- **Prevent**: Do not use Berlen ýazgylar for this heading.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Linked records heading

- **Need**: Linked records heading in Türkmençe must be **Ýüztutmadaky kişiniň resminama maglumatlary**, not Baglanan ýazgylar.
- **Cause**: Layer A used a short invented caption.
- **Fix**: `ApplicationProfileInstance.Workspace.LinkedRecords` tk-TM → that phrase.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Overview card title uses the long heading.
- **Prevent**: Do not shorten this heading back to Baglanan ýazgylar.
- **Cross-skill**: —

### 2026-09-19 — Turkmen linked-record tiles stay English

- **Need**: Overview Linked records heading, lock banner, and tiles (Passport, Education, …) in Türkmençe.
- **Cause**: Headings were literals. Tile labels come from English `LinkedRecordsCatalog.Definition.Label`.
- **Fix**: Display-only `LinkedRecordLabel`. Wired heading, lock, empty, overview tiles, People tiles, rail summary. Catalog Label stays English for tests.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Tiles read Pasport, Bilim, Wezipe, Salgysy, Wiza, Iş rugsady, Aýlyk, Saglyk.
- **Prevent**: Do not change catalog Definition.Label.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Activity rail stays English

- **Need**: Overview Activity list (Activity, Merged N profiles, Submitted / Approved / Issued) in Türkmençe.
- **Cause**: Heading was a literal. Row titles are English from `BuildActivities` / catalog DisplayName.
- **Fix**: Display-only `ActivityTitle` / `ActivitySubtitle`. Heading uses `Workspace.Activity`. Outcomes reuse ProgressStepLabel (Tabşyryldy / Ylalaşyldy / Işlendi). Builder English kept for tests.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Activity rail is Turkmen.
- **Prevent**: Do not change stored activity Title strings used by tests.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Edit is Üytget

- **Need**: Case summary **Edit** in Türkmençe must be **Üytget** (officer spelling).
- **Cause**: Organization Edit was still the English literal. Case summary Edit used Üýtget.
- **Fix**: `ApplicationProfileInstance.Workspace.Edit` tk-TM → `✎ Üytget`. Organization section uses the same key.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. The pencil button reads **Üytget**.
- **Prevent**: Do not invent Üýtget for this button. Use Üytget.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Arza on instance is Ýüztutma

- **Need**: Case summary tiles **Arza belgisi** / **Arza senesi** must use **Ýüztutma**, not Arza.
- **Cause**: Field keys reused the older Arza wording.
- **Fix**: `ApplicationProfile.Field.ApplicationNumber` / `ApplicationDate` tk-TM → **Ýüztutma belgisi** / **Ýüztutma senesi**. Doc Application → **Ýüztutma**. Instance member captions match.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Overview tiles read Ýüztutma belgisi / Ýüztutma senesi.
- **Prevent**: Do not use Arza for Application Profile instance field labels.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Office preparation is Ofisde taýýarlyk

- **Need**: Office preparation in Türkmençe must be **Ofisde taýýarlyk**, not Edara taýýarlygy.
- **Cause**: Layer A invented Edara taýýarlygy for the display map of `OfficeLabel`.
- **Fix**: `ApplicationProfileInstance.Workspace.OfficePreparation` tk-TM → `Ofisde taýýarlyk`. Logic key stays `"Office preparation"`.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Progress step reads **Ofisde taýýarlyk**.
- **Prevent**: Do not use Edara for this step label.
- **Cross-skill**: —

### 2026-09-19 — Turkmen instance Progress tab + Taslama belgisi / Ýüztutma

- **Need**: With Türkmençe selected, Application Profile instance Progress still showed English (Progress, Office preparation, History, letter upload, SLA rail, Case workspace tab). Project Contract column should be **Taslama belgisi**. Case workspace chrome should be **Ýüztutma**.
- **Cause**: Progress tab and rail used English literals. Step/history labels used English catalog `DisplayName` / `OfficeLabel`. XAF host caption stayed “Case workspace”. List ProjectContract tk-TM was “Taslama şertnamasy”.
- **Fix**: Display-only `ProgressStepLabel` / `ProgressReason` (do not change `Office preparation` logic key). Wired Progress tab, overview stepper, history, SLA rail. Host + DetailView tk-TM → **Ýüztutma**. ProjectContract member/columns/class tk-TM → **Taslama belgisi**. Header instance number tk-TM → **Ýüztutma № {0}**.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Progress tab and workspace tab should be Turkmen; list column **Taslama belgisi**.
- **Prevent**: New Progress chrome goes through `VisaUiMessages`. Do not localize `ProgressTimeline.OfficeLabel` const — map it only at display.
- **Cross-skill**: —
- **Remaining**: Overview Organization field literals; People/Issued tile catalogs; Convert dialog.

### 2026-09-19 — Turkmen nav: Ýüztutmalar

- **Need**: Left-nav folder and both children must be **Ýüztutmalar**, not “Ýüztutma profiliniň ýüzlemeleri”.
- **Cause**: Layer A used the long instance caption for `Application`, `Application_ViaMinistries`, and `Application_DirectMigration`.
- **Fix**: tk-TM → `Ýüztutmalar`, `Ýüztutmalar (ministrlik arkaly)`, `Ýüztutmalar (migrasiýa gönümel)`. Same short name on instance ListViews and officer-shell crumbs.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Left nav shows **Ýüztutmalar** and the two route children.
- **Prevent**: Do not reuse the long “profiliniň ýüzlemeleri” phrase for this nav group.
- **Cross-skill**: —

### 2026-09-19 — Turkmen progress outcomes: Ylalaşyldy / Tabşyryldy / Işlendi

- **Need**: Approved = **Ylalaşyldy**, Submitted = **Tabşyryldy**, Issued = **Işlendi**, Revert progress = **Ýagdaýy yzyna öwürmek**.
- **Cause**: Timeline used English catalog `DisplayName` (Submitted/Approved/Issued). Revert button was still an English literal.
- **Fix**: `ApplicationProfileLocalization.ProgressOutcome` maps those labels. Timeline + SLA header use it. `Workspace.Issued` tk-TM → Işlendi. Revert wired on rails and Progress tab.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Stepper/activity show Tabşyryldy / Ylalaşyldy / Işlendi; revert button is Ýagdaýy yzyna öwürmek.
- **Prevent**: Do not show raw English catalog DisplayName on the case workspace. Keep Office preparation as the logic key (do not map it).
- **Cross-skill**: —

### 2026-09-19 — Turkmen Advance progress is Ýagdaýy Ilerletmek

- **Need**: Officer button **Advance progress** in Türkmençe must be **Ýagdaýy Ilerletmek**.
- **Cause**: Rail and Progress tab still used the English literal; draft tk-TM was “Ýagdaýy öňe geçir”.
- **Fix**: `ApplicationProfileInstance.Workspace.AdvanceProgress` tk-TM → `Ýagdaýy Ilerletmek`. Wired overview rail, progress rail, and Progress tab button.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. The blue button reads **Ýagdaýy Ilerletmek**.
- **Prevent**: Do not invent a different Turkmen verb for this action.
- **Cross-skill**: —

### 2026-09-19 — Turkmen Progress caption stays Ýagdaýy

- **Need**: Case workspace Progress tab (and native Progress group) must stay **Ýagdaýy** in Türkmençe, not Ösüş.
- **Cause**: Layer A localization invented Ösüş for Progress. Existing officer copy already uses Ýagdaýy for status/progress.
- **Fix**: `ApplicationProfileInstance.Workspace.Progress`, blazor-layouts Progress group, and validation Progress template `tk-TM` → `Ýagdaýy`. Regenerated catalog + xafml.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh, Türkmençe. Case workspace left nav shows **Ýagdaýy**.
- **Prevent**: Do not translate officer “Progress” to Ösüş. Use Ýagdaýy.
- **Cross-skill**: —

### 2026-09-19 — Localize Application Profile template and instance (Layer A)

- **Need**: Officer UI for Application Profile templates and Application Profile instances in en-US / tr-TR / tk-TM / ru-RU (nav, catalog, wizard, picker, case workspace chrome).
- **Cause**: Custom Blazor catalog/wizard/picker/workspace used English literals. Instance rename also left English captions on leftover Application class members until a stub class caption was added.
- **Fix**: `ApplicationProfileLocalization` + `UiStrings.application-profile.json` (XAF class/view/enum) + `UiStrings.application-profile-messages.json` (`ApplicationProfile.*` / `ApplicationProfileInstance.*`). Wired catalog, all five wizard steps, picker, overview cards, officer-shell crumbs/staged/templates, workspace header and nav. Regenerated `VisaUiMessageCatalog.g.cs` and culture xafml.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Switch language (Türkmençe / Türkçe / Русский). Check Configuration → Application Profile Templates (catalog + Configure wizard) and Application Profile Instances (picker + case workspace tabs).
- **Prevent**: New officer chrome goes through `VisaUiMessages` / `ApplicationProfileLocalization`, not English literals. After JSON edits run `dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj`. Do not localize `ProgressTimeline.OfficeLabel` — CompletenessGate compares `"Office preparation"` as a logic key.
- **Cross-skill**: —
- **Remaining**: Case workspace Organization / Progress / SLA / People tile body copy; Convert dialog (E7b still English); some picker status strings (host-not-ready). DisplayHelper tests stay English under default en-US catalog.

### 2026-09-19 — Cannot uncheck the last border zone (Etrek comes back)

- **Need**: Case summary Border zones popup. Unchecking the last box (e.g. Farap) rechecked a previously cleared zone (Etrek). Officers could not clear all zones.
- **Cause**: Popup treated an empty draft as “not initialized” and copied committed `SelectedItems` back. Case-summary `BorderZoneLocationField` also re-parsed `Value` on every render and reset the draft.
- **Fix**: Seed the popup draft once when it opens. Empty draft = none selected (OK stores `Ýok`). Do not `SyncFromValue` while the popup is open.
- **Officer**: Stop F5, rebuild Blazor host, hard-refresh. Open Border zones, uncheck the last etrap — Selected: 0, OK saves `Ýok`.
- **Prevent**: Do not rehydrate checkbox draft from the saved string after the officer clears the last item.
- **Cross-skill**: —

### 2026-09-18 — RequirePersonVisa visible ≠ Resminamalar Visa Ready

- **Need**: After enabling Active Visa on change_workpermit, SANAW still CHECK empty Visa_*.
- **Cause**: UI uses profile RequirePersonVisa; merge dry-run needs CurrentVisa on the roster line. Type ShowCurrentVisa was still false; visibility gate cleared on Type.Show*.
- **Fix**: See resminamalar learnings 2026-09-18. Restart so ApplicationType Show* syncs.
- **Cross-skill**: visa2026-resminamalar

### 2026-09-18 — Additional WP location shows Active Visa link

- **Need**: Case 9/-007 Iş Rugsatnama goşmaça barjak ýeri People & links hid Visa (column `--`, no Visa chip).
- **Cause**: Calik profile `change_workpermit` / `App_Additional_WP_location` had `RequirePersonVisa=false` (and type `ShowCurrentVisa=false`). Workspace linked-records catalog keys off `RequirePersonVisa`.
- **Fix**: Seed `RequirePersonVisa=true` on `application-profile.calik-energi.json`; align `ShowCurrentVisa=true` on ApplicationType catalog. Does not set `ProduceVisa` (case still issues WP + location only).
- **Officer**: stop F5, rebuild Module, restart so tenant profile sync applies; open 9/-007 — Visa column + chip visible. If count stays 0, Relink or pin active visa (import historically skipped Visa for this type).
- **Prevent**: Do not treat additional WP location as visa-issuing (`ProduceVisa` stays false). Visibility is roster Active Visa, not issued-visa tile.
- **Cross-skill**: visa2026-lookup-data
### 2026-09-17 — Case workspace open: skeleton loading UI + faster first paint

- **Need**: Opening an Application Profile Instance showed a bare “Loading case workspace…” line for up to ~7s.
- **Cause**: First paint waited on a cartesian EF Include (people × progress × links × approval-leg versions), Organization catalog lists, Document copies nav completeness, Last-N counts that lazy-loaded every visa, and a full-table used-invitation scan. Loading UI was a single muted sentence.
- **Fix**: Workspace skeleton (kicker, indeterminate bar, shimmer chrome). `AsSplitQuery` + drop version-chain Include. Organization dropdowns load on **Edit**. Document copies badge after first paint. Last-N uses SQL skip-nav filters; used invitations scoped to roster. Progress timeline built once.
- **Test**: `IssuedDocumentLifecycleTests` (scoped used-ids + NotChanged filters); `Resolve_uses_instance_relations_when_set` expects empty org options. Officer: stop F5, rebuild, Ctrl+F5, open a via-ministry or direct instance.
- **Prevent**: Do not Include `ApprovalLegVersions.Legs` on every case open. Do not `ListCompanies/Signatories/Representatives` until Organization Edit. Do not scan every `Visa.IssuingInvitationItem` for Last-N.
- **Cross-skill**: visa2026-application-profile

### 2026-09-17 — This-profile template contract dropdown listed every Project contract

- **Need**: Officer bound a case letter to a different Project contract than Overview **Project**, so Resminamalar hid it.
- **Cause**: Create from yellow marks / Add existing loaded the full `ProjectContract` catalog.
- **Fix**: Instance authoring offers **All contracts** plus this case’s Project contract (or Migration service). Tests: `LoadApplicabilityItemsForInstance_*`.
- **Prevent**: Do not list unrelated contracts when the case already has one.
- **Cross-skill**: visa2026-template-scan

### 2026-09-17 — Add existing template missing Project contract (via ministry)

- **Need**: Via-ministry case Resminamalar **Add existing template** had Save to but no Project contract. Yellow marks already had **All contracts** / one contract.
- **Cause**: `TemplateConvertSaveRequest` did not pass `SetApplicability` or `ApplicableProjectContractId`.
- **Fix**: Convert dialog reuses the Scan dropdown under **This profile only**. Save sets applicability the same way as yellow marks. Shared catalog still clears the FK.
- **Test**: `Save_request_this_profile_carries_project_contract`. Officer: stop F5, rebuild; Add existing on a via-ministry case → This profile only → Project contract.
- **Prevent**: Do not treat Add existing as unbound This-profile when yellow marks binds a contract.
- **Cross-skill**: visa2026-template-scan | visa2026-resminamalar

### 2026-09-16 — Create wizard lodging list ignores To city

- **Need**: After To city = Sarahs, Business trip lodging still listed every UYJ (Aşgabat, Kaka, …).
- **Cause**: `FilterSitesByCity` kept rows with null `CityId` (`CityId == null || CityId == toCity`). Lodging.City often unloaded, so the clause matched everything.
- **Fix**: Keep only lodgings whose City Id or catalog City name matches To city. Load City + lodging.json City by FullAddress. Same filter for hotel/hospital/other site. Changing To city clears a mismatched site.
- **Test**: `FilterSitesByCity_KeepsOnlyLodgingsForSelectedToCity`, `FilterSitesByCity_MatchesCatalogCityNameWhenCityIdMissing`. Officer: stop F5, rebuild; Sarahs lodging should be Döwletabatgazçykaryş UYJ only.
- **Prevent**: Do not treat null CityId as “belongs to every city”. Site lookups follow ToCity, not legacy City.
- **Cross-skill**: —

### 2026-09-16 — Create wizard still one city per region after NameTm match

- **Need**: Officer rebuilt; Ahal From city still only Akbugdaý, Mary To city still only Mary etraby.
- **Cause**: Demo/prod often leave `City.RegionName` null. Only the profile-default city has `Region` loaded. `AsNoTracking` blocked lazy-load of RegionID on the rest, so name fallback never ran.
- **Fix**: Load cities tracked + `Include(Region)`; fill missing region from embedded `city.json` by NameTm; map that title to the selected region's Id. From and To both use this list.
- **Test**: `CatalogRegionNameForCity_ReadsEmbeddedCityJson`. Officer: stop F5, rebuild Module (not hot reload). Ahal From city should list Gökdepe, Kaka, Tejen, Akbugdaý, …
- **Prevent**: Do not query `City` with `AsNoTracking` when Region may be lazy. Do not require `City.RegionName` to be populated.
- **Cross-skill**: —

### 2026-09-16 — Create wizard From/To city show one etrap per region

- **Need**: After region filter, Ahal/Balkan To city (and From city) listed a single etrap instead of all cities in that region.
- **Cause**: Case summary reused `CitiesForRegion` but copied the English UI label (`Ahal province`) into `RegionName`. Most catalog cities store `Ahal welaýaty` and only the Region-FK row matched.
- **Fix**: Load regions with `NameTm` (`LoadRegions`). From city and To city both filter via `CitiesForSelectedRegion` + that catalog. Name fallback still uses Turkmen titles.
- **Test**: `CitiesForSelectedRegion_FiltersToCitiesInSelectedRegion` (FK + NameTm rows; English display name). Officer: stop F5, rebuild; From city and To city for Ahal should list Gökdepe, Kaka, Tejen, …
- **Prevent**: Do not set region `RegionName` from localized `DisplayName`. Keep `NameTm` for city.RegionName matching.
- **Cross-skill**: —

### 2026-09-16 — Create wizard To city lists every city

- **Need**: Instance create Case summary To city should only show cities in the selected To region (Mary showed Ahal/Lebap/…).
- **Cause**: `ApplicationWorkspaceCaseHeaderFieldsHelper.Build` bound From/To city dropdowns to the full `City` catalog. Profile Results wizard already filtered via `CitiesForRegion`; Case summary did not.
- **Fix**: Load cities with RegionId; `CitiesForSelectedRegion` filters To city by `ToRegion` and From city by `FromRegion`. Changing region still clears a mismatched city (`TryApply`).
- **Test**: `CitiesForSelectedRegion_FiltersToCitiesInSelectedRegion`. Officer: stop F5, rebuild, create Iş sapary — pick Mary To region, To city should be Mary etraps only.
- **Prevent**: Do not pass unfiltered `catalogs.Cities` to From/To city lookups. Reuse `ApplicationProfileWizardLookupData.CitiesForRegion`.
- **Cross-skill**: —

### 2026-09-15 — Imported roster: empty Medical soft completeness


- **Need**: Legacy often has no MedicalRecord; linked Medical tile stayed red (0) even on locked/complete imported cases.
- **Cause**: Medical used fixed Last-N quota (always expect ≥1), unlike Visa/TravelHistory active-pool soft rules.
- **Fix**: Medical in `UsesActivePool` + load linkable medical counts; expected 0 when none available and (links locked **or** `IsManualEntry`). Officer-created unlocked with no medical still expects 1.
- **Test**: `ApplicationWorkspaceLastNExpectedTests` Medical_* cases.
- **Prevent**: Do not invent dummy Medical rows. Soft only when person has zero linkable medicals.
- **Cross-skill**: visa2014-to-visa2026-import
### 2026-09-15 — Imported BT: empty From region/city not readiness gaps

- **Need**: Legacy has no From*; after From/To geo Require*, imported Iş Saparyna Gitmek showed red tiles + "2 missing".
- **Cause**: Case summary treated empty From* as Empty fill for all instances.
- **Fix**: `FromGeoLookupFill` — when `IsManualEntry` and From* null, FillState=Default (not Empty). Officer-created (`IsManualEntry` false) still red/required. No Ýok Region/City invent.
- **Test**: `Build_EmptyFromGeo_IsDefaultFill_WhenManualEntryImport`, `Build_EmptyFromGeo_IsEmptyFill_WhenOfficerCreated`.
- **Prevent**: Do not seed fake Ýok geography FKs for import. Do not soften To* or officer-created From*.
- **Cross-skill**: visa2014-to-visa2026-import
### 2026-09-15 — From/To Region+City profile geo (Schema A)

- **Need**: Wizard and case summary need distinct origin and destination geography for BT departure/arrival and internal check-in/out — not bare Region/City as destination-only.
- **Cause**: Instance had Region/City (+ FromCity/ToCity) with RequireRegion/City meaning destination; From city was a BT+RequireRegion hack; letters mixed Region/City with ToCity.
- **Fix**: Real `FromRegion`/`ToRegion` FKs; migrate Region/City → To*; profile Require/Default From*/To*; wizard shows four required rows only when `UsesFromToRegionCity`; case summary four tiles; import writes ToRegion/ToCity only (no From* from legacy). Bare Region/City Obsolete dual-read.
- **Test**: `ApplicationProfileInstanceBusinessTripPlaceholderTests`; build Module + Blazor + DataImporter.
- **Prevent**: Do not show bare Region/City on geo profiles. Do not invent From* on VISA2014 import. Abroad registration profiles stay without this geo block.
- **Cross-skill**: visa2014-to-visa2026-import, visa2026-user-report-templates
### 2026-09-15 — Business trip letter uses From city + Region/City

- **Need**: Cover letter four geo tokens (from region/district → to region/district) while case overview only showed one Region + City.
- **Cause**: BT profiles `RequireRegion`/`RequireCity` (destination) with `RequireRegionCity` false hid FromCity. Merge From* used obsolete `Name` instead of `NameTm`.
- **Fix**: Show From city when ActionFamily is BusinessTrip and RequireRegion. To* tokens read Region+City (`NameTm`), fallback ToCity. From* stay on FromCity.
- **Test**: `ApplicationProfileInstanceBusinessTripPlaceholderTests`.
- **Prevent**: Do not map both origin and destination from the single Region/City pair. Hotel/address type is not a letter token; sanaw uses **BTAD**.
- **Cross-skill**: visa2026-user-report-templates, visa2026-template-scan

### 2026-09-15 — Business trip destination uses site catalogs (deprecate BusinessTripAddress)

- **Need**: Instance case-summary Business trip address should resolve into Lodging/Hotel/Hospital/OtherSite like AddressOfResidence Type UX — not a parallel FreeAddress catalog, and not reuse the person `AddressOfResidence` BO.
- **Cause**: `BusinessTripAddress` was City+FullAddress find-or-create on import; profile/instance FK pointed at that catalog.
- **Fix**: Add `BusinessTripAddressType` + `BusinessTripLodging`/`Hotel`/`Hospital`/`OtherSite` + private-house text on `ApplicationProfileInstance` (and profile defaults). Case summary + wizard Type→site. Import resolves tenant site catalogs only (PrivateHouse fallback). `[Obsolete]` on `BusinessTripAddress`; dual-read legacy FK. Docs: `DEPRECATED.md`.
- **Test**: Module + DataImporter build (SourceLink off if sourcelink.json locked); `ApplicationWorkspaceCaseHeaderFieldsHelperTests.Build_IncludesBusinessTripAddressWhenRequired` expects Type field.
- **Prevent**: Do not find-or-create `BusinessTripAddress` mid-import. Do not point instance trip destination at `AddressOfResidence`. Reuse Lodging/Hotel/Hospital/OtherSite tenant JSON.
- **Cross-skill**: visa2014-to-visa2026-import


### 2026-09-15 — Link existing person misses Turkish last names (Yılmaz)

- **Need**: Officer searched Hasan Yılmaz on Çakylyk we Iş Rugsadnamasyny Ýatyrmak 9/-003 People & links. Person exists (PN 23404632120) but picker showed no matches.
- **Cause**: Search tokens are diacritic-folded (`Yılmaz` → `yilmaz`) then AND-matched with `Contains(Lower([LastName]), token)`. Stored `ı` is not folded, so last-name token never matches.
- **Fix**: `PersonSearchTextNormalizer.FoldedLowerContainsCriteria` applies the same fold map to First/Middle/Last name in Person identity criteria (link picker + Person ListView + instance linked-people search).
- **Test**: `BuildPersonIdentityCriteria_FoldsTurkishLastNameTokenAndStoredField`; `FoldedLowerContainsCriteria_ReplacesDotlessIAfterLower`. Officer: stop F5, rebuild. Search `Hasan Yılmaz` or `23404632120`. Until restart, search personal number or passport `U20433151`.
- **Prevent**: Do not fold query tokens without folding stored name columns the same way.
- **Cross-skill**: visa2026-application-profile



- **Need**: Çakylygy Ýatyrmak / Çakylyk we Iş Rugsatnamasyny Ýatyrmak must not attach an InvitationItem that already has an issued visa.
- **Cause**: `CanLinkInvitationItem` already treated `IssuedVisa != null` as used, but Relink often has that inverse unloaded. Auto-link then pinned the used line. Relink also kept existing sticky pins.
- **Fix**: `IsUsed` / Relink / People & links expected count query `Visa.IssuingInvitationItem`. Cancel Relink drops used invitation pins. `EnsureResolvedLink` refuses a used invitation id. Import snapshots still skip the officer gate.
- **Test**: `CollectMissingAutoLinks_SkipsUsedInvitationOnCancelInvitation`; `ShouldDropUsedInvitationLinkOnCancelRelink_TrueOnlyOnCancelInvitationProfile`. Officer: stop F5, rebuild, Relink on a cancel-invitation case — used (issued-visa) invitations stay off the tile; unused invitations still link.
- **Prevent**: Do not treat `IssuedVisa` navigation as loaded. Do not re-pin used invitations on cancel Relink as “sticky.”
- **Cross-skill**: visa2026-application-profile

### 2026-09-14 — WorkPermitItem expiration save blocked by parent-application rule

- **Need**: Officer changed Expiration Date on Work Permit Item 1249/12 (Serdar Nuri Küçükakkaya). Save showed "The selected employee is not part of the parent application."
- **Cause**: `IsEmployeeValid` requires the person on `WorkPermit.ApplicationProfileInstance.People`. Opening the item from Work Permit Item nav does not load that skip-nav roster, so the rule failed on any save — including a date-only edit.
- **Fix**: Skip the roster check for already-saved items (`!IsNewObject`). New lines still check, with `IsPersonOnApplication` querying ObjectSpace when People is not loaded.
- **Test**: `ApplicationRosterHelperTests`. Officer confirmed Save of Expiration Date on 1249/12 after rebuild.
- **Prevent**: Do not require a lazy People collection for scalar edits on issued WP/invitation lines.
- **Cross-skill**: visa2026-application-profile
### 2026-09-14 — Travel history only on Registration templates

- **Need**: Wiza we Iş Rugsatnamasyny Uzaltmak People & links still showed Travel history 0 (red). Officers want Travel history only on Registration templates.
- **Fix**: `ApplicationProfileTravelHistoryPolicy` allows the tile only when `ActionFamily = Registration`. Calik JSON true on the 8 registration codes; false on `extend_visa_wp` and all other families. Catalog `ApplyRow` / mapper / resolver / wizard follow the policy.
- **Test**: `ApplicationProfileTenantCatalogTravelHistoryTests` + resolver issuance vs registration facts.
- **Prevent**: Do not turn Travel history on for visa+WP extension or other non-registration templates. Restart after rebuild so catalog sync writes locked profiles.
- **Cross-skill**: visa2026-lookup-data | visa2014-to-visa2026-import
### 2026-09-14 — extend_visa_wp links WorkPermitItem being extended

- **Need**: Wiza we Iş Rugsatnamasyny Uzaltmak (5/-1636) People & links had Visa but no Work permit tile. Officers must attach the `WorkPermitItem` being extended.
- **Cause**: 2026-08-22 set `RequirePersonWorkPermitItem` / `ShowCurrentWorkPermitItem` false so dual-read would not show the checkbox. Produce Work permit stayed on (new letter).
- **Fix**: Calik `application-profile.calik-energi.json` `extend_visa_wp` `RequirePersonWorkPermitItem` **true**. ApplicationType catalog `App_Visa_and_WP_Ext` `ShowCurrentWorkPermitItem` **true**. Import strategy `calikExtendVisaWpWorkPermitItemLock`.
- **Test**: `ApplicationProfileTenantCatalogExtendVisaWpTests`.
- **Prevent**: Do not turn Work permit item off on visa+WP extension. Produce Work permit ≠ linking the existing item. Restart so catalog sync writes locked profiles; re-run document-link correction if imported cases still show Work permit 0.
- **Cross-skill**: visa2026-lookup-data | visa2014-to-visa2026-import

### 2026-09-14 — Removed Application Profile ListView passport search box

- **Need**: Officers asked to remove the passport search box from Application Profile Instance ListViews completely.
- **Fix**: Deleted `ApplicationProfileInstancePassportSearchController`. Grid FullTextSearch on instance lists matches linked people names only (not passports). Person ListView compact passport matching is unchanged. Officer-shell staged/in-process haystack still includes passport numbers.
- **Test**: Remaining identity + CompactPassportSearchKey + OfficerShell facts. Officer: stop F5, rebuild. Via ministry / direct migration toolbars have no Passport box; grid Search does not find by passport.
- **Prevent**: Do not re-add a ParametrizedAction passport box on `Application_ListView_ViaMinistries` / `_DirectMigration` unless product asks again.
- **Cross-skill**: visa2026-application-profile

### 2026-09-14 — Passport box missed numbers stored with a space

- **Need**: Direct migration Passport search `U86993401` returned Total 0. Case workspace showed linked passport **U 86993401**.
- **Cause**: Criteria used `Contains(Lower(PassportNumber), token)`. Whitespace-split tokens also AND-split `U 86993401`. Typed compact number does not contain-match a spaced stored value.
- **Fix**: `CompactPassportSearchKey` + `Replace` spaces/hyphens on `PassportNumber` in `BuildLinkedPeoplePassportCriteria` / Person passport criteria.
- **Test**: `CompactPassportSearchKey_StripsSpacesAndHyphens`; `BuildLinkedPeoplePassportCriteria_SpacedNumberIsSingleCompactKey`. Officer: stop F5, rebuild. Direct migration Passport `U86993401` should list NP 11/-12585 (if that case is on the direct-migration route).
- **Prevent**: Do not AND-split passport search on spaces. Search is still scoped to the current route list (via ministry vs direct).
- **Cross-skill**: visa2026-application-profile

### 2026-09-14 — Dedicated passport search on via-ministry and direct-migration lists

- **Need**: Officers wanted a dedicated passport-number box on **Applications via ministry** and **direct migration** ListViews, not mixed into the grid Search box.
- **Fix**: `ApplicationProfileInstancePassportSearchController` ParametrizedAction filters via `BuildLinkedPeoplePassportCriteria` (`People.Passports` or instance `Passports`). Grid FullTextSearch on those two view ids no longer ORs passport. **Clear filters** also clears the passport box. Staged / in-process officer-shell search is unchanged.
- **Test**: `IsDedicatedPassportSearchListView_TrueForRouteListsOnly`; existing `BuildLinkedPeoplePassportCriteria_UsesPeopleAndInstancePassports`. Officer: stop F5, rebuild, Ctrl+F5. Via ministry: type a passport in **Passport**, press Enter — matching cases remain; grid Search no longer finds by passport. Direct migration same. Empty the box or Clear filters restores the route list.
- **Prevent**: Do not put passport back into FullTextSearch on `Application_ListView_ViaMinistries` / `_DirectMigration`. Do not add the box to staged/in-process queues.
- **Cross-skill**: visa2026-application-profile

### 2026-09-12 — Cancel visa+WP Work permit Last-N 2 (ceiling)

- **Need**: Application for cancelling visa and work permit (`cancel_visa_wp`). Same Last-N 2 as visas, for WorkPermitItem. Also `cancel_workpermit` and `cancel_invitation_wp`.
- **Cause**: Seed already had `PersonWorkPermitItemLastCount: 2`. Cancel WP count (`CancelWPCount`) was roster lines + Previous WP, not distinct People & links pins. Scan had no CWCNT.
- **Fix**: `ApplicationProfileInstanceCancelCounts.WorkPermits` (linked WP pins, else Current + Previous). Catalog **CWCNT** / **CWCTX** / **CWNB** / **CWSB** / **CWEB**. Relink already unions ObjectSpace WP rows.
- **Test**: `CollectMissingAutoLinks_AddsSecondWorkPermitWhenLastCountIsTwo`; `CountLinked_CountsDistinctWorkPermitPins`.
- **Officer**: Stop F5, rebuild, restart. Relink the person (do not Unlink first). Work permit tile should be 2/2 when two valid WPs exist. Letter count is CWCNT, not TPCNT.
- **Prevent**: Do not count roster lines for CWCNT. Do not change `cancel_visa_ext` / `cancel_visa_wp_ext` (those cancel extension applications).
- **Cross-skill**: visa2026-user-report-templates | visa2026-template-scan

### 2026-09-12 — Cancel-visa templates hide Travel history

- **Need**: Wizany Ýatyrmak 8/-1307 People & links was incomplete because Travel history was 0. Visa cancellation and visa+WP cancellation do not use TravelHistory.
- **Fix**: Calik `application-profile.calik-energi.json` sets `RequirePersonTravelHistory` **false** on `cancel_visa` and `cancel_visa_wp`. `ApplicationProfileTravelHistoryPolicy` + catalog `ApplyRow` / type mapper / resolver hide the tile and wizard checkbox. `cancel_visa_ext` stays on.
- **Test**: `ApplicationProfileTenantCatalogTravelHistoryTests` + resolver cancel-visa fact.
- **Prevent**: Do not turn Travel history on for visa-document cancellation templates. Restart after rebuild so catalog sync writes locked profiles.
- **Cross-skill**: visa2026-lookup-data

### 2026-09-12 — Cancel-visa sanaw needs Education tokens without the Education tile

- **Need**: Officers map `EGLV` / `EGIN` / `EGSP` on Wizany Ýatyrmak sanaw. People & links Education stays hidden.
- **Cause**: Placeholder pack used `AllowsPersonEducation && RequirePersonEducation`, so Review hid Education codes.
- **Fix**: Template mapping always offers PersonEducation. Merge falls back to `PersonCurrentItems.GetCurrentEducation`. Tile policy unchanged.
- **Prevent**: Do not turn `RequirePersonEducation` back on for `cancel_visa` to make placeholders appear.
- **Cross-skill**: visa2026-template-scan | visa2026-user-report-templates

### 2026-09-11 — First Link existing pinned only one of two valid visas

- **Need**: Wizany Ýatyrmak 9/-001. Link existing left Visa **1/2** (only A17327411). Relink then showed **2/2**. Officers thought the person had one visa.
- **Cause**: Link existing only ran Refresh on the picker Person (current visa in memory). Relink reloads the person and Ensures Last-N. A fresh ObjectSpace also often has `ApplicationProfile` null (backing field), so Last-N defaulted to 1.
- **Fix**: Officer `LinkPerson` calls the same pin as Relink (import still Refresh-only). Load profile from shadow `ApplicationProfileID` without ReloadObject on the instance. Visa query goes Passport IDs → Visas.
- **Test**: `EnsureApplicationProfileLoaded_TrueWhenProfileAlreadySet`, `LastCount_DefaultsToOneWhenProfileNavigationMissing`.
- **Officer**: Stop F5, rebuild, restart. Unlink Serdar if already linked, then Link existing once. Visa should be **2/2** without clicking Relink.
- **Prevent**: Do not leave officer Link on Refresh-only. Do not ReloadObject the instance after People.Add.
- **Cross-skill**: none

### 2026-09-11 — Unlink then Relink linked only one of two valid visas

- **Need**: Wizany Ýatyrmak 9/-001. Passport Visas has A14886414 and A17327411 (both Valid). After Unlink + Link existing, People & links showed Visa **1/2** and only A17327411.
- **Cause**: The tile expected 2 from a Visa table query (`LinkableActiveCounts`). Relink/Link resolved Last-N from `Person.Passports.Visas`, which after picker/Reload often has only the current visa.
- **Fix**: `ResolveVisas` / Last-N passport, invitation, WP union ObjectSpace rows with the in-memory graph (same Visa query as the tile).
- **Test**: `UnionDistinctById_IncludesVisaMissingFromPersonGraph`, `CollectMissingAutoLinks_AddsSecondVisaWhenLastCountIsTwo`.
- **Officer**: Stop F5, rebuild, restart. Relink Serdar — do not Unlink first. Visa should be **2/2**.
- **Prevent**: Do not Last-N from `Passport.Visas` alone when an ObjectSpace is available.
- **Cross-skill**: none

### 2026-09-11 — Relink pinned the same visa twice

- **Need**: Wizany Ýatyrmak 9/-001 Relink on Serdar. Passport Visas has two rows (A14886414, A17327411). People & links showed Visa **3/2** and the same visa twice.
- **Cause**: Relink runs Refresh (creates the missing Last-2 link in memory) then EnsureResolvedLink. `LoadLinks` queried the database only, so Ensure created a second row for the same `LinkedObjectId`.
- **Fix**: Merge in-memory `PersonResolvedLinks` into LoadLinks. Relink deletes extra rows with the same kind+object. Tile/list count distinct visa IDs.
- **Test**: `MergeTrackedLinks_DoesNotAddSameVisaTwice`, `CountResolvedForPerson_DedupesSameVisaLinkedTwice`.
- **Officer**: Stop F5, rebuild, restart. Relink Serdar once. Visa should be **2/2** with each number once.
- **Prevent**: Do not Ensure after Refresh without merging tracked links.
- **Cross-skill**: none

### 2026-09-11 — Cancel-visa seed Visa Last 2

- **Need**: Wizany Ýatyrmak 9/-001. Serdar Nuri Küçükakkaya has two valid visas (A14886414, A17327411). People & links showed Visa 1 and only A17327411. Wizard Person data was Last 1. Cancel visa+WP was already Last 2.
- **Fix**: `cancel_visa` `PersonVisaLastCount` **2** in Calik tenant JSON + `ApplicationProfileCalikPersonLastCountSeeds`. Sync applies the overlay on every F5 (config lock does not block). Last-N is a ceiling: one valid visa → expect 1; two → expect 2.
- **Test**: `Apply_CancelVisa_VisaTwo`.
- **Officer**: Stop F5, rebuild, restart. Open the same case → Relink on Serdar. Visa tile should be 2 when both booklets are valid. Configure → Person data shows Last 2. Clone is not required.
- **Prevent**: Do not require two visas. Do not change `cancel_visa_ext` (that cancels the extension application, not the visas).
- **Cross-skill**: visa2026-lookup-data

### 2026-09-11 — Unlink then relink person fails on save

- **Need**: Wizany Ýatyrmak People & links. Officer linked Serdar Nuri Küçükakkaya, Unlink, then Link existing again. Banner: `An error occurred while saving entity changes. See the inner exception for details.` Roster stayed empty.
- **Cause**: Unlink deferred-deletes `ApplicationProfileInstancePersonResolvedLink` (`GCRecord`). Unique index `(Instance, Person, Kind, Object)` was not filtered, so relink INSERT hit 23505. Skip-nav inverse was not always removed.
- **Fix**: Unique index `WHERE GCRecord IS NULL` (model + startup heal DROPs the old index). Unlink removes person + child skip-nav both sides. Link banner shows the innermost save error.
- **Test**: `Visa2026DbContextModelTests` filter assertion; `EntitySaveExceptionFormatterTests`; heal SQL contains `WHERE "GCRecord" IS NULL`.
- **Officer**: Stop F5, rebuild, restart. On the same case, Link existing → Serdar Nuri Küçükakkaya again. Person should appear. No Re-Approve needed.
- **Prevent**: Do not recreate this unique index without the GCRecord filter. Do not `Clear()` skip-nav collections.
- **Cross-skill**: none

### 2026-09-11 — Cancel-visa templates hide Education

- **Need**: Wizany Ýatyrmak (3/-358) People & links was incomplete because Education was 0. Visa cancellation does not use Education.
- **Fix**: Calik `application-profile.calik-energi.json` sets `RequirePersonEducation` **false** on `cancel_visa` and `cancel_visa_wp`. `ApplicationType` `ShowCurrentEducation` false for `App_Cancel_Visa` / `App_Cancel_Visa_and_WP`. `ApplicationProfileEducationPolicy` + catalog `ApplyRow` / type mapper / resolver hide the tile and wizard checkbox. `cancel_visa_ext` stays on.
- **Test**: `ApplicationProfileTenantCatalogEducationTests` + resolver cancel-visa fact.
- **Prevent**: Do not turn Education on for visa-cancellation templates. Restart after rebuild so catalog sync writes locked profiles.
- **Cross-skill**: visa2026-lookup-data

### 2026-09-11 — Invitation templates hide Travel history

- **Need**: Çakylygy üýtgetmek (5/-888) People & links was incomplete because Travel history was 0. Invitation cases do not use TravelHistory.
- **Fix**: Calik `application-profile.calik-energi.json` sets `RequirePersonTravelHistory` **false** on produce/change/cancel invitation codes (`get_invitation*`, `change_invitation`, `cancel_invitation*`). `ApplicationProfileTravelHistoryPolicy` + catalog `ApplyRow` / type mapper / resolver hide the tile and wizard checkbox (same as Business trip). Registration stay on.
- **Test**: `ApplicationProfileTenantCatalogTravelHistoryTests` + resolver invitation fact.
- **Prevent**: Do not turn Travel history on for Invitation templates. Restart after rebuild so catalog sync writes locked profiles.
- **Cross-skill**: visa2026-lookup-data### 2026-09-11 — Dependent Position tile is sponsor Wezipesi text

- **Need**: Dependents do not own EmployeePositionHistory. Officers still need Wezipesi on FM cases: sponsor position + relationship. Profile Position toggle stays on for templates.
- **Fix**: People & links Position for `!IsEmployee` is a read-only caption (`FamilyMemberSponsorPositionCaption` / `FM_WezipesiTm`). Completeness uses that line, not a Position Relink. Auto-link and import pin skip Position on family members. Position tab lists employees only.
- **Test**: `FamilyMemberSponsorPositionCaptionTests` + people-links completeness (18 passed in filter; issued-catalog 2 failures are MayProduce, unrelated).
- **Prevent**: Do not pin the sponsor's EmployeePositionHistory onto the dependent. Do not turn RequirePersonPosition off on ForFamilyMember profiles.
- **Officer**: stop F5, rebuild, Ctrl+F5. Mixed case: employee keeps Position 1; dependent shows the sponsor Wezipesi label.
### 2026-09-11 — All profile templates include Address and Position

- **Need**: Case workspace for Çakylygy üýtgetmek (3/-352) showed no Address of residence / Position tiles. Wizard Person data had those checkboxes off on a config-locked profile.
- **Fix**: Calik `application-profile.calik-energi.json` sets `RequirePersonAddressOfResidence` and `RequirePersonPosition` true on all **36** templates. Tenant `ApplyRow` and type mapper force both. New `ApplicationProfile` defaults both on.
- **Test**: `ApplicationProfileTenantCatalogAddressPositionTests` (+ travel catalog tests) passed.
- **Prevent**: Do not leave Address/Position off in tenant JSON. Restart after rebuild so catalog sync writes locked profiles; Clone is not required.
- **Cross-skill**: visa2026-lookup-data
### 2026-09-10 — F5 crash ValueManagerContext after emptying Resminamalar catalog

- **Need**: After purging nested + shared templates, host start died: `The ValueManagerContext is not active` in `ApplicationProfileTemplate.OnSaving` during `ApplicationProfileSeedSync` (nested-template tenant JSON recreate).
- **Fix**: Audit stamp uses `TryCurrentUserName` (no SecuritySystem when context is missing). Nested-template tenant catalog sync is skipped while `SeedEmbeddedTemplatesEnabled` is false so This profile includes do not come back on F5.
- **Prevent**: Do not call `SecuritySystem.CurrentUserName` from BO `OnSaving` on host-start ObjectSpaces. Nested includes belong with the Word/Excel seed flag.
- **Cross-skill**: visa2026-resminamalar | visa2026-user-report-templates

### 2026-09-10 — Hasaba F16 empty travel and visa category merge

- **Need**: Catalog Preview on Hasaba Almak 6/-1161 (Emre Akbulut) left `Travel_DateText`, `Travel_CheckPointTm`, `Application_VisaCategory_NameTm` empty though People & links had Travel history and a visa.
- **Fix**: Merge-line getters fall back to linked `TravelHistory` and `Visa`/`Invitation` when the registration line and instance VisaCategory are empty. Tokens unchanged.
- **Test**: `ApplicationRosterMergeLineTravelHistoryPlaceholderTests` fallback facts. Officer confirmed Preview filled.
- **Prevent**: Hasaba does not store Giren wagty/ýeri on a registration TravelDate/CheckPoint. Do not hide Travel history on check-in profiles.
- **Cross-skill**: visa2026-user-report-templates | visa2026-resminamalar

### 2026-09-09 — TravelHistory merge placeholders (THKD / THDT / THCP)

- **Need**: Templates needed People & links Travel history Kind / Date / Check point. Catalog only had registration **TRDT** / **TRCK**.
- **Fix**: `TravelHistory_*` placeholders + hydrator `CurrentTravelHistory` from resolved links. Pack `PersonTravelHistory`.
- **Test**: Stop F5 then rebuild — previous `dotnet test` hung on a locked Module.dll. Officer: Ctrl+F5; Create from yellow marks Add placeholder → group **Travel history**.
- **Prevent**: Do not reuse TRDT/TRCK for linked TravelHistory.
- **Cross-skill**: visa2026-user-report-templates | visa2026-template-scan

### 2026-09-09 — All profile templates include Travel history (except business trip)

- **Need**: Officers expected **Travel history** on every Application Profile template (wizard Person data + People & links tiles). Hasaba Almak (`check_in_from_abroad`) and issuance/cancel/change seeds had `RequirePersonTravelHistory` off, so the tile never appeared. Config lock only blocks the wizard; tenant catalog sync still updates locked rows.
- **Fix**: Calik `application-profile.calik-energi.json` sets the flag **true** on all families except **Business trip**. Tenant `ApplyRow` and type mapper force the same. New `ApplicationProfile` defaults the toggle on (same as Passport). Resolver still hides Travel history for `ActionFamily=BusinessTrip`.
- **Test**: `ApplicationProfileTenantCatalogTravelHistoryTests` + resolver/mapper tests — 45 passed with Module Debug build.
- **Prevent**: Do not leave Travel history off in tenant JSON for issuance/registration/cancel/change. Do not re-enable it on business-trip profiles (wizard hides the checkbox). Restart after rebuild so catalog sync writes the flag; Clone is not required for this seed change.
- **Cross-skill**: application-profile | visa2026-lookup-data



- **Need**: Issued-record compose drawers used browser `type="date"` (US MM/DD/YYYY). Officers wanted the same feel as Passport DetailView (`dd.MM.yyyy` + advancing caret).
- **Fix**: Shared `OfficerDateEdit` (DxDateEdit Mask/DisplayFormat `dd.MM.yyyy`, `MaskCaretMode.Advancing`). Wired into `IssueIssuedHeaderSlotPanel` (Invitation/WP/Rejection/BorderZone header + WP item Start/End) and `IssueIssuedVisaSlotPanel` (Issued/Expiration). Non-nullable Issue/Primary dates use DateChanged handlers.
- **Test**: Blazor Debug build succeeded. Officer: stop F5, rebuild, Ctrl+F5. Case workspace → Edit invitation / WP / New issued visa — dates show and type as dd.MM.yyyy; caret advances day→month→year.
- **Prevent**: Do not use raw `type="date"` in issued-record slot panels. Prefer `OfficerDateEdit` for new officer date fields outside XAF DetailView.
- **Cross-skill**: application-profile | preview-slot
### 2026-09-08 — Instance ListView Month as name, not integer

- **Need**: Month column showed `4`, `6`, `10` next to Person count integers — officers confused the two.
- **Fix**: NotMapped `MonthName` (localized via CurrentUICulture / en-US fallback). Hide int `Month` on ListViews. `ApplicationProfileInstanceMonthNameColumnUpdater` swaps Month → MonthName on via/direct/staged/in-process/source and Person nested instance lists (keeps caption/index; prior Month sort stays on hidden Month).
- **Test**: Module Debug build succeeded. Officer: stop F5, rebuild, Ctrl+F5. Via ministry — Month shows April/June/…; Person count stays numeric.
- **Prevent**: Do not show raw Month int on officer ListViews. Keep Month persisted for numbering/SQL.
- **Cross-skill**: application-profile
### 2026-09-08 — Instance ListViews: no Lookup/Object hyperlinks

- **Need**: Via ministry / Direct (and other ApplicationProfileInstance ListViews) showed FK columns (Application Profile, Approval leg, Visa type, …) as clickable links. Links stole clicks from row select / workspace open.
- **Fix**: Blazor `ApplicationProfileInstanceListViewDisableObjectLinksController` sets `ShowLink = false` on `LookupPropertyEditor` / `ObjectPropertyEditor` for all `ApplicationProfileInstance` ListViews (custom route clones included). Row activate → workspace unchanged.
- **Test**: Blazor Debug build succeeded. Officer: stop F5, rebuild, Ctrl+F5. Via ministry / Direct — Profile / Visa type / Approval leg are plain text; click row still opens workspace.
- **Prevent**: Do not leave default ShowLink on instance list lookups. Do not disable ProcessCurrentObject (that opens workspace).
- **Cross-skill**: application-profile
### 2026-09-08 — Template catalog keeps list after opening overview

- **Need**: Same TabbedMDI behaviour as Application Profile Instance lists — open template overview from the catalog, close it, catalog list tab still there. Catalog used inline list↔overview (`SelectedProfileId`) and native ListView used `TargetWindow.Current`.
- **Fix**: Catalog / officer-shell row open → `ApplicationProfileOverviewOpenHelper` + `NewWindow`. Native `ApplicationProfileListViewNavigationController` / New / overview toolbar from ListView also `NewWindow`. Overview host **← Back to list** closes the tab; Delete reloads catalog then closes. Workspace rail Configure uses NewWindow.
- **Test**: Build after change. Officer: stop F5, rebuild, Ctrl+F5. Configuration → Application Profile Templates → open row → overview new tab; close / Back to list → catalog tab still present.
- **Prevent**: Do not swap catalog content to inline overview for row activate; do not open template overview/wizard from the list with `TargetWindow.Current`.
- **Cross-skill**: application-profile
### 2026-09-08 — Instance ListView / template keep list after closing detail

- **Need**: Standard XAF TabbedMDI — open ListView, open row Detail (workspace / Configure), close detail, ListView tab still there. Application Profile Instance lists and template Configure replaced the list (`TargetWindow.Current`).
- **Fix**: ListView row activate, ListView **Open workspace**, and **Start process** use `TargetWindow.NewWindow`. Template New/Configure (officer shell) and linked-instance open from catalog/overview also use NewWindow. Legacy DetailView **Open workspace** stays Current.
- **Test**: `dotnet build Visa2026.slnx -c Debug` succeeded. Officer: stop F5, rebuild, Ctrl+F5. Via ministry / Direct list → open row → workspace new tab; close workspace → list tab still present. Templates → Configure → close wizard → catalog still there.
- **Prevent**: Do not open case workspace or template wizard from a ListView/catalog with `TargetWindow.Current` (replaces the list). Same pattern as Person dossier ListView → NewWindow.
- **Cross-skill**: application-profile
### 2026-09-05 — Visa / WP / Invitation tiles match active count, not Last-N quota

- **Need**: `cancel_visa_wp` Last 2 painted Visa and Work permit red `1/2` on issued 2/-210 even with one linked visa/WP. Officers: if the person has one active visa, link one; if two active, link two. Same for work permit and invitation. Red only when linked count is below that.
- **Fix**: `ApplicationWorkspaceLastNExpected` — expected = min(Last-N, active linkable rows). Zero active: expect 1 while the case is open, expect 0 when links are locked (imported expired rows). Completeness uses that expected count. Overview tiles sum per-person expected.
- **Test**: `ApplicationWorkspaceLastNExpectedTests` + completeness tests. Officer: stop F5, rebuild, Ctrl+F5. Open 2/-210 — Visa/WP not red, count `1`. A person with two active visas and one linked still shows `1/2` red.
- **Prevent**: Do not treat profile Last 2 as a required quota for Visa / Work permit / Invitation. Passport Last-N is unchanged.
- **Cross-skill**: application-profile
### 2026-09-04 — Create Case summary hides auto number/date and stacks fields

- **Need**: Application number and date are generated on save. Showing them on create made Application number look missing and blocked Create. The 3-column grid also clipped labels.
- **Fix**: `ForCreate` drops Application number, Application date, and Process number. Fields stack one per line (`ap-picker__summary-form`). Number/date still allocate on first save. Overview Edit can still change them later.
- **Test**: `ForCreate_hides_auto_number_date_and_process_number`; `CanCreate_true_when_defaults_fill_required_fields` (4 passed). Blazor compile to temp succeeded. Officer: stop F5, rebuild, Ctrl+F5. Create step lists Visa type / Category / Period / Project / … vertically; Create is not blocked by Application number.
- **Prevent**: Do not put auto Application number/date on the create step. Do not use the Overview 3-column Case summary grid in the picker.
- **Cross-skill**: application-profile

### 2026-09-04 — Case summary is a create-picker step

- **Need**: Officers had to Edit Case summary after create (Project, Entry check point, Work permit location often empty). They want those Use fields set before the instance exists.
- **Fix**: After Choose Organization, **Continue** opens **Case summary**. Same `ApplicationWorkspaceCaseHeaderFieldsHelper` fields as Overview Edit (no Process number). Profile defaults + auto number/date pre-fill. **Create application** is disabled while required fields are empty. Updates apply on the first save only if the officer changed them. Overview Edit remains.
- **Test**: `ApplicationProfilePickerCaseSummaryDraftTests` 4 passed. Module + Blazor Debug compile to temp succeeded. Officer: stop F5, rebuild, Ctrl+F5. New via-ministry instance → legs → Organization → Case summary → fill required → Create. Overview should not start with red required tiles.
- **Prevent**: Do not keep **Use profile** on Organization. Do not skip this step. Do not require Process number at create. Do not write Application number/date as manual unless the officer edited them.
- **Cross-skill**: application-profile

### 2026-09-04 — Missing approval letter upload without Revert

- **Need**: Officers must attach a missing ministry approval/disapproval letter on the completed Progress step, without Revert. Amber cue on Progress nav; Advance stays allowed.
- **Fix**: Done decision steps show Missing + upload. File attaches to that progress row immediately (`DecisionProgressId` / `ProgressId`). Document copies stay scans-only.
- **Test**: Progress letter completeness + timeline tests (27 passed). Officer: stop F5, rebuild, Ctrl+F5. Via-ministry case with an Approved/Unapproved ministry and no letter — Progress amber count; upload on that step; Advance still works.
- **Prevent**: Do not require Revert to attach a late letter. Do not block Advance. Do not add a Progress green check when no letters are missing.
- **Cross-skill**: visa2026-application-progress

### 2026-09-04 — Document copies missing uses warning amber, not red

- **Need**: Officers asked for Missing Document copies in warning color. Red is reserved for Case summary / People gaps.
- **Fix**: Catalog `--missing` rows and Missing chip are amber (`#fff7ed` / `#ea580c`). Document copies nav uses `cw-nav__badge--warn`. Overview and People stay red.
- **Test**: CSS + nav class only. Officer: Ctrl+F5. Missing Address row and Document copies badge are orange; Overview/People badges stay red.
- **Prevent**: Do not reuse `cw-nav__badge--missing` (red) on Document copies. Do not recolor Overview or People tiles.
- **Cross-skill**: application-profile | document-copies

### 2026-09-04 — Document copies missing / complete cues match prototypes

- **Need**: Document copies still used orange Missing chips and a silent nav while People/Overview already had red-count / green-check. Prototypes: pale red dashed Missing rows; nav = missing required slot count or green check.
- **Fix**: `ApplicationWorkspaceDocumentCopiesCompleteness` (gap + partial slots; empty roster = red dot; Application form is not a gap). Catalog rows use `--missing` boxed dashed red + red Missing chip. Nav uses full roster, not header chips.
- **Test**: People completeness 6 + Document copies 3 passed. Module + Blazor Debug compile to temp succeeded. Officer: stop F5, rebuild, Ctrl+F5. Case with a missing scan — Document copies row red, nav count. All Ready — green check. Overview/People badges stay independent.
- **Prevent**: Do not count Application form as a gap. Do not paint Ready rows green. Do not put a number in the green badge. Do not use header-chip filter for the nav count.
- **Cross-skill**: application-profile | document-copies

### 2026-09-03 — Overview missing / complete Case summary cues

- **Need**: Empty required Case summary tiles were red, but Overview in the left nav had no cue. Officers on People/Progress could not see Case summary gaps without opening Overview.
- **Fix**: Overview nav uses the same red-count / green-check language as People & links. Count = empty required Use fields (`FillState.Empty`), ignoring Process number. Green check when none are empty. Empty tiles also use dashed red label/value like People short tiles.
- **Test**: `ResolveOverviewNav_missing_fields_is_incomplete`; `ResolveOverviewNav_filled_or_no_required_fields_is_complete` (gate tests 7 passed). Module + Blazor Debug compile to temp succeeded. Officer: stop F5, rebuild, Ctrl+F5. Empty Project / Entry check point — Overview badge 2, red tiles. Fill them — green check. People badge stays independent.
- **Prevent**: Do not put a number in the green Overview badge. Do not count Process number. Do not paint every nav item green. Do not change the office-prep tab lock.
- **Cross-skill**: application-profile

### 2026-09-03 — People & links missing / complete cues

- **Need**: Zero linked records (Visa / Salary / Medical) looked the same as filled tiles. Officers needed attention on People & links without opening every card. Complete roster should show a green nav cue, not a green number.
- **Fix**: Short tiles (`Count` &lt; `ExpectedCount`) use Case summary empty red. Passport/Visa table `—` is red when that kind is short. Nav: red people-count (red dot if no one linked) or green check when every configured tile is filled. Filled tiles stay unchanged. Issued empty tiles stay dashed, not red.
- **Test**: `ApplicationWorkspacePeopleLinksCompletenessTests` 5 passed. Module + Blazor Debug compile to temp succeeded. Officer: stop F5, rebuild, Ctrl+F5. Open a case with Visa/Salary 0 — red tiles + red nav count. Link the missing records / Relink — green check. No people — red dot.
- **Prevent**: Do not put a number in the green badge. Do not paint filled tiles green. Do not count kinds that are not on the person (profile-hidden). Do not treat Issued records empty tiles as People gaps.
- **Cross-skill**: application-profile

### 2026-09-03 — Remove Approval legs card from profile overview

- **Need**: Application Profile template overview listed every shared Approval leg chain (MC-02, TS-AM, …). Those are tenant catalog, not this template. Profile stores only Default; officers pick on Choose Approval legs.
- **Fix**: Dropped the Approval legs card. Overview query no longer loads `GetSharedActiveProfiles` or nested profile legs. Process & SLA still notes that instance steps follow Directed to and Approval legs.
- **Test**: `ApplicationProfileOverviewQueryServiceTests` 7 passed. Module + Blazor Debug compile to temp succeeded. Officer: stop F5, rebuild, Ctrl+F5. Open a via-ministry template — no Approval legs card. Choose Approval legs still has catalog / Default.
- **Prevent**: Do not dump the shared ApprovalLegProfile catalog onto the template overview or wizard. Do not put Default back on Configure Identity.
- **Cross-skill**: application-profile

### 2026-09-03 — Hide Organization catalogs from Configuration nav

- **Need**: Organization catalogs should not appear under Configuration in the left navigation. Officers add and edit Company / Signatory / Representative from Choose Organization (gear) and case Organization Edit.
- **Fix**: `OrganizationCatalogsModelUpdater` sets the nav item `Visible = false`. `CustomNavigationUpdater` also hides `OrganizationCatalogs` / `OrganizationCatalogsHost` if they reappear. The catalog host page remains; native Company/Signatory/Rep nav stays hidden.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Configuration group has no Organization catalogs. Choose Organization gear and case Organization Edit still open New/Edit.
- **Prevent**: Do not re-show the Configuration nav item. Do not hide inline New/Edit.
- **Cross-skill**: application-profile

### 2026-09-03 — Case summary completeness gate during Office preparation

- **Need**: Officers could leave Overview (Progress, Document copies, Resminamalar, SLA) and Advance while Case summary still had empty required tiles (Project, Work permit location).
- **Fix**: `ApplicationWorkspaceCaseSummaryCompletenessGate` — while the current step is Office preparation, `FillState.Empty` fields (except Process number) block process tabs, Advance/Revert, and New issued. People & links stay open. Red banner names the missing labels and opens Case summary Edit. Readiness **Required data complete** uses the same missing list. After Office preparation, tabs stay unlocked.
- **Test**: `MissingRequiredFields_ignores_process_number_and_filled_tiles`; `BlocksTab_office_prep_blocks_progress_not_people`; `BlocksTab_after_office_prep_does_not_lock`; `BlocksTab_blue_defaults_are_complete` (5 passed). Module + Blazor Debug compile to temp succeeded. Officer: stop F5, rebuild, Ctrl+F5. Open a WP case with empty Project / Work permit location — Overview banner; Progress/Advance stay; People works; fill both → Continue.
- **Prevent**: Do not treat blue profile defaults as missing. Do not lock People & links. Do not include Process number (Advance rule). Do not keep the lock after leaving Office preparation.
- **Cross-skill**: application-profile | application-progress

### 2026-09-03 — Remove Company, Signatories from the profile wizard

- **Need**: Company / Signatory / Representative are tenant catalogs picked at instance create and on the case. The wizard step only previewed tenant Default and linked to Configuration — not profile configuration.
- **Fix**: Dropped wizard step, Review "Default company & signatories", and profile overview Company/Signatories card. Deleted `ApplicationProfileWizardStepCompanySignatories`, `ApplicationProfileWizardOrganizationSnapshot`, and `ApplicationProfileWizardOrganizationOpenHelper`. Unused `ApplicationProfile.DefaultAuthorizedSignatory*` / `DefaultVisaRepresentative*` stay mapped but hidden.
- **Test**: Module + Blazor Debug compile to temp succeeded (0 errors). Officer: stop F5, rebuild, Ctrl+F5. Configure profile — 5 steps, Identity then Results. Overview has no Company card. Catalogs still on Configuration / Choose Organization / case Organization.
- **Prevent**: Do not add per-profile Company/Signatory/Representative onto the wizard. Tenant Default is catalog `IsDefault`. Do not drop those leftover profile FK columns in this change.
- **Cross-skill**: application-profile

### 2026-09-03 — Choose Organization + New / pencil behind gear

- **Need**: Creating an Application Profile Instance showed + New and pencil next to every Company / Signatory / Representative dropdown. Officers only need those when adding or editing a catalog record.
- **Fix**: Gear in the Choose Organization header (top-right). Default is dropdowns + Default / Make default only. Gear on reveals + New and pencil. Same catalog modal as Configuration.
- **Test**: Compile to temp (F5 may lock bin). Officer: stop F5, rebuild, Ctrl+F5. New application → Choose Organization → no + New / pencil until gear; click gear → add/edit; Use profile still works with defaults.
- **Prevent**: Do not leave catalog New/Edit always visible on create. Case Organization still uses section Edit, not this gear. Empty lists: empty dropdown hint points at the gear.
- **Cross-skill**: application-profile

### 2026-09-03 — Case Organization + New / pencil only in Edit mode

- **Need**: On Application Profile Instance Overview, Organization **+ New** and pencil were visible next to Company / Signatory / Representative while the section was still read-only. They should appear only after the section **Edit** button.
- **Fix**: Overview columns are titles only. `RenderOrgColHead` (+ New / pencil) stays on the Edit layout with the dropdowns.
- **Test**: Blazor Debug build. Officer: Ctrl+F5. Overview Organization has section Edit only; click Edit → + New and pencil appear; Done hides them again.
- **Prevent**: Do not put catalog New/Edit on the read-only Organization overview. Create picker Choose Organization still always shows + New / pencil.
- **Cross-skill**: application-profile

### 2026-09-03 — Inline + New / Edit on Choose Organization and case Organization

- **Need**: Officers should add and update Company / Signatory / Representative from Application Profile Instance create (Choose Organization) and from the case Organization section, not only from Configuration → Organization catalogs.
- **Fix**: Same `OrganizationCatalogsOpenHelper.TryOpenEditor` modal as Config. Choose Organization has **+ New** and pencil next to each dropdown (new row is selected after save). Case Organization has **+ New** / pencil on each column in overview and edit; new row is assigned to this case. Values remain live catalog FKs, not instance scalars.
- **Test**: Blazor + Module Debug build succeeded. Officer: stop F5, rebuild, Ctrl+F5. New application → Choose Organization → + New Company → Save → dropdown includes and selects it → Use profile. Case Overview → Organization → pencil / + New on Signatory → preview updates. Other cases that picked a different row stay unchanged. Config Organization catalogs still works.
- **Prevent**: Do not treat Name / Passport as per-instance copies. Editing a catalog row still updates every case that selected it. Keep `[DomainComponent]` on `OrganizationCatalogsHost`. Keep helpers in `namespace Visa2026.Module.BusinessObjects`. The older "Do not add + New on Choose Organization" line is superseded.
- **Cross-skill**: application-profile

### 2026-09-03 — Organization catalogs Configuration page (+ New)

- **Need**: Officers must create and edit Company / Signatory / Representative rows on a Configuration page (prototype), not on Choose Organization (pick-only) or case Organization Edit (dropdown + preview).
- **Fix**: Config → **Organization catalogs** (`OrganizationCatalogsHost` + three cards). **+ New** and pencil open the native DetailView modal; **Make default** uses `OrganizationCatalogHelper.TryMakeDefault`. Wizard **Open catalog** opens this page. Create granted on the three BOs (numbering stays a singleton). Native Company/Signatory/Rep nav stays hidden.
- **Test**: `DisplayPerson_joins_name_and_title`; `FilterRows_matches_name_code_and_title`. Officer: stop F5, rebuild, Ctrl+F5. Configuration → Organization catalogs → + New Company → Save → row appears. Make default on the other row. Pencil edits the live catalog record.
- **Prevent**: Do not add + New on Choose Organization. Non-persistent XAF catalog hosts need `[DomainComponent]` (XAF0016 / type-not-registered on EF Core). Same shell pattern as Application Profile catalog — not a persistent BO. Keep helpers in `namespace Visa2026.Module.BusinessObjects`.
- **Cross-skill**: application-profile

### 2026-09-03 — Demo organization rows still missing after FORCE_XAF

- **Need**: Dropdowns still had one Company / Signatory / Representative after JSON + insert-missing on LookupCatalogSyncUpdater. That updater's JSON sync is skipped when the stored manifest version matches, and F5 often skips ModuleUpdaters even with FORCE_XAF when schema looks current.
- **Fix**: Insert demo rows in host-start `ApplicationProfileSchemaSql.ApplyIfMissing` (`DEM` / Ali Demir / Orazowa Gulsat) and `ApplicationProfileSeedGate` → `OrganizationCatalogSeed.EnsureMissing`.
- **Test**: `SeedDemoOrganizationCatalogPostgres` in template-column list; `Tenant_organization_json_includes_demo_row`. Officer: **stop F5, rebuild, F5**. Company dropdown should include **Demo Hyzmatlar HJ (DEM)**.
- **Prevent**: Do not rely on LookupCatalogSyncUpdater alone for extra organization catalog rows.
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-09-03 — Organization catalog JSON not applied on F5

- **Need**: Extra Company / Signatory / Representative JSON rows never appeared. Dropdowns still had one Default. `FORCE_XAF_DB_UPDATE` ran updaters but **JSON catalog sync is skipped** when `LookupCatalogManifestVersion` already matches.
- **Fix**: `EnsureMissingOrganizationCatalogRows` insert-only on every updater run (even when full sync is skipped). Those three tenant catalogs are `InsertOnly` so officer-edited Calik rows are not overwritten.
- **Test**: `Tenant_organization_json_includes_demo_row`. Officer: stop F5, rebuild, F5. Console: `organization catalogs inserted N missing row(s)`. Case Organization Edit should list two Companies / Signatories / Representatives.
- **Prevent**: Do not expect `--forceUpdate` / FORCE_XAF to re-run full OverwriteScalars catalog sync. New organization JSON keys need the insert-missing path (or a tenant manifest version bump for a full sync).
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-09-03 — Extra demo Company / Signatory / Representative seed rows

- **Need**: Officer needed a second catalog row to try changing Organization on an Application Profile Instance.
- **Fix**: Added one demo row each in `tenant/company-profile.json` (DEM), `authorized-signatory.json` (Ali Demir), `authorized-representative.json` (Orazowa Gülşat). Existing Calik rows unchanged. No `IsDefault` in JSON.
- **Test**: Restart so `LookupCatalogSyncUpdater` runs (`FORCE_XAF_DB_UPDATE=true` if the DB is already current). Choose Organization / Case Organization Edit should list two options per dropdown.
- **Prevent**: Do not put `IsDefault` in tenant JSON. Extra Company/Signatory/Rep rows are not pruned (only numbering is a singleton).
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-09-03 — Organization catalogs are live FKs (not copies)

- **Need**: Officers must pick **different** Company / Signatory / Representative per case, keep extra catalog rows, and set a tenant **Default** for the next create. Scalar copies (10z) hid Configuration edits from other cases that should share the same person.
- **Fix**: Catalogs are **not singletons** (numbering still is). Instance stores `OrganizationCompanyId` / `OrganizationSignatoryId` / `OrganizationRepresentativeId`. Create picker **Choose Organization** (after profile, and after legs if via-ministry) has three dropdowns + **Make default**. Case Organization Edit is dropdown-only with a read-only preview. Merge reads the selected rows. Wizard **Open catalog** is the native ListView. F5 backfill assigns Default FKs when empty.
- **Test**: `Resolve_uses_instance_relations_when_set`; `AssignDefaultsIfEmpty_skips_when_company_already_set`; `DisplayCompany_includes_code`; `HostStart_AddsOrganizationCatalogColumns`. Blazor + Module compile to temp. Officer: stop F5, rebuild, Ctrl+F5. New application → Choose Organization → change a dropdown / Make default → Use profile. Case Organization Edit → change Signatory (other cases that picked a different row stay). Add a second Company on the native list.
- **Prevent**: Do not prune extra Company/Signatory/Rep rows on F5. Do not put `IsDefault` in tenant JSON. Do not revive scalar letterhead editors or Reset-from-Configuration. Keep `OrganizationCatalogHelper` in `namespace Visa2026.Module.BusinessObjects`.
- **Cross-skill**: application-profile | visa2026-resminamalar

### 2026-09-03 — Case Organization letterhead is per instance


- **Need**: Officers asked to stop editing Company / Signatory / Representative on Configure profile, because that changed every template on the next generate. Product: **per-instance copies**, not a shortcut to tenant singletons.
- **Fix**: Copy Configuration singletons onto `ApplicationProfileInstance` at create (`LetterheadCopied`). Case workspace Overview **Organization** card edits this case. Word/PDF merge tokens read the instance copy. F5 backfill copies existing rows once. Wizard Company step is **Edit defaults** for new applications. **Reset from Configuration defaults** overwrites this case only.
- **Test**: `TryApplyField_writes_and_marks_copied`; `CopyFromConfigurationIfEmpty_skips_when_already_copied`; `HostStart_AddsInstanceLetterheadColumns`. Blazor + Module compile to temp (F5 can lock `bin`). Officer: stop F5, rebuild, Ctrl+F5. Open a case → Overview **Organization** → Edit a signatory name → Done → generate Resminamalar (this case changes, another case does not). New application copies current Configuration.
- **Prevent**: Do not put the tenant singleton editor on the case. Do not attach letterhead to the Application Profile template. Do not restamp when `LetterheadCopied` is true (except Reset). Keep the letterhead helper in `namespace Visa2026.Module.BusinessObjects` — `Services.ApplicationProfile` makes `ApplicationProfile` a namespace (CS0118).
- **Cross-skill**: application-profile | visa2026-resminamalar

### 2026-09-03 — Wizard Identity has no Approval legs block

- **Need**: Default and catalog live on Choose Approval legs. The leftover Identity heading, callout, and “Current default: TE-EN” were still on Configure profile.
- **Fix**: Removed the Approval legs section from Identity, Default from Review, catalog-open plumbing from the wizard, and locked Save-for-Default. Process copy no longer says legs are on Identity.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Configure a via-ministry profile → Identity goes Directed to → may be for. No Approval legs. Review has no Default line. Choose Approval legs still has Catalog / Make default.
- **Prevent**: Do not put Approval legs copy back on Identity or Review. Keep ClearLegs when switching to Direct migration.
- **Cross-skill**: application-profile

### 2026-09-03 — Make default on Choose Approval legs

- **Need**: Officers should set this template's Default chain on Choose Approval legs, not Configure Identity radios.
- **Fix**: **Make default** on each non-default card persists `ApplicationProfile.DefaultApprovalLegProfile` (config-lock still allows this scalar). Identity shows the current Default read-only. Seed JSON fills Default only when empty.
- **Test**: `ShouldSeedTemplateDefault_true_when_empty`; `AssignTemplateDefault_sets_profile_fk`. Officer: stop F5, rebuild, Ctrl+F5. Choose Approval legs → Make default on another card. Default badge moves. New application on the same profile pre-selects that card. Configure Identity has no Default radios.
- **Prevent**: Do not put Default radios back on Identity. Do not overwrite an existing Default from approval-leg-versions JSON on F5. Do not restamp started cases.
- **Cross-skill**: application-profile

### 2026-09-03 — Choose Approval legs Catalog opens the slot list

- **Need**: Officers could + New or Open one card, but not open the shared catalog list on the right (inactive chains, search, all Open).
- **Fix**: **Catalog** next to + New (and on the empty dashed box) opens `#visa-preview-slot` with an empty `ApprovalLegCatalogSlotRequest` — same occupant list as Configuration, not the New form.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Choose Approval legs → Catalog. Right slot shows Approval leg profiles (search, Active/Inactive, Open). + New still opens the blank form; card Open still edits that chain.
- **Prevent**: Do not send Catalog to `StartNew` or `FocusProfileId`. Inactive chains stay off the picker cards; Catalog is how you find them.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-09-03 — Approval-leg Active off did not persist

- **Need**: Open a shared chain, turn Active off, Save — still Active (picker card stayed; catalog badge stayed on).
- **Fix**: Slot Save `DetectChanges` + `SetModified` before commit (same as wizard Save profile). Active toggle uses `onchange` so the draft flag is set before Save. Seed `EnsureProfiles` no longer restores `IsActive` on existing rows.
- **Test**: `ApplyScalars_writes_inactive`. Officer: stop F5, rebuild, Ctrl+F5. Choose Approval legs → Open → Active off → Save. Slot catalog shows Inactive; the card is gone from Choose Approval legs.
- **Prevent**: Do not rely on XAF to notice a lone bool change. Do not write `IsActive` from `approval-leg-profile.json` onto existing officer-edited chains.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-09-03 — + New approval-leg Create failed with generic SaveFailed

- **Need**: Officer + New on Choose Approval legs (Code `EN-AH-GU`, 3 ministries) showed **Could not save the approval-leg profile.**
- **Fix**: `SyncForeignKeys` no longer writes `ApprovalLegProfileId = Guid.Empty` while the parent is new (EF inserts `00000000-...` and Postgres rejects the FK). Slot Create/Save call `PrepareLegsForCommit` and log swallowed commit errors; `UserFriendlyException` is shown instead of the generic banner.
- **Test**: `SyncForeignKeys_copies_parent_id_while_parent_is_new`. Officer: stop F5, rebuild, Ctrl+F5. Choose Approval legs → + New → same three ministries → Create. New card selected; Use profile enabled.
- **Prevent**: Do not blank a required Guid FK to “mean unset”. Do not swallow `CommitChanges` without logging.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-09-03 — Approval legs stay tenant-shared (not per instance)

- **Need**: + New on Choose Approval legs must not look or behave like a chain for that one case.
- **Fix**: Picker lists only `GetSharedActiveProfiles` (no leftover per-profile nested versions). Copy says shared for every via-ministry application; this case only snapshots the pick. Slot hints no longer send officers to wizard Refresh.
- **Test**: Create a chain on one profile’s Choose Approval legs, start another via-ministry profile — same cards appear.
- **Prevent**: Do not attach `ApprovalLegProfile` to `ApplicationProfileInstance`. Do not fall back to `ApplicationProfileApprovalLegVersion` on the picker.
- **Cross-skill**: application-profile

### 2026-09-03 — Choose Approval legs creates and opens shared chains

- **Need**: Officers should add or edit shared ministry chains when creating a case, not from Configure Identity **Edit in Configuration**.
- **Fix**: Via-ministry picker always continues to Choose Approval legs (including 0 chains). **+ New** / **Open** reuse `ApprovalLegProfileSlotPanel`. After Create/Save the new card is selected. Configure Identity keeps **Default** radios only. Empty catalog copy no longer sends officers to Configuration.
- **Test**: `RequiresApprovalLegVersion_TrueForViaMinistryEvenWhenEmpty`. Officer: stop F5, rebuild, Ctrl+F5. New via-ministry application → Continue → empty + New → Create chain → card selected → Use profile. Open an unused card to edit. Configure Identity has no Edit in Configuration.
- **Prevent**: Do not copy chains onto the Application Profile. Do not block Continue when the shared catalog is empty. Do not put slot CRUD back on the wizard.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-09-03 — New profile Resminamalar is This profile (0) not a flat list

- **Need**: First case on a new Application Profile must look like the signed-off tabs: This profile (0) empty, Shared library with ON/OFF.
- **Fix**: `UsesProfileNestedCatalog` is true whenever the instance has an Application Profile, even with zero nested rows. Catalog no longer dumps the seeded library onto This profile.
- **Test**: `UsesProfileNestedCatalog_TrueWhenProfileHasNoNestedTemplates`. Officer: restart, hard-refresh, open Resminamalar on a new-profile case.
- **Prevent**: Do not wait for the first Create template / Shared ON before showing tabs.
- **Cross-skill**: visa2026-resminamalar

### 2026-09-03 — Wizard Person data only (templates on case)

- **Need**: Officers already create this-profile files and toggle Shared on case Resminamalar. Administrators should not manage the same lists on Configure profile.
- **Fix**: Wizard step renamed **Person data**. This-profile list, Shared Include/Exclude, and Preview are gone. Person-row checkboxes and Last 1–3 stay. Review shows person toggles only. Shared attach remains `ApplicationProfileSharedTemplateIncludeHelper` on the case.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Configure a profile → step **Person data** has checkboxes only. Case Resminamalar This profile / Shared still creates files and ON/OFF include.
- **Prevent**: Do not put TemplateScan / Convert / Shared include back on `ApplicationProfileWizardStepTemplatesPerson`. New profiles get Shared templates only when an officer turns them ON on a case (or seed already attached them).
- **Cross-skill**: application-profile | visa2026-resminamalar

### 2026-09-02 — Shared include OFF while config-locked

- **Need**: Case Resminamalar Shared ON/OFF must work after lock state A. ON already created a new nested row; OFF deleted the include and hit `ConfigLockedCannotEditNested`.
- **Fix**: `AllowsNestedEditWhenConfigLocked` allows delete of Category/Global includes (`IsResminamalarSharedIncludeMutation`). Profile-specific nested files stay Recycle Bin-only.
- **Test**: `IsAllowedResminamalarSharedIncludeMutation_AllowsSharedScopesOnly`. Officer: restart, Shared tab, toggle ON then OFF on a case that has left office preparation.
- **Prevent**: Do not treat Shared OFF as a this-profile template edit. Keep the Recycle Bin carve-out separate (recycled rows only).
- **Cross-skill**: application-profile | visa2026-resminamalar

### 2026-09-02 — Wizard Templates no longer creates files

- **Need**: Officers already create, add, and recycle Word/Excel from a case (Resminamalar). Administrators should not repeat Add / Convert / Create from yellow marks on Configure → Templates.
- **Fix**: Wizard **Templates & person** lists this-profile nested templates **view + Preview** only. Shared catalog stays **Include / Exclude**. Person-data checkboxes unchanged. Authoring stays on the case: Create from yellow marks, Add existing, Edit, Recycle, Project contract filter.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Configure a profile → Templates & person: no Add / Convert / yellow-marks buttons, no Edit/Remove on this-profile rows. Shared Include/Exclude still works. Open a case → Resminamalar to create a this-profile file; it appears on the wizard list after save/refresh.
- **Prevent**: Do not put TemplateScanDialog / TemplateConvertDialog back on `ApplicationProfileWizardStepTemplatesPerson`. Do not treat wizard as the upload path for GT-15 letters.
- **Cross-skill**: application-profile | visa2026-resminamalar | visa2026-template-scan

### 2026-08-28 — Application Profile Templates lives under Configuration

- **Need**: Officers wanted **Application Profile Templates** in the Configuration accordion, not under Application Profile Instances.
- **Fix**: `ApplicationProfileCatalogModelUpdater` creates the catalog nav item under Configuration (index 0) and removes the leftover under Application. Users / VisaOffice navigation permissions Allow `Configuration/Items/ApplicationProfileCatalog` and Deny the old Application path. Click still opens the catalog host.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Accordion **Configuration** shows **Application Profile Templates** at the top. Application Profile Instances folder has via-ministry and direct-migration only. Opening Templates still shows the catalog.
- **Prevent**: Do not leave an Allow on `Application/Items/ApplicationProfileCatalog`. Do not Deny the whole Configuration folder for UsersReadOnly (that would hide Templates).
- **Cross-skill**: application-profile | visa2026-security-access

### 2026-08-28 — Application Profile Instance search by person name and passport

- **Need**: Officers could not find an instance by passport number or a linked person's first/last name. Native ListView DxGrid only searches visible columns; officer-shell queues only matched the first person's FullName (plus profile/project/number).
- **Fix**: Native `ApplicationProfileInstance` FullTextSearch ORs linked `[People]` first/middle/last name and passport numbers (person booklets or instance-linked passports). Blazor DxGrid SearchBox routes through that FullTextSearch (same bridge as Person). Staged / in-process queues token-AND match a haystack of all linked people names, passports, application number, process number, profile, and project.
- **Test**: `BuildLinkedPeopleIdentityCriteria_UsesPeopleFirstAndLastName`; `BuildLinkedPeoplePassportCriteria_UsesPeopleAndInstancePassports`; `OfficerShellApplicationSearch_MatchesFirstLastAndPassportTokens`. Officer: stop F5, rebuild, Ctrl+F5. Staged or in-process Search: type a last name, then a passport number — matching cases stay. Native Application Profile Instance ListView Search box does the same.
- **Prevent**: Do not rely on DxGrid column search for People/Passports (`VisibleInListView(false)`). Do not search only the first roster person's FullName.
- **Cross-skill**: application-profile

### 2026-08-28 — Progress Advance errors use red banner and field borders

- **Need**: Advance without required values showed a muted peach toast; Process number (and other required Advance inputs) kept a normal gray border, so officers could miss what to fill.
- **Fix**: Progress Advance failures use `os-banner--error` (same red as Case summary empty: `#dc2626` / `#fef2f2`, thick left bar). Empty required Advance fields get `is-invalid` (Process number when shown, Result if none selected, Date if cleared). Duplicate process-number server errors also ring Process number. Highlights drop as the officer types; banner stays until the next successful Advance. Rail Advance uses the same tab validation. Softer `os-banner--warn` (e.g. staged “rows not ready”) is unchanged.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Progress → Result Submitted, leave Process number empty, Advance. Banner is red; Process number border is red. Type a number — that border clears. Advance with a real unique number still succeeds.
- **Prevent**: Do not restyle all `os-banner--warn` to red. Do not paint `background` on `<select>.os-input.is-invalid` (Edge hides the closed-list label). Do not skip highlighting when Advance is clicked from the right rail.
- **Cross-skill**: application-profile | application-progress

### 2026-08-28 — Process number is migration-service tracking, not Start-process YYYY-NNNN

- **Need**: Officers type the Migration Service process number when advancing to Submitted (`PROCESS_STARTED`). It is unique, searchable, editable after submit without Revert, and copied read-only onto issued Visa / Work permit. Visibility is a wizard Use flag, forced on when May produce Visa, Work permit, or Invitation.
- **Fix**: `RequireProcessNumber` on the profile; `HasLeftStagedQueue` so Start process can merge without writing Submitted or allocating `YYYY-NNNN`. Progress Advance requires the number when the profile shows it. Case summary identity frame edits it (unique). Issued visa `ProcessNumber` and work-permit `ASNumber` copy from the instance and are read-only. Invitation letters use the instance number (no extra Invitation column).
- **Test**: `RequireProcessNumberWhenProducingIssuedDocuments_*`; `Build_FillState_ProcessNumberEmptyIsBlueUntilSubmitted`; `TryAssign_RejectsEmptyAfterProcessStarted`; `ApplyToVisa_CopiesInstanceProcessNumber`. Officer: stop F5, rebuild, Ctrl+F5. Start process on a direct-migration case — still office, no fake number. Progress Result Submitted — type process number, Advance. ListView search by that number. Case summary Edit can fix a typo. Issued visa/WP show the same number locked.
- **Prevent**: Do not allocate `YYYY-NNNN` into `ProcessNumber`. Do not treat empty Process number as staged after Start process (`HasLeftStagedQueue`). Do not allow duplicate process numbers. Do not let officers edit the copied number on the issued document.
- **Cross-skill**: application-profile | application-progress

### 2026-08-28 — Border zone defaults to Ýok

- **Need**: Invitation/Visa Case summary Border zone should show **Ýok**, not empty/`—`. Ýok is the catalog none sentinel (same as visa/invitation).
- **Fix**: Profile seed/mapper set `DefaultBorderZoneLocation` to `CommaSeparatedSelectionHelper.NoneValue` when RequireBorderZone and default empty. Instance `ApplyDefaults` / `OnCreated` / `OnSaving` use `ApplyDefaultIfEmpty`. `FormatBorderZoneDisplay` returns Ýok. `BorderZoneFill`: empty or Ýok vs empty or Ýok default = Default (blue); Ýok vs a real zone default stays Empty (red). Wizard Results forces the Use default to Ýok.
- **Test**: `Build_FillState_BorderZoneYokIsDefaultValue`; `ApplyDefaultIfEmpty_InstanceSetsYok`; existing `Build_FillState_MultiSelectIgnoresOrderAndNoneValue` still Empty for Ýok vs `"Zone B, Zone A"`. Officer: stop F5, rebuild, Ctrl+F5 so catalog sync writes the profile default. Open an inv/visa case — Border zone tile shows **Ýok** (blue if still default).
- **Prevent**: Do not skip Ýok in display as if it were blank. Do not treat Ýok as Empty fill when the profile default is also empty/Ýok. Do not set `ProduceBorderZone` or flip type `ShowBorderZoneLocation` for this.
- **Cross-skill**: application-profile

### 2026-08-28 — Work permit templates require Work permit location

- **Need**: When May produce **Work permit** is on, **Work permit location** (instance Use field / Case summary) must be visible. Same pattern as Border zone for Invitation/Visa. Not **May produce Work location** (`ProduceWorkLocation`).
- **Fix**: `RequireWorkPermitLocationWhenProducingWorkPermit` on seed apply, type mapper, `ShowMovementPermitLocation` / `ShowWorkPermittedLocations`, Case summary tiles, overview. Wizard Results checks and locks the Use box when ProduceWorkPermit is on. Calik JSON already had Require true on all four WP templates (`get_invitation_wp`, `extend_visa_wp`, `workpermit_extension`, `change_workpermit`).
- **Test**: `ApplicationProfileConfigurationResolverTests` WP location facts. Officer: stop F5, rebuild, Ctrl+F5. Open a WP-producing case — Work permit location tile is on Case summary. Configure wizard: Use is checked and cannot be cleared while May produce Work permit is on.
- **Prevent**: Do not leave RequireWorkPermitLocation off when ProduceWorkPermit is on. Do not set ProduceWorkLocation for this (that is a different May produce output).
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-08-28 — Invitation/Visa templates require Border zone on the instance

- **Need**: Any Application Profile that may produce Invitation or Visa must show **Border zone** under ApplicationProfileInstance properties required (Case summary location, not border-zone permit).
- **Fix**: Calik catalog `application-profile.calik-energi.json`: `change_invitation`, `visa_exit`, `visa_category_change`, `pasport_change` now `RequireBorderZone: true`. Seed apply and dual-read mapper use `RequireBorderZoneWhenProducingInvitationOrVisa`. Runtime `ShowBorderZoneLocation` follows the same rule. Did **not** set `ProduceBorderZone` or ApplicationType `ShowBorderZoneLocation` (that flag still means permit output on type fallback).
- **Test**: `ApplicationProfileConfigurationResolverTests` force/visibility facts. Officer: stop F5, rebuild, Ctrl+F5 so catalog sync runs. Open Configure on Çakylygy üýtgetmek / Çykyş wiza / category change / passport change — Border zone is checked. Case summary shows the Border zone tile.
- **Prevent**: Do not leave RequireBorderZone off when ProduceInvitation or ProduceVisa is on. Do not flip type `ShowBorderZoneLocation` to turn on the instance field (that would look like May produce border zone).
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-08-28 — Current progress node stays blue pen until Advance

- **Need**: Picking Result **Issued** on Migration service painted the last node green with a tick before Advance. Office preparation (unsaved Submitted) correctly stayed blue with a pen.
- **Fix**: Timeline icon/tone/badge use saved `step.State` / `OutcomeKind` only. Current node is always blue + ✎. Result dropdown is the unsaved next action. After Advance, Issued becomes a saved `done` node (green tick).
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Progress → last step, Result Issued, do **not** Advance. Node stays blue pen. Previous saved steps stay green ticks. After Advance, last node may turn green tick.
- **Prevent**: Do not preview `_selectedAdvanceStateCode` OutcomeKind onto the current timeline node.
- **Cross-skill**: application-profile | application-progress

### 2026-08-28 — Case summary identity frame (number, date, urgency)

- **Need**: Application number, Application date, and Urgency should sit together on the **right** of Case summary, in their own frame — not mixed into the Use-field grid.
- **Fix**: Overview tiles and Edit form: left = remaining Use fields; right aside = number, date, then urgency (when the profile shows it). Fill-state colors unchanged.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Open a case Overview. Those three are stacked in a right panel; Visa type / Category / Period / Project stay on the left. Edit uses the same split. If the profile hides Urgency, the frame still has number and date.
- **Prevent**: Do not put InstanceNumber / InstanceDate / Urgency back into the main `.cw-summary-grid`. Use `IsIdentityField` / `IdentityFieldKeys`.
- **Cross-skill**: application-profile

### 2026-08-28 — Case workspace section switch showed two progress bars

- **Need**: Switching Overview / People / Progress (and other case sections) showed a thin top line *and* the center overlay. Officers want one indicator.
- **Fix**: Removed `.cw-tab-progress` (the 3px bar under the case header). Kept `.cw-tab-loading` overlay in `.cw-main`. Left `@keyframes cw-tab-progress-slide` for the overlay fill.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Open a case → click Progress, then Overview. Only the center “Opening …” panel should appear — no blue line under the title.
- **Prevent**: Do not add a second sticky/top progress bar next to `.cw-tab-loading`. Report Dashboard’s top-pinned bar is a different surface.
- **Cross-skill**: application-profile

### 2026-08-28 — Case summary Edit: Border zone save hid other dropdowns

- **Need**: Setting Border zone (or any Use field) made Visa type, Category, Period, Project, Urgency look empty until **Done**. Number/date and Border zone text stayed visible.
- **Fix**: Save rebuilds `CaseView` without lookup catalogs (`loadLookupCatalogs` defaults false). `<select>` then has a Guid value and only “— Select —”. While Edit is open, `OnParametersSet` calls `EnsureHeaderLookupCatalogs` after each snapshot reload.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Case summary **Edit** → set Border zone. Visa type / Category / Period / Project / Urgency still show their labels. **Done** tiles unchanged.
- **Prevent**: Do not skip catalog reload after `SaveHeaderFieldAsync` / `LoadWorkspaceAsync` while Edit is still open.
- **Cross-skill**: application-profile

### 2026-08-28 — Case summary Edit select text was invisible

- **Need**: After picking Project (and on other lookups), the selected label vanished until **Done**. Tiles were fine.
- **Fix**: Do not paint fill-state background on native `<select>` (Edge/Windows hides the closed-list text). Tint the field wrapper; keep the control white with a colored border and explicit ink color.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Case summary **Edit** → choose a Project. The contract name stays visible in the dropdown. Visa type / Category / Period still show their labels. **Done** still shows the green Project tile.
- **Prevent**: Do not set `background` on `.cw-summary-field__input` when it is a `<select>`.
- **Cross-skill**: application-profile

### 2026-08-28 — Case summary empty / default / officer fill colors

- **Need**: Officers should see which Case summary fields still need a value, which still use the profile default, and which they already changed.
- **Fix**: Tiles and Edit controls: **red** empty/`—`; **blue** matches live profile default (application number/date blue until `IsManualEntry`); **green** officer value. Border plus light tint. Changing a lookup back to the profile default returns blue. Editing number or date sets `IsManualEntry` so both header scalars go green.
- **Test**: `ApplicationWorkspaceCaseHeaderFieldsHelperTests` fill-state facts. Officer: stop F5, rebuild, Ctrl+F5. Open a case — empty Project/Border zone red; Visa type copied from profile blue; Edit a field then Done — that tile green.
- **Prevent**: Do not use `[NotMapped]` flags inside EF for this. Do not color Linked/Issued tiles with the same fill-state. Compare lookups to live profile `Default*` (not a create-time snapshot).
- **Cross-skill**: application-profile

### 2026-08-27 — Report Dashboard EF must not query Visa.IsCancelled

- **Need**: Dashboard crashed: `Translation of member 'IsCancelled' on entity type 'Visa' failed` (`LoadValidVisaPersonIds`).
- **Fix:** `IssuedDocumentLifecycle.WhereVisaNotCancelled` uses skip-nav Cancellation + `PROCESS_ISSUED`. Invitation/WP dashboard IQueryable filters use the same pattern.
- **Test:** Officer: stop F5, rebuild, Ctrl+F5. Open Report Dashboard (valid-visa persons on). No InvalidOperationException.
- **Prevent:** Do not put `[NotMapped]` `IsCancelled`/`IsChanged`/`IsUsed` in LINQ that EF sends to SQL.
- **Cross-skill:** application-profile | visa2026-report-dashboard

### 2026-08-27 — Link existing person requires a usable passport

- **Need**: Officers must not add a person with no passport, or only cancelled/expired/incomplete passports.
- **Fix**: `ApplicationProfileInstancePersonLinkPassportGate` on picker rows. **Link** disabled; row shows **No passport**, **Passport is cancelled**, **Passport is expired**, or **No valid passport**. Server refuses the same check. A valid current booklet still allows link when an older one is expired (Last-N can attach both after).
- **Test**: `ApplicationProfileInstancePersonLinkPassportGateTests`. Officer: stop F5, rebuild, Ctrl+F5. People & links → Link existing → person with no passport: Link greyed, reason on the row. Person with a live passport: Link works.
- **Prevent**: Do not hide the person from search (show why). Do not treat expired previous + valid current as blocked. Do not change `CanLinkPassport` (auto-link still ignores passport expiration).
- **Cross-skill**: application-profile

### 2026-08-27 — People & links Link shows progress while resolver runs

- **Need**: Linking a person takes a few seconds (Last-N auto-link). Officers saw no feedback after **Link**.
- **Fix**: Picker yields `Task.Delay(16)` so Blazor paints, then overlay **Linking {name}…** plus spinner on the clicked button. Other Link/Search/Cancel disabled. Relink uses **Relinking…** the same way.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Case → People & links → Link existing → **Link**. Overlay appears immediately and stays until the person is on the roster.
- **Prevent**: Do not run `LinkPerson` before a yield — Blazor Server will not paint the busy state. Do not leave Link clickable while linking (double-submit).
- **Cross-skill**: application-profile

### 2026-08-27 — Create instances only from picker; via-ministry always opens Approval legs

- **Need**: One create place (Application Profile Instances **New**). Via-ministry must show Approval legs even when there is only one version so the snapshot is explicit. Person / Dossier must not create instances.
- **Fix**: Picker step 1 = profiles. Via ministry → **Continue** → step 2 **Choose Approval legs** (profile name in header, Default pre-selected). Direct migration stays **Use profile** on step 1. `UseProfileAsync` refuses to create a via-ministry instance until step 2. Person and Dossier **Start process…** stay hidden.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Applications via ministry → New → pick a one-version profile → Continue → legs screen → Use profile. Direct migration: one step. Person DetailView and Dossier have no Start process.
- **Prevent**: Do not skip step 2 when there is a single approval-leg version. Do not re-enable Person/Dossier Start application.
- **Cross-skill**: application-profile | visa2026-person-dossier

### 2026-08-27 — Wizard header shows Application Profile name on every step

- **Need**: Officers lose track of which template they are editing after leaving Identity.
- **Fix**: Wizard chrome shows `Profile.Name` under **Application Profile** on all six steps. Identity name input updates the header live. Empty name shows **Unnamed profile**.
- **Test**: Officer: stop F5, rebuild, Ctrl+F5. Configure a named profile → Continue through Company / Results / Process / Templates / Review — name stays at the top. Change the name on Identity — header updates before Continue.
- **Prevent**: Do not hide the Identity name field (that is still the editor). Do not put a second name editor on later steps.
- **Cross-skill**: application-profile

### 2026-08-27 — Remove wizard Applicability scope section

- **Need**: Officers have no use for XAF applicability criteria on Application Profile templates.
- **Fix**: Removed Identity **Applicability scope (optional)** (Always available / Scoped + criteria). Overview no longer lists applicability. Nested-template project/migration applicability dropdowns stay. `ApplicabilityCriteria` column kept; empty means always pickable.
- **Test**: Blazor Debug. Officer: stop F5, rebuild, Ctrl+F5. Configure profile → Identity — no scope cards; Active checkbox remains. Templates step still has project/migration applicability.
- **Prevent**: Do not drop `ApplicabilityCriteria` schema. Do not remove template `ApplicableProjectContract` / `ApplicableMigrationService`.
- **Cross-skill**: application-profile

### 2026-08-27 — pasport_change Last 2 is flag-only (do not block create)

- **Need**: Passport-change expects two booklets, but officers still create when only one is on the person.
- **Fix**: Keep existing incomplete **warning** (`ApplicationStartFromPersonHelper`); workspace tile `1/2`. No create/link block.
- **Test**: Already covered by Last-N shortfall tests (flag, not block). Officer: `pasport_change` + person with one passport — create proceeds; tile dashed `1/2`.
- **Prevent**: Do not add a hard gate that requires two passports before create.
- **Cross-skill**: application-profile

### 2026-08-27 — Calik Last-N catalog seeds (1 or 2 valid Inv/WP/Visa)

- **Need**: Person may have **1 or 2** valid invitation items, work permits, or visas on cancel templates — Last 2 is the cap, not a hard requirement. Passport Last 2 only on `pasport_change` (old + new). `reg_info_change_passport` stays Last 1.
- **Fix**: `application-profile.calik-energi.json`: `cancel_invitation` invitation 2; `cancel_visa_wp` visa 2 + WP 2; `cancel_workpermit` WP 2. Existing `pasport_change` passport 2 / visa 1 and `cancel_invitation_wp` invitation 2 + WP 2 unchanged. `ApplicationProfileCalikPersonLastCountSeeds` so Wave 1 export keeps the same overlay.
- **Test**: `ApplicationProfileCalikPersonLastCountSeedsTests` (6). Officer: stop F5, rebuild, Ctrl+F5. Open those profiles in the wizard — Last 2 on the seeded types; link a person with one valid WP on cancel work permit — tile `1/2`, create still allowed.
- **Prevent**: Do not set Last 2 on `reg_info_change_passport`. Do not block create when actual &lt; Last 2. Do not raise visa Last 2 on `pasport_change`.
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-08-27 — Visa / invitation / work permit stay validity-gated for auto-link

- **Need**: Last-N must still only link **valid** `Visa`, `InvitationItem`, and `WorkPermitItem`. Passport expiration skip does **not** apply to these three.
- **Fix**: Keep §10.2 `CanLink*` gates: visa started + not cancelled/changed + not expired; invitation not cancelled/changed/used + parent not expired; work permit not cancelled/changed + not expired. Last N is among those valid rows only. Expired/used/cancelled are skipped; remaining valid rows still link.
- **Test**: `ResolveVisas_TakesLastTwoValidSkipsExpiredAndCancelled`, `ResolveInvitationItems_TakesLastTwoValidSkipsExpiredAndUsed`, `ResolveWorkPermitItems_TakesLastTwoValidSkipsExpired`, `CollectMissingAutoLinks_LinksValidVisaInvitationAndWorkPermitOnly`. Filter `ApplicationProfileInstancePerson` — 42 passed.
- **Prevent**: Do not skip expiration for visa / invitation / work permit. Do not treat cancelled, changed, or used invitation as linkable. Passport expiration skip stays passport-only.
- **Cross-skill**: application-profile

### 2026-08-27 — Person Last-N auto-link (Passport / Visa / Invitation / WP / Border zone)

- **Need**: Templates such as `pasport_change` and `cancel_invitation_wp` must auto-link more than the latest row. Valid invitations on one person can be 1, 2, 3+; remaining usually max 3. Expired previous passport is OK. Missing expected rows are flagged, not blocked. Last-N control only beside Passport / Visa / Invitation / Work permit / Border zone.
- **Fix**: `Person*LastCount` 1–3 on `ApplicationProfile`. ResolvedLinks unique index is `(Instance, Person, Kind, LinkedObjectId)`. Resolver Take(N) without replacing sticky IDs. Passport `CanLinkPassport` ignores expiration. Merge hydrator fills Current + Previous. Workspace tiles show `count/expected` and dash when short. Seeds: `pasport_change` Last 2 passports; `cancel_invitation_wp` Last 2 invitation + Last 2 WP.
- **Test**: `ApplicationProfileInstancePersonResolverTests` + `ApplicationProfileInstancePersonValidItemsTests`. `dotnet test` filter `ApplicationProfileInstancePerson`. Officer: stop F5, rebuild, Ctrl+F5. Configure `pasport_change` — Passport Last 2; link a person with an expired previous booklet — both passports on People & links; missing invitation on cancel invitation+WP is dashed `1/2`.
- **Prevent**: Do not restore unique `(Instance, Person, Kind)` only. Do not put Last-N next to Education / Position / Address / Salary / Medical. Do not block create when Last-N is short.
- **Cross-skill**: application-profile | visa2026-application-progress

### 2026-08-27 — Hide template-owned Configuration nav

- **Need**: Officers still opened native XAF lists for Approval Leg Profile, Approving ministries, Company, Authorized Signatory, and Authorized Representative even though those catalogs are edited from Application Profile.
- **Fix**: `[NavigationItem(false)]` on the five BOs. `CustomNavigationUpdater.HideTemplateOwnedConfigurationNavigation` + `Model.DesignedDiffs.xafml` `Visible="False"`. Tables and type permissions unchanged; slot/wizard still persist them.
- **Test**: Module Debug. Officer: stop F5, rebuild, Ctrl+F5. Configuration no longer lists those five. Numbering, project contracts, expiry alerts, upload limits stay. Edit in Configuration still opens the approval-leg slot.
- **Prevent**: Do not drop the BOs. Do not re-add `[NavigationItem("Configuration")]` for them.
- **Cross-skill**: application-profile | visa2026-security-access | visa2026-user-manual

### 2026-08-27 — Remove Duplicate from approval-leg slot

- **Need**: Officers do not want a Duplicate button on catalog cards or the in-use editor.
- **Fix**: Removed catalog and footer Duplicate. In-use chains stay locked (Code/Active still saveable). New chains use **+ New**. Locked hint no longer mentions Duplicate.
- **Test**: Blazor Debug. Officer: Ctrl+F5 catalog — used rows have Open only; Open used chain — Save, no Duplicate.
- **Prevent**: Do not re-add Duplicate on this occupant.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-08-27 — Approval-leg slot left/right gutters

- **Need**: Catalog cards and the editor ran too close to the slot edges.
- **Fix**: Wider `--approval-leg-pad-x` (clamp 1.85–3rem) shared by header, body, footer.
- **Test**: Ctrl+F5 Edit in Configuration catalog + Open unused.
- **Prevent**: Do not drop occupant pad-x back to 1.35rem.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-08-27 — Create ApprovingMinistry from approval-leg slot

- **Need**: Dropdown only listed existing ministries; officers could not add a missing `ApprovingMinistry` without leaving the slot.
- **Fix**: **+ New ministry** inline form (Short name + Official name). `TryCreateMinistry` persists tenant lookup, blocks duplicate short names, then appends the new ministry to the current chain. No XAF DetailView. Seed JSON unchanged.
- **Test**: `ApprovalLegProfileSlotEditorTests` (normalize + duplicate short name). Blazor Debug. Officer: Edit in Configuration → Open unused → **+ New ministry** → Create ministry → row appears; Save chain. Repeat with an existing short name → error to pick from the list.
- **Prevent**: Do not open Configuration `ApprovingMinistry` ListView. Do not type-create from the dropdown. Do not prune officer-created ministries from catalog sync.
- **Cross-skill**: application-profile | visa2026-preview-slot | visa2026-lookup-data

### 2026-08-27 — Approval-leg slot identity row and catalog title

- **Need**: After the first spacing pass, catalog titles still split code from ministries, Code sat in an empty-looking section, unused Save had no Cancel, and the editor lacked the prototype footer hint.
- **Fix**: Catalog title is `Code · ministries`. Code + Active sit on one identity row. Unused footer is Delete | Cancel + Save. `SaveHint` under the editor footer when not New.
- **Test**: Blazor Debug succeeded. Officer: stop F5, rebuild, Ctrl+F5. Edit in Configuration — search stays short, cards have room; Open unused — Code not full-width, sticky footer.
- **Prevent**: Do not stretch Code across the slot. Do not leave Save/Delete in the scrolling form body.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-08-27 — Approval-leg slot catalog spacing and sections

- **Need**: Catalog cards and the edit form looked cramped (tight padding, full-width Code, actions floating in empty space).
- **Fix**: Card padding/gaps, code vs ministry caption, used-row accent. Editor in sections with a sticky footer (Delete left, Save right). Code field capped; Active is a switch. Unused chains get a green hint.
- **Test**: Blazor Debug. Officer: Ctrl+F5, Edit in Configuration — catalog cards have room; Open unused — sections + footer stay put while ministries scroll.
- **Prevent**: Do not stretch Code across the full slot. Keep Save/Delete in the footer, not mid-panel.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-08-27 — Approval-leg catalog in preview slot (no XAF popup)

- **Need**: **Edit in Configuration** opened native XAF `ApprovalLegProfile` ListView; officers could not Open a chain to edit ministries.
- **Fix**: `VisaPreviewSlotMode.ApprovalLegCatalog` occupant. Catalog / empty / New / Open (locked vs unused) / Duplicate / Delete. Slot does not set Default. Wizard radios refresh via `IApprovalLegCatalogChangeNotifier`.
- **Test**: `ApprovalLegProfileSlotEditorTests` (5). Blazor Debug succeeded. Officer: stop F5, rebuild, Ctrl+F5. Via-ministry Configure profile → Identity → **Edit in Configuration** → slot catalog, not XAF OK/Cancel. Open a used chain → ministries locked + Duplicate. **+ New** → Create → radios update; Default unchanged until officer picks it.
- **Prevent**: Do not reopen XAF ListView from the wizard. Do not set Default in the slot. Do not add namespace `Visa2026.Module.Services.ApplicationProfile` (collides with the BO type).
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-08-27 — Approval-leg slot prototypes: empty catalog and New

- **Need**: Officers asked for empty catalog and **+ New** screens after 01–03 (populated catalog, locked edit, unused edit).
- **Fix**: Added `docs/prototypes/approval-leg-profile-slot-04-empty.png` (no chains; wizard hint to add first) and `-05-new.png` (blank Code placeholder, 0 ministries, Cancel / Create). README + plan §9 + skill group list.
- **Test**: Visual review of 04/05 against 01 chrome. Code field on 05 is placeholder only, not a filled value.
- **Prevent**: Do not implement the Blazor occupant until officer says 01–05 are enough. Slot still does not set Default. Do not reopen XAF ListView for Edit in Configuration.
- **Cross-skill**: application-profile | visa2026-preview-slot

### 2026-08-27 — Remove Application Migration Sla Profile

- **Need**: Officers asked to remove the unused Configuration catalog; days already live on Application Profile.
- **Fix**: Runtime SLA reads `ApplicationProfile.MigrationSlaDays` only. Dropped BO, `ApplicationTypes.MigrationSlaProfileID`, tenant JSON, type-link updater. `ApplicationMigrationSlaProfileDropSchemaUpdater` backfills zero profile days then `DROP TABLE`. Ministry review SLA singleton stays (hidden).
- **Test**: `dotnet test` filter `Sla|Migration`. Officer: stop F5, rebuild, Ctrl+F5. Configuration has no Application Migration Sla Profile. Open a profile → Process & SLA still has migration days. Case workspace SLA still uses those days.
- **Prevent**: Do not re-add `ApplicationMigrationSlaProfile` or `[NavigationItem("Configuration")]` for it. Do not read SLA from `ApplicationType`. Keep `MinistryReviewSlaSettings` until asked to drop it.
- **Cross-skill**: application-profile | visa2026-lookup-data | visa2026-application-progress

### 2026-08-27 — Hide Configuration SLA catalogs from nav

- **Need**: Officers should not edit **Application Migration Sla Profile** or **Ministry review SLA** in Configuration; days live on Application Profile.
- **Fix**: `[NavigationItem(false)]` on both BOs. `CustomNavigationUpdater` hides leftover nodes. `Model.DesignedDiffs.xafml` `Visible="False"`. Tables kept for dual-read / snapshot fallback.
- **Test**: Rebuild + restart. Configuration no longer lists those two. Approval leg profile, contracts, company remain.
- **Prevent**: Do not drop `MinistryReviewSlaSettings`. Do not re-add `[NavigationItem("Configuration")]` for it. Migration SLA catalog was later **removed** (see 2026-08-27 drop entry).
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-08-27 — SLA table heading is SLA by step

- **Need**: Table titled Deadlines listed progress steps; officers read it as the case deadline, which is the 30-day SLA cards.
- **Fix**: Heading **SLA by step**. Model property `Deadlines` unchanged.
- **Test**: Officer: Ctrl+F5 on SLA & deadlines. Table title is SLA by step.
- **Prevent**: Do not retitle that table Deadlines or Progressline.
- **Cross-skill**: application-profile

### 2026-08-27 — SLA tab in sync with deadline states

- **Need**: Header remaining, overall ring, current-step ring, timeline, and deadlines table disagreed (e.g. Issued case still showed a countdown; table “due dates” were history event dates; 30 − 1 ≠ 3).
- **Fix**: One `ApplicationWorkspaceSlaDashboardBuilder`. Header remaining = current deadline days left. Overall remaining = migration SLA from start minus elapsed working days. Table due dates = working-day SLA targets. Issued/rejected/cancelled: remaining clocks off, status Complete/Issued, all rows Completed.
- **Test**: `ApplicationWorkspaceSlaDashboardTests` + `WorkingDaysHelperTests` + progress timeline tests passed. Blazor Debug succeeded. Officer: stop F5, rebuild, Ctrl+F5. Open issued № 8/-12568 → SLA: rings say Done, no header “days remaining”, table all Completed. Open an in-process case: header number matches the highlighted table row.
- **Prevent**: Do not mix current-step ministry remaining into the overall 30-day card. Do not use progress history `Date` as the deadline due date. Do not show a remaining clock after Issued.
- **Cross-skill**: application-profile | visa2026-application-progress

### 2026-08-27 — SLA & deadlines tab: ring overlap and dashboard polish

- **Need**: On SLA & deadlines, “days left” overlapped the donut number. The tab looked like a prototype (always “On track”, dummy button, “Ministry review” even when the current step was office prep).
- **Fix**: Number only inside the ring; caption **days left** sits under the gauge. Status tone from remaining days (Due / Due tomorrow / Due soon / On track). Current-step card uses the live step label. Timeline, deadlines table, alert, and SLA source. Drop the fake control. CSS: overflow-hidden ring, caption outside `.ct-sla-ring`.
- **Test**: Blazor Debug succeeded. Officer: stop F5 if running, rebuild, Ctrl+F5. Open a via-ministry case → **SLA & deadlines**. Circles show only the number; “days left” below, no overlap. Current-step title is the real step (e.g. Office preparation), not always Ministry review. Progress tab rail uses the same gauge.
- **Prevent**: Do not put a `<span>` inside `.ct-sla-ring`. Do not hardcode “Ministry review deadline” or “On track”. Do not restore the dummy “MigrationSlaDays from template” button.
- **Cross-skill**: application-profile

### 2026-08-27 — Wiza Kategoriýasyny üýtgetmek seed is Change family

- **Need**: Same rule as Çakylygy üýtgetmek / passport-change for **Wiza Kategoriýasyny üýtgetmek** (`App_Change_Visa_Category` / `visa_category_change`).
- **Fix**: Tenant catalog: `ActionFamily` **Change**. May produce Visa unchanged. **May change** Visa on so Identity is not empty.
- **Test**: Rebuild + restart so tenant catalog seed updater runs. Open that profile → Related to **Change**; May produce Visa; May change Visa.
- **Prevent**: Do not clear ProduceVisa. Do not add May change Invitation on this profile.
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-08-27 — Wizany KP>Täze Pasporta Geçirmek seed is Change family

- **Need**: Same rule as Çakylygy üýtgetmek for **Wizany KP>Täze Pasporta Geçirmek** (`App_Change_Passport` / `pasport_change`).
- **Fix**: Tenant catalog: `ActionFamily` **Change**. May produce Visa unchanged. **May change** Visa on so Identity is not empty.
- **Test**: Rebuild + restart so tenant catalog seed updater runs. Open that profile → Related to **Change**; May produce Visa; May change Visa.
- **Prevent**: Do not clear ProduceVisa. Do not add May change Invitation on this profile.
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-08-27 — Çakylygy üýtgetmek seed is Change family

- **Need**: Profile **Çakylygy üýtgetmek** (`App_Change_Inv` / `change_invitation`) still seeded as Related to **Issuance**.
- **Fix**: Tenant catalog `application-profile.calik-energi.json`: `ActionFamily` **Change**. May produce Invitation + Visa unchanged. **May change** Invitation + Visa on so Identity is not empty when Related to is Change.
- **Test**: Rebuild + restart so tenant catalog seed updater runs. Open Configure profile for Çakylygy üýtgetmek → Related to **Change**; May produce still Invitation + Visa in seed; wizard shows May change Invitation + Visa.
- **Prevent**: Do not set this profile back to Issuance. Do not clear ProduceInvitation/ProduceVisa.
- **Cross-skill**: application-profile | visa2026-lookup-data

### 2026-08-27 — Derived cancel/change/used; Change family; drop stored flags

- **Need**: Officers confirmed (1) a document is cancelled only after the Cancellation instance finishes (`PROCESS_ISSUED`, not mere presence and not `IsWorkflowTerminal`); (2) drop stored `InvitationItem.IsUsed` — used = issuing visa; (3) add **Change** family beside Issuance / Cancellation / Registration / Business trip; (4) drop the columns in a deploy updater now; (5) leave `Passport.IsCancelled`.
- **Fix**: `ApplicationProfileActionFamily.Change = 4` + May change flags + wizard Identity radios. `IssuedDocumentLifecycle` + `[NotMapped]` getters. Cancelled wins over changed. `IssuedDocumentStatusColumnsCleanupUpdater` (Postgres) drops views then columns; Report Dashboard views recreate with skip-nav `EXISTS`. OData no longer posts the dropped fields. `Passport.IsCancelled` unchanged.
- **Test**: Module + Tests + DataImporter Debug succeeded. `IssuedDocumentLifecycleTests` + valid-item tests (37) passed. Officer: stop F5, rebuild, Ctrl+F5. Identity & purpose → Change shows **May change existing**. A cancellation case still in office prep does **not** mark the document cancelled; after **PROCESS_ISSUED** it does.
- **Prevent**: Do not store IsCancelled/IsChanged/IsUsed on InvitationItem, Visa, WorkPermitItem, BorderZone. Do not treat in-process or rejected/cancelled **process** as document cancelled. Do not drop `Passports.IsCancelled`. Historical VISA2014 cancelled docs stay active until skip-nav M2M links to completed Cancellation/Change instances exist (no import linker in this slice).
- **Cross-skill**: application-profile | visa2014-to-visa2026-import | visa2026-report-dashboard

### 2026-08-27 — Edit Application number and date on case workspace

- **Need**: Officers must change Application number (`№ 8/-007`) and date (`Started 25 Aug 2024`) on the case workspace. Those values live on the instance, not the profile, and were not in Case summary.
- **Fix**: Case summary always shows Application number + Application date (not `Require*` gated). **Edit** saves `FullApplicationNumber` (parsed into prefix/sequence, `IsManualEntry`) and `ApplicationDate` (Year/Month). Header `№` uses the full application number. Post–office-prep lock no longer blocks number/date.
- **Test**: `ApplicationWorkspaceCaseHeaderFieldsHelperTests` + `ApplicationLockedHeaderScalarsDiffer_IgnoresApplicationNumberAndDateChange`. Officer: stop F5, rebuild, Ctrl+F5. Open a case → Case summary **Edit** → change number/date → **Done**. Header `№` and Started line update.
- **Prevent**: Do not hide number/date behind profile Use flags. Do not treat ProcessNumber as the application number in the header. Do not re-add number/date to `LockedApplicationHeaderTargetItems`.
- **Cross-skill**: application-profile | visa2026-application-progress

### 2026-08-26 — Hide Start process on Person DetailView

- **Need**: Officers should not start a case from Employee / Person DetailView (`+ Start process…`).
- **Fix**: `PersonStartApplicationController` action `Active["PersonDetail"] = false`. Dossier Start process is unchanged.
- **Test**: Restart / Ctrl+F5. Open an employee — toolbar has no Start process. Dossier still has it.
- **Prevent**: Do not re-activate `PersonStartApplication` on Person DetailView without officer ask.
- **Cross-skill**: application-profile

### 2026-08-26 — Case workspace tab-switch progress bar

- **Need**: Left case nav (Overview / People / Progress / Document copies / Resminamalar / SLA) gave no feedback while the next panel rendered.
- **Fix**: Same UX as Report Dashboard — sticky indeterminate bar + top-pinned overlay. Paint that first (`Task.Delay(16)`), then mount the next tab. Nav highlights the pending item immediately.
- **Test**: Blazor Debug. Officer: Ctrl+F5, open a case, click Resminamalar then Overview.
- **Prevent**: Do not mount Document copies / Resminamalar in the same render as the first overlay paint, or the bar never appears until the heavy tab is done.
- **Cross-skill**: application-profile

### 2026-08-26 — Faster Application Profile Instance open from ListView

- **Need**: Clicking a row on Application Profile Instances (via ministry / direct) stayed on “Loading case workspace…” for a long time.
- **Fix**: Workspace `Load` is read-only — do not `RefreshApplication` (heal was thrown away without commit). Do not load cities/contracts/etc. until **Edit** on case summary. Batch linked-record queries. Do not materialize `File.Size` for issued-copy chips.
- **Test**: Module + Blazor Debug succeeded. Header-field tests still pass. Officer: stop F5, rebuild, open a via-ministry instance from the list.
- **Prevent**: Do not call `Catalogs.Load` on every workspace open. Do not `ThenInclude` ministry letter `FileData` (blobs). Auto-heal stays on Link person.
- **Cross-skill**: application-profile

### 2026-08-26 — Hide Staged profiles and In process from left nav

- **Need**: Officers should not see **Staged profiles** or **In process** under Application Profiles.
- **Fix**: `Visible="False"` on those nav items in `Model.DesignedDiffs.xafml`. `CustomNavigationUpdater` no longer re-adds them; it hides them if present. ListViews remain for other use.
- **Test**: Restart / Ctrl+F5. Application Profiles shows Templates, via ministry, and direct migration only.
- **Prevent**: Do not call `EnsureApplicationListNavItem` for `Application_Staged` / `Application_InProcess`.
- **Cross-skill**: application-profile

### 2026-08-26 — Field cues on all issued BOs from Application Profile Instance

- **Need**: Highlighting was missing on Invitation/Rejection compose and on native New DetailViews for issued headers and items.
- **Shipped**: Same orange / blue / green on all issued compose kinds. Native new DetailView cues for Invitation, InvitationItem, WorkPermit, WorkPermitItem, BorderZone, BorderZoneItem, Rejection, RejectionItem, Visa.
- **Test**: Blazor Debug succeeded. Officer: Ctrl+F5.
- **Prevent**: InvitationItem / RejectionItem / BorderZoneItem on compose are roster tables, not extra item fields.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Compose field border cues (visa / WP / border zone)

- **Need**: Empty vs default vs confirmed fields looked the same on issued compose.
- **Shipped**: Orange empty required; blue system defaults; green after officer leaves the field. Passport/computed expiration are sourced. Legend on visa, work permit, and border zone compose.
- **Test**: Blazor Debug succeeded. Officer: Ctrl+F5 on New issued visa, New work permit, New border zone.
- **Prevent**: Invitation/rejection people tables stay un-cued. Do not paint read-only passport as “please edit” blue.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Work permit compose person cards (implemented)

- **Need**: Officers set `WorkPermitItem` fields (item number, AS, position, Start/End, locations), not only checkboxes.
- **Shipped**: WP kind in `IssueIssuedHeaderSlotPanel` uses visa-style cards. Dates copy from last valid work permit. One letter per case; employees only. `EnsureRosterWorkPermitItems` skips when lines already exist.
- **Test**: `IssueIssuedHeaderWorkPermitComposeTests` + Module/Blazor Debug. Officer: stop F5, Ctrl+F5, + Add work permit on a visa+WP case.
- **Prevent**: Do not put AS on the WorkPermit header. Do not copy Start/End from current visa. Do not auto-add omitted employees on save.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Work permit compose person-card prototypes (not implemented)

- **Need**: Issued work permit cannot be checkbox-only; each `WorkPermitItem` needs officer fields (item number, AS, position, Start/End, locations).
- **Shipped (prototype only)**: `docs/prototypes/issue-work-permit-slot-01` … `04` + README. One letter per case; employees only; Start/End from last valid work permit.
- **Test**: Officer review of PNGs. Do not implement until accepted.
- **Prevent**: Do not put AS number on the WorkPermit header. Do not list non-employees. Do not copy Start/End from current visa.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Issued visa compose for visa-only profiles (roster)

- **Need**: Extension / direct (**May produce Visa**, not Invitation) uses the same `#visa-preview-slot` as invitation Path A — people from the case roster, one visa per unused person, `IssuingInvitationItem` null. Linked Visas M2M is not a source.
- **Shipped**: `CanOpenInSlot` = `ShowIssuedVisas` only. `UsesInvitationSource` keeps Path A invitation lines. Roster LoadDraft/Create; `Visa_IssuingApplicationProfileInstanceSingleUse` is one visa per person on the case. Slice **10q-iv**.
- **Test**: `IssueIssuedVisaComposeServiceTests` 4 passed. Module + Blazor Debug 0 errors. Officer: stop F5, rebuild, Ctrl+F5. Visa-only case → **+ Add issued visa** → **People on this case**; Create; already-issued card locked. Invitation+visa case unchanged.
- **Prevent**: Do not open XAF visa modal when `CanOpenInSlot`. Do not call this officer path Path B. Do not use skip-nav `Visas` as a compose source. Do not keep one-visa-per-case uniqueness.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Instance-source visa mermaid (roster vs linked)

- **Need**: Same issued-vs-input picture as invitation items, for profiles that produce Visa without Invitation.
- **Shipped**: `docs/diagrams/issued-visa-origin/instance-roster-issued-vs-input.mmd` synced in `APPLICATION_PROFILE_ISSUED_VISA_ORIGIN.md`. Roster people stamp origin; linked Visas M2M and WorkPermit are not visa sources; `IssuingInvitationItem` is null.
- **Test**: Open the `.mmd` in preview; compare to invitation-item-issued-vs-input.mmd.
- **Prevent**: Do not call this officer path “Path B” — that name is import backfill.
- **Cross-skill**: application-profile

### 2026-08-26 — Direct/extension issued-visa slot prototypes (not implemented)

- **Need**: Same preview-slot compose when the profile produces **Visa but not Invitation** (roster people; `IssuingInvitationItem` null). Work permit stays a sibling issued tile.
- **Shipped (prototype only)**: `docs/prototypes/issue-issued-visa-instance-slot-01` … `04` + README. Path A invitation compose unchanged.
- **Test**: Officer review of PNGs. Do not implement until accepted.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — New issued visa hid the third invitation person (no scroll)

- **Symptom**: Three people on the invitation; New issued visa showed two cards and no scrollbar.
- **Fix**: Compose catalog scrolls (`overflow-y: auto` on `issue-issued-header-slot-panel`). Third person is below the first two visa cards.
- **Test**: Ctrl+F5, New issued visa → scroll past Serdar to Ali.
- **Prevent**: Do not assume one/two person cards fit the slot height.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Visa Delete must not Clear skip-nav collections

- **Symptom**: Delete a0001 showed `'ObservableCollection<T>.Clear' is not supported` (XAF `Reset` / items removed).
- **Fix**: Unlink `Visa.ApplicationProfileInstances` with `Remove` per item. Do not `Clear()` XAF `ObservableCollection` navigations.
- **Test**: Module Debug build. Officer: rebuild, Ctrl+F5, Delete a0001 → confirm → row gone, no red error.
- **Prevent**: XAF collections need `Remove`, not `Clear`.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Delete issued visa from Application Profile Instance

- **Need**: Officers remove newly issued visas from the case, like invitation rows.
- **Fix**: **Delete** next to Preview under **Visas issued by this case**. Unwinds the invitation line (`IsUsed` false) so a new visa can be issued. Copies/images go with the visa.
- **Test**: Compose Delete guard unit test + Blazor Debug build. Officer: Ctrl+F5, Delete a0001/a0002, confirm, list updates.
- **Prevent**: Invitation Delete stays blocked while a visa still exists on a line — delete the visa first.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Issued visa list Preview in the slot

- **Need**: Preview visa copies from **Visas issued by this case**, like invitation rows.
- **Fix**: Workspace **Preview** on each visa row. Disabled until `VisaDocument` exists. Opens header-copies occupant (`HeaderDocumentCopiesFamily.Visa`, `OpenPreviewOnly`). No Delete on visas.
- **Test**: Catalog HasCopy unit tests passed. Officer: Ctrl+F5. Upload copy on a0002 → Preview enabled → PDF in right slot. Row title still opens compose.
- **Prevent**: Do not hide visa Preview behind invitation/WP delete capability.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Visa compose: issued place + visa copy upload

- **Need**: Path A compose was missing `VisaIssuedPlace` and could not attach a visa scan on create.
- **Fix**: Per-person **Visa issued place** lookup (defaults to catalog IsDefault). **Visa copy** upload on each card: pending until Create, immediate on edit. Stores `VisaDocument` like invitation copies.
- **Test**: Debug Blazor build succeeded. Officer: + Add issued visa → pick issued place, Upload copy, Create. Edit a0002 → place shown; Upload copy lists on the card.
- **Prevent**: Do not silently stamp only the default issued place; officers must see and can change it. Do not invent a second file store — use `Visa.Documents`.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Visa edit Save disabled until dirty

- **Need**: Edit issued visa Save should stay disabled until a field changes, like XAF DetailView.
- **Fix**: Same fingerprint as invitation compose (`CanSaveVisa` / `IsDirty`). Recapture after load and successful Save.
- **Test**: Open a0002 — Save grey. Change visa number or date — Save blue. Save — grey again.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Click issued visa row to edit in the slot

- **Symptom**: **Visas issued by this case** row opened XAF Visa / a0002 modal.
- **Fix**: Row click opens Path A compose occupant with `ExistingVisaId`. Editable compose fields + Save. `TryOpen` refuses Path A so the modal cannot return. Direct/extension visa stays modal.
- **Test**: Debug Blazor build succeeded. Officer: Ctrl+F5, click a0002 → custom edit, Save, list caption updates.
- **Prevent**: Issued visa rows must go through `TryOpenIssuedVisaInSlotAsync(id)` / `ExistingVisaId`, not `IssuedHeaderOpenHelper.TryOpen`.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Compose Border zone is the Visa popup, not a checkbox grid

- **Symptom**: Officers rejected the inline border-zone checkbox wrap on invitation Header and visa person cards. Visa DetailView uses summary + … popup.
- **Fix**: Wired `BorderZoneLocationField` into both compose panels. Domain defaulting (`Invitation.BorderZoneLocation` → visa) unchanged.
- **Test**: Debug build succeeded. Officer: stop F5, Ctrl+F5. Click … on compose → same “Border zones” popup as Visa.
- **Prevent**: New compose fields that exist on a BO with `[EditorAlias(BorderZoneMultiSelect)]` must use `BorderZoneLocationField`, not a custom checkbox list.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Invitation and Path A visa share BorderZoneLocation

- **Need**: Same Visa border-zone multi-select on Invitation; new visas default from the invitation.
- **Shipped**: `Invitation.BorderZoneLocation` (catalog `BorderZoneName`, default `Ýok`). Invitation compose Header checkboxes; visa compose per-person checkboxes prefilled invitation → case → Ýok. Schema heal `Invitations.BorderZoneLocation`.
- **Test**: `BorderZoneSelectionHelperTests` passed. Officer: stop F5, rebuild, Ctrl+F5. Set border zone on invitation → + Add issued visa → person card shows those zones.
- **Prevent**: Do not invent a second border-zone store on Invitation.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Path A issued-visa preview-slot compose (shipped)

- **Need**: **+ Add issued visa** compose in `#visa-preview-slot` per accepted prototypes 01–04.
- **Shipped**: `IssueIssuedVisaComposeService` (issued invitation lines only; one visa per unused line; unique numbers; chronology). Slot panel groups cards by invitation; used lines show Visa issued; Create blocked when none unused; stay in slot after create and refresh Issued visa count. Direct/extension (`ProduceVisa` without invitation) stays modal.
- **Test**: `dotnet build Visa2026.slnx -c Debug`; unit test `CanOpenInSlot` requires both produce flags. Officer smoke: invitation case → + Add issued visa → fill numbers → Create visas.
- **Prevent**: Do not restore Issue visa on invitation compose.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Issued-visa Path A preview-slot prototypes (before implement)

- **Need**: Officer compose for **+ Add issued visa** in `#visa-preview-slot`, per issued-vs-input invitation-item diagram.
- **Shipped (prototype only)**: `docs/prototypes/issue-issued-visa-slot-01` … `04` + README. One visa per person with an issued invitation line; per-person visa fields; grouped by letter; no Issue visa shortcut on invitation compose.
- **Test**: Officer review of PNGs. Do not implement until accepted.
- **Cross-skill**: application-profile | preview-slot

### 2026-08-26 — Invitation profiles also produce visa; service-passport does not produce invitation

- **Need**: Issued visas tile on invitation-producing Application Profiles; service-passport cases must not produce invitations.
- **Shipped**: Calik tenant seed `application-profile.calik-energi.json`: `ProduceVisa=true` on `get_invitation`, `get_invitation_wp`, `get_invitation_fm`, `get_invitation_according_to_wp`, `change_invitation`. `get_invitation_service_passport` `ProduceInvitation=false` (visa stays off).
- **Test**: Restart app so `ApplicationProfileSeedSync` overwrites catalog rows. Invitation case Overview shows Invitation + Issued visa tiles. Service-passport case hides Invitation tile.
- **Cross-skill**: application-profile | lookup-data

### 2026-08-25 — Issued-row Preview for invitation copy

- **Need**: Preview the invitation file from Issued records list, not only from compose.
- **Shipped**: **Preview** on issued Inv/WP/RJ/BZ rows → header-copies occupant `OpenPreviewOnly`. `HasCopy` from `InvitationDocument` (and siblings).
- **Test**: Upload copy on 30 → Preview on that row shows the file in the slot.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-25 — Invitation copy upload on compose panel

- **Need**: Upload invitation file copy from the case-workspace compose slot.
- **Shipped**: `IssueIssuedHeaderComposeService.AddDocument` / `RemoveDocument` → `InvitationDocument`. UI card **Invitation copy** (Upload + list + Remove).
- **Test**: Edit invitation → Upload copy → listed. Same pattern for WP/RJ/BZ copies on their compose panels.
- **Cross-skill**: preview-slot | application-profile

### 2026-08-25 — Invitation people: letter vs available lists

- **Need**: Assigned and unassigned people must not share one table (looks like an incomplete letter).
- **Shipped**: Primary **People on letter** card; secondary dashed **Available on this case** below (Add / Add all ready). Same slot card stack as Header.
- **Test**: Edit 30 — letter people only in the first card; unassigned Ali in Available.
- **Cross-skill**: preview-slot | application-profile### 2026-08-25 — Unassigned roster person can join either invitation

- **Need**: Person not yet on any invitation must be selectable on edit of 30 or 31.
- **Shipped**: Edit people list = this letter + unassigned. Occupied-on-other stay hidden.
- **Test**: Open invitation 30 or 31 → third person listed Ready/unchecked → check → Save.
- **Cross-skill**: preview-slot | application-profile### 2026-08-25 — Invitation person list is per letter

- **Need**: A person on one invitation must not appear on another invitation's compose list.
- **Shipped**: Edit shows only people on that invitation. New invitation shows only people not yet on a letter. Occupied "On invitation N" rows are hidden, not locked.
- **Test**: Invitation 31 no longer lists people from 30. New invitation lists remaining roster only.
- **Cross-skill**: preview-slot | application-profile### 2026-08-25 — Delete Invitation from Application Profile Instance

- **Need**: Officers must be able to delete an unused invitation produced by the case (Issued records), not only create/edit in the slot.
- **Shipped**: `IssueIssuedHeaderComposeService.Delete` + Issued records **Delete** + slot **Delete** (edit). Guard: refuse if any `InvitationItem` is used / has `IssuedVisa`. Refreshes list; closes open compose slot.
- **Test**: Delete unused invitation from Overview list → gone. Invitation that issued a visa → error shown.
- **Cross-skill**: preview-slot | application-profile### 2026-08-25 — Issued-header slot edit mode (Invitation fields)

- **Need**: Opening existing invitation showed only summary (number + people); officers must correct header mistakes in-slot.
- **Shipped**: `LoadExistingDraft` / `Update` hydrate editable form (number, issued/expiration, category, period, visa window dates) + people; Save stays on form; Issue visa on lines; unused invitation lines can be removed when unchecked.
- **Test**: Click invitation **005** → Edit invitation form → change field → Save → "Saved."
- **Cross-skill**: preview-slot | application-profile
### 2026-08-25 — Issue issued-header preview-slot compose (Inv/WP/RJ/BZ)

- **Shipped**: `VisaPreviewSlotMode.IssueIssuedHeader` + `IssueIssuedHeaderSlotPanel` + `IssueIssuedHeaderComposeService`. Workspace **New invitation / work permit / rejection / border zone** opens the slot (not modal DetailView). **New issued visa** still modal.
- **Compose**: header fields + roster people checkboxes; Create validates passport readiness; stamps `*.ApplicationProfileInstance` and selected line BOs only (issued output, not input M2M).
- **After create**: stay in slot; `IApplicationWorkspacePersonUiActions.NotifyWorkspaceChanged` refreshes Issued records; invitation lines offer **Issue visa**.
- **Open path**: `ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose` → `OpenIssueIssuedHeaderAsync` (same GetService pattern as document copies).
- **Build**: `dotnet build Visa2026.Blazor.Server -c Debug` green.
- **Cross-skill**: preview-slot | application-profile
### 2026-08-25 — IssuingInvitationItemID missing until rename runs

- **Symptom**: `42703: column v.IssuingInvitationItemID does not exist` on Passport.Visas lazy load / case workspace.
- **Cause**: DB still had `Visas."InvitationItemID"`; ModuleUpdater rename had not applied (or failed silently with `throwException: false`).
- **Fix**: rename column on Postgres; harden `VisaIssuingInvitationItemSchemaSql` (pg_catalog + both-columns merge) and fail loudly on SQL errors.
### 2026-08-25 — Visa.IssuingInvitationItem rename + issued-vs-input diagram

- **Rename**: `Visa.InvitationItem` → `Visa.IssuingInvitationItem`; FK column `InvitationItemID` → `IssuingInvitationItemID` (`VisaIssuingInvitationItemSchemaUpdater`). Inverse stays `InvitationItem.IssuedVisa`. Caption **Issuing invitation item**.
- **Rule**: Only **issued** lines (`Invitation.ApplicationProfileInstance` → `InvitationItems`) may be visa source. Input M2M `ApplicationProfileInstance.InvitationItems` is for cancel/change — not `IssuingInvitationItem`.
- **Diagram**: `docs/diagrams/issued-visa-origin/invitation-item-issued-vs-input.mmd` (+ sync in `APPLICATION_PROFILE_ISSUED_VISA_ORIGIN.md`).
- **Also**: Path A / Issue visa / Path B correction / OData field map / Excel Visas sheet / cleanup SQL.

### 2026-08-22 — Issued records: Issued visa tile follows ProduceVisa only

- **Rule**: Overview / DetailView **Issued records → Issued visa** tile and **Issued visas** tab visibility = **May produce → Visa** (`ShowIssuedVisas` → `CanIssueVisa` / `ProduceVisa`), same as Invitation / Work permit tiles follow their May produce flags.
- **Not** shown when only **May produce → Invitation** is on (`App_Inv`, `App_Inv_And_WP`). Visa after invitation: **InvitationItem → Issue visa** (same FKs; see issued-visa-origin doc).
- **Fix**: `ShowIssuedVisas` no longer uses `CanBeIssuingApplicationProfileInstanceForVisa` (which ORs invitation). Stamping eligibility unchanged on `CanBeIssuingApplicationProfileInstanceForVisa`.


### 2026-08-22 — Issued work permit origin (Module shipped)

- **Officer UI**: New issued work permits from Application Profile Instance → **Issued records → New work permit** (or nested **Work permits**). `WorkPermit.ApplicationProfileInstance` required on save; read-only **Issuing profile instance** on detail.
- **Blocked**: root **Work Permit** list **New** (`WorkPermitStandaloneCreateBlockController`).
- **Output lines**: `WorkPermitIssuedRosterItemsHelper` ensures one `WorkPermitItem` per roster **employee** when issuing from instance (not import). Copies `MovementPermitLocation` from instance when set.
- **UI**: Hide `WorkPermitItem.ApplicationProfileInstances` M2M when parent header has issuing FK. Model.xafml: `ApplicationProfileInstance` on work permit detail (replaces stale `Application`).
- **Single-use**: one work permit header per issuing instance.
- **Doc/diagrams**: [`docs/APPLICATION_PROFILE_ISSUED_WORK_PERMIT_ORIGIN.md`](../../../docs/APPLICATION_PROFILE_ISSUED_WORK_PERMIT_ORIGIN.md)


### 2026-08-22 — Issued visa origin diagram (canonical doc)

- **Doc**: [`docs/APPLICATION_PROFILE_ISSUED_VISA_ORIGIN.md`](../../../docs/APPLICATION_PROFILE_ISSUED_VISA_ORIGIN.md) — Mermaid (extension vs invitation profile), officer entry points, validation table, **YAML machine spec** for agents/import.
- **Diagrams**: Mermaid only — `.mmd` under `docs/diagrams/issued-visa-origin/` (embedded in canonical doc).
- Linked from `APPLICATION_PROFILE_PLAN.md` §10.1 row 11 and `reference.md`.


### 2026-08-22 — Invitation-item-centric visa create (Issue visa action)

- **Officer UI**: **Issue visa** on unused `InvitationItem` (ListView + DetailView) opens new `Visa` with `IssuingApplicationProfileInstance` from parent invitation + pre-selected `IssuingInvitationItem` + passport. Still blocks Passport nested New.
- **Multi-person invitation cases**: `Visa_IssuingApplicationProfileInstanceSingleUse` skipped when `IssuingInvitationItem` set or profile `ProduceInvitation` — uniqueness is per invitation line (`Visa_IssuingInvitationItemSingleUse`).


### 2026-08-22 — Issued invitation origin: instance-side create + roster output lines

- **Officer UI**: New issued invitations from Application Profile Instance → **Issued records → New invitation** (or nested **Invitations**). `Invitation.ApplicationProfileInstance` required on save; read-only **Issuing profile instance** on detail.
- **Blocked**: root **Invitation** list **New** (`InvitationStandaloneCreateBlockController`).
- **Output lines**: `InvitationIssuedRosterItemsHelper` ensures one `InvitationItem` per roster person when issuing from instance (not import). `IsUsed` remains **visa consumption only** (`Visa.IssuingInvitationItem`).
- **UI**: Hide `InvitationItem.ApplicationProfileInstances` M2M when parent header has issuing FK. Model.xafml: `ApplicationProfileInstance` on invitation detail (replaces stale `Application`).
- **Single-use**: one invitation header per issuing instance (validation rule).


### 2026-08-22 — Issued visa origin: instance-side create only + IssuingApplicationProfileInstance

- **Officer UI**: New issued visas must use Application Profile Instance → **Issued records → New issued visa** (or nested **Issued visas** on instance DetailView). `Visa.IssuingApplicationProfileInstance` set at create; save blocked when null on new officer visa.
- **Blocked**: nested **New** on `Passport.Visas` (`PassportVisasNestedCreateBlockController`).
- **Path A**: no longer guesses issuing instance from passport roster; only optional `InvitationItem` when `IssuingApplicationProfileInstance` already set.
- **UI**: Visa detail shows **Issuing profile instance** (read-only); hides input M2M `ApplicationProfileInstances` when issued FK set.
- **Import Path B**: `Visa2014VisaIssuingApplicationProfileInstanceIndex` (PIA index → Application) on `--entity Visa`; OData `IssuingApplicationProfileInstance`; re-import backfill; `--correct-visa2014-issuing-application-profile-instance`.
- **E2E note**: `PersonOfficerJourneyTests` still uses Passport→Visas New — update to instance path or master-data exception when touching E2E.


### 2026-08-22 — Locked reminder: person-related links only when RequirePerson* checked

- **Rule** (officer + import): Required person-related data (Passport, Visa, WorkPermitItem, InvitationItem, …) auto-link onto an **ApplicationProfileInstance** only when the related **Application Profile** has that `RequirePerson*` checkbox on.
- **Code**: `ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled` → `ApplicationProfileConfigurationResolver` / profile `RequirePerson*`. Toggle-off keeps sticky existing links; no new auto-links.
- **Import**: do not mass-link WorkPermitItem/InvitationItem (or other kinds) for a type whose profile has the flag off (e.g. App_Visa_and_WP_Ext Work permit item). Prefer `EnsureResolvedLink` only when the profile requires that kind, or explicit officer/legacy evidence that matches a checked RequirePerson*.
- **Cross-skill**: visa2014-to-visa2026-import (`--correct-visa2014-existing-item-links` must respect profile gates)


### 2026-08-22 — App_Visa_and_WP_Ext: Work permit item off required person data

- **Profile**: `Wiza we Iş Rugsatnamasyny Uzaltmak` (`App_Visa_and_WP_Ext` / `extend_visa_wp`)
- **Change**: `RequirePersonWorkPermitItem` **false** (was true). **May produce** WorkPermit + Visa unchanged (`ProduceWorkPermit`/`ProduceVisa` true; Invitation false).
- **Also**: ApplicationType catalog `ShowCurrentWorkPermitItem` **false** so dual-read does not re-show the checkbox.
- **Files**: `application-profile.calik-energi.json`, `ApplicationTypeConfigurationCatalog.json`; local PG rows patched.
- **Verify**: reopen Application Profile wizard step Templates & person → Work permit item unchecked; May produce still Work permit + Visa.
- **Note**: UI “Work permit item” is `WorkPermitItem` / `RequirePerson*`, not header `WorkPermit` (outcome).


# Application Profile — learnings (append-only)

Read **before** Application Profile work; **append** after verified fixes and slice completions. Promotion rules: [MATURITY.md](./MATURITY.md).

---

### 2026-08-21 — Import ApplicationProfile FK with type-only tenant catalog

- Symptom: after `--import-visa2014` App_Inv_And_WP, ListView **Application Profile** empty; Approval leg profile filled.
- Cause: tenant JSON is **36 type-only** profiles (`DefaultProjectContract` null; do not restore Wave 0b 176). Import/`FindProfile` required contract-variant match → FK omitted.
- Fix: `ApplicationProfileCatalogGroupKey.FindProfile` (+ importer DTO `FindProfileId`) falls back to type-only profile when variant missing. Wave 2 `--patch-visa2014-application-profile` patched **1181** → `get_invitation_wp`.
- Prevent: Do not re-add Wave 0b contract clones just to make import set the FK; keep type-only + fallback.
- Cross-skill: visa2014-to-visa2026-import

### 2026-08-21 — Show Application Profile name on case workspace DetailView

- ListView already showed `ApplicationProfile.Name`; workspace only buried it as footer "Template: …". Surface the same name under the case title and in Case summary subtitle; footer label is **Application Profile:** (not nested Word/Excel template).
- Verify: F5 → open a via-ministry case → header shows Turkmen profile name under №; Case summary line starts with Application Profile: ….
- Prevent: Do not confuse `ProfileTemplateName` chrome field with nested `ApplicationProfileTemplate` rows — it is the live profile `Name`.
- Cross-skill: -

### 2026-08-21 — Hide deprecated Type column on instance ListViews

- **Type** on Applications via ministry / direct / staged / in-process is deprecated `ApplicationType` (dual-read FK). Officers already see **Application Profile**. Hide via `VisibleInListView(false)`, Blazor `Model.xafml` column removal, and `ApplicationProfileInstanceHideDeprecatedTypeColumnUpdater` (Index=-1). Do not drop the FK yet (slice 13).
- Verify: stop F5, rebuild, F5 — Applications via ministry grid has no Type column; Application Profile remains.
- Prevent: Do not re-add `ApplicationType` ColumnInfo to Application_* ListViews in `Model.xafml`.
- Cross-skill: -

### 2026-08-21 — Use profile (live link): OptimisticLockField on create

- Symptom: Choose Application Profile → pick shared approval-leg card → **Use profile (live link)** shows *"The object you are trying to save was changed by another user"* (no other user). Same OptimisticLockField class of bug as Phase B soft-delete heal.
- Cause: create commit path ran `EnsureSnapshots` → `ApplySnapshot`, which soft-deleted just-Created snapshot rows (never inserted) and/or loaded the full shared `ApprovalLegProfile` catalog into the write ObjectSpace.
- Fix: skip `EnsureSnapshots` recreate when the new instance already has snapshots; discard new snapshot objects via `RemoveFromModifiedObjects` instead of soft-delete; resolve only the chosen chain with `LoadSharedProfileWithLegs`; catch `DbUpdateConcurrencyException` in picker completion with a clear retry message.
- Verify: stop F5, rebuild Module, F5. Via-ministry ListView → New → select profile + TE-EN (or Default) → Use profile — workspace opens without concurrency toast.
- Prevent: Do not soft-delete Added approval-leg snapshots before first commit. Do not call `GetSharedActiveProfiles` (full Include catalog) inside the create write ObjectSpace when the officer already picked a version id.
- Cross-skill: -

### 2026-08-21 - Local F5 Azure OpenAI for Template Convert

- **Config**: `appsettings.Development.json` sets `Provider=AzureOpenAI`, `Endpoint=https://visa2026-openai.openai.azure.com/`, `Deployment=gpt-4o-mini`. Base `appsettings.json` stays `Provider=None` so non-dev slots do not call Azure by default.
- **Secret**: `TEMPLATE_AI_CONVERT_AZURE_OPENAI_API_KEY` is a Windows **User** env var only (ApiKey left empty in JSON). Restart Visual Studio / Cursor after setting the User env so F5 inherits it.
- **Naming**: Azure list **Name** is `gpt-4o-mini` (model gpt-4o-mini). Do not put a mismatched deployment nickname in `Deployment`.
- Cross-skill: -


### 2026-08-21 - Roster loop derivation unblocks Convert on people tables

- **Shipped**: `TemplateRosterLoopPlanner` + `TemplateConvertAnalysis.DeterministicPlan`. Analyze builds header + first-row substitutions and `{{#ds.rows}}` / `{{/ds.rows}}` markers; `RosterLoopBlocksConversion` only when a loop cannot be placed. Convert uses that plan; AI sanitize keeps local loops when the provider returns none (marker cells are not in the extract). Dialog copy updated. 4 tests; **175** TemplateConvert green.
- **Excel markers sit beside the prototype row.** `ExcelReportGenerator` treats the `#ds.rows` row as the clone prototype and deletes the `/ds.rows` row when it is below. Planner puts open on the first data row (free column after matched cells) and close on the next physical row — not on the last sample person row (that would leave middle rows as junk after merge).
- **Only the first roster `RowIndex` is tokenized.** Later people stay as literals; residual scan + an explicit warning tell the officer to delete them. Tokenizing every row would produce a template that still hard-codes N people.
- **Word loops only need paragraph addresses.** Writer prepend/append ignores Start/Length; planner uses `WordSpan(addr, 0, 0)` for boundaries so they do not collide with substitution spans in the sanitizer overlap check when AI later proposes loops.
- Cross-skill: visa2026-user-report-templates (same `{{#ds.rows}}` generator contract)

### 2026-08-21 - E10 Azure OpenAI adapter (HTTP, no vendor SDK)

- **Shipped**: `Adapters/AzureOpenAiTemplateConvertAiProvider` + options `TemplateAiConvert:AzureOpenAI`; `ConvertAsync` calls `ProposeMappingAsync` when `IsEnabled`, sanitizes, falls back to deterministic with a warning. Chat already hit the same `ITemplateConvertAiProvider`. Default stays `Provider=None`. 6 tests; **171** TemplateConvert green; Blazor host builds.
- **No Azure.AI.OpenAI package.** Q14 forbids vendor SDK namespaces/assemblies in the Module. The adapter posts Chat Completions JSON over `HttpClient` and parses `choices[0].message.content`. Adapter type names under `...Adapters` may say AzureOpenAi; SDK namespaces may not.
- **Secrets are env-first.** Prefer `TEMPLATE_AI_CONVERT_AZURE_OPENAI_API_KEY`. appsettings may hold Endpoint/Deployment for Demo; ApiKey in committed JSON stays empty.
- **AI is an accelerator, not a gate.** If the HTTP call fails or returns an empty sanitized plan, Convert continues with local matches and adds a warning - the officer still gets a draft.
- **Demo pilot:** only the Demo slot sets `Provider=AzureOpenAI` plus endpoint/deployment + API key env. Other slots keep None.
- Cross-skill: visa2026-analytics-ai-chat (same pluggable None-then-Azure pattern, separate interface)

### 2026-08-21 - E9 Preview chat: L8 intent gate before the provider

- **Shipped**: `TemplateConvertChatIntentClassifier`, `ITemplateConvertChatService`, Preview **Adjust mapping** panel in `TemplateConvertDialog` (hidden when Validate hard-fails / V10). `ApplyPlanAsync` on the orchestrator so chat remaps reuse the same diff / residual / validate path as Convert. 14 tests (Q11 / Q12); **165** TemplateConvert green; Blazor host builds.
- **Out-of-scope must win before the provider.** Q11 wants `OutOfScopeContentEdit` on a rewrite ask even with `Provider = None`. The None adapter alone would answer `NotUnderstood` / "AI off". The classifier short-circuits first with the PNG-09 copy; a stub that would happily rewrite never sees those messages (`RewriteReached` stays false).
- **Accepted turns change the plan only through sanitizer + ApplyPlanAsync.** The chat service does not write OOXML itself - that keeps one write path and one Errors/Warnings merge. Against None, mapping asks still refuse without touching the draft; a stub remap proves the draft bytes change and a following rewrite leave them identical (Q12).
- **Razor class attributes cannot nest quoted ternaries.** `tac-msg--@(cond ? "officer" : "assistant")` fails CS1073; use a small `ChatBubbleCss` helper instead.
- Cross-skill: -

### 2026-08-21 - E8 AI provider seam: None adapter + plan sanitizer

- **Shipped**: `ITemplateConvertAiProvider` / `NoneTemplateConvertAiProvider`, `ITemplateMappingPlanSanitizer`, `TemplateMappingRequestBuilder`, extended `TemplateAiConvertOptions` (`Provider`, timeout, max chars, redact flag), DI in `AddTemplateConvert`. 7 tests (Q7 / Q13 / Q14 + sanitizer / mask / stub); **151** TemplateConvert green.
- **E-D1 is a type-system property, not prompt discipline.** `TemplateMappingRequest` carries `MaskedPreview` and token *names* only - Q7 walks the property graph by reflection and fails if a BO, `IObjectSpace`, or `RawValue` appears. A future cloud adapter cannot "accidentally" be handed a passport number without changing the DTO.
- **Sanitizer runs before the writer, on every provider path.** Out-of-set tokens, unknown regions, and overlaps are dropped with reasons - that is what makes Q13 (and later Q11/Q12) hold even if a model hallucinates `{{ds.HACK}}`. The None adapter never needs this for its own output, but chat (E9) and real adapters (E10) must call it the same way.
- **None is not "feature off".** Convert UI stays available; `IsEnabled = false` means no cloud assistance. Chat replies that AI is turned off. Unknown `Provider` keys fall back to None so a misconfigured slot still converts deterministically (Q14).
- **Not wired into `ConvertAsync` yet.** The officer path is still pure deterministic (E1-E6). E9 attaches the chat panel to this seam; E10 registers a real adapter under a new key without touching domain services (vendor types stay out of `Visa2026.Module` - Q14 already asserts that).
- Cross-skill: -

### 2026-08-21 — E7b profile side: the same dialog inside the wizard's transaction

- **Shipped**: **Convert existing document** beside `+ Add template` in `ApplicationProfileWizardStepTemplatesPerson.razor`, driven by a second entry on the same dialog — `TemplateConvertDialog.OpenForProfileAsync(profile, objectSpace)`. Host builds clean; no Module change was needed, which is the payoff for putting the sequencing in `ITemplateConvertOrchestrator` first.
- **The wizard's object space must be borrowed, not replaced.** The dialog's case entry creates and disposes its own object space, but doing that inside the wizard would commit a template into a profile the officer is still editing — and then Cancel would leave it behind. The profile entry therefore reuses `ObjectSpace`, **skips `CommitChanges`**, and lets **Save profile** commit it, exactly like the existing `CreateTemplate` + `ApplicationProfileTemplateUserReportBridge` path. Consequences worth remembering: the dialog must never dispose an object space it did not create (the `ownedObjectSpace` local exists only for that), and the Done screen has to say "press Save profile" or the officer will think it is stored.
- **No case in context means the officer picks one.** Matching needs real values, so V1 grows the PNG 15 picker over the 25 newest instances of that profile, and **Check document** stays disabled until one is chosen. A profile with no cases yet (including an unsaved one) cannot convert at all — the picker says so and points at `+ Add template`, which is honest rather than showing an empty dropdown.
- **The L13 switch does not apply here.** It exists to keep an authoring action out of the caseworker's Resminamalar tab; someone editing a profile is already authoring templates. The wizard entry is gated by `TemplateAiConvert:Enabled` + `TemplateConvertAccess.CanConvertTemplates()` only.
- **Encoding trap, second hit**: `TemplateConvertDialog.razor` had been rewritten once through `Get-Content -Raw` (PS 5.1 reads as ANSI), which turned every `—` into `â€"` and `·` into `Â·` — invisible in the editor's rendering of the diff, and the file still compiled. Repaired by replacing the three known mojibake sequences and writing back UTF-8 no BOM. **Never round-trip repo text through `Get-Content`/`Out-File`; use `[System.IO.File]::ReadAllText` / `WriteAllText` with `UTF8Encoding($false)`** — see the `visa2026-utf8-encoding` rule.
- Cross-skill: visa2026-user-report-templates (both entries save through the same bridge)

### 2026-08-21 — E7b instance side: convert dialog wired to the real services

- **Shipped**: `TemplateConvertDialog.razor` + `TemplateConvertOutlineView.razor` (`Visa2026.Blazor.Server/Editors/`), `wwwroot/css/template-convert.css` (E7a CSS ported verbatim, so the prototype stays the reference), entry buttons in the top action row of `ApplicationReportPackageComponent.razor`, and in the Module `ITemplateConvertOrchestrator` + `ITemplateDocumentOutlineReader`. Verified by `TemplateConvertOrchestratorTests` (real .docx in, `{{ds.AFNUM}}` out, diff gate passed, residual clean) — 144 TemplateConvert tests green, host builds.
- **The sequencing belongs in the Module, not the component.** Seven services in a fixed order, each feeding the next, with two non-obvious rules (pass the writer's **applied** substitutions to the diff gate, never the requested ones; residual probes are built from replaced literals, not from E2 types). Left in Razor that becomes untestable and gets copied wrong onto the profile side. `Analyze` / `ConvertAsync` / `Save` also gave the round-trip test a seam that needs no browser.
- **The UI needed a projection nobody had written.** E5 returns highlights addressed as `DocumentRegion.WordSpan("body/3", 12, 9)` but nothing exposed *what body/3 says*, so there was no way to draw the document under the highlights. `ITemplateDocumentOutlineReader` reuses `WordTemplateAddressing.EnumerateParagraphs`, which matters more than it sounds: any second addressing scheme would drift from the writer's and silently highlight the wrong span.
- **Roster conversion is blocked, deliberately and visibly.** The writer accepts `{{#ds.rows}}` markers, but nothing derives them from a candidate report, and header-only substitution on a roster would emit a template that repeats row one for every person — wrong output that looks plausible. Candidate check surfaces `RosterLoopDetected`, the rail explains it, and Convert stays disabled with "Add prepared template" as the exit.
- **Preview lands in the V12 fallback by design, not by omission.** A merged preview needs either a persisted template (E4 draft) or an in-memory generate + PDF path; neither exists, so Preview shows the template with placeholders plus the amber notice. Fabricating a "filled" view from the value map would have shown the officer something the generator never produces.
- **The message catalog generator is currently unsafe to run.** `dotnet run --project tools/GenerateModelLocalization` with **no** input change rewrote 8 files and *reverted* the hand-made `ApplicationItem` → `ApplicationProfileInstance` key renames in `VisaUiMessageCatalog.g.cs` — the generated file is ahead of its `UiStrings.messages.json` source. Reverted the run; the dialog therefore ships English literals rather than `Msg()` keys that would render as raw key names. **Fix the JSON to match the cutover before adding any new UI strings**, otherwise the next person to regenerate silently undoes them.
- Gate: `TemplateConvertAccess.CanConvertTemplates()` requires write on **both** `UserReportTemplate` and `ApplicationProfileTemplate` — the Resminamalar `CanEditTemplates()` check alone would let someone convert who cannot own the profile template it creates. On top of that `TemplateAiConvert:Enabled` + `ShowInstanceEntry` stand in for the undecided L13 per-user switch.
- Cross-skill: visa2026-resminamalar (the catalog component now hosts the entry), visa2026-user-report-templates (`Save` goes through `ApplicationProfileTemplateUserReportBridge`, same path as the wizard), visa2026-security-access (dual write permission)

### 2026-08-21 — Template convert entry points: where the buttons actually go in the real app

- **The prototype's page names do not map onto the real app, and one of them is a trap.** The mock's *Profile templates* catalog page has no counterpart: the navigation item of that name renders `ApplicationProfileCatalogComponent`, which lists **`ApplicationProfile`** rows — there is no `ApplicationProfileTemplate` ListView at all, because templates are `ApplicationProfile.NestedTemplates` children edited in the wizard. The real template list is **wizard step 5** (`ApplicationProfileWizardStepTemplatesPerson.razor`), so **Convert existing document** goes beside the existing `+ Add template` there. The L12 manual button already exists on that side — `+ Add template` *is* it.
- **Instance side goes in the shared component, not in either host.** `ApplicationReportPackageComponent.razor` is rendered by **two** hosts: `ResminamalarSlotPanel.razor` (the preview-slot drawer opened from the XAF DetailView via `WordReportsController`) and `OfficerShellCaseResminamalarTab.razor`. Copying the prototype's banner bar into the officer-shell tab would leave the drawer path — the main one — without the feature. One insertion in the component's existing top action row (`Select all` / `Clear selection` / `Placeholder manual`) covers both.
- **L13's switch has no home yet.** The prototype parks it in an officer-shell topbar that does not exist in XAF, and L13 requires it to persist per user across cases, so the Resminamalar gear cannot hold it as-is: that gear's `ShowDetails` is component-local and resets on re-render. The two real candidates are an `ApplicationUser` column following `PreferredThemeCaption` / `PreferredCulture` (consistent, but a schema change, so it queues behind the slice 10 heal like E4) or a per-user `ModelDifference` value (no schema change). **Left open**; E7b ships both buttons behind a hardcoded-off flag plus the §8 permission so the lift is not blocked on it.
- Cross-skill: visa2026-resminamalar (the catalog component and its two hosts), visa2026-preview-slot (drawer path), visa2026-security-access (template-authoring permission gates both entries)

### 2026-08-21 — E7a template convert: fill-preview fallback + manual add / AI off (all 16 PNGs covered)

- **V12** (PNG 16) and **V13** (PNG 08) close the prototype: `?convert=fill-error`, `?convert=manual`, `?ai=off`. Two PNGs never needed a view of their own — **09** is the chat refusal inside V4 and **15** is the V1 instance select — and both were *verified by rendering*, not assumed. Worth doing before claiming coverage: "the behavior is in there somewhere" is how a gap survives to the Blazor lift.
- **V12 is the one failure mode where Approve stays enabled.** Validate passed and the merge failed, which is an *instance data* problem, not a template defect (spec §6.1). So the UI degrades instead of blocking: tab relabelled *Filled preview (error)*, preview opens on Placeholders, amber notice explains the fallback. The `previewTab` default is now driven by one predicate — hard failure **or** fill failure — because both mean "the filled view is a lie".
- **Manual add is a `mode`, not a stage.** It reuses the modal shell but swaps body, title and footer and hides the stepper, because there is nothing to step through. It deliberately drops the **context instance** field: the file already carries its placeholders, so asking for an instance would imply a matching step that never runs. Validation against the profile set still happens on save — only matching is skipped.
- **AI off disables Convert rather than hiding it**, with an `AI off` badge, and promotes **Add prepared template** to primary. A disappearing entry point reads as a bug and generates support traffic; a disabled one with a reason answers the question in place. Like `configLocked`, `aiEnabled` gets **no switch** — it is per-slot deployment config (spec §7), so the prototype exposes it as `?ai=off`. Adding a topbar toggle would have taught officers that it is theirs to flip.
- Class-name collision worth remembering: `.tac-entry` was already the case-workspace entry *bar*; the new button group needed `.tac-entry-actions`. Reusing the name silently inherited `margin-left: auto` onto the wrong element and clipped the second button off the bar — caught only in a screenshot, since the markup assertions passed.
- Verify: `node parity/smoke-edge.mjs` now 32 assertions (adds V12 fallback, V13 manual guards and Done copy, AI-off entry markup); headless Edge at 1360×900 for all four new states.
- Cross-skill: visa2026-resminamalar (manual template upload is the same L12 promise made in the report package dialog)

### 2026-08-21 — E7a template convert: edge states (candidate Fail / Warn, validate fail, config locked)

- Views **V8–V11** built on the existing V2/V4 layouts, so the whole convert flow (V1–V11) is now reviewable: `?convert=fail|warn|validate-fail|locked`, plus `?locked=1` which locks any stage. Two fixtures carry them — an HR memo with zero instance overlap (Fail) and a hand-tokenized draft with a bad token, an unclosed loop and a row token in a header template (Warn at candidate, hard fail at validate). One document covering both stages is worth more than two clean ones: it is exactly the file officers actually upload.
- **A Warn candidate now blocks Convert until the officer ticks "Continue with warnings"** (PNG 07), which contradicts what the flow doc first said. The reason is cost asymmetry: from **E10** a conversion run spends an AI provider call with a p95 of 90 s, so the cheap moment to stop a doubtful document is *before* the run, not at Approve. The two acknowledgements answer different questions — `acknowledgedCandidate` gates spending a run, `acknowledgedWarnings` (E-D2) gates saving a template — so they are separate state, not one flag reused.
- **When validation hard-fails, the mapping chat is replaced by the error rail** rather than shown beside it. Chat is mapping-only by **L8**, so it cannot repair an unknown token, a broken loop, or an out-of-scope row token; leaving it on screen invites the officer to type at a problem the assistant is forbidden to fix. The preview also opens on the **Placeholders** tab and marks each rejected token red inline, so the rail and the document point at the same thing.
- Config lock is **not** a user preference and gets no switch: it mirrors the parent profile, shows a `CONFIG LOCKED` badge plus banner, and disables Approve *only* — upload, convert, preview, chat and the gap packet all stay live, which is what makes "you can still preview" true rather than a slogan.
- Verify: `node parity/smoke-edge.mjs` (20 assertions over the four states — every disabled button, both acknowledge gates, and the roster warning-only path) then headless Edge screenshots at 1360×900. Two things the assertions did not catch and the screenshots did: `"1 fields matched"` pluralization, and a rail reason claiming 2 matches when the chip counted 1. Guard tests confirm behavior; only a rendered page catches copy.
- Cross-skill: visa2026-resminamalar (validation-failure UX vocabulary), visa2026-security-access (config lock is policy, never a preference)

### 2026-08-21 — E7a template convert: HTML modal in the officer shell

- Slice **E7a** shipped in `Visa2026.Blazor.Server/wwwroot/officer-shell/`: `template-convert-ui.js`, `template-convert-data.js`, `template-convert.css`, plus a `headActionsHtml` hook on `renderTemplateCatalog` and a modal host (`#os-modal`) in `index.html`. Covers PNGs **01–05 + 13** (Upload, Candidate check, Converting, Preview + chat, Done, Excel roster); edge cases 06–12 and 14–16 are still `⬜` in `parity/CHECKLIST.md`.
- **The mock store is written against the shipped C# DTOs, not against the screens.** Highlights carry a `region` shaped like `DocumentRegion.WordSpan` (`paragraphAddress`, `start`, `length`) or `.ExcelCell` (`sheetName`, `cellReference`), and the reports mirror `TemplateCandidateReport` / `TemplateValidationReport` including `hasHardFailure` / `hasWarnings`. That is what makes E7b a fetch swap: `canApprove()` already encodes the **E-D2** rule (hard failure blocks, warnings need the checkbox), and rendering already consumes real offsets. Shaping the mock to whatever was convenient for HTML would have bought a translation layer that has to be deleted.
- **Two decisions were locked before any markup:** host is a **self-contained modal**, not a `#visa-preview-slot` occupant (convert is authoring, and it must not fight the slot's exclusive mode while Resminamalar is open — spec §4); and the instance entry point is **opt-in per user** (spec **L13**), so the case workspace stays clean for officers who never author templates. The switch sits in the **topbar**, not on the case — putting it next to the button it reveals would defeat the point.
- Re-rendering: `renderModal()` repaints only `#os-modal` and rebinds only modal listeners. Calling the shell's `bindEvents()` after a partial repaint **double-binds every page control** (two `addEventListener` on the same surviving node → double navigation), so page-level and modal-level binding must stay separate. Entry buttons (`[data-tac-open]`) live in page content, so they belong to `bindEvents`.
- Review aid: `?convert=upload|candidate|roster|converting|preview|done|help|confirm` and `?editor=1` deep-link a stage and the L13 switch, so parity sign-off needs no click-through and headless screenshots are possible. Prototype-only — they must not survive the Blazor lift.
- **The interaction scenario is now its own doc**, [`docs/TEMPLATE_AI_CONVERT_UI_FLOW.md`](../../../docs/TEMPLATE_AI_CONVERT_UI_FLOW.md): views V0–V11, every control, its target view, and the guards. Writing it before wiring exposed four transitions the PNGs never show and that would otherwise have been invented twice (once in HTML, once in Blazor): **Cancel on Converting aborts the run back to Candidate check instead of closing** (the officer wants to stop the run, not lose the upload); **closing between Upload and Done needs a discard prompt** while Done does not; **Needs help is a view with a Back target, not an alert**, so it must remember which stage opened it (`returnStage`); and **Shared confirm plus gap confirm are one dialog, not chained prompts**. The confirm is a *layer* over the current view (`state.confirm`), never a stage, so Cancel is genuinely a no-op.
- Guard placement worth keeping: `canConvert()` (suitability Fail) belongs on V2 and `canApprove()` (validation error / unacknowledged warning) on V4. Putting both on Approve would let an officer spend a conversion run on a document that can never be saved.
- Verify: rendered all five stages in headless Edge at 1440×900 against a local static server (`user-manual/.tools/python312/python.exe -m http.server` — the Windows `python` alias is the Store stub and fails). Module smoke-tested with `node -e "import(...)"`; no build or test impact (`wwwroot` assets only).
- Cross-skill: visa2026-preview-slot (host decision — convert deliberately stays out), visa2026-resminamalar (case entry sits above the Resminamalar catalog)

### 2026-08-21 — E6 template convert: ephemeral extract/validate + warning tier

- Slice **E6** shipped: `IEphemeralTemplateValidationService` (`Services/TemplateConvert/`, 15 tests). Runs the existing Word/Excel stream extractors and placeholder validators over **in-memory bytes**, adds the L10 allowed-set check, and splits findings into `Error` (blocks Approve, **Q3**) and `Warning` (acknowledge checkbox, **E-D2**). Registered **`AddScoped`, not `AddSingleton`** like the rest of `AddTemplateConvert` — `IUserReportPlaceholderExtractor` and both validators are scoped in `Startup.cs`, and a singleton wrapper would be a captive dependency.
- The **exclusion reason from the E1 set is what makes the severity split possible.** A token missing from `Allowed` is not automatically unknown: look it up in `Excluded` first, because `PersonPackDisabled` means the token resolves and merges as empty text (Warning) while `OutOfDataScope` and `StructuralUnsupportedForKind` mean nothing can ever bind it (Error). Without that lookup every disabled pack would hard-block a template that is actually usable. `ApplicationProfilePlaceholderSet` now echoes **`DataScope` and `TemplateKind`** so the merge root comes from the set alone (`PeopleM2M` → `ApplicationItem`, else `ApplicationProfileInstance`); Excel always validates as `ExcelMergeMode.ItemList`, since `SingleItem` is a seed-time authoring choice with no convert equivalent.
- **Loop markers must be kept out of the property validator.** `ValidateRowsCollection` rejects `rows` outright when the root is `ApplicationItem`, so passing `{{#ds.rows}}` through would hard-fail every roster conversion; and the collection name is authoring-defined (`rows`, `ApplicationItems`), so it cannot be judged at all. Both extractors also **de-duplicate into a `HashSet`**, so document order is gone and nesting cannot be checked — what remains checkable, and what actually breaks the generators, is set equality of open vs close names.
- Verify: `dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug` → 515 passed; `dotnet build Visa2026.slnx -c Debug` → 0 errors.
- Gap to carry into **E7**: product spec §6.1 also lists a *low-confidence leftover literal* warning, which E6 cannot produce — leftover literals come from the **E3 residual scanner**, which needs the instance value map. E7 must **merge both issue lists** before deciding Approve, or that warning silently disappears. Issues carry a `TemplateValidationIssueCode` so the modal never matches on message text.
- Cross-skill: visa2026-user-report-templates (extractors, placeholder maps), visa2026-resminamalar (template validation UX)

### 2026-08-21 — E5 template convert: candidate check (suitability + highlights)

- Slice **E5** shipped: `ITemplateCandidateAnalyzer` + `TemplateTextIndex` + `TemplateCandidateModels` (`Services/TemplateConvert/`). Highlights reuse the **E3 `DocumentRegion`**, so a `Match` converts straight into a `TokenSubstitution` with no second addressing pass, and tokens can only come from the E2 map (built from the E1 set) — that is how **Q8** holds by construction rather than by a check.
- **The load-bearing piece is offset mapping, not scoring.** Matching must run on normalized text (folded diacritics, collapsed whitespace, invariant lowercase) while the writer needs offsets into the *original* text, and normalizing changes lengths. `TemplateTextIndex` records the source range of every normalized character so a hit maps back exactly. Without it, any paragraph with a double space or a `ý` highlights the wrong span, the writer edits the wrong characters, and **the E3 diff gate then rejects the whole conversion** for touching unapproved text — a failure that would look like a diff-gate bug, not a matching bug. `TemplateTextNormalizer` gained `Fold(char)` and `IsIdentifierSeparator(char)` so the index shares the one folding table (no second normalizer).
- Rules worth remembering: overlaps resolve **longest-match-wins** (`PFN` "Dowletmyrat Amanov" must beat `PLN` "Amanov" nested in it); an Excel **cell is replaced whole**, so it carries at most one token and the region is the cell; a roster loop needs **2+ distinct `RowIndex`** because one roster row is indistinguishable from a one-off mention (a single-person instance never detects a loop); gaps are limited to unmatched **date-like and 6+ digit** literals, since anything looser marks ordinary prose as missing data; an already-tokenized file demotes Pass→Warn but never rescues a Fail; an unreadable upload returns **Fail with the parser message**, never an exception, because this is an officer-supplied-file boundary.
- Thresholds live in `TemplateSuitabilityOptions` bound to `TemplateAiConvert:Suitability` per **E-D6** (proceed 3 / pass 6 / pass-with-roster 2) — never `const`. `AddTemplateConvert` now takes an optional `IConfiguration`; defaults apply when the section is absent.
- Verify: `dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug` → 500 passed; `dotnet build Visa2026.slnx -c Debug` → 0 errors.
- Doc drift found: spec §6 was headed "E5 — AI provider abstraction" from an older slice numbering (it is **E8**), and E5 had **no contract section at all** — only L7, E-D6, and Q8/Q9. Heading corrected and the shipped contract written up as **§4.4**. Check the slice table, not the section headings, when picking up a slice.
- Cross-skill: visa2026-user-report-templates (placeholder tokens), visa2026-preview-slot (highlight rendering, later slice)

### 2026-08-20 — E2 template convert: instance value map + ambiguity rejection

- Slice **E2** shipped: `IApplicationProfileInstanceValueMapService` + `TemplateValueMatchKeys` (`Services/TemplateConvert/`). Reads each allowed placeholder's `CanonicalPath` off the `ApplicationProfileInstance` (header) or an `ApplicationRosterMergeLine` (row) through the existing **`UserReportMergeDataHelper.GetPropertyValue`**, so **no `UserReportTemplate` is needed** — the point of the slice. Sync, takes the instance BO plus an optional `Rows` list, which makes it fully unit-testable with no database. Requires the **E1 placeholder set**, so a token the profile disallows can never reach the matcher. A candidate carries **`MatchKeys`** (many forms), not one normalized string: date re-renderings, swapped name order, separator-stripped identifiers.
- Three real bugs surfaced only because tests ran against the actual BOs: (1) **unset dates leak** — `DateTime.MinValue` renders as `01.01.0001` via computed text properties like `ApplicationDateText`, becoming a candidate that would highlight a date no document contains; treat sentinel dates as *absent*, not rejected. (2) **`1,500` parsed as 1.5** — a single comma is a decimal mark in one convention and a thousands separator in another; emit **both readings** plus a separator-stripped key instead of choosing. (3) **Composed tokens collapse onto their source** — `Person_ForeignAddressWithCountry` prefixes a country code, so with no country it equals `Person_ForeignAddress`; both are then unattributable and both must drop out. Ambiguity = one match key resolving to 2+ short codes; the same short code repeating across rows is normal and must not trigger it.
- Verify: `dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug` → 482 passed; `dotnet build Visa2026.slnx -c Debug` → 0 errors.
- Prevent: order the rejection checks so `SmallNumber` is reported before the generic `TooShort` length gate, otherwise every 1–2 digit number reports the uninformative reason. Skip `IsImage` entries — a photo property returns `byte[]`, and `ToString()` on it yields `"System.Byte[]"` as a text candidate. **Turkmen month names in `TemplateValueMatchKeys` are not sourced from a repo lookup** — confirm against real ministry documents before trusting long-form date matching.
- Cross-skill: visa2026-user-report-templates (merge dictionaries, placeholder maps), visa2026-resminamalar

### 2026-08-20 — E1 template convert: profile-scoped placeholder set + catalog `packKey`

- Slice **E1** shipped: `IApplicationProfilePlaceholderSetService` (`Services/TemplateConvert/`) plus a new **`packKey`** on all **66** entries of `Resources/UserReportPlaceholderCatalog.json` and a `UserReportPlaceholderPack` enum. Query takes the **`ApplicationProfile` BO**, not a Guid, so the service needs no `IObjectSpace` and is unit-testable against the real embedded catalog. Unknown/missing `packKey` parses to `Unknown` and is **excluded** — fail-closed, so a JSON typo hides a token instead of leaking it into every profile (a test asserts zero `Unknown` in the shipped catalog). Packs were assigned by tracing each `ApplicationRosterMergeLine` property to the navigation it actually reads, which caught two traps: **`Contract_StartDateText` / `Contract_ExpirationDateText` read `CurrentVisa.ExpirationDate`** (pack `PersonVisa`, not salary/contract), and **`Passport_PersonalNumber` falls back to `Person.PersonalNumber`** (pack `Core` — it resolves with no passport record). Both are locked by regression tests. `PdfForm` kind allows nothing (convert is Word/Excel only); Excel drops `IsImage`.
- Verify: `dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug` → 445 passed; `dotnet build Visa2026.slnx -c Debug` → 0 errors. Registered via the existing `AddTemplateConvert()`.
- Prevent: **Never infer a pack from a token prefix or canonical path** — trace the merge-line property to its navigation. Do not default an unknown pack to allowed. Do not filter the placeholder set by `RootBoType`: the catalog's `rootBoTypes` uses `"Application"`, which is **not** a `UserReportBoType` member (`ApplicationProfileInstance` / `ApplicationItem` / `Person`), so `Enum.TryParse` drops it — `["Application"]` silently falls back to both types while `["Application","ApplicationItem"]` resolves to `ApplicationItem` only. Correcting those 66 values would change the existing manual placeholder browser, so it needs its own decision; `Scope` already separates header from row.
- Cross-skill: visa2026-user-report-templates (catalog JSON + placeholder maps), visa2026-resminamalar (merge dictionaries)

### 2026-08-20 — E3 template convert: token writer + diff gate + residual scan

- Slice **E3** of template AI convert shipped in `Visa2026.Module/Services/TemplateConvert/` (15 files, 37 tests) with **no AI, no schema, no Blazor**: `ITemplateTokenWriter`, `ITemplateConversionDiffGate`, `ITemplateResidualValueScanner`, registered by `AddTemplateConvert()` in `Startup.cs`. **Run splitting turned out unnecessary** — writing the token into the **first `w:t` the span touches** and deleting the rest of the span from later text nodes preserves run count and `rPr`, so the token inherits the first run's formatting for free (same walk `WordUserReportImageInjector` already uses). Paragraph addresses are **ordinal** (`body/12`, `header0/3`) from `WordTemplateAddressing`, not `w14:paraId`, which real ministry documents often lack; table and text-box paragraphs fall out of `Descendants<Paragraph>()` for free. The gate compares **structural invariants, not bytes** (OpenXml and ClosedXML both renormalise what they rewrite) and must be fed `TokenWriteResult.AppliedSubstitutions` / `.AppliedLoops`, never the requested set, or every skipped edit reads as a violation. `TemplateTextNormalizer` (invariant casefold + Turkmen/Turkish folding + identifier compaction) landed here because the scanner needs it — **E2 must reuse it**, not write a second one.
- Verify: `dotnet build Visa2026.Module/Visa2026.Module.csproj -c Debug` (0 errors) and `dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug` (422 passed). Fixtures are built in code (`TemplateConvertFixtures`), so no binary documents entered the repo. ClosedXML round-trip was confirmed to preserve number formats, column widths, and merged ranges under the Excel gate.
- Prevent: Do not gate E1/E2/E3 on the slice 10 heal — only **E4** adds a table. Do not pass requested (rather than applied) edits to the diff gate. Do not target a **formula** cell or a **non-anchor merged** cell in Excel; both are skipped by design and a silent write would be lost. Do not compare documents byte-wise. Do not use `tr-TR` casing anywhere in matching. Runs nested in `w:hyperlink` are not `rPr`-checked by the gate (text still is) — revisit if a golden document depends on it.
- Cross-skill: visa2026-user-report-templates (placeholder maps, loop syntax `{{#ds.rows}}`), visa2026-resminamalar (merge/preview engine)

### 2026-08-20 — Phase B: instance approval-leg snapshot backfill (shared catalog)

- Imported via-ministry applications keep their inferred `ApprovalLegProfile` (do not restamp to the template Default). Empty FK uses `ApplicationProfile.DefaultApprovalLegProfile`. F5/deploy runs `ApplicationProfileInstanceApprovalLegBackfill` after Default seed; CLI remains `--backfill-application-approval-leg-snapshots`. Also stamps `ApprovalLegVersionName`. Does not rewrite progress rows. Query **Includes** instance/default `ApprovalLegProfile` + `MinistryLegs` + snapshots so an unloaded nav is not treated as “FK empty”.
- Verify: stop F5, rebuild, F5. Open an imported App_Inv that was AH in VISA2015 — snapshot/Ministrlik is AH, not TE-EN. Empty-snapshot via-ministry cases pick up legs. Optional: `dotnet run --project Visa2026.DataImporter -- --backfill-application-approval-leg-snapshots --dry-run`.
- Prevent: Do not overwrite a set instance `ApprovalLegProfile` with the profile Default. Do not delete progress to heal Ministrlik. Do not query instances without Include on approval-leg navigations when deciding assign vs keep. Do not filter EF queries on `CreationProgressRoute` — it is `[NotMapped]` (create-time only); use profile/type `ProgressRoute` in SQL. Commit catalog before instance heal; heal in a **fresh** ObjectSpace. Bulk heal must **not** soft-delete/recreate snapshots (`ApplySharedSnapshot`) — that causes OptimisticLockField / “changed by another user” with no other user; only **insert** when snapshot count is 0. SeedGate must not fail host start if heal fails (CLI remains).
- Cross-skill: application-profile, visa2014-to-visa2026-import
### 2026-08-20 — Shared ApprovalLegProfile catalog (not per-profile copies)

- Via-ministry templates now reuse Configuration **Approval leg profiles** (like Company / Signatory). Each profile stores only `DefaultApprovalLegProfile`. Wizard Identity lists the shared catalog + **Edit in Configuration**. Create picker snapshots the chosen shared chain. Phase A seed sets Defaults and **deletes nested** `ApplicationProfileApprovalLegVersion` copies. Plan §2.1 #7 locked to this redesign (revises slice 8l).
- Verify: stop F5, rebuild, F5. Configure profile → Identity shows TE-EN / TG / … with a Default radio. Edit in Configuration opens the shared list. New application → pick a shared version. Nested Add version / Duplicate is gone.
- Prevent: Do not copy ministry chains onto each Application Profile. Do not treat `ProjectContract.ApprovalLegProfile` as the officer catalog. Do not overwrite a set instance `ApprovalLegProfile` with the template Default.
- Cross-skill: application-profile, visa2014-to-visa2026-import
### 2026-08-20 — Phase A: seed Approval leg versions from VISA2015 frequency

- Via-ministry Calik profiles get a **Default** shared `ApprovalLegProfile` from legacy frequency (`ApplicationType` × `ApprovalLegProfileCode`). Exporter reuses `Visa2014ApplicationApprovalLegProfileInference` + lookup-translations. **Retargeted the same day:** seed no longer copies legs onto each profile (see Shared ApprovalLegProfile catalog entry). Fallback **TE-EN** when a type has no legacy apps. Matrix: `docs/VISA2014_MIGRATION/lookup-comparisons/ApplicationProfileApprovalLegVersions.calik-energi.md`. Deploy/host-start: `ApplicationProfileApprovalLegVersionTenantCatalogSync`.
- Verify: stop F5, rebuild, F5. Configure profile → Identity → Approval leg versions (e.g. App_Inv has TE-EN Default plus TG/NG/AH…). Regenerate: `ApplicationProfileApprovalLegVersions-CalikEnergi.ps1` (needs `VISA2014_SQL_PASSWORD`, defaults to `.15`).
- Prevent: Do not put approval legs on nested Word/Excel templates. Do not invent a second inference algorithm. Phase B (instance snapshot backfill) is separate.
- Cross-skill: application-profile, visa2014-to-visa2026-import
### 2026-08-20 — Work permit location uses Border zone comma-separated multi-select

- Case summary and wizard **Work permit location** now use the same `CommaSeparatedMultiSelectComponent` as Border zone (`BorderZoneLocationField` with `WorkPermittedLocationMultiSelect` alias / `WorkPermittedLocationName` catalog). Instance storage is comma-separated `Application.MovementPermitLocation` text (not `MovementPermitLocation` FK). Profile default is `DefaultWorkPermitLocation`. Wizard Results kind tag is **multi-select**.
- Verify: stop F5, rebuild, F5. Configure profile → Results & fields → Work permit location shows the … popup (not a dropdown). Case summary Edit on a WP-location profile uses the same control.
- Prevent: Do not bind work permit location to `MovementPermitLocation` FK or single `<select>`. Do not reuse Border zone `Ýok` none-value on this field (empty string).
- Cross-skill: application-profile

### 2026-08-20 — App_Additional_WP_location type-only seed

- Calik profile `App_Additional_WP_location` / `change_workpermit` / **Iş Rugsatnama goşmaça  barjak ýeri**. Route **Via ministries**. Related to **Issuance**. Audience **Employee** only. May produce **Work permit + Work location**. SLA ministry **4** / migration **30**. Results: project + **work permit location** — no visa type/period/category/urgency/border zone. Person: passport, education, **work permit item**, salary — no position, address, visa, or medical. Nested templates not seeded. Last remaining Application Type in the Calik catalog.
- Verify: stop F5, rebuild, F5. Catalog shows additional WP location name. May produce Work permit + work location; Results work permit location on; person WP item + salary on, Visa off.
- Prevent: Do not seed per-project Wave 0b rows. Do not require border zone or urgency. Do not turn on person Visa (type `ShowCurrentVisa` is false).
- Cross-skill: application-profile

### 2026-08-20 — App_WP_Ext type-only seed

- Calik profile `App_WP_Ext` / `workpermit_extension` / **Iş Rugsatnamasyny Uzaltmak**. Route **Via ministries**. Related to **Issuance**. Audience **Employee** only. May produce **Work permit** only. SLA ministry **4** / migration **30**. Results: project, border zone, **work permit location** — no visa type/period/category/urgency. Person: passport, education, position, address, **visa**, **work permit item**, salary, medical. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Iş Rugsatnamasyny Uzaltmak**. May produce Work permit only; Results work permit location on; person Visa + WP item on.
- Prevent: Do not produce visa or invitation. Do not require urgency on this type.
- Cross-skill: application-profile

### 2026-08-20 — App_Visa_and_WP_Ext type-only seed

- Calik profile `App_Visa_and_WP_Ext` / `extend_visa_wp` / **Wiza we Iş Rugsatnamasyny Uzaltmak**. Route **Via ministries**. Related to **Issuance**. Audience **Employee** only. May produce **Visa + Work permit**. SLA ministry **4** / migration **30**. Results: visa type **WP**, period **Month6**, category **Multiple**, project, urgency, border zone, **work permit location**. Person: passport, education, position, address, **visa**, **work permit item**, salary, medical. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows combined extension name. May produce Visa + WP; Results work permit location on; person Visa + WP item on.
- Prevent: Do not produce invitation. Do not treat border zone as produce (location field only). Do not add Family/visitor.
- Cross-skill: application-profile

### 2026-08-20 — App_Visa_For_New_Born_FM type-only seed

- Calik profile `App_Visa_For_New_Born_FM` / `visa_for_new_born_fm` (type Code `visa_extension` shared). Name **Täze dogulan çaga wiza resmileşdirmek FM**. Route **Via ministries**. Related to **Issuance**. Audience **Family member** only. May produce **Visa** only. SLA ministry **4** / migration **30**. Results: visa type **FM**, project, urgency, border zone. Person: passport, education, address — **no current visa** (newborn). Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows newborn FM name. May produce Visa; Results FM; person Visa off.
- Prevent: Do not reuse `visa_extension` / `visa_ext_fm` as Code. Do not require person Visa on newborn cases.
- Cross-skill: application-profile

### 2026-08-20 — App_Visa_Ext_FM type-only seed

- Calik profile `App_Visa_Ext_FM` / `visa_ext_fm` (type Code `visa_extension` shared). Name **Wiza Möhletini Uzaltmak FM**. Route **Via ministries**. Related to **Issuance**. Audience **Family member** only. May produce **Visa** only. SLA ministry **4** / migration **30**. Results: visa type **FM**, project, urgency, border zone — no visa period/category. Person: passport, education, address, **visa**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Wiza Möhletini Uzaltmak FM**. May produce Visa; Results FM; audience Family only.
- Prevent: Do not reuse `visa_extension` / `visa_ext` as this profile Code. Do not add Employee audience. Do not produce border zone permit (RequireBorderZone is location only).
- Cross-skill: application-profile

### 2026-08-20 — App_Change_Passport type-only seed

- Calik profile `App_Change_Passport` / `pasport_change` / **Wizany KP>Täze Pasporta Geçirmek**. Route **Direct to migration**. Related to **Issuance** (transfer visa to new passport). Audience **Employee + Family member**. May produce **Visa** only. Migration **10** working days (UP-TO-TWO-WEEKS). Results: urgency only. Person: passport, education, address, **visa**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Wizany KP>Täze Pasporta Geçirmek**. Direct migration; May produce Visa; person Passport + Visa on.
- Prevent: Do not use Via ministries / Project. Do not cancel visas. Do not confuse with registration `App_Reg_Info_Change_Passport`.
- Cross-skill: application-profile

### 2026-08-20 — App_Change_Visa_Category type-only seed

- Calik profile `App_Change_Visa_Category` / `visa_category_change` / **Wiza Kategoriýasyny üýtgetmek**. Route **Direct to migration**. Related to **Issuance** (change, not cancel). Audience **Employee + Family member**. May produce **Visa** only. Migration **10** working days. Results: **visa category** (default Multiple) + urgency; no visa type/period, project, or border zone. Person: passport, education, address, **visa**. Nested templates not seeded. (Type catalog `ShowVisaCategory` is false; profile still Uses category so officers can pick the new category.)
- Verify: stop F5, rebuild, F5. Catalog shows **Wiza Kategoriýasyny üýtgetmek**. Direct migration; May produce Visa; Results category + urgency; person Visa on.
- Prevent: Do not use Via ministries / Project. Do not cancel visas. Do not add Temporary visitor unless officer asks.
- Cross-skill: application-profile

### 2026-08-20 — App_Exit_Visa type-only seed

- Calik profile `App_Exit_Visa` / `visa_exit` / **Çykyş  Wiza Resmileşdirmek**. Route **Via ministries**. Related to **Issuance**. Audience **Employee + Family member** (Category Both). May produce **Visa** only. SLA ministry **4** / migration **30**. Results: visa type **EX**, period **Day10**, category **Single**, project, urgency. No border zone. Person: passport, education, address, **visa**, medical. Nested templates not seeded. (Type catalog has `ShowVisaType: false`; profile still Uses visa type with fixed EX default so issued exit visas get a type.)
- Verify: stop F5, rebuild, F5. Catalog shows exit visa name. May produce Visa; Results EX / 10 days / Single; audience Employee + Family.
- Prevent: Do not reuse WP as visa type. Do not add Temporary visitor unless officer asks. Do not require border zone.
- Cross-skill: application-profile

### 2026-08-20 — App_Visa_Ext type-only seed

- Calik profile `App_Visa_Ext` / `visa_ext` (type Code `visa_extension` shared). Name **Wiza Möhletini Uzaltmak**. Route **Via ministries**. Related to **Issuance**. Audience **Employee** only. May produce **Visa** only. SLA ministry **4** / migration **30**. Results: visa type **WP**, period **Month6**, category **Multiple**, project, urgency, border zone. Person: passport, education, address, **visa**, medical (no WP item / salary / position — unlike According_to_WP). Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Wiza Möhletini Uzaltmak**. May produce Visa; Results WP / 6 months; person Visa on, Work permit item off.
- Prevent: Do not reuse `visa_extension` or `visa_ext_according_to_wp` as this profile Code. Do not require work permit item on this type.
- Cross-skill: application-profile

### 2026-08-20 — Restore App_Cancel_Visa_and_WP_Ext type-only seed

- Restored Calik profile `App_Cancel_Visa_and_WP_Ext` / `cancel_visa_wp_ext` / **Wiza we Iş Rugsatnamany Uzaltmak Barada Ýüztutmany Ýatyrmak**. Direct migration. **Cancellation**. Audience **Employee** only. May cancel **Application(s)** (visa+WP extension request) — no produce. Migration **3** working days. Person: passport, education, position, **visa**, **work permit item**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows the cancel visa+WP extension name. Related to Cancellation; May cancel Application(s); person Visa + WP item.
- Prevent: Do not cancel Visas/Work permits documents on this type (that is `App_Cancel_Visa_and_WP`). Do not add Family/visitor. Do not use Via ministries.
- Cross-skill: application-profile

### 2026-08-20 — App_Border_Zone_Permission type-only seed

- Calik profile `App_Border_Zone_Permission` / `get_border_zone` / **Serhet Ýaka Üçin Rugsatnama Almak**. Route **Via ministries**. Related to **Issuance**. Audience **Employee** only. May produce **Border zone** only. SLA ministry **14** / migration **10** (UP-TO-TWO-WEEKS). Results: border zone (comma-separated multi-select) + project. No visa type/period/category/urgency. Person: passport, education, **position**, address, **visa** (no border zone item — new permit is issued on this case). Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Serhet Ýaka Üçin Rugsatnama Almak**. May produce Border zone; person Position on, Border zone item off.
- Prevent: Do not produce visa/invitation/WP. Do not add Family/visitor audience. Pick approval-leg version at instance create (no per-project profile rows).
- Cross-skill: application-profile

### 2026-08-20 — Border zone uses Visa comma-separated multi-select

- Case summary and wizard **Border zone** now use `BorderZoneLocation` (comma-separated `BorderZoneName` catalog), same as `Visa.BorderZoneLocation` — not a single-select Guid lookup. New `BorderZoneLocationField` wraps `CommaSeparatedMultiSelectComponent`. Wizard Results kind tag is **multi-select**.
- Verify: stop F5, rebuild, F5. Case summary Edit on visa-extension profile shows multi-select popup (…); wizard default uses same control.
- Prevent: Do not bind border zone to `BorderZoneName` FK or single `<select>`. Do not use deprecated `BorderZoneLocation` lookup BO.
- Cross-skill: application-profile

### 2026-08-20 — App_Visa_Ext_According_to_WP type-only seed

- Calik profile `App_Visa_Ext_According_to_WP` / `visa_ext_according_to_wp` (type Code `visa_extension` shared with other visa-extension types). Name **Iş Rugsatnamasyna Görä Wizany Uzaltmak**. Route **Via ministries**. Related to **Issuance**. Audience **Employee** only. May produce **Visa** only (existing WP context). SLA ministry **4** / migration **30**. Results: visa type **WP**, period **PerWorkPermit**, category **Multiple**, project, urgency, border zone. Person: passport, education, position, address, **visa**, **work permit item**, salary, medical. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Iş Rugsatnamasyna Görä Wizany Uzaltmak**. May produce Visa; person Visa + Work permit item on; Results WP / per work permit.
- Prevent: Do not reuse `visa_extension` as this profile Code. Do not produce invitation or new work permit. Do not add Family/visitor audience.
- Cross-skill: application-profile

### 2026-08-20 — Hide Travel history for BusinessTrip profiles (hard guard)

- Business-trip templates could still show `Travel history` when existing DB rows had stale `RequirePersonTravelHistory=true`. Added hard guard: `ApplicationProfileConfigurationResolver.RequirePersonTravelHistory` returns false for `ActionFamily=BusinessTrip`.
- Wizard Person step now hides that checkbox for BusinessTrip and auto-clears stale true values in `OnParametersSet`. Overview/linked-item derivations also ignore Travel history for BusinessTrip.
- Verify: stop F5, rebuild, F5. Business-trip profile Overview chips and person requirements should not include Travel history.
- Cross-skill: application-profile

### 2026-08-20 — Business-trip profiles should not require Travel history

- Removed mapper coupling `RequirePersonTravelHistory = type.ShowBusinessTrips`; business-trip profiles now default to **not requiring** `TravelHistory`.
- Updated Calik business-trip seeds (`App_Business_Trip_Departure`, `App_Business_Trip_Arrival`) to `RequirePersonTravelHistory: false`.
- Verify: stop F5, rebuild, F5. In Configure Application Profile, Business-trip person toggles should keep Travel history off unless officer explicitly enables it.
- Cross-skill: application-profile

### 2026-08-20 — App_Business_Trip_Arrival type-only seed

- Added Calik profile `App_Business_Trip_Arrival` / `business_trip_arrival` / **Iş Saparyna Gelmek**. Mirrors departure business-trip requirements: migration service, start/end dates, region + city, business trip address, purpose; person passport/address/visa/travel history on.
- Verify: stop F5, rebuild, F5. Templates list shows **Iş Saparyna Gelmek** and opening an arrival case shows the same business-trip case-summary fields.
- Prevent: keep profile `Code` unique from departure (`business_trip_departure` vs `business_trip_arrival`) even though type `Code` is shared (`business_trip`).
- Cross-skill: application-profile

### 2026-08-20 — BusinessTripAddress table missing (no DbSet)

- Case summary lookup queried `BusinessTripAddress` but the table was never created: BO existed without `DbSet`, so EF EnsureCreated skipped it on existing DBs. Added `DbSet` + entity mapping + host-start `BusinessTripLookupSchemaSql.ApplyIfMissing` (also `BusinessTripPurpose`).
- Verify: stop F5, rebuild, F5. Open business-trip case — no 42P01. Admin can maintain Business trip address lookup rows.
- Prevent: Any queried lookup BO needs `DbSet` in `Visa2026EFCoreDbContext` and schema heal when ModuleInfo is current.
- Cross-skill: application-profile

### 2026-08-20 — Purpose is a 700-char case-summary text field

- Business-trip profiles need free-text **Purpose** (not the `BusinessTripPurpose` lookup). Added `RequirePurpose` / `DefaultPurpose` on profile and `Purpose` on instance (`MaxLength(700)`). Wizard Results row is **text** with textarea default. Case summary Edit uses textarea.
- Verify: stop F5, rebuild, F5. Iş Saparyna Gitmek shows Purpose in summary + wizard. Values up to 700 chars save.
- Prevent: Do not reuse `BusinessTripPurpose` lookup for this header field.
- Cross-skill: application-profile

### 2026-08-20 — Business trip address is a case-summary lookup

- `RequireBusinessTripAddress` only toggled the wizard row; case summary did not show it and Results had no catalog dropdown. Instance now has `BusinessTripAddress` FK (`BusinessTripAddresses` lookup, `FullAddress`). Wizard Results has Use + default lookup. Case summary Edit is a lookup. Host-start adds `DefaultBusinessTripAddressId` / `BusinessTripAddressId`.
- Verify: stop F5, rebuild, F5. Iş Saparyna Gitmek case summary shows **Business trip address**. Configure Results default list is `BusinessTripAddress` rows.
- Prevent: Do not treat this as free text. Do not hide it when Use is on.
- Cross-skill: application-profile

### 2026-08-20 — App_Business_Trip_Departure type-only seed

- Calik profile `App_Business_Trip_Departure` / `business_trip_departure` (type Code `business_trip` shared with arrival). Name **Iş Saparyna Gitmek**. Direct migration. **Business trip**. Audience **Employee** only. Migration **2** working days. Results: migration service; start/end dates; region + city (type `ShowToCity`); business trip address. No Urgency. Person: passport, address, visa, travel history (education/position off). Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows departure name. Related to Business trip. Results start/end + region/city + trip address. Person Travel history on.
- Prevent: Do not reuse `business_trip` as this profile Code. Do not produce or cancel documents.
- Cross-skill: application-profile

### 2026-08-20 — Remove App_Cancel_Visa_and_WP_Ext profile (for now)

- Dropped Calik seed `App_Cancel_Visa_and_WP_Ext` / `cancel_visa_wp_ext` / **Wiza we Iş Rugsatnamany Uzaltmak Barada Ýüztutmany Ýatyrmak**. Application Type row stays in the type catalog. Tenant catalog sync does not delete existing DB rows — if the profile already synced, delete it from Application Profile Templates (Linked 0).
- Verify: stop F5, rebuild, F5. New DBs have no this profile. Existing DB: Delete if still listed.
- Prevent: Do not prune Application Type `App_Cancel_Visa_and_WP_Ext`.
- Cross-skill: application-profile

### 2026-08-20 — App_Cancel_BZ display name

- Profile and type NameTm for `App_Cancel_BZ` is **Serhet Ýaka Rugsady Ýatyrmak** (was Serhet Ýaka Üçin Rugsatnamany Ýatyrmak). Also `ApplicationTypeLookupStrings.json` tk-TM.
- Verify: stop F5, rebuild, F5. Catalog shows the shorter name.
- Prevent: Do not keep the old Üçin Rugsatnamany wording on this type.
- Cross-skill: application-profile, visa2026-lookup-data

### 2026-08-20 — Remaining Cancellation type-only seeds

- Seven Calik cancellation profiles (Direct migration, Migration **3** working days from UP-TO-3-DAYS, no produce, no Urgency). Nested templates not seeded. Keep type name spelling **`App_Cancell_WP`**. `App_Cancel_Visa_and_WP_Ext` was seeded then **removed for now**.
  - `App_Cancel_BZ` / `cancel_borderzone` — Employee; cancel **Border zone**; person BZ item.
  - `App_Cancel_App` / `cancel_application` — all three audiences; cancel **Application(s)**.
  - `App_Cancel_Visa_and_WP_Ext` / `cancel_visa_wp_ext` — Employee; cancel **Application(s)** (extension request); person visa + WP.
  - `App_Cancel_Visa_Ext` / `cancel_visa_ext` — all three; cancel **Application(s)** (visa-extension request); person visa.
  - `App_Cancel_Visa` / `cancel_visa` — all three; cancel **Visas**; person visa.
  - `App_Cancel_Visa_and_WP` / `cancel_visa_wp` — Employee; cancel **Visas + Work permits**; person visa + WP + address.
  - `App_Cancell_WP` / `cancel_workpermit` — Employee; cancel **Work permits**; person WP item.
- Verify: stop F5, rebuild, F5. Catalog shows all seven names. Related to Cancellation. May cancel matches the list above.
- Prevent: Do not set CancelVisas on the *extension-application* cancel types. Do not reuse shared type Codes. Do not produce documents.
- Cross-skill: application-profile

### 2026-08-20 — App_Reg_Info_Change_Address type-only seed

- Sixteenth Calik profile `App_Reg_Info_Change_Address` / `reg_info_change_address` (type Code `check_in_info_change` shared). Name **Hasaba alyş maglumatyň üýtgemegi (Salgy Çalyşmagy)**. Direct migration. **Registration · Info change**. Audience all three. Migration **2** working days. Results: migration service; region/city off; no Urgency. Person: passport, education, position, address, visa, travel history. Nested templates not seeded. Last remaining `App_Reg_*` type is now seeded.
- Verify: stop F5, rebuild, F5. Catalog shows address info-change name. Related to Registration; **Info change**. Person Address of residence on. Overview has no Urgency.
- Prevent: Do not reuse `check_in_info_change`, `reg_info_change_passport`, or `reg_info_change_visa` as this Code.
- Cross-skill: application-profile

### 2026-08-20 — App_Reg_Info_Change_Visa type-only seed

- Fifteenth Calik profile `App_Reg_Info_Change_Visa` / `reg_info_change_visa` (type Code `check_in_info_change` shared). Name **Hasaba alyş maglumatyň üýtgemegi (Visa Çalyşmagy)**. Direct migration. **Registration · Info change**. Audience Employee + Family + Temporary visitor. Migration **2** working days. Results: migration service; region/city off; no Urgency. Person: passport, education, position, address, visa, travel history. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows visa info-change name. Related to Registration; **Info change**. Person Visa + Travel history on. Overview has no Urgency.
- Prevent: Do not reuse `check_in_info_change` or `reg_info_change_passport` as this Code. Do not set Check in/out/extension.
- Cross-skill: application-profile

### 2026-08-20 — Registration profiles never use Urgency

- Officer rule: **Related to = Registration** → **Urgency** off and no default. Overview was showing Normal priority because seeds kept `DefaultUrgencyLocalizationKey: NORM` while Use was false. Catalog apply, type mapper, wizard family switch, and Save now clear `RequireUrgency` / `DefaultUrgency`. Results hides the Urgency row for Registration.
- Verify: stop F5, rebuild, F5. Hasaba alyşy uzaltmak overview has no Urgency row. Configure → Results has no Urgency.
- Prevent: Do not seed NORM (or any default) on Registration. Do not show Urgency on Registration Results.
- Cross-skill: application-profile

### 2026-08-20 — App_Reg_ext includes Travel history

- `App_Reg_ext` person toggles now include **Travel history** (`RequirePersonTravelHistory`). Type `ShowBusinessTrips` is false, so catalog must set this flag explicitly.
- Verify: stop F5, rebuild, F5. Configure Hasaba alyşy uzaltmak → Templates & person → Travel history checked.
- Prevent: Do not map travel history only from `ShowBusinessTrips` for registration extension.
- Cross-skill: application-profile

### 2026-08-20 — App_Reg_ext type-only seed + Reg extension RegistrationKind

- `RegistrationKind.Extension` (enum 4). Wizard **Registration is** adds **Reg extension**. Infer `App_Reg_ext` / `Reg_ext` types. Fourteenth Calik profile `App_Reg_ext` / `reg_extension` (type Code `check_in_extention`). Name **Hasaba alyşy uzaltmak**. Direct migration. **Registration · Reg extension**. Audience all three. Migration **2** days. Results: migration service; region/city off. Person: passport, education, position, address, visa, **travel history**. Dashboard predicate `RegistrationExtensionProfilePredicate` ready; views not switched.
- Verify: stop F5, rebuild, F5. Catalog shows reg extension name. Registration is **Reg extension**. Review shows Registration · Reg extension.
- Prevent: Do not reuse `check_in_extention` as profile Code. Do not set Check in/out/info change.
- Cross-skill: application-profile, report-dashboard

### 2026-08-19 — Registration profiles always require Position

- Officer rule: **Related to = Registration** → **Required person-related data → Position** on. Calik seeds: Check-in from abroad and Check-out from abroad were off; now on. Catalog apply, type mapper, and wizard (when switching to Registration) force `RequirePersonPosition`.
- Verify: stop F5, rebuild, F5. Check-in from abroad and Check-out templates show Position checked.
- Prevent: Do not leave Position off on future `App_Reg_*` seeds (internal, info-change, extension, internal check-out).
- Cross-skill: application-profile

### 2026-08-19 — App_Reg_Check_Out type-only seed

- Twelfth Calik profile `App_Reg_Check_Out` / `check_out` (type Code `check_out` is shared with internal check-out). Name **Hasapdan Çykarmak (Daşary ýurda gitmegi)**. Direct migration. **Registration · Check out**. Audience Employee + Family + Temporary visitor. Migration **2** working days. Results: migration service; region/city off. Person: passport, education, **position**, address, visa, **travel history**. Nested templates not seeded. Skipped unseeded info-change visa/address and `App_Reg_ext` at officer request.
- Verify: stop F5, rebuild, F5. Catalog shows check-out-abroad name. Related to Registration; **Check out**. Person Travel history on.
- Prevent: Do not reuse `check_out` later for `App_Reg_Check_Out_Internal` — use a unique Code (e.g. `check_out_internal`). Do not set Check in. Do not produce documents. Keep dashboard `Code = 'check_out'` until views switch to `RegistrationKind`.
- Cross-skill: application-profile, report-dashboard

### 2026-08-20 — App_Reg_Check_Out_Internal type-only seed

- Thirteenth Calik profile `App_Reg_Check_Out_Internal` / `check_out_internal` (type Code `check_out` shared with abroad checkout). Name **Hasapdan Çykarmak (Başga welaýata gitmegi)**. Direct migration. **Registration · Check out**. Audience Employee + Family + Temporary visitor. Migration **2** working days. Results: migration service + **region/city** (type `ShowFromCity` / `ShowToCity`). Person: passport, education, position, address, visa, **travel history**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows internal check-out name. Registration is Check out; Results region/city on; person Travel history on.
- Prevent: Do not reuse `check_out` as this profile Code. Do not set Check in or Info change.
- Cross-skill: application-profile

### 2026-08-19 — Info-change passport person includes Travel history

- `App_Reg_Info_Change_Passport` **Required person-related data** now includes **Travel history** (`RequirePersonTravelHistory`). Wizard already had the checkbox; seed was off because type `ShowBusinessTrips` is false.
- Verify: stop F5, rebuild, F5. Configure passport info-change → Templates & person → Travel history checked.
- Prevent: Do not map travel history only from `ShowBusinessTrips` when officers require `TravelHistory` on registration info-change.
- Cross-skill: application-profile

### 2026-08-20 — App_Reg_Check_Out_Internal type-only seed

- Twelfth Calik profile `App_Reg_Check_Out_Internal` / `check_out_internal` (type Code `check_out` shared with abroad checkout). Name **Hasapdan Çykarmak (Başga welaýata gitmegi)**. Direct migration. **Registration · Check out**. Audience Employee + Family + Temporary visitor. Migration **2** working days. Results: migration service + **region/city** (from/to city on type). Person: passport, education, position, address, visa, **travel history**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows internal check-out name. Registration is Check out; Results region/city on; person Travel history on.
- Prevent: Do not reuse `check_out` as this profile Code. Do not set Check in or Info change.
- Cross-skill: application-profile

### 2026-08-19 — Registration Info change option

- `RegistrationKind.InfoChange` (enum 3). Wizard **Registration is** radios: Check in, Check out, **Info change**. Infer `App_Reg_Info_Change_*` before Check_In. `App_Reg_Info_Change_Passport` seed is InfoChange. Dashboard predicate `RegistrationInfoChangeProfilePredicate` ready; views not switched.
- Verify: stop F5, rebuild, F5. Passport info-change profile → Registration is **Info change**. Review shows Registration · Info change.
- Prevent: Do not infer Info_Change types as Check in. Do not leave info-change as None.
- Cross-skill: application-profile, report-dashboard

### 2026-08-19 — App_Reg_Info_Change_Passport type-only seed

- Eleventh Calik profile `App_Reg_Info_Change_Passport` / `reg_info_change_passport` (type Code `check_in_info_change` is shared with visa/address info-change). Name **Hasaba alyş maglumatyň üýtgemegi (Pasport Çalışmagy)**. Direct migration. **Registration · Info change**. Audience Employee + Family + Temporary visitor. Migration **2** working days. Results: migration service; region/city off. Person: passport, education, position, address, visa, **travel history**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows passport info-change name. Related to Registration; **Info change**. Person Passport + Travel history on.
- Prevent: Do not reuse `check_in_info_change` as this profile Code. Do not set CheckIn/CheckOut. Do not produce documents.
- Cross-skill: application-profile

### 2026-08-19 — Wizard Region and City are separate lookups

- Results & fields no longer has combined **Region (city)**. Separate **Region** (`RequireRegion` / `DefaultRegion`) and **City** (`RequireCity` / `DefaultCity`) map to `ApplicationProfileInstance.Region` and `.City`. City default list filters by selected region. `App_Reg_Check_In_Internal` seeds both Use flags. Host-start adds profile + instance columns; old `RequireRegionCity` copies into both flags when they are still false.
- Verify: stop F5, rebuild, F5. Internal check-in Results shows Region and City rows, both Use on. Case summary shows Region and City.
- Prevent: Do not keep a single Region (city) checkbox. Do not reuse FromCity/ToCity for this header pair. Load City.Region (or RegionID) when filtering City defaults — GetObjects without Include leaves Region null and the City list looks empty.
- Cross-skill: application-profile

### 2026-08-19 — App_Reg_Check_In_Internal type-only seed

- Tenth Calik profile `App_Reg_Check_In_Internal` / `check_in_internal` (type Code `check_in` shared). Name **Hasaba Almak (Welaýatdan gelmegi sebäpli)**. Direct migration. **Registration · Check in**. Audience Employee + Family + Temporary visitor. Migration **2** working days. Results: migration service + **region/city** (from/to city). Person: passport, education, **position**, address, visa, **travel history**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows internal check-in name. Registration is Check in; Results region/city on; person Travel history on.
- Prevent: Do not reuse `check_in` or `check_in_from_abroad` as this Code. Do not set Check out.
- Cross-skill: application-profile

### 2026-08-19 — Wizard Registration Check in / Check out

- `ApplicationProfile.RegistrationKind` (None / CheckIn / CheckOut). Wizard Identity shows **Registration is** radios when Related to = Registration. Leaving Registration clears the kind; entering Registration defaults Check in. `App_Reg_Check_In` seed is CheckIn. Dashboard: `RegistrationCheckInProfilePredicate` / `RegistrationCheckOutProfilePredicate` ready; views still use `ActionFamily` / `Code = check_out`.
- Verify: stop F5, rebuild, F5. Configure Hasaba Almak (from abroad) → Related to Registration → Check in selected. Review shows Registration · Check in.
- Prevent: Do not store Check in/out on Issuance/Cancellation. Do not rewire `vw_rd_to_be_checked_*` until officer confirms.
- Cross-skill: application-profile, report-dashboard

### 2026-08-19 — App_Reg_Check_In type-only seed

- Ninth Calik profile `App_Reg_Check_In` / `check_in_from_abroad` (type Code `check_in` is shared with internal check-in). Name **Hasaba Almak (Daşary ýurtdan gelmegi sebäpli)**. Route **Direct to migration service**. Related to **Registration**. Audience **Employee + Family member + Temporary visitor**. No produce/cancel. Migration SLA **2 working days** (DurationInDays). Results: **Migration service** on; visa type/period/project off. Person: passport, education, **address**, **visa**, **travel history**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows the check-in-from-abroad name. Related to Registration; Directed to Direct migration; person Visa + Address + Travel history.
- Prevent: Do not reuse `check_in` as this profile Code. Do not set Issuance. Do not skip Family/visitor (Category Both).
- Cross-skill: application-profile

### 2026-08-19 — App_Cancel_Inv_WP type-only seed

- Eighth Calik profile `App_Cancel_Inv_WP` / `cancel_invitation_wp` / Çakylyk we Iş Rugsatnamasyny Ýatyrmak. Route **Direct to migration service**. Migration SLA **3 working days**. Audience **Employee** only. Related to **Cancellation**. May cancel **Invitations + Work permits** (no produce). Person: passport, education, position, invitation item, **work permit item**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Çakylyk we Iş Rugsatnamasyny Ýatyrmak**. Cancellation; May cancel Invitation + WP; person WP item on.
- Prevent: Do not treat legacy flags as cancel-invitation-only (`ShowInvitationItemIsCancelled` is false on the type). Do not add Family/visitor audience. Do not use Via ministries.
- Cross-skill: application-profile

### 2026-08-19 — App_Cancel_Inv type-only seed

- Seventh Calik profile `App_Cancel_Inv` / `cancel_invitation` / Çakylygy Ýatyrmak. Route **Direct to migration service**. Migration SLA **3 working days** (UP-TO-3-DAYS). Audience **Employee + Family member + Temporary visitor**. Related to **Cancellation**. May cancel **Invitations** only (no produce). Visa/project/urgency off. Person: passport, education, **position**, **invitation item**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Çakylygy Ýatyrmak**. Directed to Direct migration; Related to Cancellation; May cancel Invitations; Migration 3 days.
- Prevent: Do not use Issuance / Produce Invitation. Do not cancel WP or visa on this type.
- Cross-skill: application-profile

### 2026-08-19 — App_Change_Inv type-only seed

- Sixth Calik profile `App_Change_Inv` / `change_invitation` / Çakylygy üýtgetmek. Route **Direct to migration service**. Migration SLA **2 working days**. Audience **Employee + Family member + Temporary visitor**. Related to **Issuance** (change, not cancel). May produce **Invitation** only. Visa type/period/category, project, urgency, and border zone **off**. Person: passport, education, **invitation item**. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Çakylygy üýtgetmek**. Directed to **Direct migration service**; Process & SLA **Migration 2 working days**.
- Prevent: Do not use Via ministries for this type. Do not use ActionFamily Cancellation. Do not produce visa/WP.
- Cross-skill: application-profile

### 2026-08-19 — App_Inv_According_to_WP type-only seed

- Fifth Calik profile `App_Inv_According_to_WP` / `get_invitation_according_to_wp`. Audience **Employee**. Invitation only (existing WP, not a new WP). Default visa **WP**, period **PerWorkPermit**. Person includes **work permit item**, plus passport/education/position/address/salary/medical.
- Verify: stop F5, rebuild, F5. Catalog shows **İş Rugsatnama görä Çakylyk Almak**. Results WP / per work permit; person Work permit item checked.
- Prevent: Do not produce a new work permit on this type. Do not reuse `get_invitation` as Code.
- Cross-skill: application-profile

### 2026-08-19 — App_Sevice_Passport type-only seed

- Skipped `App_Inv_According_to_WP` for now. Fourth Calik profile `App_Sevice_Passport` / `get_invitation_service_passport` (keep type name spelling). Audience **Employee**. Invitation only. Default visa type **OF** (OF-Gulluk). Default period **Day10**. Person: passport, education, address; position, salary, medical off. Nested templates not seeded.
- Verify: stop F5, rebuild, F5. Catalog shows **Gulluk Pasporty Üçin Çakylyk Almak**. Identity Employee; Results visa OF / 10 days.
- Prevent: Do not reuse `get_invitation` as this profile Code. Do not rename the ApplicationType to fix Sevice.
- Cross-skill: application-profile

### 2026-08-19 — App_Inv default visa period 30 days

- Default visa period **Day30** (30 gün).
- Verify: stop F5, rebuild, F5. Configure Çakylyk Almak → Results period 30 days.
- Prevent: Do not use Month6 on App_Inv.
- Cross-skill: application-profile

### 2026-08-19 — App_Inv default visa type BS1

- Default visa type **BS1** (BS1-İşerwürlik).
- Verify: stop F5, rebuild, F5. Configure Çakylyk Almak → Results default visa type BS-1.
- Prevent: Do not use WP or FM on App_Inv.
- Cross-skill: application-profile

### 2026-08-19 — App_Inv audience is employee + temporary visitor

- `App_Inv` / `get_invitation` / Çakylyk Almak: **Employee** and **Temporary visitor**, not family. May produce **Invitation** only (no work permit). Same SLA 4/30. No default visa type (not WP; visitor vs employee types differ). Person packs like Inv+WP minus WP location (passport, education, position, address, salary, medical).
- Verify: stop F5, rebuild, F5. Catalog shows Çakylyk Almak. Identity: Employee + Temporary visitor; Family off. May produce Invitation only.
- Prevent: Do not treat catalog Category Both as Family. Do not produce WP on App_Inv.
- Cross-skill: application-profile

### 2026-08-19 — App_Inv_FM visa type FM and person packs

- Default visa type **FM** (FM-Maşgala). Person education, position, and salary **off**.
- Verify: stop F5, rebuild, F5. Configure Çakylyk Almak FM → Results default visa type Family; person checkboxes Education/Position/Salary unchecked.
- Prevent: Do not default FM invitation to WP.
- Cross-skill: application-profile

### 2026-08-19 — App_Inv_FM type-only seed

- Second Calik profile `App_Inv_FM` / `get_invitation_fm` (type Code `get_invitation` is shared with App_Inv — profile Code is unique). Audience **Family member only**. May produce **Invitation** only. SLA 4 / 30. Visa type required with no default (not WP). Person: passport, education, address, medical; position and salary off.
- Nested templates not seeded for this type (Borcnama is still Shared/global; Sahsy kagyz / Contract Inv stay App_Inv_And_WP).
- Verify: stop F5, rebuild, F5. Catalog shows **Çakylyk Almak FM**. Configure → Identity audience Family member; Templates & person Shared includes are empty until officer Include.
- Prevent: Do not reuse `get_invitation` as ApplicationProfile.Code. Do not copy Inv_And_WP WP produce/salary/position onto FM.
- Cross-skill: application-profile

### 2026-08-19 — App_Inv_And_WP default Shared includes

- Tenant nested JSON now seeds **Borcnama**, **Contract Inv**, and **Sahsy kagyz** on `App_Inv_And_WP` (`SignOff: approved`). Nested rows use Shared catalog scope (Global / Category), not Profile-specific, so Templates & person shows them as included in Shared.
- Verify: stop F5, rebuild, F5. Configure App_Inv_And_WP → Templates & person → those three are **included**. Other Shared rows stay not included unless the officer already saved extras.
- Prevent: Nested-template tenant rows need `SignOff: approved` or startup sync skips them. Do not seed as CatalogScope ProfileSpecific.
- Cross-skill: application-profile | visa2026-user-report-templates

### 2026-08-19 — Wizard Preview links did not open the side panel

- Preview on Templates & person used C# `OpenFileAsync` from the wizard editor. The File occupant is owned by `VisaPreviewSlotHost` (separate root in `_Host.cshtml`). Result: Preview links rendered, slot stayed closed. Preview now calls `visaPreviewDrawer.open` like other file links.
- Verify: stop F5, rebuild, F5, **Ctrl+F5**. Click Preview on Borcnama — right panel PDF, not only the wizard.
- Prevent: Do not `@inject IVisaPreviewSlotService` for File preview from the profile wizard.
- Cross-skill: application-profile | visa2026-preview-slot

### 2026-08-19 — Wizard template Preview is the stored master in the side panel

- Preview on Profile-specific and Shared rows (and Edit → Preview actual layout) opens `#visa-preview-slot` File occupant. Word/Excel is converted to PDF so officers see page layout with placeholders. This is not Resminamalar merge (no application instance in Configure). Unsaved uploads use the wizard ObjectSpace. Preview stays available when the profile is config-locked.
- Verify: stop F5, rebuild, F5. Templates & person → Preview Borcnama; slot PDF; leave Configure → slot closes.
- Prevent: Do not invent a new slot mode. Do not merge against a live application from the wizard.
- Cross-skill: application-profile | visa2026-preview-slot | visa2026-resminamalar

### 2026-08-19 — Shared catalog search + inner scroll

- Shared catalog has a search box (name, Word/Excel, People/header, included). The list scrolls inside the pane after **8** visible rows so person checkboxes stay reachable. Count shows filtered / total.
- Verify: stop F5, rebuild, F5. Templates & person → type Borcnama in Shared search; list shrinks. With many rows, scroll inside Shared, not the whole wizard. `MatchesSharedSearch_FiltersByNameKindAndData`.
- Prevent: Do not grow Shared to full page height. Do not add a second Suggested filter.
- Cross-skill: application-profile

### 2026-08-19 — GT-15 letters stay off Shared catalog

- Names containing **GT-15** are contract-bound letters. Shared Include list skips them; officers upload via **+ Add template** on Profile-specific (and pick the Project contract). Already-included GT-15 nested rows still show in the Profile-specific list so they can be edited or removed. Seeded `UserReportTemplate` rows are not deleted.
- Verify: stop F5, rebuild, F5. Shared catalog has no `GT-15_*`. Add those files under Profile-specific. `MergeShared_OmitsGt15ContractLetters`.
- Prevent: Do not put GT-15 masters back on Shared Include. Do not deactivate the seed files unless asked — hide from this catalog only.
- Cross-skill: application-profile | visa2026-user-report-templates

### 2026-08-19 — Officer template scopes are Profile-specific and Shared only

- Templates & person shows two lists: **Profile-specific** (this profile, contract/migration filter) and **Shared** (Include/Exclude). No Category/Global labels, no suggestion chips, no family tags. Add/Edit Scope pills are the same two values. Shared Add writes Global (no type links). Promoting a profile-only file to Shared is Global + confirm; an already-typed master stays typed internally. Overview scope chip also says Shared.
- Verify: stop F5, rebuild, F5. Step 5 has two sections. Add template: Profile-specific | Shared. Shared list shows every master with Include. `ApplicationProfileWizardTemplateScopeHelperTests` + overview scope "Shared".
- Prevent: Do not put Category/Global back on the officer pills. Do not reintroduce Suggested for this profile.
- Cross-skill: application-profile | visa2026-resminamalar | visa2026-user-report-templates

### 2026-08-19 — Shared catalog replaces Category + Global lists

- Wizard Templates & person now has one **Shared catalog** (typed User Report Templates + global masters). Include/Exclude attaches to this profile. Family tags sit on the **right** of each row (all families, not first two). Data scope is muted under the name. **Suggested for this profile** uses May produce + Registration related-to; Global (no type links) always appears; already-included rows stay visible. **All shared** shows the rest. Category/Global remain Edit Scope on the master, not a second attach list.
- Verify: stop F5, rebuild, F5. App_Inv_And_WP step 5: Borcnama + Invitation/WP-tagged files in Suggested; Registration-only hidden until All shared. Include still attaches. `ApplicationProfileWizardTemplateCatalogTests`.
- Prevent: Do not bring back Issuance & process family chips as the attach UI. Do not treat family tags as Include.
- Cross-skill: application-profile | visa2026-resminamalar | visa2026-user-report-templates

### 2026-08-19 — Edit template dialog can change Scope

- Scope in **Edit Word/Excel template** uses the same pills as Add: Profile-specific, Category, Global. **Global/Category → this profile** copies the file onto a renamed nested row; the shared User Report Template is unchanged. **Promote to Global or Category** requires **Confirm scope change** then Save — Category writes shared type links. Profile-specific shows the contract / migration applicability dropdown.
- Verify: stop F5, rebuild, F5. Include **Borcnama** → Edit → Profile-specific → Save metadata. Nested copy named `Borcnama (…profile…)`. Global Borcnama still in catalog, not included. Promote needs Confirm. `ApplicationProfileWizardTemplateScopeHelperTests` (4 passed).
- Prevent: Do not rewrite Borcnama’s master type/group links when making a profile-only copy. Do not apply Global/Category visibility without confirm.
- Cross-skill: application-profile | visa2026-resminamalar | visa2026-user-report-templates

### 2026-08-18 — App_Inv_And_WP Results defaults (officer)

- Tenant JSON now sets default lookups: visa type **WP**, category **Multiple**, period **Month6**, urgency **NORM**. Seed sync resolves LocalizationKey onto `DefaultVisaType` / Category / Period / Urgency. Border zone, Project, work-permit location stay Use-on with no default.
- Verify: stop F5, rebuild, F5. Configure profile → Results & fields shows those four Has default values (WP — Work visa, Multiple entry, 6 months, Normal priority).
- Prevent: Do not store GUIDs in tenant JSON; use LocalizationKey. Empty key clears the default on next F5.
- Cross-skill: application-profile | visa2026-lookup-data

### 2026-08-18 — App_Inv_And_WP type-only seed flags (officer)

- One Calik profile `App_Inv_And_WP` / `get_invitation_wp`. Audience **Employee only**. May produce **Invitation + Work permit** only. SLA ministry **4** / migration **30**. No Description, SelectionCode, or MigrationSlaProfileCode in JSON. Person border-zone item and rejection item **off**.
- Verify: stop F5, rebuild, F5. Overview/wizard matches those flags; Description and selection code cleared.
- Prevent: Do not copy Wave 0b Both/visa/border/rejection produce flags onto this type-only template.
- Cross-skill: application-profile

### 2026-08-18 — Tenant profile JSON did not seed on F5 (ModuleUpdater skipped)

- Symptom: Application Profiles catalog showed **No profiles match** after adding one `App_Inv_And_WP` row to `application-profile.calik-energi.json`. Local `"ApplicationProfiles"` count was 0.
- Cause: `ApplicationProfileSeedGate` (every F5) called `ApplicationProfileSeedSync`, which returned immediately when tenant JSON was present so type-derived rows were not invented. Tenant catalog upsert lived only in `ApplicationProfileTenantCatalogSeedUpdater`, which XAF skips when the module version is already current.
- Fix: when tenant JSON is present, `ApplicationProfileSeedSync` upserts tenant catalog rows (and nested templates) and commits. Empty `Rows` still creates nothing.
- Verify: stop F5, rebuild, F5. Catalog should show **Çakylyk we Iş Rugsatnamasyny Almak** (`get_invitation_wp`). Log: `tenantCatalog=True`, `created=1`.
- Prevent: Do not rely on ModuleUpdater / `FORCE_XAF_DB_UPDATE` for tenant application-profile JSON changes. Do not restore the 176 Wave 0b rows.
- Cross-skill: application-profile | visa2026-lookup-data

### 2026-08-18 — Tenant catalog JSON emptied (no 176 auto-seeded profiles)

- Local `visa2026` has 0 Application Profiles / instances / nested templates. Tenant JSON `application-profile.calik-energi.json` and `application-profile-nested-templates.calik-energi.json` now have empty `Rows`. `ApplicationProfileSeedSync` skips type-derived profiles when tenant JSON is present, so F5 will not recreate 21 type-only rows.
- Verify: stop F5, rebuild, F5. Application Profiles catalog is empty.
- Prevent: Do not put the 176 Wave 0b rows back into tenant JSON for this Templates & person review. Add one profile at a time.
- Cross-skill: application-profile | visa2026-lookup-data

### 2026-08-18 — Locked profile still edits approval-leg versions

- Config lock A still freezes Name, Directed to, May produce, templates, person toggles, and SLA. **Approval leg versions** stay editable (Add, Duplicate, rename, ministries, Default) because instances snapshot ministries at create. Cannot remove the last version while locked. Started cases are not restamped.
- Slice: 8m **Done**. Plan §2.6 exception.
- Verify: `ApplicationProfileLockHelperTests` (13 passed). Blazor Server build. Stop F5, rebuild, F5. Open a **Config locked** Via ministry profile → Identity → add/edit a version → Review → **Save profile**. Name / May produce stay disabled. In-process case ministries unchanged.
- Prevent: Do not unlock the whole wizard. Do not restamp existing instance snapshots when a version changes. Do not allow deleting the last version on a locked profile.
- Cross-skill: application-profile

### 2026-08-18 — Case summary: edit instance Use fields

- Overview tiles show profile **Use** fields; **Edit** on the Case summary title opens the 3-column form; **Done** returns to tiles. Changes persist on the instance (`TryApply` + commit) and do **not** edit the Application Profile template. Later profile default changes still do not overwrite saved instance values. **Project** is editable here when `RequireProject` is on (accepted prototype; do not re-lock via `IsProjectContractLocked`). New instance column `EntryCheckPointID`. Officer shell must wire `HeaderFieldChanged` — the case tab UI is a no-op without it.
- Slice: 10x **Done**
- Verify: `dotnet test` filter `ApplicationWorkspaceCaseHeaderFieldsHelperTests` + `ShowEntryCheckPoint_UsesProfileRequireFlag` (4 passed). Blazor Server build. Stop F5, rebuild, F5. Open a case → Case summary tiles → **Edit** → change a Use field → **Done** → tiles show the new value. Template unchanged.
- Prevent: Do not use a page-level Overview/Edit toggle. Do not live-follow later profile default changes onto existing cases. Do not skip `OfficerShellPropertyEditor.SaveHeaderFieldAsync`.
- Cross-skill: application-profile

### 2026-08-18 — Case summary instance fields (prototype)

- Officers must change per-instance values copied from Application Profile **Use** fields. Default view is **overview tiles**; **Edit** on the card opens the form; **Done** returns to tiles. Same field set in both modes. Does not edit the template. Slice **10x Pending**.
- Slice: 10x **Pending** — mockups only. Do not implement until both images are accepted.
- Verify: `docs/prototypes/application-profile-instance-case-summary-overview-properties-prototype.png` and `application-profile-instance-case-summary-edit-properties-prototype.png`. Inventory in plan §9.
- Prevent: Do not use a page-level Overview/Edit toggle. Do not live-follow later profile default changes onto existing cases.
- Cross-skill: application-profile

### 2026-08-18 — Save profile: Column 'GCRecord' is null

- Host-start created `ApplicationProfileApprovalLegVersions` with nullable `GCRecord` / `OptimisticLockField` (no default). Sibling profile tables use `NOT NULL DEFAULT 0`. XAF omits `GCRecord` on insert; Postgres stored NULL; RETURNING then threw **Column 'GCRecord' is null.** Heal: `NOT NULL DEFAULT 0` plus always-run ALTER. Backfill now uses `COALESCE(GCRecord,0)=0` (live rows are `0`, not NULL).
- Slice: 8l bugfix
- Verify: stop F5, rebuild, F5. Configure profile → Review & save → **Save profile**. Local `visa2026` already healed; host-start SQL covers other DBs.
- Prevent: Do not create new XAF tables with `"GCRecord" integer NULL`. Match Application Profiles: `"GCRecord" integer NOT NULL DEFAULT 0`.
- Cross-skill: application-profile

### 2026-08-18 — Approval leg versions: per-profile copies + instance snapshot

- One profile holds named approval-leg versions (own copies). Officers must pick a version at create. Ministries are copied onto `ApplicationProfileInstanceApprovalLegSnapshot`; later wizard edits do not change already-started cases. Existing profile legs backfill into Default **Version 1**. Progress timeline reads snapshots first.
- Slice: 8l **Done**. Plan §2.1 #7 locked. Host-start SQL in `ApplicationProfileSchemaSql`.
- Verify: unit tests `ApplicationProfileApprovalLegVersionHelperTests` + timeline snapshot preference. Stop F5, rebuild, F5. Configure profile → Identity versions. New application → pick a version. Edit ministries on the profile — in-process case keeps the snapshot.
- Prevent: Do not share one version BO across profiles. Do not live-follow later ministry edits on started instances. Do not drive legs from `ProjectContract` for this slice.
- Cross-skill: application-profile | application-progress

### 2026-08-18 — Approval leg versions: per-profile copies + instance snapshot (prototype)

- Officers do not want one profile per Project contract. One profile (e.g. Visa extension) holds **named approval-leg versions**; the officer **must pick a version** at instance create. **Reuse:** each profile keeps **its own copy** of a version (not a shared catalog; not `ProjectContract` / `ApprovalLegProfile`). **After create:** instances **snapshot** ministries; later wizard edits do not change already-started cases.
- Slice: 8l **Pending** — mockups only. Plan §2.1 #7 remains **proposed** until the images are accepted. Do not implement BOs/wizard/picker yet. After implement, create must copy the chosen version onto `ApplicationProfileInstanceApprovalLegSnapshot`; in-process timeline must read snapshots, not live profile versions.
- Verify: mockups in `docs/prototypes/application-profile-wizard-approval-leg-versions-prototype.png` and `application-profile-instance-create-choose-approval-legs-prototype.png`. Inventory in plan §9.
- Prevent: Do not share one version BO across profiles. Do not live-follow later ministry edits on started instances. Do not drive legs from `ProjectContract` for this slice.
- Cross-skill: application-profile | application-progress

### 2026-08-18 — Profile-specific templates bind to contract or migration service

- Profile-specific wizard rows have a dropdown: Project contract when Directed to is Via ministry, Migration service when Direct migration. Empty = visible on every instance. Resminamalar nested catalog filters by the instance lookup. Project is back on Results & fields (not Identity).
- Slice: 8k
- Verify: unit tests `ApplicationProfileNestedTemplateCatalogHelperTests`. Stop F5, rebuild, F5. Configure profile → Results has Project; Identity has no Project lookup; Templates profile-specific row shows the dropdown. Open an instance Resminamalar catalog — only matching (or unscoped) profile-specific files appear.
- Prevent: Do not put Project back on Identity. Do not apply this filter to Category/Global includes.
- Cross-skill: application-profile | visa2026-resminamalar

### 2026-08-18 — Process & SLA is duration only

- Removed ministry/migration Include and SLA-track tables from the wizard and profile overview. Instance steps follow Directed to + Approval legs + the fixed progress graph. SLA days remain live on the profile. Advance no longer filters by `ProgressStateSettings.IsIncluded`.
- Slice: 8j
- Verify: stop F5, rebuild, F5. Configure profile → Process & SLA shows Ministry/Migration days only. Overview Process card has no state table.
- Prevent: Do not put those checklists back as a process designer. Do not wire `ApplicationProfileProgressStateSetting` into the transition helper.
- Cross-skill: application-profile | application-progress

### 2026-08-18 — Identity wizard hides Description, Code, SelectionCode

- Identity & purpose only edits the profile **Name**. Description, Code, and Selection/quick code remain on `ApplicationProfile` (Code is still auto-assigned `NEW-…` at create) and still show on overview/review, but not on the wizard Identity form.
- Verify: stop F5, rebuild, F5. Configure profile → Identity shows Name, Directed to, Project contract, legs — no Description/Code/quick code fields.
- Prevent: Do not add those three fields back onto Identity for “completeness”; they add noise. Catalog uniqueness still uses Code.
- Cross-skill: application-profile

### 2026-08-18 — Project contract lives on Identity (Via ministry)

- Project contract is profile configuration, not a Results instance field. Wizard Identity shows it only when Directed to = Via ministry; Direct migration hides and clears it. Save requires a contract for Via ministry. Instances still copy the default at create and cannot edit it.
- Slice: 8i
- Verify: unit tests `ApplicationProfileWizardPersistHelperTests` + `ApplicationProfileOverviewQueryServiceTests`. Stop F5, rebuild, F5. Configure profile → Identity Via ministry shows Project contract; Results & fields has no Project row; Save without a contract fails; Direct migration hides the lookup.
- Prevent: Do not put Project back on Results Use/default table. Do not let officers change `ApplicationProfileInstance.ProjectContract`.
- Cross-skill: application-profile

### 2026-08-18 — Submitted on Office bar after later Approvals

- Overview stepper Office node now shows Submitted + date for the life of that row; ministries still show their latest result.
- Verify: stop F5, rebuild, F5. **8/-010** Overview — Office preparation Submitted; Türkmenenergo Approved.
- Prevent: Do not rely on Activity alone for Submitted.
- Cross-skill: visa2026-application-progress

### 2026-08-18 — Submitted remains in History after later steps

- Overview Activity and Progress History list every row with catalog names. Submitted keeps its date after Türkmenenergo is Approved.
- Verify: stop F5, rebuild, F5. Submit then Approve — History shows Submitted and Approved.
- Prevent: Do not cap History at 3 rows. Do not use “Sent for agreement” for Submitted.
- Cross-skill: visa2026-application-progress

### 2026-08-18 — Office Result includes Submitted

- Office Result is now Submitted (default) + Cancelled. Advance records Submitted on the first ministry with the Date field. Office done is de-emphasized.
- Verify: stop F5, rebuild, F5. **8/-010** Office Result Submitted → Advance — Türkmenenergo current · Submitted.
- Prevent: Do not hide Submitted behind a blank Result on Office.
- Cross-skill: visa2026-application-progress

### 2026-08-18 — Office Cancelled officer-verified

- Officer confirmed Office Result Cancelled → Advance shows Cancelled on the Office node.
- Cross-skill: visa2026-application-progress

### 2026-08-18 — Office Cancelled is shown on Office after Advance

- History already stored Cancelled; Office badge/header stayed on Office preparation. Timeline now overlays Cancelled on the Office node (same pattern as ministries).
- Verify: stop F5, rebuild, F5. **8/-010** Office Result Cancelled → Advance — Office shows Cancelled; ministries Pending; header Cancelled.
- Prevent: Do not treat empty previous history as “no cancelled overlay” on implied office.
- Cross-skill: visa2026-application-progress

### 2026-08-18 — Cancelled Result belongs to this progress step

- Selecting Cancelled no longer leaves Submitted / jumps to Migration. Badge follows Cancelled; Advance stores it on the ministry that was current.
- Verify: stop F5, rebuild, F5. **8/-006** Tarkusenergo Result Cancelled — badge Cancelled; Advance keeps it on that node.
- Prevent: Do not treat `PROCESS_CANCELLED` as a Migration-only slot.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — Progress Result includes Cancelled last

- Workspace Result on the current step now ends with **Cancelled**. Default remains Approved (Office Advance still starts the first ministry).
- Verify: stop F5, rebuild, F5. **B/-008** Energetika Result — Approved, Unapproved, Cancelled.
- Prevent: Do not auto-select Cancelled as the default Result.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — Letter upload only on the current Progress edit form

- Done ministries showed a dashed upload on every Approved node. Upload is only on the current ministry Result form; completed steps keep View letter.
- Verify: stop F5, rebuild, F5. **8/-005** Progress — no upload boxes on Approved ministries.
- Prevent: Do not show `InputFile` on `slotState != current`.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — Approval letter upload on current ministry Result

- After Result belonged to the current ministry, that node often has no decision row yet so letter upload hid. Upload sits next to Result; the file is stored on Advance onto that ministry’s new decision row. Completed ministries can still add a letter on their decision row.
- Verify: `Build_FirstLegApproved_NextMinistryIsCurrent`. Stop F5, rebuild, F5. On **8/-010** Energetika — upload a letter, Advance Approved; letter sits on Energetika.
- Prevent: Do not require a saved decision row before showing upload. Do not attach a pending Energetika letter to the previous ministry’s latest row.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — Progress Result is this step, not the next one

- Workspace Progress **Next step** showed the following node’s state (Office → Submitted). Relabeled **Result**; after Approve the next ministry becomes current so Approved/Disapproved belong to that ministry.
- Verify: stop F5, rebuild, F5. **8/-010** Office — Date + Advance only; first ministry becomes Submitted. Energetika current — Result Approved/Disapproved is Energetika.
- Prevent: Do not put Türkmenenergo Submitted on the Office form.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — Current-step badge follows Next step

- Progress tab badge/header stayed on the recorded result (Approved) after the officer picked Disapproved in Next step. Same-slot preview now updates the current node before Advance.
- Verify: stop F5, rebuild, F5. On **8/-010** Progress — Energetika Next step Disapproved updates the badge immediately.
- Prevent: Do not bind the current-step badge only to `CurrentStateLabel` when a same-slot Next step is selected.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — Revert to here is revealed after Revert progress

- Workspace Progress showed Revert to here on every completed node. Officers only need that jump after they have already started correcting. **Revert progress** stays; **Revert to here** appears after a successful revert on this case and hides again after Advance or leaving the case.
- Verify: stop F5, rebuild, F5. On **8/-006** Progress — Revert progress only; revert once → Revert to here; Advance → hidden.
- Prevent: Do not treat the first Revert progress click as reveal-only.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — Workspace Advance records officer-entered step date

- Each Advance now takes a Date (default today) onto the new progress row. Overview rail Advance switches to the Progress tab so the officer can set it. Revert unchanged.
- Verify: stop F5, rebuild, F5. On **8/-009** Progress — pick a date, Advance; the new node shows that date.
- Prevent: Do not hard-code `DateTime.Today` in workspace Advance without using the request date.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — Workspace Progress revert walks back to implied office

- Advance is append-only; workspace had no backward path so a wrong step or letter stuck. Progress tab now has **Revert progress** (delete latest row) and **Revert to here** on completed slots, including Office preparation (clears all history). Flexible for now: terminal states can revert; People lock unlocks when Issued/Rejected/Cancelled is no longer latest.
- Verify: `ApplicationProgressRevertHelperTests`. Stop F5, rebuild, F5. On **8/-009** Progress — revert one step, then Revert to here on Office preparation.
- Prevent: Do not write `IS_BEING_PREPARED` rows to represent office after revert. Empty `ProgressHistory` remains implied office.
- Cross-skill: visa2026-application-progress

### 2026-08-17 — People grid Unlink is per person, next to Relink

- Toolbar **Unlink** opened a person picker. Officers need to unlink the row they are looking at. Each People-on-this-case row now has **Relink | Unlink | Open person detail**. Row Unlink calls `UnlinkPerson` for that `PersonId` and reloads. Disabled when `ResolvedLinksLocked`.
- Verify: stop F5, rebuild, F5. On **8/-009** with two people — Unlink on Karen removes only Karen; Gabriel stays. Issued cases keep Unlink disabled. Toolbar has **Link existing…** only.
- Prevent: Do not put Unlink only in the toolbar. XAF `ShowUnlinkPersonPicker` remains for the native workspace action.
- Cross-skill: —

### 2026-08-15 — People grid Relink syncs missing person records

- Officers add Salary/Visa/Medical on **Open person detail**. Those rows stay off the case until Relink. The People table now has a **Relink** column that calls `ApplicationProfileInstancePersonService.RelinkPerson` (`RefreshResolvedLinks` + `EnsureResolvedLink` for current candidates). Sticky `LinkedObjectId` is not replaced. Disabled when `ResolvedLinksLocked`.
- Verify: `RelinkPerson_NoOpWhenWorkflowTerminal`. Stop F5, rebuild, F5. On **8/-009** add a salary on the person, return to People & links, click **Relink** — Salary tile becomes 1. Visa stays 0 if no started/valid visa exists. Issued cases keep Relink disabled.
- Prevent: Do not add in-tab **New {type}** again. Relink is the sync action; Person detail is the create surface.
- Cross-skill: —

### 2026-08-15 — People & links does not add person-owned records

- In-tab **New salary / New address / …** made the People panel a second create surface and still needed a post-save relink. Officers add missing person data from **Open person detail** instead.
- Removed `ApplicationWorkspaceLinkedRecordOpenHelper` and the New button. Invitation / WP / border zone / rejection still hint Overview → Issued records. Process-complete lock hint stays.
- Verify: stop F5, rebuild, F5. On **8/-009** People & links → Address / Salary / Medical — no **New** button. **Open person detail** remains on the person row and panel header.
- Prevent: Do not add a second create button on People & links until create+relink is one explicit officer action.
- Cross-skill: —

### 2026-08-15 — New People & links BO must pin ResolvedLink by created ID

- `RefreshResolvedLinks` after **New salary / New medical / …** often left the tile at 0. Resolve uses `PersonCurrentItems` / valid-item pickers on a new ObjectSpace, so the just-saved row is frequently not the candidate. `CollectMissingAutoLinks` also skips a kind if any ResolvedLink row already exists (even with empty `LinkedObjectId`).
- Post-save now calls `EnsureResolvedLink` with the created `BaseObject.ID`. Creates a link when none; fills an empty `LinkedObjectId`; does **not** replace a sticky id (second Address stays off the case). `detailView.Closing` is a backup if Blazor modal `Committed` does not fire.
- Verify: `DecideEnsureResolvedLink_*` tests. Stop F5, rebuild, F5. On **8/-009** (unlocked) People & links → Salary 0 → **New salary** → save → tile becomes 1 and the row appears. Same for Medical. New address on an already-linked Address must not replace the sticky address.
- Prevent: Do not rely on `RefreshResolvedLinks` to attach a just-created person-owned BO. Always pass the created object id.
- Cross-skill: —

### 2026-08-15 — People & links can add missing person-owned records

- Empty Salary/Medical (and other person-owned kinds) had no Add. Issued items (invitation/WP/border zone/rejection) still need a header from Overview.
- `ApplicationWorkspaceLinkedRecordOpenHelper` opens a modal DetailView with Person (or Visa.Passport) set, then `RefreshResolvedLinks` after save. Hidden when `ResolvedLinksLocked`.
- Verify: stop F5, rebuild, F5. On an **in-process** (not issued) case → People & links → Salary 0 → **New salary** → save → tile count becomes 1. On 8/-008 (locked) the button stays hidden.
- Prevent: Do not create InvitationItem/WorkPermitItem from this panel without a parent header. Do not bypass the process-complete lock.
- Cross-skill: —

### 2026-08-15 — Overview linked-record tiles include empty required types

- Overview hid Salary/Medical (and any other configured kind) when `CountResolved` was 0. People & links already listed every profile-enabled type, including 0.
- `BuildLinkedTiles` now emits one tile per `IsConfigured` kind. Empty tiles use `is-empty` (dashed) and still open People & links for that type.
- Verify: stop F5, rebuild, F5. Open 8/-008 Overview — Salary and Medical show as 0 next to Passport/Education/…. Click Medical → People tab with Medical selected.
- Prevent: Do not filter Overview tiles with `count == 0`. Profile `RequirePerson*` / Show* is the visibility gate; zero means “required but not linked yet.”
- Cross-skill: —

### 2026-08-15 — People & links tiles match Overview linked-record cards

- People tab used a different `ct-rec` grid (icon / label / count / Valid—) and tiles were not clickable. Officers could not see the actual passport/visa/… rows.
- Tiles now use the same `cw-link-tile` cards as Overview (icon, label, count, chevron). Click opens that person's records from the workspace tab rows. Overview click still jumps to People and opens that type.
- Verify: stop F5, rebuild, F5. Open 8/-008 People & links — cards match Overview. Click Passport under a person → table shows that person's passport. Overview Passport tile → People with Passport selected.
- Prevent: Do not keep a second tile chrome on People. Display records in-tab; do not open a second catalog in `#visa-preview-slot`.
- Cross-skill: visa2026-preview-slot

### 2026-08-15 — Officer validity gate does not apply to VISA2014 import

- §10.2 valid/not-expired auto-link is **officer-only** (`EnforceOfficerLinkValidity`). `MigrationImportContext.IsDataImport` (headless `--inprocess` / `X-Visa2014-DataImport`) uses `PersonCurrentItems` so expired historical passport/visa/WP/invitation/border-zone/medical still link on Wave 2b `LinkPerson`.
- Verify: `CollectMissingAutoLinks_AllowsExpiredPassportDuringDataImport` + `ResolvePassport_UsesCurrentIncludingExpiredDuringDataImport`. Officer tests still reject expired candidates outside an import scope.
- Prevent: Do not apply `CanLink*` during import. Do not use the officer gate as a reason to skip past related data in `Visa2014ApplicationProfileInstancePersonImporter`.
- Cross-skill: visa2014-to-visa2026-import

### 2026-08-15 — Only valid not-expired records auto-link on Person add

- Linking a Person auto-resolves Passport, Visa, WorkPermitItem, InvitationItem, BorderZoneItem, and MedicalRecord only when the row is valid and not expired (`ApplicationProfileInstancePersonValidItems`). Visa also requires started + not cancelled/changed; invitation requires not cancelled/changed/used and a live parent; work permit / border zone require not cancelled (border zone parent not expired).
- Resolve picks the latest **linkable** row, not PersonCurrentItems “current then null”. Existing sticky `LinkedObjectId` is not replaced. `PersonCurrentItems` is unchanged (reports/UI current record).
- **Import exception:** VISA2014 `IsDataImport` skips this gate (see entry above).
- Verify: `ApplicationProfileInstancePersonValidItemsTests` + resolver tests. Link a person who has only an expired passport — no Passport ResolvedLink is created. A person with an expired current passport and an older valid one links the older valid passport.
- Prevent: Do not use `PersonCurrentItems.GetCurrent*` as the Application Profile **officer** link gate.

### 2026-08-15 — Document copies follow People & links records

- Workspace Document copies no longer uses ApplicationItem Current/Previous/Next slots. Each row is a sticky ResolvedLink, labeled by passport/visa/work-permit/invitation number (same records as People & links).
- Verify: stop F5, rebuild, F5. Open 8/-007 — Andy’s copies should list Passport X1453316 and Visa A3303830, not “Current passport”.
- Cross-skill: visa2026-document-copies

### 2026-08-15 — Header people chips filter Document copies

- Case workspace header chips (Andy / Katie) are toggles for the Document copies roster. Default all selected. Catalog is person-grouped; Preview stays `#visa-preview-slot` viewer-only (`OpenPreviewOnly`) with the clicked person or the current filter.
- Verify: stop F5, rebuild, F5. Open a two-person in-process case → Document copies. Hide one chip; that person section disappears; Preview/package follow the remaining people.
- Cross-skill: visa2026-document-copies | visa2026-preview-slot

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

### 2026-08-29 — Template Scan Analyze: DeploymentNotFound on gpt-4o-mini

- Symptom: Analyze scan shows "Analysis failed" (generic catch) after PNG upload; case field showed literal `_applicationNumber`.
- Cause: `TemplateAiScan:AzureOpenAI:Deployment` was `gpt-4o-mini`, but that deployment is **gone** on `visa2026-openai` (HTTP 404 DeploymentNotFound). Convert already uses working `gpt-4.1-mini` (vision OK). Separate bug: Resminamalar passed `ApplicationNumber="_applicationNumber"` (literal string) instead of `@(_applicationNumber)`.
- Fix: Point Scan Deployment at `gpt-4.1-mini`; surface `ex.Message` in the dialog; fall back to deterministic field plan when the AI provider throws (Convert parity); fix ApplicationNumber bindings in officer shell + Resminamalar slot.
- Verify: stop F5, rebuild, F5 — Analyze on Turkmen letter PNG should reach Field Review (vision via gpt-4.1-mini). Case hint shows the real application number.
- Prevent: Before changing Scan deployment names, probe `POST .../deployments/{name}/chat/completions` with the Scan API key. Prefer the same deployment Convert already uses on this resource.
- Cross-skill: -

### 2026-08-29 — Scan draft must keep letter layout (not Label: token list)

- Symptom: Analyze mapped fields, but Preview/Resminamalar merge showed a flat `Company name: …` catalog — original Turkmen letter structure (header, addressee, body, signature) was lost.
- Cause: Azure `ProposeDocxLayoutAsync` was a stub returning empty blocks; `ScanDocxLayoutService` fell back to `DeterministicScanDocxLayoutPlanner`, which emitted `Kind=field` as `Label: {{token}}` lines.
- Fix: Implement vision layout reconstruction (paragraphs with embedded tokens + align); pass page PNGs/OCR/value hints into `ScanDocxLayoutRequest`; builder renders `paragraph`/`blank` with alignment; OCR fallback replaces value hints when AI is off.
- Verify: stop F5, rebuild, F5 — same PNG → Continue → Preview should show letter prose with `{{ds.*}}` inside sentences, not a field catalog. Unit: 45 TemplateScan green.
- Prevent: Do not ship empty AI layout stubs that silently fall back to flat lists for vision scans.
- Cross-skill: visa2026-resminamalar (merge preview of scan-authored templates)

### 2026-08-29 — Scan layout: no leftover-token dump at footer

- Symptom: Letter body preserved, but Preview/merge ended with a stack of bare `{{ds.*}}` / filled values (ACODE, ADAT, AFNUM, …) under the signature.
- Cause: ParseLayout (and OCR deterministic path) appended every mapped token that was not embedded — intended as a safety net, ruined letter layout.
- Fix: Remove end-of-document token dumps; strengthen vision layout prompt to place every mappedFields token in-context; warn in Generate when a Review token was not placed.
- Verify: Regenerate same PNG — footer should be position + name only (as on scan), not a token/value list. Unplaced tokens appear as Preview warnings.
- Prevent: Never append unused merge tokens as extra paragraphs to satisfy Emit-all.
- Cross-skill: -

### 2026-08-29 — Scan: yellow-highlight-only placeholders

- Rule: Officers mark replaceable values with yellow highlighter. Vision maps **only** yellow spans to the placeholder library. No yellow → Fail. Yellow but nothing maps → Fail. Gaps on some yellows → Warn. Non-highlighted text stays literal. Scan is always filled-sample + case hints; Blank form removed. Placeholder set for scan is **Both** (full Word library for the profile). Catalog adds `Urgency_NameTm` (`Adaty tertipde!`).
- Verify: Upload yellow-marked PNG → Analyze reaches Review with only highlighted fields. Unmarked PNG → Scan fail. Regenerate draft embeds only those tokens.
- Prevent: Do not fall back to OCR catalog matching when vision fails (invents non-yellow fields). Do not dump unused tokens at footer.
- Cross-skill: visa2026-user-report-templates (catalog)

### 2026-08-29 — Placeholder Manual missing tokens (Application rootBoType alias)

- Symptom: Case Resminamalar → Placeholder manual listed only a subset; many header/person tokens missing. Scan gaps for AFNUM/ADAT/etc. looked like “library missing” even though JSON had them.
- Cause: Catalog JSON uses `rootBoTypes: ["Application", …]`. Enum has `ApplicationProfileInstance`, not `Application`. For dual lists, `"Application"` failed parse and only `ApplicationItem` remained → case filter (`ApplicationProfileInstance`) dropped those rows.
- Fix: Map `"Application"` → `ApplicationProfileInstance` in `ParseRootBoTypes`. Also local yellow split/match for compound highlights + stronger field-plan prompt.
- Verify: Rebuild, F5, Placeholder manual from case Resminamalar — AFNUM, ADAT, ACNAM, TPCTX, PFN, Urgency_NameTm visible. Re-Analyze yellow scan should map number/date/count/period.
- Prevent: Do not filter Manual by root BO without aliasing legacy `"Application"`. Prefer writing `ApplicationProfileInstance` in new catalog rows.
- Cross-skill: visa2026-user-report-templates | visa2026-resminamalar

### 2026-08-29 — Scan draft: keep alignment / two-column / bold-italic

- Need: Officers want the Word template from scan to match letter layout (№/date left + addressee right; justified body; italic urgency; bold split signature) — not a left-stacked paragraph list.
- Fix: `ScanDocxBlock` gains `twoColumn`, `RightText`, `RightAlign`, `Style`, `RightStyle`. `ScanDraftDocxBuilder` renders borderless two-cell tables + italic/bold runs. Azure layout prompt/schema/ParseLayout require twoColumn + justify/italic/bold from the vision scan.
- Verify: stop F5, rebuild, F5 — Create from scan on the yellow Turkmen letter → Generate → Preview: addressee opposite header, body justify, urgency italic, signature title|name side-by-side. Unit: `Build_twoColumn_and_styles_match_letter_layout`, `ProposeDocxLayout_parses_twoColumn_and_styles`.
- Prevent: Do not model side-by-side bands as two sequential left paragraphs. Do not claim pixel-perfect fonts/margins — Edit template in Word remains for polish.
- Cross-skill: visa2026-resminamalar
### 2026-08-29 — Scan layout still left-stacked vs original letter

- Symptom: After twoColumn prompt work, officer Preview still looked like a left text stack; merge PDF closer but not matching original (header opposite addressee, italic urgency, bold split signature).
- Cause: (1) Vision often still emitted sequential left paragraphs; (2) Scan Preview used TemplateConvertOutlineView which flattens table cells into a line list — hides real Word alignment.
- Fix: ScanLetterLayoutNormalizer rebuilds ministry-letter structure (twoColumn header/signature, italic urgency, justify body) after AI/OCR layout; Preview shows DOCX→PDF iframe so officers see page layout.
- Verify: stop F5, rebuild, F5 — same yellow letter → Generate → Preview should show PDF with №/date left and addressee right; signature title|name side-by-side. Unit: ScanLetterLayoutNormalizerTests.
- Prevent: Do not judge scan draft layout from outline text alone. Prefer PDF preview for alignment QA.
- Cross-skill: visa2026-resminamalar
### 2026-08-29 — Scan wizard Preview is outline only (no preview-slot PDF)

- Need: Officers do not want the Resminamalar/template PDF preview viewer embedded inside Create template from scan.
- Fix: TemplateScanPreviewView uses TemplateConvertOutlineView again (placeholder placement). Page layout is checked after save via catalog Preview / Edit template. DOCX layout normalizer still builds alignment in the file.
- Verify: Generate → Preview shows text outline + placeholder list, not a PDF iframe. Catalog Preview still opens #visa-preview-slot.
- Prevent: Do not inject ApplicationWordReportOfficePreviewPdfConverter into the scan wizard.
- Cross-skill: visa2026-preview-slot | visa2026-resminamalar
### 2026-09-24 — Seretmezlik (roster exclusion letters) on the case workspace

- Need: after submission, officers send a letter asking the current holder (ministry leg or Migration Service) to stop processing some roster people; several letters per case, each numbered.
- Fix: new child BOs `ApplicationProfileInstanceExclusion` / `…ExclusionPerson` (no roster-line BO exists — the roster is a skip-nav M2M, so exclusion state lives on its own table). Letter numbers share the company counter: `ApplicationProfileInstance.OnSaving` now also takes the max exclusion sequence, and `ApplicationProfileInstanceExclusionNumbering.Allocate` takes the max instance number. Current holder: `N_REVIEW_STARTED` / `N_REVIEW_REJECTED` → leg N, `N_REVIEW_APPROVED` → leg N+1 or Migration, else Migration. Excluded people are filtered via `ApplicationWorkspaceCaseView.ActivePeople` (Resminamalar tab, Document copies tab, header document filter, document-copies completeness); `PersonHasGap` ignores them.
- Verify: `dotnet test Visa2026.Module.Tests --filter ApplicationProfileInstanceExclusionTests` (13 pass: holder, Turkmen dative/genitive, default template merge with the `{{#ds.People}}` table-row loop). F5 → submitted case → Seretmezlik → New → save → person struck through on People & links and header; Resminamalar/Documents no longer list them.
- Prevent: DocxTemplater loops that must produce one line per person belong in a single table row (start token in the first cell, end token in the last). Progress has no ministry letter **number** field — only the date can be prefilled. The 11 Module.Tests failures on HEAD a42a359a (IssuedRecordsCatalog, TemplateScan patterns, PlaceholderSet, ProfileOverview, ValueMap) pre-date this slice.
- Cross-skill: visa2026-resminamalar | visa2026-document-copies | visa2026-application-progress
## 2026-09-24 — Seretmezlik editable templates (slice 14b, Phase 1)

- **Context:** officers could not change the Seretmezlik letter layout; asked for Resminamalar-like templates for the letter and roster.
- **Decision:** company-wide `ApplicationProfileInstanceExclusionTemplate` (Letter / Roster) instead of `UserReportTemplate` — any active `UserReportTemplate` shows in the Resminamalar **Shared** pane (`ApplicationProfileWizardTemplateCatalog.Build(...).Shared`), and Scan / Convert are profile-bound (instance id, profile placeholder set, `ApplicationProfileTemplateSaveHelper`), so reusing them needs a non-profile placeholder set (Phase 2).
- **Excel:** `ExcelReportGenerator` is tied to `UserReportTemplate` + `ApplicationRosterMergeLine`; Seretmezlik has its own small ClosedXML merger (loop row = row containing `{{#ds.People}}`, `InsertRowsBelow` keeps style).
- **Validation before save:** unknown tokens block the upload (DocxTemplater would fail at download time otherwise); roster requires the people loop; trial merge with catalog sample data.
- **Gotcha:** `BorderValues` (OpenXML SDK 3) is a struct, not an enum — cannot be `const`.