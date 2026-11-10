# Learnings (append-only): Person document copies

Purpose: **Person resolver, sectioned catalog, preview slot occupant, ListView entry** — not ApplicationItem ministry ZIP.

**Read when implementing or fixing Person document copies.**

**Canonical design:** [docs/PERSON_DOCUMENT_COPIES.md](../../../docs/PERSON_DOCUMENT_COPIES.md)

**Maturity:** [MATURITY.md](./MATURITY.md)

**After a verified fix:** append one entry below. **Do not** edit or delete prior entries.

```markdown
### YYYY-MM-DD — <short title> (phase N)

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: person-document-copies | preview-slot | document-copies | —
```

---

## Entries

_No verified implementation entries yet. Design drafted 2026-06-06._

### 2026-07-30 - Person dossier becomes a 4th entry point (phase 2)

- **Ask**: From a person search result, open the dossier ("dosye") and every file copy (passport, visa, invitation, diploma, work permit, CV) for that person.
- **Try**: New read-only dossier page (`PersonDossierHost` + `PersonDossierPropertyEditor` + `PersonDossierComponent`), toolbar **Document copies** button calling `OpenPersonDocumentCopiesAsync`.
- **Root cause (of the design question)**: The ask looked like new work, but sections Passports / Visas / Education / WorkPermit / Invitation / Person files already exist in `PersonLinkedDocumentsResolver`. Only an entry point was missing.
- **Fix**: Reused the existing occupant unchanged. Entry points are now DetailView toolbar, ListView toolbar, ListView **Copies** column, **and the dossier**. Dossier `RecordKey` format is deliberately identical to `PersonLinkedDocumentRecord.RecordKey` (`Passport:{id}`, `Passport:{pid}/Visa:{vid}`, `Education:{id}`, ...) so a dossier row can be deep-linked to its copies row later.
- **Prevent**: Before building a "show me the person's files" surface, check this resolver first - the catalog is BO-family complete; what is usually missing is a caller.
- **Not verified in a running app session**: `dotnet build Visa2026.slnx -c Debug` clean; no UI run yet. Per-section paperclip deep-link still open.
- **Cross-skill**: preview-slot, report-dashboard (planned Person search category)

## 2026-07-30 - Person dossier verified against real data (headless Edge)

**What:** Drove the running dev host (localhost:5000, Postgres `visa2026`) with Selenium + msedgedriver
from `Visa2026.E2E.Tests/.webdrivers` to open **Open dossier** on an employee and a family member, then
the dossier's **Document copies** button.

**Confirmed:** dossier renders in the main area while the copies catalog occupies `#visa-preview-slot`
side by side - the `OwnerViewId` = `PersonDossierHost_DetailView` keeps the slot from self-closing.

**Two runtime-only bugs the compiler could not catch** (details in `docs/PERSON_DOSSIER.md`):

- `ObjectViewController<DetailView, PersonDossierHost>` does **not** activate for a non-persistent host
  view; match on `View.Id` from a plain `ViewController` instead.
- Hiding Save / Delete only in `OnActivated` is undone when the view replaces the current one; reapply
  in `OnViewControlsCreated`.

**Tip:** when checking whether a rebuilt DLL contains a change, search string literals as **UTF-16**
(metadata `#US` heap) - an ASCII search finds type names but never literals, which reads as a false
"my code did not build".

### 2026-07-30 - Director hand-over export: two entry-naming bugs a green build hides (phase 4)

- **Symptom**: The first end-to-end export succeeded and produced a valid ZIP - with only `Dossier.pdf` in it. The dev Postgres has **zero rows** in every `*Document` table, so `PersonExportPacker`'s per-record loop never ran. `(1 / 1)` in the toast was truthful and useless.
- **Try**: Seeded fixtures directly in the dev DB - four `FileData` rows loaded with `pg_read_binary_file`, wired to two passports (one with a PDF **and** a PNG, to force a real merge) and one visa (to force a second section). Fixture files must sit somewhere the Postgres **service account** can read; `C:\Users\<me>\AppData\Local\Temp` is not that place.
- **Test**: Selenium: search -> open dossier -> **Export for director** -> wait for the toast -> download -> list ZIP entries.
- **Root cause**: Two assumptions in `BuildEntryName`, both invisible until a record actually had files.
  1. Folder came from `section.SectionLabel`, but the catalog nests a visa **under its passport** (`RecordKey` = `Passport:x/Visa:y`, section `Passports`) - so the visa scan landed in `Passports/`.
  2. Leaf came from `PersonDocumentCopyPdfMerger`'s file name, which returns the **uploaded** name for a single-file record - so the package read `visa-scan.pdf` instead of `Visa A1742149.pdf`.
