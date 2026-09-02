# Create template from scan — learnings

Append-only. Newest first under **## Entries**.

## Entries

### 2026-09-02 — Sample Word photo becomes `{{IMAGE:Person_Photo}}`

- Need: Officers asked whether inserting a person photo on the yellow-marked Word file is enough. Yellow scan only saw highlighter on **text**; a picture stayed a static sample.
- Cause: Create from yellow marks had no picture detector. Catalog `PPH` emitted `{{IMAGE:PPH}}`, but merge injects photos keyed as `Person_Photo`, so that token was cleared.
- Fix: Body inline pictures (not header/footer, not tiny icons) are photo slots. Generate replaces the drawing with `{{IMAGE:Person_Photo}}`. `BuildWordToken` for images uses the canonical path; `TryGetShortCode` maps `Person_Photo` → `PPH`; injector resolves `PPH` to `Person_Photo`. Excel still has no photo inject. PNG/JPG/PDF uploads stay retired.
- Officer: Restart, hard-refresh. Analyze a yellow-marked Word that contains a sample portrait. Review shows **Person photo**. Continue / Approve. Catalog Preview fills the live `Person.Photo`. Do not yellow-highlight the picture itself.
- Cross-skill: visa2026-user-report-templates

### 2026-09-02 — Create template Save to: Project contract / All contracts

- Need: Under **Save to** on Create from yellow marks, officers needed the same visibility as the Application Profile Templates wizard: one `ProjectContract` (via ministry) or all contracts on this profile; direct-migration uses Migration service the same way.
- Cause: Upload only had This profile / Shared catalog. `ApplicableProjectContractId` existed on the nested row and `IsVisibleForInstance` already filtered profile-specific rows, but Scan Approve never persisted the FK.
- Fix: When **This profile only**, show the wizard dropdown (**All contracts** vs one Project contract; **All migration services** vs one service). Shared catalog hides it and clears the binding. `ApplicationProfileTemplateSaveHelper.ApplyCatalogApplicability` on Scan save (`SetApplicability`). Convert still does not overwrite an existing wizard binding.
- Officer: Restart, hard-refresh. Create template → Save to **This profile only** → leave **All contracts** or pick one Project contract → Approve. Resminamalar on another contract of the same profile should hide a one-contract template. Shared catalog stays visible on every case of this profile.
- Cross-skill: visa2026-resminamalar · visa2026-application-profile

### 2026-09-01 — Şahsy Preview `{{ds.PVFM}}` blocked Approve

- Need: Create from yellow marks Preview on Şahsy kagyz listed blocking `{{ds.PVFM}}` / `{{ds.PDBT}}` / `{{ds.PCBT}}` / `{{ds.PBPL}}` / `{{ds.PFWC}}` — property not found on ApplicationProfileInstance. `{{.PFN}}` was fine. Approve stayed disabled.
- Cause: Word yellow on a letter is Header-scoped. Officer remap / Azure kept `{{ds.CODE}}`. Validator resolves short codes to `Person_*` and looks them up on the instance, not `ApplicationRosterMergeLine`. Merge only promotes `{{.CODE}}` onto the root when there is no `{{#ds.rows}}`.
- Fix: Catalog Row-only entries always emit `{{.CODE}}` (`BuildWordToken` ignores Header usage). `ScanLibraryTokenRewriter` rewrites leftover `{{ds.PVFM}}` on merge, Azure sanitize, and Generate.
- Officer: Restart, hard-refresh, Analyze, Continue (or Regenerated). Blocking list should clear so Approve can enable. Catalog Preview still uses first-roster promotion for letters without a rows loop.
- Cross-skill: visa2026-user-report-templates

### 2026-09-01 — Review row × removes a redundant detected field

- Need: Şahsy kagyz family block split to 10.1 / 10.2; officers needed to dismiss the extra row without clearing **PVFM** on the yellow span.
- Fix: Detected fields **×** (right of each row). Compound part is hidden (`HiddenPartIndexes`); parent token stays. Last remaining part or a simple mark is removed so Generate leaves the printed text. Unmapped gaps can be dismissed the same way.
- Officer: Restart, hard-refresh, Analyze. Click **×** on the extra row (e.g. 10.2). Continue. The family highlight still writes **PVFM** if 10.1 kept it.
- Cross-skill: none

### 2026-09-01 — PNTM must not steal Sanaw Raýatlygy; isolated I-AŞ+phone stays RPCL

- Need: Catalog **PNTM** (`Person_NationalityTm`) used the same tk-TM label **Raýatlygy** as the Sanaw ISO-code column, so yellow `TUR` mapped to PNTM. Isolated wekil `I-AŞ … +993…` comma lines were split to RPPL+ACPHN instead of **RPCL**.
- Fix: PNTM label **Raýatlyk ady** / Nationality name. Column profile `rayatlygy` → **PNAT**; `rayatlyk ady` → **PNTM**. Profile prefer-codes beat a competing catalog exact-label. Compound binder keeps **RPCL** when there is no form-caption slot list. Catalog examples skip **PSEF** (same sample as **SPFNM**). Comma yellows skip catalog-example matching.
- Officer: Restart, hard-refresh, Analyze. Sanaw **Raýatlygy** + `TUR` is **PNAT**. Wekil passport+phone without a parenthetical caption is **RPCL**.
- Cross-skill: visa2026-user-report-templates

### 2026-09-01 — Resminamalar Review placeholders reopens scan Review

- Need: After Approve, yellow is stripped. Officers still need to remap placeholders on a catalog template (e.g. Gaybo-BORÇNAMA_115) without desktop Edit template and without uploading a new yellow file.
- Fix: Catalog row **Review placeholders** (this-profile nested Word/Excel) opens `TemplateScanDialog.OpenForExistingTemplateAsync`. `ScanOfficeLibraryTokenExtractor` builds Review from library `{{…}}` clusters when yellow count is 0. Comma compounds stay one Generate span (6.1 / 6.2). Locked profiles cannot overwrite the same name — rename to save a copy. Not `#visa-preview-slot`.
- Officer: Restart, hard-refresh. On Resminamalar, click **Review placeholders** next to Preview. Remap Short codes, Continue, Approve. If the profile is locked, change the template name before Approve.
- Cross-skill: visa2026-resminamalar

### 2026-09-01 — Comma yellow = combination; captions under the line guide parts