- **Fix**: Folder now resolves from the record's own document class (`PersonExportPacker.FolderKeyByRecordType`, currently only `Visa` -> new `PersonDocumentCopies.Section.Visas` key), falling back to the section label; leaf now prefers `record.RecordLabel`. Result: `Passports/Passport U40412139.pdf`, `Visas/Visa A1742149.pdf`.
- **Prevent**: The merger's single-file naming is **right for the preview** (the officer recognizes the file they uploaded) and **wrong for a package** - do not "fix" it in the merger; adapt at the packer. And treat "batch completed with 1 record" as a red flag to check whether the fixture data exists at all, not as a pass.
- **Also**: `PersonExportBatches` needs idempotent DDL (`PersonExportBatchSchemaSql`) run from **both** the `ModuleUpdater` and host start - a database already at the current module version skips updaters, and the worker then fails with `relation "PersonExportBatches" does not exist`.
- **Cross-skill**: person-document-copies, report-dashboard (search category is the entry point)

### 2026-07-30 - Verifying an export: the toast can hand you a stale package (phase 4)

- **Symptom**: After adding `PersonDossier.<Enum>.<Member>` keys and rebuilding, the exported PDF *still* showed raw `PrivateHouse` / `Entry` / `External`. The catalog contained the keys, the deployed `Visa2026.Module.dll` contained the literals (UTF-16 `#US` search), and only one host process was running, started after the build.
- **Root cause**: The package was **not the one the run had just produced**. The toast host resolves the user's latest batch, and a previously completed batch surfaced immediately; the smoke followed its download link. The giveaway was inside the artifact itself - the PDF footer read `Сформировано: 12:26`, minutes *before* the rebuild.
- **Fix (to the verification, not the code)**: Pulled the newest batch's ZIP straight out of Postgres (`lo_from_bytea` + `\lo_export`) instead of trusting the toast link. The enum labels were correct all along in the new build.
- **Prevent**: Any artifact a batch produces should be checked against a **freshness marker it carries itself** (generated-on stamp, batch id) before concluding anything about the code. A green toast plus a downloaded file is not evidence that *this* run produced *that* file.
- **Also (real finding)**: `PersonDossierResolver.LOr` silently falls back to the raw enum name, so a missing key is invisible until someone reads the output in a non-English culture. The dossier duplicates the XAF enum captions into the message catalog deliberately - `CaptionHelper` has no XAF application context inside the export worker.
- **psql on Windows**: PowerShell strips the doubled quotes in `psql -c "... \"Table\" ..."`, which surfaces as `relation "people" does not exist` for a table that plainly exists. Put the SQL in a file and use `-f`.
- **Cross-skill**: person-document-copies, lookup-data (`ApplicationItem.LastApplicationState` still renders English)

### 2026-07-31 — Shared doc-copies-catalog chrome (phase 2+)

- **Symptom**: Person copies sectioned table looked different from AppItem/Header flat group cards.
- **Fix**: Person classes renamed to `.doc-copies-catalog*`; shared CSS extracted; Person remains visual source of truth.
- **Prevent**: New Person catalog markup must use `doc-copies-catalog` classes, not resurrect `app-person-doc-copies`.
- **Cross-skill**: person-document-copies | preview-slot | document-copies

### 2026-07-31 — Prototype A Foxit-style vertical nav (Person catalog)