- Need: Borçnama yellow values like `U37109249, T.C. AŞKABAT BE, 19.02.2024ý.` or company `№263407090, 02.02.2009ý., …, +993…` were one Review row. Officers need 6.1 / 6.2 / 6.3 with separate preview borders. The printed `(pasportyň seriýasy we belgisi, nirede we haçan berildi, möhleti)` under the line and the left label (`pasporty:`, `ygtyýarly wekili`) say what each comma part is.
- Fix: `IsCommaCombination` keeps one Generate span. Review `ExpandCompounds` always splits on comma. Binder uses left label + parenthetical slots (`ScanFormCaptionHints`) so wekil lines go to `RPPN`/`RPPA`/`RPPH`, applicant passport to `PPN`/`PPAT`/`PPED`, company registry to `ACRDT`/`ACADR`/`ACPHN`. pdf.js overlays use segment text. Not `#visa-preview-slot`.
- Officer: Restart, hard-refresh, Analyze again. Comma highlights become 6.1 / 6.2 / 6.3. Click a sub-row to add/fix that part’s placeholder. Continue still writes the whole yellow mark as one combined token.
- Cross-skill: visa2026-user-report-templates

### 2026-09-01 — Review Add-placeholder grouped by related BO; passport type/country/authority

- Need: Officers and Ask AI could not find passport type, issued country, or authority. The Add-placeholder list was a flat A–Z dump.
- Fix: Catalog `relatedBo` groups Review `<optgroup>` and Azure `allowedTokensByBo` (Passport, Person, Company, wekil, …). New codes `PPTP` / `PPAT` / `PPCC` / `PPCT`. Placeholder manual sections match.
- Officer: Restart, hard-refresh, Analyze. Add placeholder shows groups (Passport, Person, …). For type / issued country / authority pick `PPTP` / `PPCC`+`PPCT` / `PPAT` — not wekil `RPPA`.
- Cross-skill: visa2026-user-report-templates

### 2026-09-01 — Review: one yellow mark can be several placeholders

- Need: Some highlights are compound (passport number + authority + phone, name + date). A single Short dropdown could not represent that.
- Fix: Selected row uses chips + **Add placeholder**. `ScanFieldPlanOfficerOverride.ApplyTokens` writes `{{ds.RPPN}}, {{ds.RPPA}}` (separator from the printed text) onto the same yellow span. `TemplateTokenSyntax.GetShortCodes` reads compounds.
- Officer: Restart, hard-refresh, Analyze. Click the mark, add each library code. Continue. Same highlight, combined tokens.
- Cross-skill: none

### 2026-09-01 — Review: remap placeholder from selected row; optional Ask AI

- Need: AI sometimes assigns the wrong library token. Officers need to correct it on Review without leaving the letter, and optionally ask AI with that mark in context.
- Fix: Selected Detected fields row shows a Short dropdown (`ScanFieldPlanOfficerOverride.ApplyToken` keeps the yellow span). Ask AI / Ask for clarification docks compact chat on Review and prefixes the focused mark (label + current token) on send. Not `#visa-preview-slot`.
- Officer: Restart, hard-refresh, Analyze. Click the wrong row (e.g. ADAT on a company date) and pick `ACRDT` from Short. Continue. Ask AI only if you want a suggestion.
- Cross-skill: none

### 2026-09-01 — Review PDF: no nested viewer; restore numbers and row highlight

- Need: Chrome/Edge PDF iframe showed toolbar + thumbnail sidebar (preview-in-preview). Numbered squares disappeared. Clicking a Detected fields row did not highlight the letter.
- Fix: Render converted PDF with pdf.js canvases (`template-scan-pdf-preview.js`). Overlay `#` badges by matching yellow labels in page text. Click a table row to keep the matching mark highlighted and scroll it into view. Not `#visa-preview-slot`.
- Officer: Restart, hard-refresh. Left pane is the Word page only (no PDF sidebar). Marks 1…n sit on the yellow text. Click a row on the right to highlight the same number on the page.
- Cross-skill: none

### 2026-09-01 — Review/Preview uses the uploaded Office file as PDF

- Need: Left Review pane was HTML outline; borçnama layout did not match the submitted Word page.
- Fix: Convert the uploaded (Review) / generated (Preview) `.docx`/`.xlsx` with `ApplicationWordReportOfficePreviewPdfConverter` and show it in a modal iframe. Same engine as catalog Preview. Not `#visa-preview-slot`. HTML outline remains fallback.
- Officer: Restart, hard-refresh CSS, Analyze again. Left pane should look like the Word pages (yellow highlighter kept on Review). Numbers stay in Detected fields.
- Cross-skill: resminamalar (converter)

### 2026-09-01 — Postgres 42703 CompanyProfiles.RegistrationDate

- Need: F5 after ACRDT work threw `column c.RegistrationDate does not exist` on roster merge (`CompanyProfile.TryGetInstance`).
- Cause: Property was added in code; XAF skipped schema because ModuleInfo already current.
- Fix: `CompanyProfileRegistrationDateSchemaSql.ApplyIfMissing` on host start + ModuleUpdater. Tenant manifest 43 so catalog can seed `2009-02-02` when lookup sync runs.
- Officer: Restart the app. If Configuration company date is still empty, set it or one-shot `FORCE_XAF_DB_UPDATE=true`. Then Analyze again.
- Cross-skill: lookup-data

### 2026-09-01 — Review/Preview sheet follows file orientation

- Need: Left preview stayed portrait A4 even when the uploaded Word/Excel is landscape.
- Fix: `TemplateDocumentOutline.PageOrientation` from Word `sectPr`/`pgSz` (Orient or width>height) and Excel `PageSetup`. Review + Generate Preview use `tas-a4-page--landscape` (297×210mm) and a wider left column. Not `#visa-preview-slot`.
- Officer: Hard-refresh CSS, Analyze again. Portrait borçnama stays 210×297. Landscape letters/sanaw show a landscape sheet.
- Cross-skill: none

### 2026-09-01 — Review left preview: A4 column width

- Need: In-process letter was a narrow pane (~0.9fr) so A4 text wrapped and scrolled.
- Fix: Review modal ~1760px; left column up to `210mm`; viewport height `min(80vh, 297mm)`. Not `#visa-preview-slot`.
- Officer: Hard-refresh CSS, Analyze again. Left sheet should be letter-width; Detected fields stay on the right.
- Cross-skill: none