- **Ask**: Redesign document-copies navigation like Foxit Editor tool cards (user chose Prototype A).
- **Try**: Person multi-section catalog: colored circle icons, uppercase titles, record/file summary, blue Open/Close; all collapsed by default; exclusive expand on Open.
- **Fix**: `PersonDocumentCopiesComponent` nav headers + `document-copies-catalog.css` `__nav-*` / `__section-head--nav`; `PersonDocumentCopies.Action.Open` l10n.
- **Prevent**: Keep row table under expanded section; do not flatten Person into single Documents card. Header/AppItem can adopt same head chrome later for parity.
- **Cross-skill**: person-document-copies | preview-slot

### 2026-07-31 — Prototype A nav across all document-copies catalogs

- **Ask**: Apply Foxit-style vertical nav layout across all document copies in the project.
- **Fix**: Shared `DocumentCopiesCatalogNavIcons`; Person/Header/ApplicationItem section heads use `__section-head--nav` + Open/Close; exclusive expand (AppItem/Person multi-section); Header single Documents card collapsed by default.
- **Prevent**: Do not keep flat always-open section heads on Header/AppItem while Person has nav cards.
- **Cross-skill**: person-document-copies | document-copies | invitation-work-permit-document-copies | preview-slot

### 2026-07-31 — Dedicated Document copies brand mark (smiling paperclip)

- **Ask**: Use a dedicated icon/label for Document copies across the project (pill + smiling paperclip).
- **Fix**: `DocumentCopies.svg` (XAF ImageName), `document-copies-clip.svg` + `document-copies-brand.css`, `DocumentCopiesBrandMark`; wired toolbar actions (Person/Header/AppItem), ListView Copies pills, dossier button, slot titles.
- **Prevent**: Do not reuse `BO_FileAttachment` for document-copies entry points; use `DocumentCopies` / `.doc-copies-brand*`.
- **Cross-skill**: person-document-copies | document-copies | invitation-work-permit-document-copies | preview-slot | person-dossier

### 2026-07-31 — Prototype C icon-only ListView cells

- **Ask**: Compact Document copies column (text took too much space); user chose Prototype C (stacked pages + paperclip).
- **Fix**: Replaced brand SVG with C; ListView cells are icon-only pills (`font-size: 0` + mask icon); full title in `title`/`aria-label`; column width 48.
- **Prevent**: Do not put "Document copies" text in every grid row; keep label on dossier button / slot header / toolbar.
- **Cross-skill**: person-document-copies | preview-slot

### 2026-07-31 — Remove Document copies ListView toolbar (Person + Header)

- **Ask**: Drop "Person document copies" from Employees toolbar; row icon is enough. Same for header BOs.
- **Fix**: `PersonDocumentCopiesController` and `HeaderDocumentCopiesController` are `ViewController<DetailView>` only. ApplicationItem keeps ListView toolbar (multi-select package; no row link).
- **Prevent**: Do not re-add ListView toolbar copies when a Copies column already opens the slot.
- **Cross-skill**: person-document-copies | invitation-work-permit-document-copies | document-copies

### 2026-09-23 — Linux Razor treats `@section.` as the section directive

- **Symptom**: Docker Hub `docker build` failed RZ9979 / RZ2005 / RZ1011 in `PersonDocumentCopiesComponent.razor` (`aria-label="@section.SectionLabel"`).
- **Try**: Warnings in the log are noise; the 9 errors are Razor `@section` parse.
- **Test**: `dotnet build Visa2026.Blazor.Server -c Release` → 0 errors after wrap.
- **Root cause**: Markup `@section.Property` is the Razor `@section` directive, not the foreach variable. Linux SDK Razor is stricter than the local Windows compile that previously succeeded.
- **Fix**: Use `@(section.SectionLabel)` (explicit expression).
- **Prevent**: Never write `@section.` in `.razor`; use `@(section.… )` or rename the loop variable.
- **Cross-skill**: person-document-copies | person-dossier

### 2026-11-09 — Passports merge: one passport page per person (no standalone+beside dupe)