### 2026-09-01 — Borçnama company date must be ACRDT, not ADAT

- Steps attached: Review (`6aylık-BORÇNAMA_111.docx`, mark 2 `02.02.2009ý.`)
- Need / Symptom: Isolated company hasaba alyş date mapped to `ApplicationDateText` / `{{ds.ADAT}}`
- Cause: Date regex always emitted `ADAT`. No Company Registration Date on `CompanyProfile` / catalog.
- Fix: Persisted `CompanyProfile.RegistrationDate`; placeholder `ACRDT` → `Application_Company_RegistrationDateText`. `ScanCompanyRegistrationDateGuard` rewrites `ADAT` when nearby is hasaba alyş / şahamça / tescil.
- Officer: Restart so schema + tenant JSON (`2009-02-02`) apply. Analyze again. Mark 2 → Company registration date. Confirm Configuration → Company shows 02.02.2009.
- Cross-skill: user-report-templates

### 2026-09-01 — Review numbered preview verified on borçnama_111

- Steps attached: Review (`6aylık-BORÇNAMA_111.docx`, 10 mapped)
- Need / Symptom: Confirm left in-process letter + numbered squares match Detected fields `#`
- Cause: n/a
- Fix: No code change — confirmed good. Mark 1 company → `ASPN`; 2 `02.02.2009ý.` → `ADAT`; 3 Hilmi → `PFN`; same numbers in the table.
- Officer: Continue to Generate when ready. Hover a row to highlight the matching square.
- Cross-skill: none

### 2026-09-01 — Review left preview: numbered marks matching field rows

- Need: In-process preview of the uploaded Word/Excel on Review, with numbered rounded squares on placeholder candidates, top-to-bottom, matching Detected fields row numbers.
- Fix: `ScanReviewFieldOrder` sorts by Word/Excel address. Left A4 outline (`TemplateScanReviewDocumentView`) wraps yellow spans with `#` squares. Table has a `#` column. Not `#visa-preview-slot`.
- Officer: Restart, Analyze again. Left letter shows 1…n on yellow names; the same numbers lead Detected fields. Hover a row to highlight the mark.
- Cross-skill: none

### 2026-09-01 — Review table: manual Full name / Description / Sample

- Symptom: Detected fields showed only short token + yellow label. Left pane was unused “Yellow-marked Word” help. Officers need Placeholder manual Full name, Description, and sample next to the short code.
- Fix: Office Review is a full-width table: Label, Short, Full name (`CanonicalPath`), Description (`LabelEn`), Sample (`ExampleValue`), Conf. Left help card removed.
- Officer: Restart / hard-refresh. Review of a borçnama should list e.g. `RPFN` / `Representative_FullName` / representative description / catalog example.
- Cross-skill: user-report-templates (catalog)

### 2026-09-01 — Borçnama_06: Review 10 mapped, Preview 0 placeholders

- Symptom: Review of `6aylyk-BORÇNAMA_06.docx` mapped 10 fields (Nepesowa `{{ds.RPFN}}` correct). Generate Preview: **0 placeholders**, blocking *No yellow-marked spans could be written*, original letter text, Approve disabled. A4 sheet looked narrow in the gray well.
- Cause: Generate only writes fields that still have `SourceRegion`. Merger re-split header tokens (ADAT/RPFN/CHFN) and clarification mapper dropped spans. Without an address the orchestrator returned the original package.
- Fix: Keep office drafts that already have a token + span. Recover yellow addresses by label if a span is missing. Clarification mapper copies `SourceRegion`. A4 sheet fills the preview column; Review field rows have more padding.
- Officer: Restart, Analyze `_06` again, Continue. Preview must list tokens (`{{ds.RPFN}}` on wekil). Approve when that list is not empty.
- Cross-skill: none

### 2026-09-01 — Review/Preview layout: fields width, A4 sheet, taller tokens

- Symptom: Review Detected fields were a 320px rail while the Word hint pane was a tall empty box. Preview placeholders were short chips; the draft stretched full width instead of looking like a letter page.
- Fix: Office Review uses `tas-split--review-office` (fields take remaining width). Word Preview wraps the outline in an A4 sheet (`210/297`). Placeholder chips use taller padding. Modal Review/Preview width `1340px`.
- Officer: Restart or hard-refresh so `template-scan.css` reloads. Review table should be the wide pane; Preview should show a portrait page and larger placeholder rows.
- Cross-skill: none

### 2026-09-01 — Borçnama_03: wekil slot must be RPFN, not Person

- Symptom: Review mapped Nepesowa under **Kärhananyň wiza işleri boýunça ygtyýarly wekili** to `{{.PFN}}` (“Person full name (roster) not representative”). After Approve, catalog Preview filled those lines with a case person (Serdar Nuri…), not Configuration `AuthorizedRepresentative` (Nejepowa). Hilmi + DOB and Mehmet `CHFN` were fine; `RPCL` was fine.
- Cause: The name guard treated any person-shaped yellow that is not the exact catalog wekil as roster `PFN`. The borçnama **slot** is wekil; the yellow sample name is fictitious and must not decide the BO.
- Fix: `ScanLetterRoleHint` + previous-paragraph printed label (`wekili` / `ygtyýarly`). Person-shaped yellow next to a wekil caption → `{{ds.RPFN}}`. Isolated Nepesowa (own paragraph, no wekil words) stays `PFN`. `RPCL` is not overwritten.
- Officer: Restart, Analyze `6aylık-BORÇNAMA_03.docx` again. Nepesowa under wekili → `{{ds.RPFN}}`. Hilmi stays `{{.PFN}}`. Approve, then catalog Preview should show Configuration wekil, not a roster person.
- Cross-skill: user-report-templates (RPFN ↔ AuthorizedRepresentative)

### 2026-09-01 — Borçnama_02: Nepesowa still RPFN; catalog Preview empty

- Symptom: Review 10 mapped; Nepesowa still `{{ds.RPFN}}`. Generate wrote 8 placeholders (overlap collapse worked — no skip warnings). Wizard outline still showed `___ Mehmet ÇIRAK ___` / `___ Nepesowa ___` on the signature lines. After Approve, Resminamalar catalog Preview: **Preview could not be generated** (READY chip).
- Cause: (1) Instance `RPFN` was the sample name Nepesowa, so the wekil guard kept `RPFN` even though catalog wekil is **Nejepowa Gurlar Aglyyowna**. (2) Word letter used row tokens `{{.PFN}}` / `{{.ASPN}}` / `{{.PPN}}` with no `{{#ds.rows}}`. DocxTemplater looks those up on `ds` and merge returns no file → generic Preview error. Signature names stay literal if that occurrence was not yellow (yellow-only replace).
- Fix: Person-shaped yellow stays `RPFN` only when it matches the catalog wekil example (or instance wekil when no example). Merge copies first-roster values onto `ds` when `{{.X}}` appears without a rows loop so catalog Preview can generate.
- Officer: Restart, Analyze `6aylık-BORÇNAMA_02.docx` again (or Mark another file). Nepesowa must be `{{.PFN}}`. Approve again, then catalog Preview. Yellow only the signature names if those lines should become tokens. Download Word if PDF still fails.
- Cross-skill: resminamalar | user-report-templates

### 2026-09-01 — Azure ambiguous payload: role + nearby snippet, not the file

- Ask: Pass long placeholder names instead of short codes, and send the Word/Excel file so Azure has more context.
- Decision: Keep short codes as the reply key (Extract/Validate). Add `role` (Applicant / Signatory / Wekil / Company / Case) and a one-line `description` on `allowedTokens`. For each escalated mark send `printedLabel` + `surroundingSnippet` (Word paragraph with `<<<yellow>>>`) or Excel `sheetName` + `headerRow`. Never send Office bytes, page images, or live case values.
- Officer: No wizard change. Restart only if Azure is on and a mark is still ambiguous after Analyze.
- Cross-skill: none

### 2026-08-31 — Borçnama Preview skipped CHFN/RPFN (overlapping duplicate yellows)

- Symptom: After Analyze, Review showed 10 mapped including `{{ds.CHFN}}` ×2 (Mehmet) and `{{ds.RPFN}}` ×2 (Nepesowa). Generate/Preview had only 6 placeholders; warnings `Skipped {{ds.CHFN}}: Overlapping spans in one paragraph` and the same for `{{ds.RPFN}}`. Approve needed warning ack. Nepesowa was still the wekil token — Configuration wekil is **Nejepowa Gurlar Aglyyowna**.
- Cause: (1) Duplicate yellow of the same name in one paragraph (`Mehmet` / `Mehmet __`, `Nepesowa` / `Nepesowa__`) share nested `WordSpan`s; the writer skipped the **entire** overlapping group. (2) Instance `RPFN` preference `0` beat roster `PFN` (default 50) when both matched the same person, so Review tagged applicants as wekil.
- Fix: Same-token overlapping spans keep the longest write and drop the duplicate silently. Different-token overlaps still warn. `RPFN` only when the yellow **exactly** matches the wekil and is not also roster `PFN`; person-shaped names rewrite to `{{.PFN}}`. Catalog example no longer maps `RPFN`.
- Officer: Restart, Analyze `6aylık-BORÇNAMA.docx` again, then Generate. Expect `{{ds.CHFN}}` and `{{.PFN}}` in the draft, **no** overlapping-span warnings for the duplicate Mehmet/Nepesowa highlights, and Nepesowa as `{{.PFN}}` not `{{ds.RPFN}}`.
- Cross-skill: user-report-templates (RPFN vs PFN)

### 2026-08-31 — Borçnama: Nepesowa tagged `RPFN` / gap (wekil vs person)

- Symptom: Review mapped `I-AŞ 476479…+993…` to `{{ds.RPCL}}` (correct wekil passport/phone) but `Nepesowa Tumar Aşyrowna` became `{{ds.RPFN}}` or an UNMAPPED gap (`Nepesowa…___`). Officer read this as Authorized Representative not identified.
- Cause: Tenant wekil is **Nejepowa Gurlar Aglyyowna** (`AuthorizedRepresentative`). Nepesowa is the **roster person**. AI/catalog treated any 3-word name as RPFN. Header uniqueness then blocked the underscored duplicate; merger also unique-constrained Row `PFN`.
- Fix: Person-shaped yellow → `{{.PFN}}` (not RPFN). Duplicate normalized labels reuse the first token. Row/High Word drafts kept. Name+DOB splits to PFN+PDBT. AI prompt: RPFN* = tenant wekil only.
- Officer: Restart, Analyze `6aylık-BORÇNAMA.docx` again. Expect `{{ds.RPCL}}` for the I-AŞ line, `{{.PFN}}` for Nepesowa (both highlights), `{{ds.CHFN}}` for Mehmet. RPFN only if the yellow name is the Configuration wekil.
- Cross-skill: user-report-templates (RPFN/RPCL catalog)

### 2026-08-31 — `Sanaw_clk_012` Approve blocked: `{{ds.PLN}}` on row 4

- Symptom: Review 14 mapped as `{{ds.ADRS}}` / `{{ds.RNUM}}` (not `{{.PLN}}`); Generate Preview had no `#ds.rows`; BLOCKING `Person_FirstName` not found on ApplicationProfileInstance; Approve disabled.
- Cause: Yellow sample row is **row 4** (headers on row 3). `DetermineScope` treated `dataRow < 5` as case header, so person cells became `ds.*` and loop planner skipped them. `clk_011` worked because yellow was on row 5.
- Fix: If the column has a roster header/profile (or row-scoped catalog match), scope is **Row** regardless of Excel row number.
- Officer: Restart; Analyze `Sanaw_clk_012` again. Review tokens must be `{{.PLN}}` not `{{ds.PLN}}`; wizard Preview must show `{{#ds.rows}}` in column A. Then Approve.
- Cross-skill: user-report-templates (Extract validates `ds.*` on ApplicationProfileInstance)

### 2026-08-31 — Officer verified `Sanaw_clk_011` catalog Preview (filled, no yellow)

- Symptom: Follow-up pack after strip fix — wizard + catalog Preview of a **new** Approve (`Sanaw_clk_011`).
- Try: People data, 14 mapped, `#ds.rows` in A5; restart; Approve; catalog Preview `Sanaw_clk_011.xlsx`.
- Result: Filled sanaw PDF with people; pane title `.xlsx`; screenshot does not show leftover highlighter (unlike `clk_010`).
- Prevent: Existing rows saved before the strip (`clk_010` and earlier) stay yellow until a new Approve.
- Cross-skill: resminamalar