- **Symptom**: Multi-person Passports preview showed the same person's passport twice (standalone passport page plus visa-beside-passport page).
- **Try**: Prefer current passport/visa; when nested visas with files exist, emit only visa records (beside layout already embeds passport); else emit passport alone.
- **Test**: Module + Blazor Debug build succeeded; retest Passports with two checked people each having passport+visa.
- **Root cause**: `EnumerateSectionRecordsForMerge` yielded both the passport and each nested visa; visa-beside pages already include the passport scan.
- **Fix**: In `PersonDocumentCopyPdfMerger.EnumerateSectionRecordsForMerge` for section `Passports`: prefer `IsCurrent` passport/visa; if `visas.Count > 0` yield visas only, else yield passport.
- **Prevent**: Never emit a standalone passport row when visa-beside pages will be built for that passport.
- **Cross-skill**: person-document-copies | preview-slot | application-profile
### 2026-11-10 — Suppress Spire evaluation warning on document-copies preview

- **Symptom**: Passports / visa-beside preview pages showed red `Evaluation Warning : The document was created with Spire.PDF for .NET.` on every page.
- **Try**: Stop using Spire `SaveToStream` for scan composition; build pages with PdfSharpCore (`XPdfForm` / `XImage`); TIFF fallback via System.Drawing → PNG. Optional `SPIRE_LICENSEKEY` at startup for remaining XFA Spire saves.
- **Test**: Module Debug build succeeded; Blazor rebuild; F5 Passports preview for checked people — no red evaluation line.
- **Root cause**: Unlicensed Spire stamps red evaluation text when saving composed PDFs (`TryWriteVisaBesidePassport` / print-layout / raster fallback).
- **Fix**: `SupportingDocumentsPdfSharpHelper` PdfSharp-only saves; `SpirePdfLicense.ApplyFromEnvironment` in Blazor `Program`.
- **Prevent**: Do not `SaveToStream` with Spire for document-copies scan pages; keep PdfSharpCore for those outputs.
- **Cross-skill**: person-document-copies | document-copies | pdf-form-mapping | preview-slot
### 2026-11-10 — Profile-required Document copies filter + missing preview upload

- **Symptom**: People Preview document copies submenu listed every section with files, not only Application Profile Required person-related data; missing required scans had no clear upload path.
- **Try**: Allow-list from `RequirePerson*`; submenu shows required ready rows + clickable No file rows; left-nav By-type catalog uses same family allow-list (empty required families kept); missing opens preview empty state with Upload via `LinkedRecordDetailOpenHelper.ShowNewEditor`.
- **Test**: Module + Blazor Debug build succeeded.
- **Root cause**: Menu built from resolver sections with files only; no profile filter; Spire/path unrelated.
- **Fix**: `PersonDocumentCopiesProfileSectionFilter`; `BuildDocumentSectionMenuItems`; `ApplicationItemDocumentCopiesTypeCatalog.Build(..., allowedFamilyKeys)`; missing empty states on Person/AppItem slot panels.
- **Prevent**: Do not list PersonDocuments/FamilyRelation/BorderZone/Salary/Travel on these filtered surfaces; keep Visa under Passports for Person submenu.
- **Cross-skill**: person-document-copies | document-copies | application-profile | preview-slot

### 2026-11-10 — People & Links: no No file chip; table Upload + slot multi-file uploader

- **Symptom**: Preview document copies submenu showed **No file** even when some checked people still had scans; missing files had no row-scoped upload into the preview slot.
- **Try**: Remove submenu No file chip always; empty preview title **File is not uploaded yet**; Upload only when linked Passport/Education/Position/Address(/Medical) BO exists; multi-file PDF/image attach to that BO in `#visa-preview-slot`; People & Links only.
- **Test**: Module Debug build (Blazor via F5); retest People table Upload + empty section preview Upload.
- **Root cause**: Section menu readiness chip was global/misleading; missing upload opened BO editor instead of file attach UI.
- **Fix**: `PersonLinkedDocumentUploadService`; `PersonDocumentCopiesSlotRequest.OpenUpload*`; `PersonLinkedDocumentUploadPanel`; people-table Upload; `PersonDocumentCopiesSlotPanel` empty→upload.
- **Prevent**: Do not show No file on People submenu; do not create owner BO from this upload flow.
- **Cross-skill**: person-document-copies | preview-slot | application-profile

### 2026-11-10 — Passports preview error after duplicate-scan guard