### 2026-08-31 — Excel catalog Preview still yellow after re-Approve (`Sanaw_clk_010`)

- Symptom: Wizard OK (14 mapped, `#ds.rows` in A5); catalog Preview fills people but one data cell stays bright yellow. Re-Approve of a **new** name did not clear it.
- Cause: `StripAllYellowFills` only visited `CellsUsed()` value cells and only `XLColorType.Color`. Excel highlighter is often indexed/theme, on a merged non-anchor, or left in shared `xl/styles.xml` fills — merge then paints instance text on that fill.
- Fix: Strip all formatted/merged/row/column yellowish fills; neutralize yellow pattern fills in styles.xml (rgb + indexed 5/13/43/51).
- Officer: Restart app, **Create from yellow marks** again (or Mark another file) → Approve → catalog Preview with **no yellow**. Existing `SANAW_CLK_010` was saved before this strip; it will stay yellow until re-Approved.
- Cross-skill: resminamalar

### 2026-08-31 — Catalog Preview blank because Excel was converted as Word

- Symptom: Wizard Approve OK (`Sanaw_clk_09`); Resminamalar Excel Preview white page; pane title `report_….docx` while an Excel row is selected; Word sanaw Preview filled.
- Cause: Nested catalog `profile:` keys never matched `user:` lookup → filename `report_yyyyMMdd.docx` → Word PDF on xlsx bytes.
- Fix: In resminamalar generator, name from template format + `UserReportTemplateId`; PDF converter sniffs OpenXML `xl/` vs `word/`.
- Officer: Restart the app, then catalog **Preview** on the Excel row. Title should be `.xlsx`/`.pdf` and the grid should show people. No need to re-Approve if the template already saved.
- Cross-skill: resminamalar

### 2026-08-31 — Sanaw Excel analyzed as Case header → gaps + wrong ds.* tokens

- Symptom: `Sanaw_clk_08` Review: 5 gaps (Erkek, TUR, education), names as `{{.ACFNM}}`, dates as `{{ds.ADAT}}`; Continue disabled; Excel pane text overlapped.
- Cause: Data defaulted to **Case header** (copied from Convert); header library has no person-row tokens. `.tas-scan-stage { line-height: 0 }` stacked the Excel hint.
- Fix: Default Data to **Both**; picking `.xlsx` upgrades Header → Both; warn if officer switches back; placeholder `line-height: 1.45`.
- Officer: **People** or **Both**, then Analyze again. Check the warning box only after mappings look like `{{.PLN}}` / `{{.RNUM}}`, not `{{ds.ADAT}}`.
- Cross-skill: application-profile

### 2026-08-31 — Excel catalog Preview blank after extra col A; Word Preview OK

- Symptom: Wizard Approve OK after inserting empty column A for `{{#ds.rows}}`; Resminamalar Excel Preview is a white PDF; Word sanaw Preview is filled.
- Cause: (1) Diff gate expected only `{{#ds.rows}}` or `{{.RNUM}}` in A5, not prepended both. (2) Stale Excel **Print_Area** (often leftover on empty col A) is exported as the first PDF page.
- Fix: Diff expectation prepends loop open onto the row token; PDF converter `ClearPrintRange` + used range + `ExportToPdf(..., sheetName)` + fit-to-width.
- Officer: Restart app; **Download** the Excel to confirm filled rows; then Preview. Extra empty column A is optional now.
- Cross-skill: resminamalar

### 2026-08-31 — Yellow marks Upload: Data scope selector (parity with prepared template)

- Ask: Create from yellow marks Upload had disabled “Placeholder library”; prepared template has required **Data** (Case header / People / Both).
- Fix: Same **Data** dropdown as Convert; wire `_dataScope` into placeholder set, Generate, Save, gap packet (was hard-coded Both).
- Officer: Sanaw Excel → choose **People** or **Both** before Analyze.
- Cross-skill: application-profile | template-convert

### 2026-08-31 — Excel #ds.rows belongs in column A (prepend when A has RNUM)

- Ask: Blank Resminamalar Preview after Application→ApplicationProfileInstance; downloaded `Sanaw_clk_06` had `{{#ds.rows}}` in **T5**; seed puts it in **A**.
- Answer: Merge already uses `ApplicationProfileInstance` + Linked People (`ApplicationRosterMergeLine`) — not the BO rename. Loop was pushed to T because A5 held `{{.RNUM}}` and occupied cells were skipped.
- Fix: Prefer column A always; **prepend** `{{#ds.rows}}` onto existing A token; close stays optional on A6.
- Verify: Download shows `{{#ds.rows}}{{.RNUM}}` in A5; catalog Preview shows filled sanaw (case must have people).
- Cross-skill: resminamalar | user-report-templates (`Sanaw_ckl_map.md`)

### 2026-08-31 — Excel scan: skip merged cells for ds.rows (Sanaw_clk_05)

- Symptom: Generate warning `Skipped ds.rows: Cell is a non-anchor member of merged range 'J5:K5'`; catalog Preview "could not be generated"; READY chip still.
- Cause: Loop marker wrote into/near merged span (POSN layout); writer correctly refused non-anchor.
- Fix: `PlanExcelLoopsFromSubstitutions(..., workbookContent)` skips **any** merged cell; prefer col A then next free unmerged.
- Verify: `PlanExcelLoopsFromSubstitutions_skips_merged_cells_when_workbook_provided`; officer re-Approve `Sanaw_clk_05` after rebuild.
- Cross-skill: resminamalar (catalog Download added same day)

### 2026-08-31 — Excel scan loop markers + catalog PDF align with seeded sanaw templates

- Symptom: Wizard OK (`#ds.rows` in sidebar) but Resminamalar Preview PDF blank for `Sanaw_clk_04`.
- Reference: `Resources/Templates/Excel/Sanaw_ckl_map.md` — **`{{#ds.rows}}` in A5**, `{{/ds.rows}}` optional A6; single sheet; `ExcelMergeMode.ItemList`.
- Cause: Scan placed loop markers after last data column (e.g. D5); multi-sheet upload + full-workbook PDF export could yield blank first page; Approve only Extract (no Validate).
- Fix: `TryPlaceExcelLoopMarker` prefers **column A** (seed convention); PDF converter keeps **first sheet only** + `SetPrintRange(GetUsedRange())`; Approve runs **ExtractAndValidate**.
- Verify: `TemplateRosterLoopPlannerTests` / `TemplateScanOrchestratorTests` expect **A5/A6**; officer re-Approve → catalog Preview shows filled sanaw grid.
- Cross-skill: user-report-templates (`Sanaw_ckl_map.md`) | resminamalar

### 2026-08-31 — Excel scan: first worksheet only (map + preview)

- Symptom: Multi-sheet sanaw workbooks showed extra sheets in wizard Preview; yellow on sheet 2+ could map or confuse Review.
- Rule: **Only the first worksheet** is scanned, mapped, previewed, and written. Other sheets stay untouched in the saved copy.
- Fix: `ScanOfficeYellowExtractor` first sheet only; `ScanExcelWorkbookPolicy`; orchestrator skips non-first cells + outline limited to sheet 1; loop planner uses first sheet group.
- Verify: `ScanOfficeYellowExtractorTests.Extract_Excel_ignores_yellow_cells_on_sheets_after_the_first`; `TemplateScanOrchestratorTests.GenerateAsync_yellow_excel_*`.
- Officer: Put yellow marks on **sheet 1**; re-Approve after deploy for catalog Preview (`{{#ds.rows}}` on first sheet).

### 2026-08-31 — Resminamalar catalog Preview fails for scan-saved Excel sanaw

- Symptom: Create-from-yellow-marks wizard Preview OK; after Approve, Resminamalar catalog Preview shows **Preview could not be generated** (GÜMAN chip).
- Cause: Excel **ItemList** merge requires `{{#ds.rows}}` / `{{/ds.rows}}`; scan Generate wrote row tokens only (Convert uses `TemplateRosterLoopPlanner`). Missing loop → `ExcelReportGenerator` throws. Linked `UserReportTemplate` also had no Extract/Validate → readiness warning.
- Fix: `TemplateRosterLoopPlanner.PlanExcelLoopsFromSubstitutions` in scan Generate; Approve runs `IUserReportTemplateMaintenanceService.ExtractPlaceholdersAsync`.
- Verify: `TemplateScanOrchestratorTests.GenerateAsync_yellow_excel_writes_rows_loop_marker`; officer re-Approve → catalog Preview PDF.
- Prevent: Any Excel roster template from scan must emit loop markers before save; run Extract on Approve.
- Cross-skill: resminamalar | user-report-templates

### 2026-08-31 — Garabogaz low confidence: missing ABZLN in placeholder manual

- Symptom: Sanaw Excel column **Barjak serhet ýakasy** yellow `Garabogaz` mapped to `.PLN` at 35% Low; officer cannot fix (placeholder not in manual).
- Cause: `Application_BorderZoneLocation_NameTm` (Case summary **Border zone** on `ApplicationProfileInstance`) was missing from `UserReportPlaceholderCatalog.json`; no Excel column profile.
- Fix: Catalog entry **`ABZLN`** → `Application_BorderZoneLocation_NameTm` (label tk **Barjak serhet ýakasy**, example `Garabogaz`); `ScanExcelColumnProfiles` header keys for column 14.
- Verify: `ScanExcelYellowResolverTests.Resolve_maps_border_zone_from_column_header`; officer re-Analyze → `{{.ABZLN}}` High confidence.
- Prevent: New Case summary / sanaw columns need catalog + column profile before scan can rank them; officers report missing manual entries to developer.
- Cross-skill: user-report-templates | application-profile

### 2026-08-31 — Rules-first Excel inference + Azure ambiguous refinement (no case matching)

- Symptom: Sanaw roster Excel mapped only header regex tokens; person literals (Erol, Hilmi) stayed gaps or wrong when matched against live case roster.
- Cause: (1) Office path relied on regex only for Excel. (2) Brief value-hint matching against case picker was wrong — yellow cells are **fictitious sample literals**, not instance values.
- Fix: **Rules first:** column-header profiles (`ScanExcelYellowResolver`), placeholder manual index, shape matcher, compound `,`/`/` splits; Review shows ranked alternatives with %. **Azure only when ambiguous:** `ScanAmbiguousYellowGate` (score &lt; 80, gap &lt; 15, unmapped, Low) → `RefineAmbiguousYellowMarksAsync` (text JSON; manual + column header; **no case/DB values**). Case picker = workspace context only.
- Config: `TemplateAiScan:RefineAmbiguousYellowWithAi` (default true), `AmbiguousYellowMinConfidencePercent=80`, `AmbiguousYellowScoreGapPercent=15`.
- Verify: 82 TemplateScan tests; officer re-Analyze `Sanaw_clk_02.xlsx` → Erol→`.PLN`, Hilmi→`.PFNM` from headers; Azure refines only uncertain marks.
- Prevent: Never match yellow sample text to selected case people; do not send roster values to Azure for scan authoring.
- Cross-skill: TemplateConvert (different: instance value-match) | user-report-templates

### 2026-08-31 — Excel roster value-hint matching (Sanaw_clk) — SUPERSEDED

- ~~Symptom: Yellow Excel roster mapped only letter regex tokens…~~
- ~~Fix: `ScanYellowValueHintResolver` matches yellow text to case ValueCandidates…~~
- **Retired:** Case value-hint path is wrong for sample roster uploads; use rules-first + ambiguous Azure (entry above).
### 2026-08-29 — Verified: Sazakow_5 catalog Preview clean (full yellow strip)

- Steps attached: Upload / Review (6 mapped) / Preview (`{{ds.*}}`, no yellow) / Done / catalog Preview of filled letter
- Symptom: Prior run left yellow on `6 (alty)`; this re-Approve of yellow-marked Word shows **no yellow** in Resminamalar catalog Preview.
- Fix: Confirmed `StripAllYellowMarkup` path after Generate (no further code).
- Verify: Officer screenshots — placeholders in wizard Preview; filled catalog letter without highlighter.
- Prevent: Re-Approve templates created before full-strip; do not treat wizard outline Preview as proof of catalog formatting.
- Cross-skill: resminamalar | preview-slot
### 2026-08-29 — Leftover yellow after partial map (e.g. 6 (alty))