- **Symptom**: People Preview document copies → Passports showed **Could not open document preview.**
- **Try**: Guard that skipped visa files matching the passport FileDataId/bytes left no pages when passport bytes failed to load, or visa-beside failed with no passport fallback.
- **Test**: F5 Passports for checked people (including issued/locked cases with Upload links).
- **Root cause**: `sameAsPassport` + `continue` dropped the only renderable bytes; section merge had no passport fallback when visa path returned empty.
- **Fix**: Only skip duplicate visa half when passport bytes are available; fall through to slice otherwise; passport-alone fallback; section merge falls back to passport record if visa build fails.
- **Prevent**: Never `continue` on duplicate detection unless a passport-alone page can still be emitted.
- **Cross-skill**: person-document-copies | preview-slot

### 2026-11-10 — People submenu: Passports vs Passports & visas

- **Symptom**: Officers needed passport-only preview and optional passport|visa beside without auto-guessing validity.
- **Try**: Two People Preview document copies rows; left-nav Document copies out of scope (deprecated).
- **Test**: F5 People submenu → Passports (passport pages only) and Passports & visas (each linked visa scan beside passport; passport only when no visa scan).
- **Root cause**: Single Passports merge always preferred visa-beside when nested visas had files.
- **Fix**: Menu key `PassportsWithVisas`; `IncludesNestedVisas` / `NormalizeCatalogSectionId`; Enumerate passport-only vs every linked visa with scan.
- **Prevent**: Do not collapse the two People menu modes into one Passports section id.
- **Cross-skill**: person-document-copies | preview-slot | application-profile

### 2026-11-10 — People Upload panel: existing files, staged remove, stacked preview

- **Symptom**: Upload opened an empty drop zone only; officers could not see/remove existing scans or preview without losing the editor.
- **Try**: List BO Documents; stage removals until Save; Preview opens FileData in a stacked slot tab.
- **Test**: F5 People Upload on a passport with scans — list, Preview (stack), Remove+Save, add files, stay on panel.
- **Root cause**: Upload panel was add-only.
- **Fix**: `ListDocuments`/`TryApplyChanges`; `FileDataPreviewSource`; `OpenFileAsync(..., stackWithCurrent)`; panel existing/new lists.
- **Prevent**: Do not ReplaceStack when previewing from an open upload occupant.
- **Cross-skill**: person-document-copies | preview-slot

### 2026-11-10 — People Nusga preview + Edit + missing-scan badge

- **Symptom**: Officers needed Copies/Nusga to open scan preview (not upload), Edit on preview, and a missing-scan warning under the link.
- **Try**: Localized Nusga; open FocusRecordKey preview with upload context; same-slot Edit mode; No file badge when BO has zero scans.
- **Test**: F5 People cell Nusga (with/without scans), Edit pencil, Save returns to preview; badge under Nusga.
- **Root cause**: Former Upload link opened the editor immediately.
- **Fix**: `OpenPeopleDocNusgaAsync`; SlotPanel EnterUploadMode/ReturnToPreview; InlinePreview Edit; `People.Nusga`/`People.NoFile`.
- **Prevent**: Do not open OpenUpload from the people-table link; keep Edit as same-occupant switch.
- **Cross-skill**: person-document-copies | preview-slot | application-profile

### 2026-11-10 — Submenu missing copies = printable page (no Edit)

- **Symptom**: Preview document copies submenu showed web empty + Edit when a section scan was missing; mixed people lost present scans when any person lacked files.
- **Try**: Merge placeholder A4 pages into section PDF; gate Edit behind AllowEditFiles (Nusga only).
- **Test**: F5 — check 3 people, open Passports; ready scans + missing page with Person + Document type; no Edit. Nusga/No file still Edit.
- **Root cause**: Section path treated “no files” as empty UI and skipped people without scans in the merge.
- **Fix**: `TryWriteMissingDocumentCopyPlaceholderPage`; section merge inserts placeholders; `AllowEditFiles`.
- **Prevent**: Do not offer Edit on submenu section preview; keep placeholders inside the PDF stack.
- **Cross-skill**: person-document-copies | preview-slot