- Symptom: Catalog Preview still showed yellow on `6 (alty)` while most mapped marks were gone.
- Cause: Token writer only cleared highlight on runs that received a placeholder. Unmapped yellow leftovers stayed.
- Fix: After Create-from-yellow-marks Generate, strip all yellow highlighter/shading (Word) and yellowish fills (Excel).
- Verify: Unit `StripAllYellowMarkup_clears_unmapped_leftover_highlights`; officer re-Approve → no yellow in catalog Preview.
- Prevent: Yellow is scan markup only; never leave it in the saved template.
- Cross-skill: TemplateConvert token writer | resminamalar
### 2026-08-29 — Strip yellow after placeholder write

- Symptom: Wizard Preview looked clean (`{{ds.*}}`); Resminamalar catalog Preview still showed yellow on filled values for a template created from yellow marks.
- Cause: Token writer replaced text but left Word `w:highlight` / Excel yellow fill on those runs/cells; merge Preview then painted instance values on yellow-marked spans.
- Fix: `WordTemplateTokenWriter.TryReplaceSpan` clears highlighter (+ yellowish shading) on touched runs; `ExcelTemplateTokenWriter.TryWriteCell` sets fill pattern to None after writing the token. Diff gate fingerprints ignore yellow so Generate still passes.
- Verify: Unit `Word_yellow_highlight_is_cleared_when_token_is_written`, `Excel_yellow_fill_is_cleared_when_token_is_written`, `GenerateAsync_yellow_word_writes_tokens_into_copy` (no yellow Highlight left). Officer: re-run Create from yellow marks → Approve → catalog Preview without yellow.
- Prevent: Do not leave highlighter on substituted spans; templates must not carry officer mark-up into merge output.
- Cross-skill: TemplateConvert token writer | resminamalar
### 2026-08-29 — Office-only + rename to Create from yellow marks

- Need: Focus on Word/Excel yellow marks; PNG/JPG/PDF less efficient and confusing next to Convert.
- Fix: Upload accepts `.docx`/`.xlsx` only (image/PDF throw retired). Field plan + Generate are Office yellow / token-writer only. Officer label **Create from yellow marks**. Entry no longer requires vision AI. Skill/specs updated.
- Verify: TemplateScan tests; UI shows new label; PNG upload rejected with retired message.
- Prevent: Do not reintroduce scan/photo as primary path without product unlock; keep Convert separate (value-match).
- Cross-skill: application-profile | resminamalar
### 2026-08-29 — Yellow-marked Word/Excel as Create-from-scan input

- Need: Officers often have editable .docx/.xlsx with yellow marks; interpreting OpenXML is easier than OCR/vision boxes.
- Fix: Upload accepts .docx/.xlsx; `ScanOfficeYellowExtractor` + office field plan (no vision); Generate uses `ITemplateTokenWriter` + diff gate on a **copy** of the source (layout preserved). Image/PDF path unchanged. Convert stays separate (instance value-match).
- Verify: Unit `ScanOfficeYellowExtractorTests`; officer: yellow Word → Analyze → Review tokens → Generate → Approve (.docx copy with `{{…}}`).
- Prevent: Do not route yellow Office files through Convert L7; do not rebuild letter layout when source is already OOXML.
- Cross-skill: TemplateConvert token writer | user-report-templates
### 2026-08-29 — Stray boxes on de/sa/sany; Urgency yellow missing (v4)

- Symptom: After v3, body yellows mostly OK; Review still showed teal on non-yellow fragments (`de`, `sa`, `sany`); `Adaty tertipde!` yellow had no overlay.
- Cause: Warm anti-aliased text edges passed as highlighter; weak leftover assignment forced fields onto those tiny blobs instead of real urgency yellow.
- Fix: Stricter chroma/size/density for yellow blobs; share yellow only for AFNUM+ADAT / TPCNT+TPCTX / VPER+VCAT; require MinAcceptScore — else keep AI box (do not park on fragments).
- Verify: Unit `Detect_rejects_small_warm_text_fragments`, `Apply_does_not_park_urgency_on_text_fragment`. Officer: re-Analyze → urgency boxed; no boxes on plain words.
- Prevent: Never force every field onto some yellow blob when the best score is weak.
- Cross-skill: -
### 2026-08-29 — Ghost teal boxes between paragraphs (v3)

- Symptom: Review shows correct tokens; urgency/header often OK; body has teal boxes floating in whitespace above yellow ink (`6 (alty) aý` missed); extra ghost boxes between paragraphs.
- Cause: Sparse warm pixels / dilated samples created low-density “yellow” blobs in the gap. AssignBoxes then matched upward-shifted AI boxes to those ghosts (closer in Y than real ink).
- Fix: Density filter on detected blobs (≥12% yellow pixels). Score matches by horizontal overlap + prefer yellows at/below AI Y (not nearest Y). Pale highlighter RGB/HSV accepted.
- Verify: Ctrl+F5 → re-Analyze → body teal boxes sit on `18 (on sekiz)`, `6 (alty) aý`, `köp gezeklik`; no empty boxes in the paragraph gap.
- Prevent: Never zip fields to yellows by Y-order alone when AI boxes are vertically drifted.
- Cross-skill: -
### 2026-08-29 — Review boxes still above body yellow (v2)

- Symptom: After yellow snap, header/urgency OK; body teal boxes floated in white space above paragraph yellow ink; some ghost empty boxes.
- Cause: (1) Stage `aspect-ratio` from WidthPx/HeightPx could disagree with displayed PNG → vertical drift lower on page. (2) AssignBoxes trusted AI IoU order. (3) MergeNearby glued vertically separated blobs into tall regions.
- Fix: Overlays sit in `tas-scan-overlays` sized to the image (`inset:0` on stage wrapping img only; no aspect-ratio). Assign fields to yellow blobs by document token order only. Merge yellows only on the same line.
- Verify: Ctrl+F5 → Analyze → Review body boxes on `18` / `on sekiz` / `6 (alty) aý` / `köp gezeklik` yellow ink.
- Prevent: Do not position overlays on a stage whose aspect ratio is independent of the `<img>` box.
- Cross-skill: -
### 2026-08-29 — Review teal boxes misplaced vs yellow ink

- Symptom: Upload/Analyze OK; Review mapped 7 tokens correctly but teal candidate squares sat on empty space / non-yellow text (e.g. company name), not on yellow highlights.
- Cause: Vision returns coarse/wrong normalized boxes; local yellow splits reused the parent box. Overlay CSS % positioning was fine.
- Fix: `ScanYellowRegionDetector` finds highlighter blobs on the page PNG; `ScanFieldBoxLocalizer` snaps field boxes to those regions after merge. Resolver slices compound parent boxes by snippet index. Stronger AI box prompt.
- Verify: Unit `ScanFieldBoxLocalizerTests`. Officer: hard-refresh → Analyze same yellow letter → Review overlays sit on yellow ink; hover row ↔ box.
- Prevent: Do not trust vision boxes alone for Review overlays when yellow ink is detectable on the PNG.
- Cross-skill: -
### 2026-08-29 — Screenshot-per-step feedback is the experience engine

- Need: Officer will submit a screenshot for each Create-from-scan step so the skill accumulates experience and improves generation.
- Fix: SKILL **Screenshot feedback loop** + MATURITY screenshot-driven loop + prompts pack openers. Agent must append learnings after every pack (good or bad).
- Verify: Next chat with Upload…Done images → compare → fix or confirm → learnings entry with Steps attached.
- Prevent: Do not treat screenshot packs as one-off UI comments without logging experience.
- Cross-skill: -
### 2026-08-29 — Skill mission: Word and Excel from scans

- Need: Skill should drive improving generation of **Word and Excel** templates from scanned document images, not Word-only forever.
- Fix: SKILL mission + scope include Excel-from-scan as improvement target; Word remains shipped path. Product S3 (Word-only v1) noted until Excel ships and specs are updated.
- Prevent: Do not treat Excel-from-scan as permanently out of scope or dump it into Convert.
- Cross-skill: visa2026-user-report-templates (Excel merge families)
### 2026-08-29 — Skill carved out from application-profile

- Need: Create from scan had grown enough (yellow gate, layout normalizer, Azure vision) to own a dedicated skill.
- Fix: `.cursor/skills/visa2026-template-scan/` (SKILL, reference, prompts, MATURITY, learnings). Cross-link from AGENTS + application-profile.
- Verify: Agent loads this skill for Create from scan / TemplateScan work.
- Prevent: Do not dump scan pipeline fixes only into application-profile learnings.
- Cross-skill: application-profile | resminamalar | preview-slot | user-report-templates

### 2026-08-29 — Wizard Preview is outline only (no preview-slot PDF)

- Need: Officers do not want the Resminamalar/template PDF preview viewer inside Create template from scan.
- Fix: `TemplateScanPreviewView` uses `TemplateConvertOutlineView`. Page layout after save via catalog Preview / Edit template.
- Prevent: Do not inject `ApplicationWordReportOfficePreviewPdfConverter` into the scan wizard.
- Cross-skill: visa2026-preview-slot | visa2026-resminamalar

### 2026-08-29 — Letter layout: AFNUM+ADAT left, addressee right

- Symptom: Draft/merge put date on the right; addressee missing or stacked left.
- Cause: Vision emitted `twoColumn` as AFNUM \| ADAT; normalizer did not recover addressee.
- Fix: `ScanLetterLayoutNormalizer` rebuilds header (left = number+date, right = addressee); OCR inject when AI drops recipient; prompt forbids ADAT-only right cell.
- Verify: Unit `ScanLetterLayoutNormalizerTests`; officer catalog Preview after Approve.
- Cross-skill: -

### 2026-08-29 — Yellow-highlight-only + compound split

- Rule: Map only yellow spans; Fail if no yellow / yellow unmapped; `ScanYellowHighlightTokenResolver` splits AFNUM/ADAT, TPCNT/TPCTX, VPER/VCAT, Urgency_NameTm; drop duplicate compound gaps.
- Prevent: Do not fall back to OCR inventing non-yellow fields when vision fails.
- Cross-skill: visa2026-user-report-templates (catalog)

### 2026-08-29 — Analyze DeploymentNotFound on gpt-4o-mini

- Cause: Scan deployment gone on Azure resource; Convert already on `gpt-4.1-mini`.
- Fix: Point `TemplateAiScan:AzureOpenAI:Deployment` at working vision deployment; surface `ex.Message`.
- Prevent: Probe deployment before renaming; prefer Convert’s deployment on same resource.
- Cross-skill: -

### 2026-08-29 — No leftover-token dump at footer

- Cause: ParseLayout appended every unused mapped token → ruined letter.
- Fix: Remove footer dumps; place tokens in-context; warn if Review token not in draft.
- Prevent: Never append unused merge tokens as extra paragraphs.
- Cross-skill: -

### 2026-08-29 — Placeholder Manual missing tokens (Application alias)

- Cause: Catalog JSON `rootBoTypes: ["Application"]` ≠ enum `ApplicationProfileInstance`.
- Fix: Alias in `UserReportPlaceholderCatalogService.ParseRootBoTypes`.
- Cross-skill: visa2026-user-report-templates

### 2026-08-31 — Sample data ≠ case values (manual inference)

- Symptom: Erol/Hilmi never map when case has Serdar/Ali Enes; value-hint path wrong for yellow-mark roster samples.
- Cause: Filled-sample upload uses **fake row literals**; officer case is workspace context only.
- Fix: **`ScanExcelYellowResolver`** — column header + placeholder manual + content shape; compound cells split on `,` and `/`; ranked **`Alternatives`** on Review; case value map removed from Analyze.
- Verify: `ScanExcelYellowResolverTests`; officer re-Analyze `Sanaw_clk_02.xlsx`.
- Cross-skill: visa2026-user-report-templates (Sanaw column map)

- Symptom: Case `8/-015` Excel `Sanaw_clk_02.xlsx` — only 4 mapped (ADAT, VPER, PGND, VCAT); gaps for Erol, Hilmi, TUR, Garabogaz, addresses, education.
- Cause: `RejectAmbiguous` dropped shared literals (`TUR` on PNAT/PCBC/PFAC); regex date path used header `ADAT` instead of row `PDBT`; row tokens must use `{{.CODE}}` via `BuildWordToken`; long address cells need substring match against case values.
- Fix: `RetainAmbiguousLiterals` on scan value-map build; `ScanYellowValueHintResolver` disambiguation (PNAT over PCBC/PFAC, PDBT over ADAT), substring contains (min length 3 only), correct token scope; Excel `DateTime` cells read as `dd.MM.yyyy`; **`ScanFieldPlanMerger` must not re-run date regex over value-hint drafts** (was forcing `ADAT`).
- Verify: `ScanYellowValueHintResolverTests` (5 tests); officer re-Analyze after F5 restart.
- Cross-skill: visa2026-application-profile (instance value map)
