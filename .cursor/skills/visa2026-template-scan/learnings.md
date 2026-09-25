# Create template from scan — learnings

Append-only. Newest first under **## Entries**.

### 2026-09-25 — Create template wizard follows the active theme

- Need: Upload / Review / Generate / Preview / Done stayed a white card with dark labels on Blazing Dark.
- Cause: `template-scan.css` painted `#fff` / `#0f172a` on `.tas-modal` and the step controls.
- Fix: Modal tokens fall back from `--DS-*` to `--bs-body-*`. Surfaces, labels, inputs, drop zone, stepper, and buttons use those tokens. The letter preview itself stays the document color.
- Officer: Hard-refresh, open **Create template**. The dialog matches the selected theme on every step.
- Prevent: Do not put raw white panels or navy labels back on `.tas-modal`.
- Cross-skill: visa2026-application-profile theming pattern

### 2026-09-25 — Officer Create template link hidden on Resminamalar

- Need: Visa officer (`Users`, e.g. arzygul) should see **Create template** on the case Resminamalar catalog.
- Cause: The link is `ShowScanEntryVisible`, which calls `TemplateScanAccess.CanCreateFromScan` → Write on `ApplicationProfileTemplate`. Users were read-only on that type.
- Fix: `EnsureReadWriteCreatePermission<ApplicationProfileTemplate>` for Users, plus Write on `ApplicationProfile.NestedTemplates` so Approve can attach the new row. UsersReadOnly stays read-only.
- Officer: Sign out and sign in again, open Resminamalar. **Create template** is next to Placeholder manual.
- Prevent: Do not leave `ApplicationProfileTemplate` on the shared read-only process-tracking grant for Users.
- Cross-skill: visa2026-security-access

### 2026-09-22 — Sanaw "Iş saparyna barýan ýer" mapped to PNAT not BTAD

- Need: CI `ScanExcelBtadProbeTests` — header **Iş saparyna barýan ýer** expected `{{.BTAD}}`, got `{{.PNAT}}`. Sibling **Iş saparynda boljak salgysy** already passed.
- Cause: Column profile already matched BTAD, but `MergeScores` only boosted codes that catalog labels had already scored. Catalog tk-TM is *boljak salgysy*, so BTAD never entered the merge. Sample `… Çalyk Enerji UYJ.` matched `\b[A-Z]{3}\b` and won as nationality.
- Fix: Seed missing column-profile codes at 96 ("Column header"). Form-field hint **BTAD** for *barýan ýer* / *boljak salgysy* (before generic *salgy* → ADRS). `LabelCompatible` treats BTAD like ADRS.
- Officer: Stop F5, rebuild, **Analyze**. Destination column → **BTAD**, not nationality. Filter `BTAD` / `business trip address` / `barýan ýer`.
- Prevent: Do not rely on catalog label overlap to apply a Sanaw column profile. Incidental three-letter tokens in an address are not PNAT.
- Cross-skill: visa2026-user-report-templates

### 2026-09-21 — Contract group + CCUR for Zähmet şertnamasy

- Need: Yellow labor contract `1.667.00 USD` and `18.02.2026 - 18.08.2026`. Review Add had salary under **Salary** and dates under leftover **Visa**. Currency was not in the officer library.
- Cause: `Salary_CurrencyCode` merged but was missing from `UserReportPlaceholderCatalog.json`. CSDT/CEDT used `relatedBo: Visa`.
- Fix: Catalog **CCUR** → `Salary_CurrencyCode` (PersonSalary pack). **CSAL**, **CCUR**, **CSDT**, **CEDT** → **Contract**. Analyze splits amount+currency and contract date ranges. Letterhead `Aşgabat şäheri` stays static (no company city BO).
- Officer: Stop F5, rebuild, hard-refresh Review / Placeholder Manual. **Contract** → CSAL, CCUR, CSDT, CEDT. Filter `CCUR` / `currency` / `şertnama`.
- Prevent: Do not put labor-contract salary/dates under Visa or a lone Salary group.
- Cross-skill: visa2026-user-report-templates

### 2026-09-19 — Excel loop tags must not sit between ACPOS and ACFNM

- Need: Yellow-marks Excel Generate wrote `{{#ds.rows}}` / `{{/ds.rows}}` on the signatory footer, between **ACPOS** and **ACFNM**.
- Cause: ACPOS/ACFNM are Header+Row, so footer yellows became `{{.ACPOS}}` and counted as a second roster row. Close then parked in the empty cell between the two mapped properties. Excel merge deletes that close row.
- Fix: Signatory codes are not roster-loop rows. Do not write `{{/ds.rows}}` onto a row that already has `{{` placeholders (close stays optional).
- Officer: Stop F5, rebuild, **Generate**. Footer stays position + name only. Loop tags stay on the people row.
- Prevent: Do not treat the signatory line as a second `{{#ds.rows}}` table.
- Cross-skill: visa2026-user-report-templates

### 2026-09-19 — Placeholder Manual matches Review Add groups

- Need: Catalog **Placeholder Manual** still used a flat RelatedBo enum filter and simple search. Officers want the same groups and hide-joined rules as yellow-marks Review Add.
- Cause: `GetGroupedEntries` only wrapped `GetEntries`. The manual listed every enum (empty leftover **Application** / **Visa**) and did not hide **EGIY**/**VNAT** or expand search aliases.
- Fix: `GetGroupedEntries` uses `ScanPlaceholderChoiceList.RemainingGroups`. Filter dropdown lists only groups that have officer-visible codes. `GetEntries` still returns the full catalog (joined tokens stay for old templates).
- Officer: Stop F5, rebuild, hard-refresh **Placeholder Manual**. Expect **Application — general / family member / cancellation / business trip**, **Visa — linked active** / **Visa — cancel**, separate **EGLV**/**EGIN** and **VNUM**/**VTYP**/**VSTD**. Type `EGIY` or `VNAT` only for the old joined tokens.
- Prevent: Do not keep a second grouping/search path for the manual. Change Add and Manual together.
- Cross-skill: visa2026-user-report-templates

### 2026-09-19 — Linked visa number/type stay separate; VSTD in Add list

- Need: Review Visa showed joined **VNAT** (number and type). Officer wants **VNUM** and **VTYP** separately. **Visa start date** (**VSTD**) was not offered / not mapped on *Möhleti we gezekligi*.
- Cause: Add list showed **VNAT**. Excel profile started with **VNAT**, so Analyze never assigned **VSTD** as its own code.
- Fix: Hide **VNAT** unless typed. *Möhleti we gezekligi* Çakylyk/gezeklik → **AVPRD**/**AVCAT**; booklet `A1635317 WP, 20.01.2026, 06.07.2026` → **VNUM**, **VTYP**, **VSTD**, **VEDT**. Search `visa start` / `VSTD`.
- Officer: Stop F5, rebuild, hard-refresh Review. **Visa — linked active** → **VNUM**, **VTYP**, **VSTD**. Type `VNAT` only for the old joined token.
- Prevent: Do not map number+type to one Review row. Start date is **VSTD**, not only **VEDT**.
- Cross-skill: visa2026-user-report-templates

### 2026-09-19 — Visa picker subgroups: linked active vs cancel

- Need: Review Add had one flat Visa group. Officers need roster CurrentVisa (People & links) separate from cancel stacks. Requested period/category stay on Application.
- Cause: All PersonVisa codes used `relatedBo: Visa`.
- Fix: **Visa — linked active** (VNUM, VTYP, VCTM, VNAT, VPLC, VISD, VSTD, VEDT, VBLK). **Visa — cancel** (CVNB, CVSB, CVEB). VPER/VCAT/AVPRD/AVCAT stay **Application — general**. CSDT/CEDT stay on leftover **Visa**.
- Officer: Stop F5, rebuild, hard-refresh Review. Filter `linked active` / `VNUM` or `cancel` / `CVNB`.
- Prevent: Do not put requested case visa period/category under Visa until a Visa — requested group is approved.
- Cross-skill: visa2026-user-report-templates

### 2026-09-19 — Education level and institution stay separate (not EGIY)

- Need: Review Education placeholders were one joined token (**EGIY** / **FMEIY**) on *Bilimi we okan ýeri* (`Ýokary, Gündogar …uniwersiteti`). Officer wants **Education level** and **Education institution** as two placeholders.
- Cause: Excel/Word column profile treated `bilimi` / `bilimi we okan yeri` as a 3-code compound (`FMEIY`, `EGLV`, `EGIY`). Shape/value hint also preferred the joined **EGIY**.
- Fix: Combined header → **EGLV** then **EGIN**. Bare *Bilimi* → **EGLV**. *Okan ýeri* → **EGIN**. Add list hides **EGIY** unless the officer types `EGIY`. Catalog **EGIY** remains for old templates.
- Officer: Stop F5, rebuild, **Analyze**. Combined yellow shows **N.1 EGLV** / **N.2 EGIN**. Two columns map one code each. Filter `education level` / `EGLV` / `EGIN`.
- Prevent: Do not map Education Review to `Education_LevelAndInstitutionTm`. Level and institution are separate library codes.
- Cross-skill: visa2026-user-report-templates

### 2026-09-18 — Header letters less stable than roster (TPCNT cloned; AFNUM on the 3)

- Need: After Goşundy overlay fix, cover-letter Review still guessed worse than sanaw Excel/Word. Detected had three **TPCNT** rows (person `1` plus both enclosure `1`s). Overlay **#1 AFNUM** sat on the yellow person-count `3`. Roster column headers stay one code per column.
- Cause: (1) Short-count clone copied Header-scoped TPCNT onto later `1`/`-1`. (2) Goşundy lines mention *raýat*, so they looked like person count. (3) FollowingCaption appended the Goşundy paragraph onto the person-count `3`, so that yellow became “enclosure” and lost TPCNT. (4) Word overlay mapped header % onto the PDF body cluster, so AFNUM’s box covered the `3`.
- Fix: Enclosure (Goşundy / nusgalar / maglumaty sany) ≠ TPCNT. Do not clone Header codes. Do not append a Goşundy next paragraph as the person-count caption. Official-letter prefers for tertipde / gezeklik. Word Review uses the full page for mark geometry (`?v=tasmarks16`); header boxes stay short.
- Officer: Stop F5, rebuild, hard-refresh Review, **Analyze**. Expect one **TPCNT** (the person count), Goşundy `1`/`-1` identified but not TPCNT, **AFNUM** `#` on the № (not on the `3`).
- Prevent: Do not treat cover-letter yellows like repeating roster cells. Header slots are one-shot, like Excel columns.
- Cross-skill: -
### 2026-09-18 — Goşundy `#8` on list `1.`; `#12` AFNUM missing; dashed `-1` unidentified

- Need: Cover letter Review (Wiza we Iş Rugsatnamasyny Uzaltmak). Overlay **#8** sat on Goşundy list marker `1. Daşary…`, not the yellow `1 sany`. Second line yellow `-1 sany` had no Detected row / no `#`. Detected **#12 AFNUM** had no mark on the letter preview.
- Cause: (1) Duplicate short `1`s were zipped to PDF hits in Y-order, so Word auto-number `1.` stole the attachment count. (2) `-1` / `–1` was not an isolated count digit, so the second Goşundy yellow did not clone/map. (3) Header/footer AFNUM often has no PDF text hit (`№` vs `1/-2`); unmatched Word marks were not painted from the OpenXML box.
- Fix: Skip list-number PDF hits for digits; snap counts to the OpenXML box (`?v=tasmarks15`). Treat leading dash as a count. If sample text is missing, still paint the geometry box (AFNUM). Clone short count digits.
- Officer: Stop F5, rebuild, hard-refresh Review, **Analyze** the same letter. Expect `#` on both Goşundy `1` / `-1 sany` (not on `1.`), and **#12** visible (header/footer application number).
- Prevent: Do not match numbered-list `1.` when the yellow is the count before `sany`. Do not drop overlay `#` when PDF text cannot find `№`.
- Cross-skill: -
### 2026-09-18 — Goşundy yellow `1` stole ACPOS; Mehmet left unmapped

- Need: Cover letter Review — attachment count `1` above şahamçasynyň müdiri mapped to **ACPOS**; title got **CHFN**; Mehmet ÇIRAK had no Short (officer: yellow not identified as placeholder). Extra Goşundy `1` looked unmarked.
- Cause: (1) `ExtractFollowingCaption` appended the next paragraph, so müdiri became nearby for the count yellow. (2) `LabelCompatible` treated almost any text as ACPOS-compatible, so digit `1` stole the header code and shifted the signatory pair.
- Fix: Skip signatory-title / person-name next paragraphs in FollowingCaption. Reject isolated count marks for ACPOS/POSN; keep real titles via `LooksLikeBranchDirectorTitle` / position phrase. `preferLeftLabel` ignores director nearby on count digits.
- Officer: Stop F5, rebuild, hard-refresh, **Analyze** the same letter. Expect attachment `1` ≠ ACPOS; müdiri → **ACPOS**; Mehmet → **CHFN**.
- Prevent: Do not use the next content block (signature) as a left field label for Goşundy counts.
- Cross-skill: -
### 2026-09-18 — FMRLH under Application — family member

- Need: Map `adamsynyň` without sponsor; find FM header codes.
- Fix: **FMRLH** + Application — family member / cancellation / general / business trip groups.
- Officer: Stop F5, rebuild. **Application — family member** → **FMRLH**.
- Cross-skill: visa2026-user-report-templates

### 2026-09-18 — FMWZP under Family member for sanaw Wezipesi

- Need: Review Add Family member had FMEIY/FMESP but no FM_WezipesiTm.
- Cause: Education-only catalog round deferred Wezipesi.
- Fix: Catalog **FMWZP** → `FM_WezipesiTm`. Analyze prefers FMWZP on wezipesi headers.
- Officer: Stop F5, rebuild. Family member → **FMWZP**.
- Prevent: Do not leave FM Wezipesi uncatalogued when POSN alone omits sponsor-ň relationship.
- Cross-skill: visa2026-user-report-templates

### 2026-09-18 — FMEIY/FMESP under Family member for sanaw education

- Need: FM invitation sanaw Bilimi / Hünäri columns — dependent Çaga / Orta defaults, not plain EGIY/EGSP.
- Cause: Only Education-group EGIY/EGSP existed; adult empty education printed blank.
- Fix: Catalog **FMEIY** / **FMESP** (`relatedBo` FamilyMember). Analyze prefers them on bilimi/hünäri headers.
- Officer: Stop F5, rebuild. Family member → **FMEIY** (level+institution), **FMESP** (specialty).
- Prevent: Do not use EGIY/EGSP when FM adult fallbacks are required.
- Cross-skill: visa2026-user-report-templates

### 2026-09-18 — FMSPH/FMREL under Application (not Family member)

- Need: Yellow-marks Review Add for mark `adamsynyň (İzzet…-wezipe)` — officer looked in Application; FMSPH was missing there.
- Cause: Catalog `relatedBo` was FamilyMember while SPFNM/SPPOS were Application. Header picker groups by relatedBo.
- Fix: Set **FMREL** and **FMSPH** `relatedBo` to **Application** (Header scope unchanged).
- Officer: Stop F5, rebuild. Filter `FMSPH` under Application. Map the whole yellow phrase to **FMSPH**.
- Prevent: Header letter tokens that merge on Application stay under Application with SPFNM/SPPOS; Family member is for roster/row FM fields.
- Cross-skill: visa2026-user-report-templates

### 2026-09-18 — Linked WorkPermitItem codes missing from Review Add

- Need: Case 9/-007 People & links Work permit item (number, AS-№, location, valid to). Review Add showed cancel-stack codes / WPNM only — no current AS or Valid to.
- Cause: Catalog had WPNM + WPLC only. AS / start / expiration lived on the merge line without short codes. Excel/sanaw row dict omitted those keys so even WPNM would print empty.
- Fix: **WPAS** **WPST** **WPED** plus WPNM/WPLC in sanaw/excel rows. *Rugsat edilen möhleti* → WPED (column profile beats passport PPED on a date tie). Filter `work permit item` / `valid to` / `WPAS`.
- Officer: Stop F5, rebuild, Analyze. Add **WPAS** (AS-№), **WPNM** (tassyknama), **WPED** (valid to), **WPLC** (item location), **AWPLC** (case locations). Cancel columns stay CW*.
- Prevent: Do not leave required WorkPermitItem scalars uncatalogued. Do not use CWAB/CWEB for the current linked item.
- Cross-skill: visa2026-user-report-templates

### 2026-09-18 — AWPLC for Goşulmaly hereket çäkleri

- Need: Additional WP-location Excel sanaw last column *Goşulmaly hereket çäkleri* = Case summary Work permit location on every row.
- Cause: Only WPLC/CWLB existed (person WorkPermitItem). No Row short code for instance `MovementPermitLocation`.
- Fix: Catalog **AWPLC** (Row only). Analyze maps *Goşulmaly hereket çäkleri* to `{{.AWPLC}}`. Merge prints stored catalog text (`Aşgabat şäheri, Mary welaýaty`).
- Officer: Stop F5, rebuild, Analyze. Last column → **AWPLC**. Filter `AWPLC` / `goşulmaly`. Not WPLC/CWLB.
- Prevent: Do not map “areas to be added” to the person’s current permit locations.
- Cross-skill: visa2026-user-report-templates

### 2026-09-17 — This-profile Project contract list was the full catalog

- Need: Case 9/-007 Iş Rugsatnama goşmaça barjak ýeri (1574-KIYANLI). Officer bound a yellow-marks letter to another Project contract, so Resminamalar on this case stayed empty.
- Cause: Upload **This profile only** loaded every `ProjectContract` via `LoadApplicabilityItems`. The case already has one contract from create.
- Fix: Case (and Add existing) dropdown is **All contracts** plus this instance’s Project contract only. Direct-migration: this case’s Migration service. Profile wizard with no case: All contracts only until a case is picked.
- Officer: Stop F5, rebuild. Create from yellow marks on this case — Project contract list is All contracts and **1574-KIYANLI** only.
- Prevent: Do not offer unrelated catalog contracts when authoring a This-profile template from a case.
- Cross-skill: visa2026-application-profile | visa2026-resminamalar

### 2026-09-17 — Add existing template shows CHECK (placeholders not extracted)

- Need: Case 6/-1024 Çakylyk we Iş Rugsatnamasyny Almak. After **Add existing template**, the This-profile card was **CHECK** (placeholders not extracted or validated). Yellow-marks Approve is **Ready**.
- Cause: Convert save wrote the Word/Excel and linked `UserReportTemplate` but never ran Extract/Validate. Catalog `EvaluateUserTemplate` treats an empty `Placeholders` list as **CHECK**.
- Fix: After Add existing / Convert save (same as Scan Approve), `ExtractAndValidatePlaceholdersAsync` on the linked user template.
- Officer: Stop F5, rebuild. Add the same file again — chip should be **Ready** when tokens validate. Already-saved CHECK rows stay until re-added (or Configuration Extract placeholders).
- Prevent: Do not persist an Add-existing nested row without extracting placeholders the way yellow-marks Approve does.
- Cross-skill: visa2026-resminamalar | visa2026-user-report-templates

### 2026-09-17 — Add existing template missing Project contract (via ministry)

- Need: Case 3/-477 Çakylyk we Iş Rugsatnamasyny Almak. Create from yellow marks **This profile only** has **Project contract**. **Add existing template** did not.
- Cause: Convert save never set `SetApplicability` / `ApplicableProjectContractId`. Scan already persisted the wizard dropdown.
- Fix: **Add existing** (and Convert) show the same nested dropdown when **This profile only**. Via ministry = **All contracts** or one Project contract. Direct = Migration service. Shared catalog hides it and clears the FK.
- Officer: Stop F5, rebuild. Add existing template → This profile only → pick a Project contract (or leave All contracts) → Add to profile. Resminamalar on another contract of the same profile hides a one-contract letter.
- Prevent: Do not leave Convert without the Scan Save-to contract dropdown on via-ministry This-profile rows.
- Cross-skill: visa2026-application-profile | visa2026-resminamalar

### 2026-09-17 — Şahsy Review #4 still on Bilimi after zip (Raýatlygy TUR missing in PDF)

- Need: Case 8/-1470 ŞAHSY KAGYZ Review. After tasmarks13, Detected **4** stayed **PNAT**, but overlay **#4** was still on **Bilimi / ýokary**. Raýatlygy `TUR` had yellow and no number.
- Cause: The PDF often exposes only two `TUR`s (birth and address). Zip then skipped `kind !== "word"` marks, or mapped the second hit onto **#4**. Leftover nationality then painted the OpenXML cell (education).
- Fix: Overlay (`?v=tasmarks14`) keeps birth `TUR` on **3.2** and address `TUR` on **13.1**. Leftover **#4** sits in the visual gap between birth (3.x) and passport (5.x), same column as the birth `TUR`. No table-cell fallback for duplicate short labels.
- Officer: Stop F5, rebuild, hard-refresh Review. **#4** on **Raýatlygy TUR**. **3.2** birth `TUR`. **13.1** address `TUR`. Mapping unchanged; no Remap.
- Prevent: Do not give leftover ISO `TUR` the next PDF hit or the OpenXML cell Y when Raýatlygy is missing from the text layer.
- Cross-skill: none

### 2026-09-17 — Şahsy Review #4 still not on Raýatlygy TUR (landed on Bilimi)

- Need: Case 8/-1470 ŞAHSY KAGYZ. After tasmarks12, **#4** moved off Şahsy belgis onto **ýokary** (education). Raýatlygy TUR still had no number.
- Cause: Neighbor-band fallback used the OpenXML cell box when birth/passport gap was too small. Şahsy has three `TUR`s (birth 3.2, nationality, address 13.1); they must pair in page order, not nearest to a bad box.
- Fix: Overlay (`?v=tasmarks13`) zips duplicate short labels (`TUR`) to PDF hits top-to-bottom in Detected order.
- Officer: Stop F5, rebuild, hard-refresh Review. **#4** on **Raýatlygy TUR**. **3.2** stays birth TUR. **13.1** stays address TUR.
- Prevent: Do not place leftover ISO `TUR` by table-cell Y when other `TUR`s are already on the page.
- Cross-skill: none

### 2026-09-17 — Şahsy Review #4 Nationality sits on Şahsy belgis

- Need: Case 5/-789 ŞAHSY KAGYZ.docx. Detected **4** is **PNAT** / `TUR` (Raýatlygy). Overlay **#4** was on Şahsy belgis next to **#6** PPIN. Raýatlygy TUR had yellow and no number.
- Cause: Short duplicate `TUR` often has no unused PDF hit (birth 3.2 already took one). Table-cell fallback used equal row heights, so nationality landed on the personal-number line. Roster şahsy is a left-label form, not a sanaw grid.
- Fix: Review overlay (`?v=tasmarks12`) places leftover short marks in the band between already-placed neighbors (birth 3.3 and passport 5.1).
- Officer: Stop F5, rebuild, hard-refresh Review. **#4** on **Raýatlygy TUR**. No Remap.
- Prevent: Do not paint a table-cell fallback for ISO `TUR` onto a later form row when birth and passport marks already sit above and below nationality.
- Cross-skill: none

### 2026-09-17 — Sanaw Preview № column empty though RNUM is mapped

- Need: Case 5/-789. Catalog Preview of This-profile *Daşary ýurt raýatlarynyň sanawy* listed all three people, but **№** was blank. Review showed **RNUM** mapped (sample `1`).
- Cause: Yellow-marks Word uses `{{.RNUM}}` (catalog `RowNumber`). Sanaw merge only stored `RowNo`. The row expander looked up RNUM → RowNumber and wrote an empty cell.
- Fix: Copy `RowNo` / `RowNumber` / `RNUM` onto each other at enrich. Expander fills `{{.RNUM}}` from `RowNo`.
- Officer: Stop F5, rebuild. Preview the same Word sanaw — **№** is 1, 2, 3. No Re-Approve.
- Prevent: Do not treat `RowNo` and `RNUM` as different merge keys.
- Cross-skill: visa2026-resminamalar

### 2026-09-17 — Invitation yellow-marks sanaw Preview fails; direct-to-migration seeded sanaw lists everyone

- Need: Case 5/-789 Çakylyk Almak, 3 people. This-profile *Daşary ýurt raýatlarynyň sanawy* Word/Excel Preview was **Preview could not be generated.** Case 8/-1307 Wizany uzaltmak **SANAW_WIZANY_UZTURMEK** Excel Preview listed all 3 people.
- Cause: Direct-to-migration nested seeds already have a valid `{{#ds.rows}}` wrap. Invitation yellow-marks copies are a ministry table with `{{.CODE}}` only (or a scan-injected wrap that DocxTemplater cannot parse). Excel `InsertRowsBelow` through a vertical merge (title/row numbers) threw. Merge-time Word `{{#ds.rows}}` wrap across cells also threw.
- Fix: Word merge clones the prototype table row (and strips a same-row scan loop). Excel writes the loop onto the merge master cell and unmerges ranges that cross the insert before expanding.
- Officer: Stop F5, rebuild. Preview the invitation This-profile Word and Excel sanaw with all chips selected — **one row per person**, same as Wizany uzaltmak SANAW_WIZANY_UZTURMEK. No Re-Approve.
- Prevent: Do not inject DocxTemplater loops across Word table cells at merge; do not InsertRowsBelow through a vertical merge.
- Cross-skill: visa2026-resminamalar

### 2026-09-17 — Catalog Preview of a sanaw shows only one of five linked people (Word and Excel)

- Need: Invitation case 8/-1048 with 5 selected people. Catalog Preview of the yellow-marks Word/Excel *Daşary ýurt raýatlarynyň sanawy* listed one table row (first person). Officer tied it to DataScope (roster / header / both).
- Cause: Scan Generate wrote Excel `{{#ds.rows}}` but **Word loops were empty**. Without a table loop, merge copies the first person onto `{{.PLN}}`. Names that contain *sanaw* but do not *start with* Sanaw were also treated as one Word file per person. Header-only letters stay one document; şahsy / Forma 16 stay per person.
- Fix: Word Generate wraps the yellow table row with `{{#ds.rows}}`. Merge also injects that loop on already-saved Word sanaws. Excel injects a loop if the sheet has `{{.CODE}}` but no `#ds.rows`. Template names containing *sanaw* are list documents.
- Officer: Stop F5, rebuild. Preview the same This-profile Word/Excel sanaw with all five chips selected — **five data rows**. No Re-Approve required for the already saved file. New Create from yellow marks writes the loop on Continue. Header letters stay one page; şahsy stays one page per person.
- Prevent: Do not Preview a roster table without `{{#ds.rows}}`; do not treat *Dasary_…sanawy…* as a per-person Word form.
- Cross-skill: visa2026-resminamalar

### 2026-09-17 — Word roster Approve reused the existing catalog row (Excel was fine)

- Need: Invitation Word sanaw (`Dasary_yurt_rayatlarynyn_sanawy_cakylyk.docx`). Create from yellow marks saved, but This profile still showed the two older templates. Excel Create added a new card.
- Cause: Save matched NestedTemplates by name (case-insensitive), not kind. The Word roster filename matched `DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK`, so Approve overwrote that row. Excel used a free name.
- Fix: Create (not Review placeholders) allocates `…_2` / `…_3` when the name is taken. Done and Resminamalar select the new name.
- Officer: Stop F5, rebuild, Create from yellow marks again (not Review placeholders). Expect a third This profile row `…_2`, checked. Review placeholders still updates the same Word file.
- Prevent: Do not upsert Create-from-scan Word roster onto an existing nested name; uniquify. Overwrite only when remapping that catalog row.
- Cross-skill: visa2026-resminamalar

### 2026-09-17 — Approve did not show the template in Resminamalar selection

- Need: Invitation Word sanaw (`Dasary_yurt_rayatlarynyn_sanawy_cakylyk`). After **Approve — save to profile** (This profile only), the officer did not see it in the Resminamalar ZIP selection. It was on **This profile**; **Shared** correctly hid the private name.
- Cause: Approve reload unmounted the catalog (stale cache / lost pane). ZIP checkboxes did not add the new row. Shared is not the selection list for This profile only.
- Fix: Keep the catalog mounted, switch to **This profile**, check and highlight the saved name. Done offers **View in Resminamalar**.
- Officer: Stop F5, rebuild. Approve, Close (or View in Resminamalar). Look at **This profile**, not Shared. Shared catalog only if Upload **Save to = Shared catalog**.
- Prevent: Do not look for This-profile-only letters on Shared; do not drop new catalog keys from the ZIP selection.
- Cross-skill: visa2026-resminamalar

### 2026-09-17 — Word yellow cells missing from Review (`TUR`, `Ýok`, Mehmet Çirak)

- Need: Invitation Word sanaw. Yellow **Raýatlygy TUR**, footer **Mehmet Çirak**, and **Ýok** (border zone) must each be a Detected row with a `#`. Officers: yellow highlighted content should never be missed.
- Cause: Extractor only saw run highlight, not table-cell / paragraph **shading**. Consecutive yellow runs glued `müdiri` to the signatory name. Overlay skipped a mark when PDF text (`TUR`) was already used.
- Fix: Shaded Word cells are yellow (like Excel fill). Director title + name split into two spans. Table marks with no PDF hit still paint the cell box (`?v=tasmarks11`).
- Officer: Stop F5, rebuild, hard-refresh Review, **Analyze**. Expect a row for every yellow cell, including TUR, Ýok, and Mehmet Çirak (name separate from müdiri).
- Prevent: Do not require `w:highlight` on the run when the cell fill is yellow; do not drop overlay `#` when sample text is a duplicate.
- Cross-skill: none

### 2026-09-17 — Word sanaw placeholder guesses ignore column headers

- Need: Invitation Word *Daşary ýurt raýatlarynyň sanawy*. Overlay `#` matched Detected fields, but Özer/Arıta stayed unmapped and birth 4.2 Türkiye / 4.3 Iskenderun had no Short codes (Excel sanaw mapped PLN/PFNM and PDBT/PCBT/PBPL).
- Cause: Word Analyze used the previous table cell as the nearby label. Excel walks **up the column** to Familiýasy / Doglan senesi we ýeri. Compound birth then kept only PDBT.
- Fix: `ScanWordTableHeader` reads the caption in the same column. Word table yellows reuse Excel column-header inference. Birth caption prefers **PCBT** (country name), not PCBC (code).
- Officer: Stop F5, rebuild, **Analyze**. Familiýasy → **PLN**, Ady → **PFNM**, birth cell → **PDBT / PCBT / PBPL**.
- Prevent: Do not guess Word table yellows from the cell to the left; use the header row like Excel.
- Cross-skill: none

### 2026-09-17 — Word Review overlay numbers lose table order

- Need: Invitation Word sanaw (*Daşary ýurt raýatlarynyň sanawy*). Detected fields listed 1.1 / 2.1 / 13.1 in column order, but the left pdf.js pane numbered the data row 6–18 (Excel yellow sanaw stayed aligned).
- Cause: Excel Review paints used-range cell boxes. Word Review matched PDF text only (`kind: word` boxes were never sent), so short duplicates (`TUR`, `E`) took the first hit and visual LTR rewrote `#` away from OpenXML table order.
- Fix: Word table cells get a row/column grid; pdf.js uses those boxes as snap hints (still PDF text, not ghost %). Table-heavy files keep Detected `#` order. Script `?v=tasmarks10`.
- Officer: Stop F5, rebuild, hard-refresh Review. Overlay `#` on the Word table should match Detected fields the same way Excel does.
- Prevent: Do not number Word table marks from raw PDF item order; snap to the OpenXML cell and keep document `#` when most yellows sit in a table.
- Cross-skill: none

### 2026-09-17 — Foreign address country (PFAC) missing on sanaw 13

- Need: Invitation Excel *Daşary ýurtdaky salgysy*. Yellow `TUR, Pazara evin…` (or 13.1 TUR / 13.2 street). Analyze mapped **PFAD** (Foreign address) only; **PFAC** (country) was missing. Officer rule: a comma in the yellow means a second placeholder.
- Cause: PFAD was a single-span address (commas treated as street punctuation). Compound inference also forced PFAC onto the first street fragment. Sub-headers `13.1` / merged parent hid the real column caption.
- Fix: Leading ISO3 + comma → **PFAC** + **PFAD** (street commas stay on PFAD). Two columns: TUR-only → PFAC, street-only → PFAD. Header walk skips `13.1` and reads merged caption. Binder upgrades PFAD-only `TUR, …` cells.
- Officer: Stop F5, rebuild, **Analyze**. 13.1 **TUR** = PFAC, 13.2 street = PFAD.
- Prevent: Do not keep PFAD as one row when the yellow starts with a country code and a comma.
- Cross-skill: visa2026-user-report-templates
### 2026-09-17 — Hide 11.1 then Visa period (item) on 11.2 jumps to another code

- Need: Invitation Excel sanaw. Officer × unused compound **11.1**, then picked **Visa period (item)** on **11.2**. The row showed an unrelated placeholder (e.g. From City) instead of AVPRD.
- Cause: Removing 11.1 skipped that comma slot when rebuilding the parent token. Two remaining tokens no longer matched three label segments, so AlignParts parked AVPRD on the hidden first hole. The Add list then dropped AVPRD and the native select landed on the next option.
- Fix: Hidden and unmapped compound parts keep positional `{{.}}` holes (`BuildPositionalCompoundToken`). Add on 11.2 writes `{{.}}, {{.AVPRD}}, {{.}}`. Generate still strips holes.
- Officer: Stop F5, rebuild, hard-refresh Review. × leftover 11.1, then Add Visa period (item) on 11.2 — Short chip stays **AVPRD**.
- Prevent: Do not collapse hidden compound parts out of the parent token; slot order must stay 1:1 with the yellow commas.
- Cross-skill: none
### 2026-09-17 — Lock on compound 11.2 / 11.3 reset to Part

- Need: Invitation Excel sanaw Review. Officer picked **Visa period** / category on **11.2** / **11.3** (`1 (bir) aý`, `iki gezeklik`), then **Lock**. Rows showed orange **Part** instead of the Short codes.
- Cause: `ApplyPartCodes` flattened codes and dropped empty sibling slots. `AlignParts` then parked shape-unmatched codes (VPER/AVPRD/AVCAT) on the first empty segment, so the chosen sub-rows stayed unmapped. Lock only hides the Add UI → **Part**.
- Fix: `ApplyPartCodes` writes positional holes (`{{.}}`) for unmapped parts; `AlignParts` maps tokens 1:1 when token count matches segments; Generate strips holes via `StripEmptyPartMarkers`. Locked empty rows label **Unmapped**.
- Officer: Stop F5, rebuild, hard-refresh Review. Add placeholders on 11.2 / 11.3, then Lock — Short codes stay (AVPRD / AVCAT).
- Prevent: Do not flatten compound part codes before AlignParts; keep empty slots when the officer maps only some parts.
- Cross-skill: none
### 2026-09-16 — Business trip address (BTAD) showed as empty 11.1

- Need: Sanaw column *Iş saparyna barýan ýer* / *boljak salgysy*. Officers want one placeholder like **Address_FullAddress** but for the trip: region + city + street. Review showed **11.1** unmapped / Low while suggestions listed **100% BTAD**.
- Cause: Analyze already mapped **BTAD** (BusinessTripAddress_FullAddress). Review then treated address commas as combination parts and split the cell into **11.1/11.2…**, clearing each part’s token. Add-placeholder search also missed common typos (*adress*) and ADRS-shaped queries.
- Fix: Keep **ADRS/BTAD/ACADR/PFAD** as one Review row (IsSingleSpanAddressCode). Stronger Excel header keys + ChoiceList search (BTAD, *adress*, Address_FullAddress). Do not AI-escalate a High **Column header** address winner when ADRS/PFAC sit nearby. Merge text uses the same Region+City+street join as ADRS.
- Officer: Stop F5, rebuild, **Analyze**. Destination column → **BTAD** High as one row (not 11.1). Search BTAD, usiness trip address, or Address_FullAddress. Click the **100% BTAD** chip if still open.
- Prevent: Do not ExpandCompounds on full-address tokens; commas inside addresses are street punctuation.
- Cross-skill: visa2026-user-report-templates
### 2026-09-16 — Excel compound list OK but left badge stayed a single 6

- Need: After all-comma birth mapping, Detected showed `4.1/4.2/4.3` but the sheet preview still showed one `#6` on the cell.
- Cause: Compound parts shared one Excel cell box (stacked). pdf.js `applyReadingOrder` then rewrote badges to flat integers.
- Fix: Slice shared-cell boxes by part index. `applyReadingOrder` (`?v=tasmarks9`) keeps `N.1/N.2/N.3` and groups siblings.
- Officer: Stop F5, rebuild, hard-refresh Review (cache bust). Birth cell should show three borders `4.1 4.2 4.3` matching the list.
- Prevent: Do not flatten compound badge text when sorting marks on the page.
- Cross-skill: none
### 2026-09-16 — Excel comma birth cell lost 6.1/6.2/6.3 and PBPL

- Need: Sanaw Review. Yellow `05.04.1989, TUR, Fatih` should be one compound with **6.1 PDBT / 6.2 PCBT / 6.3 PBPL**. UI showed flat 4/5/6, Fatih unmapped, order scrambled.
- Cause: `SplitCompoundSegments` only took two comma parts unless InnerSeparator `/` was present. Visual `Renumber` after LTR overlay work flattened `N.1` labels to `4,5,6` and could reorder siblings.
- Fix: Split all-comma cells to the profile slot count. `Renumber` / visual / reading-box order keep compound siblings as `N.1/N.2/N.3` in segment order. PBPL shape no longer steals full names.
- Officer: Stop F5, rebuild, Analyze. Birth cell shows **6.1 / 6.2 / 6.3** with Date, Country, Place.
- Prevent: Do not flatten compound OrderLabels when re-numbering for page reading order.
- Cross-skill: none
### 2026-09-16 — Yellow digits `15` and `20` not identified next to `(on bäş)` / `(ýigrimi)`

- Need: Business-trip Yuztutma Review. Yellow ink on `15` and `20` had no Detected rows / no `#` (only `on bäş` → TPCTX and `ýigrimi` → BTDCTX).
- Cause: After BT count tokens, Analyze often kept the Turkmen words and skipped the printed digits (same class as `1`/`2`). Review pdf.js also folded spaces so `sanawdaky15` failed the digit boundary check, so even mapped digits got no overlay `#`.
- Fix: `AttachCountPairSpans` completes `N (words)` pairs for any digit length. pdf.js `?v=tasmarks8` treats letter↔digit edges as boundaries and rejects `20` inside `2026`.
- Officer: Stop F5, rebuild, hard-refresh Review, **Analyze** (not Remap unmarked first). Expect `15` → TPCNT, `on bäş` → TPCTX, `20` → BTDCNT, `ýigrimi` → BTDCTX, with `#` on each.
- Prevent: Do not treat word-form yellow alone as the full count pair; do not strip spaces without keeping digit boundaries.
- Cross-skill: visa2026-user-report-templates

### 2026-09-16 — Yellow digits `1` and `2` not identified next to `(bir)` / `(iki)`

- Need: Business-trip letter Review. Yellow `1` before `(bir)` and `2` before `(iki) gün` had no Detected rows (only `bir` / `iki`). Overlay # sat on the digit while the row was the words.
- Cause: Word often yellows the count-words, not the digits. When digits were added, mapping used the rest of the sentence, so later `gün` stole the first pair (or the isolated digit had no count context).
- Fix: Extract a leading `1`/`2` before yellow `bir`/`iki`. Clip each pair’s caption so person `1 (bir)` stays **TPCNT**/**TPCTX** and duration `2 (iki) gün` is **BTDCNT**/**BTDCTX**. Pair a lone digit with the next yellow words.
- Officer: Stop F5, rebuild, **Analyze** (do not Remap unmarked until `1` and `2` appear). Detected: `1` → TPCNT, `bir` → TPCTX, `2` → BTDCNT, `iki` → BTDCTX.
- Prevent: Do not treat the whole sentence as context for an isolated count mark. Do not Remap unmarked to invent missing digits.
- Cross-skill: visa2026-user-report-templates

### 2026-09-16 — Isolated `2` next to `iki` dropped; From Region merge empty

- Need: Business-trip letter. Yellow `2` before `(iki) gün` had no Detected row. Preview **BTFRG** (#8 From Region) printed blank.
- Cause: Isolated digit after person `1 (bir)` was classified as a second TPCNT and swallowed (`IsYellowTextFullyMapped`). BTFRG used `FromRegion` / `FromCity.Region` only — City.RegionName and FromRegionId were often unset.
- Fix: Later isolated `2` / `iki` with `gün` → **BTDCNT**. Do not drop standalone count yellows. BTFRG also reads `FromCity.RegionName`. Saving From city reloads Region onto FromRegion.
- Officer: Stop F5, rebuild, Analyze. `2` next to `iki` → **BTDCNT**. Fill From region (or From city). Preview **BTFRG** should print e.g. `Mary welaýatynyň`.
- Prevent: A second isolated digit in a person-count sentence is duration when `gün` is nearby. Origin region merge must not require FromRegion nav to be loaded.
- Cross-skill: visa2026-user-report-templates, visa2026-application-profile

### 2026-09-16 — Add list missed Purpose on ApplicationProfileInstance

- Need: Maksady yellow (Aşgabat / Türkiye ilçihanasy). Officer could not find the instance **Purpose** property in Add placeholder.
- Cause: **BTPRP** existed as “Business trip purpose” in the Business trip group, not the BO display name **Purpose**.
- Fix: Label **Purpose**. RelatedBo Application. Search **Purpose** / **Maksady** / **BTPRP**.
- Officer: Stop F5, rebuild, Analyze. Filter `Purpose` or `Maksady`. Pick **Purpose — BTPRP** from the **Application** group.
- Prevent: Add-list English names must match XafDisplayName on the case field.
- Cross-skill: visa2026-user-report-templates

### 2026-09-16 — Ghost `#` boxes in whitespace; body yellows unnumbered

- Need: After LTR re-number, Review showed empty 3–10 / 14 in the margin, 11–13 stacked on the migration block, and no `#` on Mary / dates / Maksady yellows.
- Cause: Word paragraph % boxes were painted when PDF text was rejected as “too far”. Visual sort then numbered those ghost boxes. Body hits never placed.
- Fix: Word overlays are PDF text only (longest label first). No Word-geometry fallback. Visual order uses placed text boxes; unplaced rows stay at the end. Script `?v=tasmarks7`.
- Officer: Stop F5, rebuild, hard-refresh Review. Numbers sit on yellow text, increasing left-to-right on each line.
- Prevent: Do not draw Word Review marks from estimated paragraph %.
- Cross-skill: none

### 2026-09-16 — Review numbers 10 12 11 14 on one line

- Need: Business-trip letter. Yellows on one sentence read `10 12 11 14` instead of `10 11 12 13 14`.
- Cause: Overlay `#` used Detected-list order. Duplicate `welaýatynyň` put From Region on Ahal, so left-to-right badges jumped.
- Fix: After placing, re-number by page position (same-line left→right). Detected rows follow that sequence. Word geometry does the same before PDF paints. Script `?v=tasmarks6`.
- Officer: Stop F5, rebuild, hard-refresh Review. The numbers on a line should increase left to right.
- Prevent: Do not leave overlay numbers in FieldPlan order when boxes sit in reading order.
- Cross-skill: none

### 2026-09-16 — Review #12 missing and some numbers on the wrong yellow

- Need: Business-trip letter Review. Detected row 12 (From City / Mary etrabyndan) had no `#` on the left page. Other numbers sat on the wrong highlights (duplicate `12.02.2026`, a lone `2`).
- Cause: pdf.js walked sample text with one advancing cursor. A later label consumed text after From City, so #12 was skipped. Short/duplicate labels always took the first PDF hit. Header yellows were numbered after body.
- Fix: Unused-occurrence match + token boundaries + Turkmen fold. Word payload sends paragraph boxes (`ScanWordPreviewMarkGeometry`); snap to nearby text, else show the box. Header ranks before body. Script `?v=tasmarks5`.
- Officer: Stop F5, rebuild, hard-refresh Review. Every Detected row should have a `#` on its yellow. Click row 12 — the From City mark highlights.
- Prevent: Do not place Word Review marks by first-match sample text only.
- Cross-skill: none

### 2026-09-16 — Add list missed From Region / From City / To Region / To City

- Need: Officer could not find those ApplicationProfileInstance properties in Review Add placeholder (Mary welaýatynyň mark).
- Cause: Codes existed as **BTFRG** **BTFCT** **BTTRG** **BTTCT** but English labels said “district” / genitive, and they sat in the Business trip group.
- Fix: Labels are exactly **From Region**, **From City**, **To Region**, **To City**. RelatedBo is Application. ExpandTerms maps FromCity / ToCity / FromRegion / ToRegion.
- Officer: Stop F5, rebuild, Analyze. Filter `From Region`, `From City`, `To Region`, `To City`, or the short codes. Pick from the **Application** group.
- Prevent: Add-list labels must match the BO display names officers type, not only the Turkmen case form.
- Cross-skill: visa2026-user-report-templates, visa2026-application-profile

### 2026-09-15 — Business trip letter + sanaw placeholders

- Need: İş Saparyna Gitmek / Gelmek cover letter and sanaw. Add list had no trip dates, duration words, from/to region-district, purpose, or destination address.
- Cause: Merge properties existed (`BusinessTripStartDateText`, From/To case forms) but catalog had no short codes. Overview Region/City is destination; FromCity was hidden. `2 (iki) gün` was guessed as TPCNT.
- Fix: **BTSD** / **BTED** (keep `-den`/`-ne` in Word). **BTDCNT** / **BTDCTX**. **BTFRG** / **BTFCT** from FromCity; **BTTRG** / **BTTCT** from Region+City. **BTPRP** = Purpose. Sanaw **BTAD**. Show From city on BT cases. Excel *Iş saparynda boljak salgysy* → BTAD.
- Officer: Stop F5, rebuild, Analyze. Letter: dates `-den`/`-ne` → BTSD/BTED; `gün` → BTDCNT/BTDCTX; Maksady → BTPRP. Sanaw last column → BTAD. Fill From city (origin) plus Region/City (destination).
- Prevent: Do not reuse ADAT/TPCNT/RGEL for trip duration or Maksady. Hotel type is not a template token.
- Cross-skill: visa2026-user-report-templates, visa2026-application-profile

### 2026-09-14 — Cancel invitation sanaw missing CINB/CISB/CIEB

- Need: Sanaw-çakylygy ýatyrmak. Add list had INVN/INVS/INVE only. Officer searched Cancel Invitation AS Numbers / issued dates / expiration dates.
- Cause: Catalog had current-only invitation tokens, not the cancel stack.
- Fix: **CINB** (Çakylygyň belgisi / AS numbers), **CISB** (resmileşdirilen), **CIEB** (möhleti). Current+Previous, numbered when 2+. Excel headers map those columns.
- Officer: Stop F5, rebuild, Analyze. Filter `CINB` / `Cancel invitation AS numbers`. #10 → CINB, issued → CISB, möhleti → CIEB. INVN stays current-only.
- Prevent: Do not use INVN for a cancel Last-N stack.
- Cross-skill: visa2026-user-report-templates

### 2026-09-14 — Cancel invitation count missing from Add list

- Need: Çakylyk we iş rugsatnamany ýatyrmak letter. Officer could not find cancel invitation count / words in Add placeholder (CVCNT/CWCNT were listed).
- Cause: Catalog had no CICNT/CICTX. Guessing only knew visa/WP *ýatyrmak*.
- Fix: Catalog **CICNT** / **CICTX** (`CancelInvCount` = distinct invitation headers). Nearby *çakylyk*+*ýatyrmak* maps isolated `3` / `üç`. WP earlier in the sentence stays CWCNT.
- Officer: Stop F5, rebuild, Analyze. Filter `CICNT`, `CICTX`, `cancel invitation`, or `çakylyk`. *çakylygyny ýatyrmak* → CICNT/CICTX.
- Prevent: Do not reuse CWCNT/TPCNT for invitation-cancel counts.
- Cross-skill: visa2026-user-report-templates

### 2026-09-14 — Single WP stack has no 1) prefix

- Need: *Iş Rugsatnamany Ýatyrmak* with one valid WP must not print `1)`.
- Cause: Join numbered every line, including a single value.
- Fix: Ordinals only when two or more lines. Locations still never numbered.
- Officer: Stop F5, rebuild, Preview. Two linked WPs still show `1)` `2)`.
- Prevent: Do not prefix a one-line cancel block.
- Cross-skill: visa2026-user-report-templates

### 2026-09-14 — CWLB must not print 1) 2)

- Need: Work-permit location block unnumbered. Other stacked blocks keep order prefixes.
- Cause: CWLB used the numbered join.
- Fix: Locations join without `1)` / `2)`.
- Officer: Stop F5, rebuild, Preview. Hereket edýän çägi is the place name only.
- Prevent: Do not number CWLB.
- Cross-skill: visa2026-user-report-templates

### 2026-09-14 — Cancel stack Preview prints 1) 2)

- Need: Sanaw blocks should look like the ministry sample (`1)` / `2)` on each stacked value).
- Cause: Join was newline-only.
- Fix: `JoinVisaFieldLines` prefixes `1) `, `2) ` on CV* and CW* blocks.
- Officer: Stop F5, rebuild, Preview. One linked document still prints `1)`.
- Prevent: Numbering is merge, not yellow sample text.
- Cross-skill: visa2026-user-report-templates

### 2026-09-14 — Add filter missed CWLB for Hereket / Work Permitted Locations

- Need: After WPLC/CWLB existed, Add still missed them when typing `hereket` or `Work Permitted Locations`.
- Cause: tk-TM labels did not include the sanaw column caption; CWLB English was “Cancel work permit locations”.
- Fix: Labels include *Hereket edýän çägi*. Search expands `hereket` / `work permitted` to the locations codes.
- Officer: Stop F5, rebuild, Analyze. #9 → **CWLB**. Filter `CWLB` / `hereket` / `Work Permitted Locations`.
- Prevent: Catalog tk-TM should use the printed column caption officers type.
- Cross-skill: visa2026-user-report-templates

### 2026-09-14 — Cancel WP Excel #9 Hereket edýän çägi had no placeholder

- Need: Sanaw-Wiza we Iş rugsatnamany ýatyrmak. Yellow 9 *Hereket edýän çägi* = `WorkPermitItem.WorkPermittedLocations` (Aşgabat şäheri). Add list had no locations code.
- Cause: Merge property existed; catalog had no short code, so Review could not list it.
- Fix: **WPLC** (current) and **CWLB** (stacked cancel). Column header *Hereket edýän çägi* → CWLB.
- Officer: Stop F5, rebuild, Analyze. #9 should map CWLB. Or Add `Work permitted locations — WPLC` / `CWLB`.
- Prevent: Catalog every required WP field officers map on cancel sanaw.
- Cross-skill: visa2026-user-report-templates

### 2026-09-14 — Cancel WP Excel AS-№ vs Tassyknama

- Need: Sanaw *AS-№* = `WorkPermitItem.ASNumber`; *Tassyknama belgisi* = `WorkPermitNumber`. Two linked WPs stack like dates.
- Cause: No AS cancel block. Excel *AS-№* could match RNUM via `№`.
- Fix: Catalog **CWAB**. Column *AS-№* / *AS-No* → CWAB (before generic №). *Tassyknama* → CWNB.
- Officer: Stop F5, rebuild, Analyze. AS-№ → CWAB. Tassyknama → CWNB. Preview stacks COO lines from Last-N 2.
- Prevent: Do not use the old XtraReports map (AS-№ was WorkPermit_Number).
- Cross-skill: visa2026-user-report-templates

### 2026-09-12 — Review gap #10 could not take a placeholder

- Need: Wiza we iş rugsatnamany ýatyrmak. Yellow 10 Mehmet Çırak was a Gap. Officer could not Add `CHFN` (or any code).
- Cause: Merger turned unmapped Office yellows (null token, but with a write address) into Gaps. Review hid Add on gaps (`CanRemap` excluded `IsGap`). ApplyTokens only searched Fields, so a pick would no-op.
- Fix: Unmapped yellows with `SourceRegion` stay Detected fields. Gaps still show Add and promote to a field when a Short code is chosen.
- Officer: Stop F5, rebuild, restart, Analyze. Click #10 — Filter `CHFN` / signatory / Mehmet, Add placeholder.
- Prevent: Do not convert Office yellows that have a span address into Gaps.
- Cross-skill: none

### 2026-09-12 — Split yellow `3` / `üç` left CWCTX out of the letter

- Need: Wiza we iş rugsatnamany ýatyrmak. After `3` the second highlight `üç` was unidentified. Officer could not put CancelWPCountText in the file.
- Cause: CountWithWords only matches `3 (üç)` in one span. Word highlighted the digit and the words separately. Following caption kept only form parenthetical lists, so the letter text after `üç` (`iş rugsatnamasyny ýatyrmak`) was dropped.
- Fix: Isolated digit → CWCNT / isolated Turkmen words (`üç` → `uc`) → CWCTX when nearby is a count caption. Following caption keeps the rest of the sentence. Add list shows `Label — CWCTX (CancelWPCountText)`.
- Officer: Stop F5, rebuild, restart, Analyze again. `3` → CWCNT. `üç` → CWCTX. Filter Add with `CancelWPCountText` or `CWCTX` if a mark is still empty.
- Prevent: Do not require `N (words)` in one yellow for letter counts.
- Cross-skill: visa2026-user-report-templates

### 2026-09-12 — Cancel visa+WP letter WP count mapped to TPCNT/CVCNT

- Need: Application for cancelling visa and work permit. Yellow `1 (bir)` next to *iş rugsatnamasyny ýatyrmak* is work permits to cancel, not persons and not visas.
- Cause: `CountWithWords` only distinguished person vs visa-cancel. No CWCNT. Clone could copy TPCNT onto the WP `1 (bir)`.
- Fix: *yatyr*+*rugsat* → **CWCNT**/**CWCTX**. Visa wins when *wiza* is first; person *raýat* still TPCNT. Do not clone TPCNT onto a document-cancel nearby. Context is the text after each count so a trailing *ýatyrmak* is visible.
- Officer: Stop F5, rebuild, restart, Analyze the visa+WP letter. Person → TPCNT. Visa → CVCNT. WP → CWCNT.
- Prevent: Do not map every *ýatyrmak* count to CVCNT.
- Cross-skill: visa2026-user-report-templates | visa2026-application-profile

### 2026-09-12 — Officer confirmed PBPL city-only on cancel-visa sanaw

- Need: 8/-1307 after city-only change. Preview birth place was blank, then officer confirmed **fixed**.
- Cause: Empty or country-only `Person.BirthPlace` prints blank (country stays on PCBT). Yellow Kahramanmaraş is sample text, not the case roster.
- Fix: No further code. `PersonBirthPlaceText.CityOnly` already in use.
- Officer: Confirmed good. Fill Person **Birth place** with the city when PBPL is blank.
- Prevent: Do not put CountryOfBirth back onto PBPL to fill a blank.
- Cross-skill: visa2026-user-report-templates

### 2026-09-12 — PBPL showed birth country instead of city

- Need: 8/-1307 SANAW-WIZANY YATYRMAK. Review 4.2 PCBT = Türkiye, 4.3 PBPL = Kahramanmaraş. Preview Doglan column printed Türkiye for the place token. Review SAMPLE for PBPL was `Türkiye/Gaziantep`.
- Cause: Catalog example was country/city. Merge printed `Person.BirthPlace` raw, so `Türkiye/Gaziantep` or a country-only BirthPlace looked like PCBT.
- Fix: `PersonBirthPlaceText.CityOnly` on `Person_BirthPlace`. Example is now Kahramanmaraş. PCBT stays country.
- Officer: Stop F5, rebuild, Preview the same Excel. Place = city (Kahramanmaraş / Gaziantep). Country stays on PCBT. Fill Person Birth place with the city if Preview is still blank.
- Prevent: Do not use CountryOfBirth as a BirthPlace fallback. Do not keep a country prefix on the PBPL example.
- Cross-skill: visa2026-user-report-templates

### 2026-09-12 — Review Add list dropped codes used on other templates

- Need: Same library on every Create from yellow marks / Review placeholders. Codes that work on invitation or sanaw sometimes missing on cancel-visa or on a header letter.
- Cause: `GetSet` hid packs the profile tile turned off (travel, invitation, WP, …) and hid Row tokens when the saved file was ApplicationHeader. Opening a letter then hid PFN/PBPL/EGLV even though other templates use them.
- Fix: Scan uses `OfferFullLibrary` (`ScanPlaceholderLibrary.Query`). Convert stays pack- and scope-gated. Excel still drops images.
- Officer: Stop F5, rebuild, restart, hard-refresh Review. Filter `PBPL` / `EGLV` / `INVN` — they stay in Add placeholder on every profile and letter/sanaw.
- Prevent: Do not rebuild the Review list from the saved Header/People scope. Infer that scope only on Approve.
- Cross-skill: visa2026-user-report-templates

### 2026-09-12 — Review could not find birth place (PBPL)

- Need: Wizany Ýatyrmak sanaw Review. Officer opened Add placeholder and could not find birth place (Doglan ýeri). List showed country-of-birth style codes.
- Cause: Catalog already has **PBPL** / `Person_BirthPlace`. Native `<select>` text was `PBPL — Birth place`, so typing `birth` does not jump to it. Filter `birthplace` (one word) and `doglan yeri` (no ý) also missed.
- Fix: Options are `Birth place — PBPL`. Filter folds diacritics and compact words (`birthplace`, `doglan yeri`). Cancel-visa sanaw row dict includes `Person_BirthPlace` / `Person_CountryOfBirthTm`.
- Officer: Stop F5, rebuild, restart, hard-refresh Review. Filter `birth place` or `PBPL`, then Add. City is PBPL; country name is PCBT.
- Prevent: Do not tell officers to type the Short code first in the native select.
- Cross-skill: visa2026-user-report-templates

### 2026-09-12 — Review could not add a placeholder on unidentified yellows

- Need: After the searchable-picker change, cancel-visa sanaw Review row 4 stayed **Part**. Officer could not assign a library code to an unidentified yellow.
- Cause: Native `<select>` was replaced with a custom list. That list only opened on focus and sat inside `overflow: auto`, so it never popped over the table. Unmapped rows also hid Add until the row was selected, so they showed **Part**.
- Fix: Restore Filter + **Add placeholder…** `<select>` (browser list escapes overflow). Search still filters options (`education`, `speciality`). Unmapped unlocked rows always show Add. Education codes stay in the cancel-visa set.
- Officer: Stop F5, rebuild, restart, hard-refresh Review. Unidentified yellows show **Add placeholder…**. Filter `education` / `EGLV` / `EGIN` / `EGSP`, then pick from Add.
- Prevent: Do not replace the Review Add `<select>` with an in-cell custom dropdown.
- Cross-skill: visa2026-user-report-templates

### 2026-09-12 — Cancel-visa Review could not find education placeholders

- Need: Wizany Ýatyrmak sanaw Review. Officer could not find education level / institution / specialty (`EGLV` / `EGIN` / `EGSP`). Typed in the native Add-placeholder `<select>`.
- Cause: `PersonEducation` pack was gated by `AllowsPersonEducation && RequirePersonEducation`. Cancel-visa hides the People & links Education tile, so those codes were not in `PlaceholderSet.Allowed`. Native `<select>` typeahead only matches the start of `EGLV — Education level`, so typing `education` or `speciality` never hit the option text.
- Fix: Always offer PersonEducation tokens for mapping. Hydrator fills `CurrentEducation` from a linked Education or `PersonCurrentItems.GetCurrentEducation`. Review picker is a searchable list (`education`, `speciality`→specialty, pack name). Do not turn the Education tile back on.
- Officer: Stop F5, rebuild, restart, hard-refresh Review. Select the Hünäri/bilimi yellow. Search `education` / `EGLV` / `EGIN` / `EGSP` / `speciality`. Preview fills from the person’s current education even when Education is not linked.
- Prevent: Do not hide catalog Education tokens when the People & links tile is off. Do not rely on native `<select>` search.
- Cross-skill: visa2026-user-report-templates | visa2026-application-profile

### 2026-09-12 — Cancel-visa Review 6/7 CVCNT sample stayed 1

- Need: 9/-001. Two valid visas linked. Review placeholders 6 (`CVCNT`) and 7 (`CVCTX`) still showed sample `1` / `bir`. Preview printed `1 (bir)`.
- Cause: Mapping was already CVCNT. Merge counted CurrentVisa + NextVisa (future start), not Last-N pins.
- Fix: Merge count is distinct Visa resolved links (user-report-templates). Short codes unchanged. No Re-Approve.
- Officer: Stop F5, rebuild, Preview Ýüztutma — `2 (iki)`.
- Prevent: Do not remap CVCNT when the sample is wrong; fix the count.
- Cross-skill: visa2026-user-report-templates | visa2026-resminamalar

### 2026-09-11 — Cancel-visa letter visa count mapped to TPCNT

- Need: Wizany Ýatyrmak cover letter. Yellow `1 (bir)` next to *daşary ýurt raýaty* is person count; the second `1 (bir)` next to *wizasy ýatyrmak* is visas to cancel. Scan always emitted TPCNT/TPCTX.
- Cause: `CountWithWords` always mapped to TPCNT. Catalog had no CVCNT. Header merge dict omitted `CancelVisaCount` (property already existed on the instance). Duplicate-label clone copied the first TPCNT onto the second `1 (bir)`.
- Fix: Catalog **CVCNT** / **CVCTX** → `CancelVisaCount` / `CancelVisaCountText`. Header dict + Enrich. Scan uses the window after each count (or following caption): *raýat* → TPCNT; *wiza*+*yatyr* → CVCNT. Do not clone TPCNT onto a visa-cancel nearby.
- Verify: `Resolve_CancelVisaLetter_MapsPersonThenVisaCount`, isolated nearby tests, catalog RelatedBo, header dict keys.
- Officer: Stop F5, rebuild, restart. Analyze the cancel-visa letter. Person `1 (bir)` → TPCNT/TPCTX. Visa `1 (bir)` → CVCNT/CVCTX. Preview should fill both from the case (visas = CurrentVisa + NextVisa per roster line).
- Prevent: Do not treat every `N (words)` as person count. Do not add a second NotMapped property — `CancelVisaCount` already exists.
- Cross-skill: visa2026-user-report-templates

### 2026-09-11 — Passport-change sanaw Review/Preview only latest passport

- Need: Case 5/-814 Wizany KP-i Täze Pasporta Geçirmek. Excel DAŞARY ÝURT RAÝATYNYŇ SANAWY has Kiçirak (previous booklet) then Täze (new). Preview filled only the last valid passport.
- Steps attached: Review + Resminamalar Preview
- Cause: Both yellow tables mapped to `PPN`. Excel generator expanded the first `{{#ds.rows}}` only; the second table was merged with `rowData: null`. Loop planner used the minimum yellow row. People & links already hydrates `CurrentPassport` + `PreviousPassport`.
- Fix: Kiçirak Analyze → `PRPN` family. Merge expands both stacked tables; Kiçirak `{{.PPN}}` overlays previous booklet so already-approved files fill without Re-Approve. Close-marker rows that hold titles are stripped, not deleted.
- Verify: `PassportChangeSanawSectionTests`, `ExcelReportPassportChangeSanawTests`, `Resolve_maps_kicirak_passport_column_to_previous_short_codes`, two-row loop planner. Filter passed 49.
- Officer: Stop F5, rebuild, restart. Preview the same Excel. Kiçirak = old booklet, Täze = new. Link two passports on the person. New Analyze should show `PRPN` on Kiçirak.
- Prevent: Do not remap `RPPN`. Do not delete the Täze title row as `{{/ds.rows}}`.
- Cross-skill: visa2026-user-report-templates | visa2026-resminamalar

### 2026-09-11 — Excel Review `#` squares sat at the page corner

- Need: Case 3/-15202 Çakylygy üýtgetmek. Review of `Sanaw-cakylygy-uytgetmek.xlsx` put every numbered square at the top-left of the white PDF page, not on the yellow sanaw cells.
- Steps attached: Review screenshot
- Cause: Left pane is Excel→PDF + pdf.js. Marks were placed by matching sample text (`1`, `TUR`, dates) in page text. Short labels hit the first PDF text item at the origin. Excel cells also had `Box = FullPage` and no cell geometry in the overlay payload.
- Fix: `ScanExcelPreviewMarkGeometry` maps each yellow cell onto the first-sheet used range. Review payload sends `l/t/w/h` (+ aspect). pdf.js (`?v=tasmarks4`) places those marks on the printed table cluster, or on a Fit-to-width page frame when text positions are junk. Word letters still use label match.
- Verify: `ScanExcelPreviewMarkGeometryTests` (4). Full `TemplateScan` filter: 276 passed; 6 unrelated guessing tests already failing.
- Prevent: Do not locate Excel Review marks by sample text.
- Cross-skill: none

### 2026-09-11 — Change-invitation letter Invitation group confirmed

- Need: Çakylygy üýtgetmek Ýüztutma. Officer confirmed after rebuild.
- Cause: Invitation number / dates were row-only (`INVN`) or missing (`INVS`, `INVE`).
- Fix: Invitation group Header+Row codes. One invitation per case in the paragraph (`{{ds.INVN}}` `{{ds.INVS}}` `{{ds.INVE}}`). Several people on that invitation stay one letter.
- Officer: Already verified. Review Add placeholder → **Invitation**.
- Cross-skill: visa2026-user-report-templates | visa2026-resminamalar

### 2026-09-10 — Yuztutma catalog Preview repeated one page per person

- Need: Case 9/-1444 Hasapdan Çykarmak. Resminamalar **This profile** Preview of **YUZTUTMA-HASAPDAN ÇYKARMAK** showed 3 identical letter pages (one per Cengiz / Mustafa / Izzet). Officer expected one letter for the template.
- Cause: Preview always passes selected people (`RosterPerson`). Cover letters still had `RootBoType` ApplicationItem (Scan default Both, and Re-Approve did not sync RootBo). `UsesPerItemWordOutput` then merged one Word file per person and PDF-stitched them.
- Fix: Header-only Word (`{{ds.*}}` / `ds.*`, ignore IMAGE) emits one document even on a roster context. Nested catalog `ApplicationHeader` does the same. Approve / Re-Approve now writes `RootBoType` from DataScope. Forma 16 / şahsy `.PFN` stay per person; Sanaw `#ds.rows` stays one list.
- Officer: Stop F5, rebuild, restart. Preview the same Yuztutma row — **one page**. No Re-Approve required for this Preview fix. SANAW / FORMA 16 should still be a list or one form per person.
- Cross-skill: visa2026-resminamalar | visa2026-user-report-templates

### 2026-09-10 — This profile only letter appeared on Shared

- Need: Case 3/-308 Hasapdan Çykarmak. Create from yellow marks **Save to** = This profile only (`…_2`). After Approve the letter was on the **Shared** tab with Preview OFF. This profile count also went up.
- Cause: Approve always writes a tenant `UserReportTemplate` (needed for merge). Shared lists every active user template, so a this-profile-only name showed as a library row other cases can Include.
- Fix: Shared catalog skips names that exist only as This-profile nested rows. This-profile Approve does not overwrite an existing Shared master of the same name.
- Officer: Stop F5, rebuild, restart. Open Resminamalar → **This profile**. `_2` stays there. Shared should not list it. Already-saved `_2` does not need Re-Approve.
- Cross-skill: visa2026-resminamalar

### 2026-09-10 — Analyze crashed: Specified part does not exist in the package

- Need: Create from yellow marks Analyze on a ministry Word (Hasaba / Yuztutma). Visual Studio stopped on `InvalidOperationException` in `WordTemplateAddressing.EnumerateParagraphs` → `MainDocumentPart.Document`.
- Cause: Open XML loads every related part when reading the body. The `.docx` ZIP still listed a relationship (image, header, styles, mail-merge, …) whose file was not in the package. Ingest OCR opened that Word and threw before yellow marks were read.
- Fix: `WordOpenXmlPackage.EnsureLoadable` drops relationships and content-type overrides that point at missing parts, then Analyze / Generate open that copy. OCR and yellow extract no longer throw on a dangling part.
- Officer: Stop F5, rebuild, restart. Analyze the same `.docx` again. Yellow marks should list. If Word still will not open, Save As `.docx` in Microsoft Word and Analyze that copy.
- Cross-skill: visa2026-resminamalar

### 2026-09-10 — Cover letter catalog Preview left AFNUM/ADAT/MSRV/TPCNT blank

- Need: Yuztutma Hasaba Almak case 10/-12521. Wizard Generate showed `{{ds.AFNUM}}` `ADAT` `MSRV` `TPCNT` `ACPOS` `CHFN`. Catalog Preview filled only signatory (Demo mudir orunbasary / Ali Demir); number, date, addressee, and count were empty. Download name was `… ERDOGAN Arzu.docx`.
- Cause: Create from yellow marks defaults DataScope Both → RootBo ApplicationItem → per-person Word. Merge looked up FullApplicationNumber / MigrationService_NameTm / TotalPersonCount on the roster line (those properties are not there). ACPOS/CHFN exist on the line so they filled. Wizard Preview is the tokenized copy (expected).
- Fix: Item-root merge seeds the application header dictionary and falls back to the instance for missing keys (AFNUM, MSRV, TPCNT, …). Approve infers ApplicationHeader when Review tokens are only `{{ds.*}}` (photo tokens do not force per-person). Official-letter guessing maps addressee → MSRV, şahamçasynyň müdiri → ACPOS, following name → CHFN.
- Officer: Stop F5, rebuild, restart. Preview the same Yuztutma row — number, date, migration service, and person count should fill. Re-Approve once so the catalog row is Application header (one letter for the case, not a file named after Arzu).
- Cross-skill: visa2026-resminamalar | visa2026-user-report-templates

### 2026-09-10 — Review placeholders lost photo mapping after Open yellow file

- Need: After uploading the yellow `_F16` Word, Review showed the letter and text Short codes, but **Person photo** / `{{IMAGE:PPH}}` was gone. The sample portrait stayed on the page. Continue left catalog Preview as a static picture.
- Cause: Photos are Word drawings, not yellow highlighter. Restore only extracted yellows. Snapshot often stored IMAGE as a WordSpan (`{{IMAGE:PPH}}` on the mapped file) or a stale drawing address. Unused non-drawing IMAGE rows were dropped. Generate bound yellows only, so the portrait was never replaced.
- Fix: Restore merges live body portraits and re-pins IMAGE/PPH onto them (exact drawing, then leftover pictures). Generate binds IMAGE tokens to drawings only — never onto text yellows. Yellow Word without a portrait does not invent PPH.
- Officer: Stop F5, rebuild, restart. Review placeholders → Open yellow file. Detected fields should list **Person photo** (`PPH`). Continue should replace the sample portrait with `{{IMAGE:PPH}}`. Re-Approve `_F16` once so the snapshot stores the live drawing.
- Cross-skill: visa2026-resminamalar | visa2026-user-report-templates

### 2026-09-10 — Review placeholders showed mapped tokens, not the yellow letter

- Need: Review placeholders on `_F16`. Left page showed `{{.PFN}}` / `{{.PNAT}}` in the form (photo still there), Detected fields labels were tokens. Officer expected the yellow-highlighted sample (Yerkin Didem, dates).
- Cause: No usable yellow `SourceFile` (never stored, or Approve wrote the mapped catalog copy into SourceFile). Review ingested `TemplateFile`. Labels came from token spans.
- Fix: Open Review only treats SourceFile as yellow when it still has highlighter. Approve writes SourceFile only from yellow bytes. Mapped-only Review shows a banner; **Upload different file** then **Open yellow file** restores saved Short codes onto the yellow letter.
- Officer: Stop F5, rebuild, restart. Review placeholders → Upload different file → pick the yellow `_F16` Word → Open yellow file. Left page should show highlighted samples. Continue / Approve stores that yellow file for next time.
- Cross-skill: visa2026-resminamalar

### 2026-09-10 — Approve save disabled after placeholder edits (config lock)

- Need: Review placeholders on `_F16.00` (Hasaba, profile locked). Officer changed Short codes, Continue, Preview looked right. **Approve — save to profile** stayed grey. Hint said to rename.
- Cause: Config lock blocked overwrite of an existing catalog name (`IsLockedExistingOverwrite`). Review placeholders is how officers fix the Word file on a live case, so forcing a copy left the old `_F16.00` wrong.
- Fix: Approve on the same name updates `TemplateFile` / `SourceFile` / `ReviewPlanJson` only. Catalog scope and applicability stay locked. Rename still creates a new catalog row.
- Officer: Stop F5, rebuild, restart. Review placeholders → Continue → Approve on `_F16.00` should save. Change the name only if you want a second catalog copy.
- Cross-skill: visa2026-application-profile | visa2026-resminamalar

### 2026-09-10 — Continue then Back shifted locked placeholders

- Need: `_F16` Review placeholders. Locked rows. Continue → Preview (`{{.AVCAT}}, {{.PFN}}, {{.PPN}}` on the visa line) → **Back to field list**. Short codes looked shifted (11 / 11.2 / 11.3 compound, PFN duplicated onto the wrong yellow) even though rows stayed locked.
- Cause: Mapped-file overlay treated a comma-joined library cluster as one span and pinned all three codes on the first visa yellow. Generate re-extracted yellows and could steal another mark by sample text when Start/Length drifted. Back did not keep a clone of the Review list.
- Fix: Overlay / snapshot restore split a cluster 1:1 onto sibling yellows in the same paragraph (keep the compound only when that cell is one yellow). Duplicate PFN is allowed. Generate binds by exact OpenXML key, then the same paragraph/cell slot — never label text. Compound writes also split onto leftover yellows. Back restores the pre-Continue list.
- Officer: Stop F5, rebuild, restart. Re-Approve `_F16` once so `ReviewPlanJson` is 1:1. Continue then Back to field list should keep the same locked Short codes. Remap unmarked is still the only re-guess.
- Cross-skill: visa2026-resminamalar

### 2026-09-10 — Review placeholders must not re-guess

- Need: Catalog **Review placeholders** on `_F16` rebuilt the list from yellow (Analyze). Locked / approved Short codes were lost. Officers wanted to tweak one row or Continue with no change.
- Cause: `OpenForExistingTemplateAsync` always called Analyze. Yellow `SourceFile` made that a full re-guess. Locks lived only in the modal.
- Fix: Approve stores `ReviewPlanJson` (tokens, locks, regions; mapped rows locked). Review placeholders ingests the yellow file for the page only and restores that snapshot. No snapshot → overlay mapped `TemplateFile` tokens onto yellows by paragraph/cell. Remap unmarked is the only re-guess.
- Officer: Stop F5, rebuild, restart. Re-Approve `_F16` once. Review placeholders should show the last Short codes, locked. Continue without Remap unmarked to keep them.
- Cross-skill: visa2026-resminamalar

### 2026-09-10 — Persist yellow source next to mapped catalog file

- Need: After Approve, Review placeholders / Remap unmarked reopened the mapped `TemplateFile` (tokens, yellow stripped). Officers could not remap the original yellow Word/Excel after the dialog closed.
- Cause: Approve wrote only the generated copy. Upload bytes lived in modal state (SD-D5). No mid-wizard draft BO — but the **approved** yellow file was not stored.
- Fix: `ApplicationProfileTemplate.SourceFile` holds the yellow upload. Approve writes both files. Review prefers `SourceFile`, else falls back to `TemplateFile` (seeds / old rows). Convert omit leaves `SourceFile` unchanged. Resminamalar Preview/ZIP still merge `TemplateFile` only.
- Officer: Stop F5, rebuild, restart (schema adds `SourceFileID`). Re-Approve `_F16` once so the yellow upload is stored. After that, Review placeholders opens the yellow letter. Cards approved before this build still use mapped tokens until one re-Approve.
- Cross-skill: visa2026-resminamalar | visa2026-application-profile

### 2026-09-10 — Remap unmarked still showed reconnect then Review

- Need: After the off-circuit build, Remap unmarked on `_F16` still flashed "Attempting to reconnect 1 of 8", then Review returned (Word page + list, footer remapped). "Unidentified yellows remain" was ticked.
- Cause: Remap still rebuilt instance value maps on the circuit, then exported Forma 16 pdf.js JPEGs for AI. Either step blocks SignalR. Local field plan was already enough.
- Fix: Remap reuses cached value candidates. Local list paints first. Remap AI is text-only (no `exportPagePngs`). Ask AI chat can still attach a page if needed.
- Officer: Stop F5, rebuild, Ctrl+F5. Remap unmarked should keep Review visible. Ticking leftover-yellows no longer captures the page (avoids the white reconnect).
- Cross-skill: none

### 2026-09-10 — Remap unmarked blanked Review then HTML outline

- Need: Remap unmarked on `_F16` Review placeholders: page went white ("Attempting to reconnect 1 of 8") for a couple of seconds, then Review came back as the HTML outline (numbered tokens), not the Word pdf.js page.
- Cause: Remap re-ran OpenXML field-plan extract on the Blazor circuit. SignalR dropped. After reconnect, pdf.js interop was dead and Office preview treated that as paint-failed → outline fallback. Re-ingest was unnecessary; Review already had the file.
- Fix: Remap reuses the current ingest (same Office bytes, PDF stays mounted). Local field-plan build runs on a thread pool (`ScanFieldPlanBuildOffCircuit`). Preview retries JS after disconnect instead of immediately showing the outline.
- Officer: Stop F5, rebuild, Ctrl+F5. Review placeholders → lock / unlock → Remap unmarked. The Word page should stay; the numbered list updates. Do not wait on the reconnect banner.
- Cross-skill: none

### 2026-09-10 — Remap unmarked opened Preview instead of Review

- Need: After Approve, Review placeholders on `_F16`. Officer locked all rows, unlocked 12.1 / 12.2 / 12.3, clicked Remap unmarked. Wizard jumped to Preview (Back to field list / Regenerate / Approve) instead of staying on the numbered list.
- Cause: Remap sets `_busy` and stays on FieldReview, but `GenerateAsync` (Continue) did not check busy/stage. A second click or a queued Continue event during remap ran generate and won the race. Hiding hint checkboxes while busy also shifted the footer.
- Fix: `ScanWizardGenerateGuard` — Continue/Regenerate ignored while busy or off Review/chat/Preview. Remap success stays on Review with a footer note. Hint checkboxes stay mounted (disabled while remapping).
- Officer: Stop F5, rebuild, Ctrl+F5. Review placeholders → lock the good rows → unlock 12.1–12.3 → Remap unmarked. You should stay on Review and see the remapped Short codes. Continue is a separate click.
- Cross-skill: none

### 2026-09-10 — F16 Review TRDT/TRCK/AVCAT empty in Resminamalar

- Need: Officer `_F16.docx` Review used TRDT, TRCK, and Application_VisaCategory_NameTm. Catalog Preview on Hasaba said those fields were empty.
- Cause: Those tokens are registration-line / instance VisaCategory. Hasaba stores entry on Travel history and category on the linked visa.
- Fix: Merge getters fall back (user-report-templates). Do not remap Giren wagty/ýeri to THDT/THCP just to fill Preview. Catalog short codes stay distinct.
- Officer: Stop F5, rebuild, restart. Open Resminamalar Preview of F16 again — no re-Approve needed. Confirmed filled.
- Cross-skill: visa2026-user-report-templates | visa2026-application-profile

### 2026-09-09 — Remap unmarked hung on leftover-yellows AI

- Need: After Remap unmarked (both hint boxes ticked) Review sat on **Sending leftover yellows to AI…** / **Remapping…** with Continue disabled. Detected fields did not update.
- Cause: Remap captured a full-page JPEG via pdf.js `toDataURL` *before* rebuilding the field plan, then sent it to Azure. Forma 16 pages are large; capture blocked the circuit.
- Fix: Local remap first (SkipAi). Then capture a smaller JPEG (800px, 8s timeout). Then AI. Footer shows a progress bar; hint checkboxes hide while busy.
- Officer: Stop F5, rebuild, Ctrl+F5. Lock good rows, tick the hints, Remap unmarked. The numbered list should refresh quickly; AI may still run a few seconds after that.
- Cross-skill: none

### 2026-09-09 — Remap unmarked officer hint checkboxes

- Need: Before Remap unmarked, officers wanted to tell the tool that yellows were missed and/or unlocked placeholders were wrong, so AI could focus.
- Cause: Remap only locked vs unlocked. Ambiguous AI skipped High-confidence wrong tokens and did not get the Review page for leftover yellow.
- Fix: Review footer checkboxes **Unidentified yellows remain** and **Some placeholders are wrong** (optional, both allowed). Unidentified: unmapped marks first + page images to Azure. Incorrect: send unlocked marks even when local confidence is High. Locked rows stay out.
- Officer: Stop F5, rebuild, Ctrl+F5. Lock correct rows. Tick one or both boxes, then **Remap unmarked**. Do not use Preview Regenerate for this.
- Cross-skill: none

### 2026-09-09 — Lock reviewed placeholders and Remap unmarked

- Need: Forma 16 Review left unnumbered yellows. Officers wanted to keep reviewed Short codes while re-guessing the rest of the same uploaded Word/Excel.
- Cause: Analyze always rebuilt the whole plan. FieldId is a new Guid each pass. Preview already has Regenerate (token write), Review had only Upload different file.
- Fix: Per-row **Lock** (whole yellow span, including 12.1/12.2). **Lock mapped** for every tokened row. **Remap unmarked** re-runs Analyze on the same bytes, seeds used header codes from locked rows, and restores locked tokens by OpenXML/Excel region. Unlocked and previously unmapped yellows are guessed again. Ask AI cannot overwrite locked rows.
- Officer: Stop F5, rebuild, Ctrl+F5. On Review, lock rows you already checked (or Lock mapped). Click **Remap unmarked** — not Preview Regenerate, not Upload different file. Unnumbered yellows should pick up leftover placeholders.
- Cross-skill: none

### 2026-09-09 — Remove corner Add placeholder button

- Need: Officers wanted the previous Short-column way to change placeholders, not the new bottom-right **Add placeholder** button.
- Cause: Review last column pinned a corner button and popup over High / ×.
- Fix: Restored Filter + grouped **Add placeholder…** dropdown on the selected Short column. Row × stays on the right. Append-sibling (12.3) is unchanged.
- Officer: Stop F5, rebuild, Ctrl+F5. Select a row, use Short list **Add placeholder…**. Do not look for a button under High.
- Cross-skill: none

### 2026-09-09 — Ask AI sends Azure the page plus optional PNG/JPG

- Need: Ask AI put visa dates (VISD/VSTD/VEDT) on the selected WP mark instead of form line 12. Officers asked to send the template to Azure and to attach an image when needed.
- Cause: ClarifyAsync was text-only JSON. Wizard Upload still rejects PNG (correct). Azure never saw the printed line.
- Fix: Ask AI sends Review page canvases (pdf.js JPEG) plus optional officer PNG/JPG. Prompt: remap the named form line, not the focused mark unless printed text matches. Analyze Upload stays .docx/.xlsx only.
- Officer: Stop F5, rebuild, Ctrl+F5. Select the date yellow on line 12, or attach a PNG/JPG of that line, then Ask AI. Do not expect Upload to accept images.
- Cross-skill: visa2026-user-report-templates

### 2026-09-09 — Add placeholder at selected-row bottom-right

- Need: Officers needed a missed placeholder on a specific yellow (e.g. 12.3). The Short-column dropdown was easy to miss; they asked for a button at the right bottom of the selected row.
- Cause: Add lived in the Short column as Filter + dropdown, mixed with chips and Ask AI.
- Fix: Selected row last column: × at the top, **Add placeholder** pinned bottom-right. Click opens a filter + grouped catalog. Pick a code still appends a sibling (`AppendShortCodes`). Chip × remaps/drops a part.
- Officer: Stop F5, rebuild, Ctrl+F5. Select 12.2. Use **Add placeholder** under High / ×. Filter e.g. VPLC. A 12.3 row appears on the same yellow mark.
- Cross-skill: visa2026-user-report-templates

### 2026-09-09 — Add missed compound subsection (12.3)

- Need: Forma 16 Review had 12.1 / 12.2. AI missed a third placeholder on the same yellow. Officers needed to add 12.3.
- Cause: Add placeholder on a 12.2 row called ApplyPartCodes and replaced 12.2 instead of appending a sibling. Extra tokens beyond comma segments were dropped from Review numbering.
- Fix: Add placeholder on 12.1 / 12.2 appends a sibling (AppendShortCodes). Empty unmapped parts still fill that slot. Split keeps leftover codes as 12.3. Dropdown hides codes already on the parent span.
- Officer: Stop F5, rebuild, Ctrl+F5. Select 12.2, Add placeholder (e.g. VPLC). A 12.3 row appears on the same yellow mark. Continue / Generate writes all three tokens. Chip x still remaps or drops a part.
- Cross-skill: visa2026-user-report-templates

### 2026-09-09 — ADRS Preview omitted City

- Need: After ADRS mapping was correct, hasaba Preview still printed only the street. Officers expected City from People & links (`Turkmenbashy etraby`) in the same cell.
- Cause: `Address_FullAddress` was `FullAddress` only.
- Fix: Prefix `City.NameTm` when missing from the street. Token stays ADRS.
- Officer: Stop F5, rebuild, Ctrl+F5, Preview Hasaba almak sanawy. Column *Türkmenistandaky salgysy* should start with the City name, then the street.
- Cross-skill: visa2026-user-report-templates

### 2026-09-09 — Hasaba Excel 11.1 attached company address (ACADR)

- Need: Create from yellow marks Review on hasaba almak.xlsx mapped *Türkmenistandaky salgysy* (Aşgabat etrap / köçe) to company ADDR / ACADR. Officers needed the residence address linked on the Application Profile Instance (People & links Address), same as person/passport/visa.
- Cause: Catalog already had ADRS → Address_FullAddress, but it was gated by RequirePersonAddressOfResidence so Review often hid it. Any caption containing salgy preferred ACADR (company legal address).
- Fix: ADRS is Core (always offered next to person data). Caption/label/Excel header: residence salgy → ADRS; yuridiki/kärhana → ACADR. Merge fills Address_FullAddress from the instance-linked AddressOfResidence.
- Officer: Stop F5, rebuild, Ctrl+F5. Analyze again. Row 11.1 should show ADRS — Residence address (person on this case). Do not pick Company address.
- Cross-skill: visa2026-user-report-templates | visa2026-application-profile

### 2026-09-02 — Drop profile-lock banner from Create from yellow marks

- Need: Review needs vertical space for the letter and Detected fields. The orange “Profile locked — new templates only” banner did not change what the officer can do on Create new.
- Cause: `_configLocked` painted the banner on every step, including add-new.
- Fix: Removed the top banner. Lock still blocks overwrite (`IsLockedExistingOverwrite`); the name field keeps the short hint to rename and save a copy.
- Officer: Restart, hard-refresh. Create from yellow marks Review should start at the document, no orange bar.
- Cross-skill: visa2026-application-profile

### 2026-09-02 — Remove Add existing template from Create from yellow marks

- Need: Officers still saw **Add existing template instead** on Upload after the quieter form. Catalog already has Add existing template.
- Cause: Scan dialog kept an escape hatch to Convert/manual upload.
- Fix: Removed the link, `OnAddPreparedTemplate` callback, and the wizard binding. Add existing stays on Resminamalar / profile Templates.
- Officer: Restart, hard-refresh. Create from yellow marks Upload should end at the file drop, then Cancel / Analyze file.
- Cross-skill: visa2026-resminamalar

### 2026-09-02 — Upload step quieter: Both default, tooltips, Analyze progress

- Need: Officers said Create from yellow marks Upload had too much scattered help (Data, scan requirements, contract, case). Analyze file gave no visible progress.
- Cause: Every field had a paragraph of help. Data dropdown repeated Add-existing wording. Case was shown again even when the header already had the case number.
- Fix: Data scope is always Both (dropdown removed). Help moved to `?` tooltips. Case picker stays on the profile wizard only. Analyze shows a footer progress bar (Reading file / Finding yellow marks / Matching placeholders).
- Officer: Restart, hard-refresh CSS. Open Create from yellow marks — Upload should be name, save-to, file. Click Analyze and watch the bar. Hover `?` for hints.
- Cross-skill: visa2026-resminamalar

### 2026-09-02 — Five layout guessing patterns, not one surround rule

- Need: Officers sent Sahsy kagyzy (left labels), Borcnama (caption under the line), Excel sanaw (column headers), Zahmet sertnamasy (inline prose + Isgar/Is beriji), and a ministry letter (No, date, Gyssagly, 1 (bir), 1 (bir) ay, iki gezeklik). Each layout needs its own guessing pattern.
- Cause: Caption-under-the-line surround was treated as the only nearby pattern. Sahsy field names without parentheses never preferred PNAT/PPIN/EGSP/POSN. Inline "Mudiri Name" looked like a roster person. Excel missed Gelmegin maksady / Cagyran tarap.
- Fix: `ScanGuessingPatternRegistry` names OfficialLetter, CaptionUnderLine, LeftLabelForm, InlineProse, ExcelColumnHeader. Letter regex stays first. Rank uses `ScanFormFieldLabelHints` plus titled-director / money / personal-number shapes. Excel profiles add RGEL and ACNAM.
- Officer: Restart, hard-refresh, Analyze any of those yellow-marked files. Sample names still change every time.
- Cross-skill: visa2026-user-report-templates

### 2026-09-02 — Surround text is a guessing pattern for every yellow file

- Need: Immediate left label + caption under the line must drive placeholders on **all** similar Word/Excel files, not only one BORÇNAMA hired-person row. Sample names change every time.
- Cause: Word guessing scored the yellow **value shape** first and ignored nearby text except inside the comma binder. Company rows above also leaked into nearby.
- Fix: `ScanSurroundPlaceholderPattern` ranks catalog codes from immediate surround + shape (caption slots, left-label catalog match, group boost). Same pattern is one of the guessers in `ScanOfficeFieldPlanBuilder`, `ScanExcelYellowResolver` (header + compound cells), and `ScanCompoundYellowBinder` part pick. Compound comma split still uses `ScanCompoundYellowBinder`.
- Officer: Restart, hard-refresh, Analyze any yellow-marked letter. Captions like `(ady … doglan senesi)` or `pasporty: (… möhleti)` should pick Person/Passport codes for whatever sample is highlighted.
- Cross-skill: visa2026-user-report-templates

### 2026-09-02 — Hired-person yellow guessed as company address (ACADR)

- Need: BORÇNAMA Review 3.1 on a roster full name (sample was `Hilmi Erol`; next file can be any name) plus 3.2 DOB. Left label `Işe çagrylýan adam:` and caption `(ady, familiýasy, atasynyň ady, doglan senesi)` mean **Person full name** + birth date. Guess was **ACADR** / **ACRDT**.
- Cause: Comma yellow used company-address shape (`length >= 8`) for the name part. Nearby mixed in `kärhana` from **two rows above**. Caption slots (`ady`…) did not map to **PFN**. Applicant role from `doglan senesi` was not turned into the Person catalog group.
- Fix: Guess from **immediate** left label + caption under the line only (not company paragraphs above). Applicant / `çagrylýan adam` → Person. Caption `ady` / `familiýasy` / `atasynyň ady` → **PFN**; `doglan senesi` → **PDBT**. Two-to-four letter-word names fit **PFN**, not street **ACADR**. No sample-name hardcode.
- Officer: Restart, hard-refresh, Analyze. Hired-person line should Review as **PFN** then **PDBT** for whatever name is yellow-marked.
- Cross-skill: visa2026-user-report-templates

### 2026-09-02 — Signatory passport expiration missing from Review (CHPE)

- Need: BORÇNAMA Review row 5.1 (`19.02.2034ý.`) is passport **möhleti**. Authorized signatory dropdown had CHPD (issue date) but no expiration token.
- Cause: `AuthorizedSignatory` had no `PassportExpirationDate`. Catalog Signatory stopped at CHPD. Caption `mohlet` remapped person **PPED** onto **CHPD**.
- Fix: BO + tenant seed `PassportExpirationDate`. Catalog **CHPE** (`CompanyHead_PassportExpirationDateText`). Signatory remap `PPED` → **CHPE**. Label-group bind prefers CHPE when nearby contains möhleti.
- Officer: Restart, hard-refresh. Configuration → Authorized Signatory: fill Passport Expiration Date. Analyze again. Row 5.1 should pick **CHPE**.
- Cross-skill: visa2026-user-report-templates · visa2026-lookup-data

### 2026-09-02 — Comma yellow parts stay in the printed label's catalog group

- Need: Combined yellow `Yokary, TUR, Gundogar mediterian uniwersiteti` must map to Education (`EGLV` / `EGCC` / `EGIN`), not Person nationality `PNAT` for `TUR`.
- Cause: Each comma segment was scored against the whole catalog. `TUR` looks like a country code, so the binder picked Person/Passport codes. The printed label (`Bilimi`) was not used to lock the related-BO group first.
- Fix: `ScanCompoundLabelGroup.Identify` reads the left-side / nearby label (and wekil/signatory captions) to pick the group, then `BindParts` assigns unused codes from that group only. Caption-slot binding remains the fallback.
- Officer: Restart, hard-refresh, Analyze. An education line labeled Bilimi / Okuw should Review as **EGLV**, **EGCC**, **EGIN**.
- Cross-skill: visa2026-user-report-templates

### 2026-09-02 — Review Add-placeholder hid CompanySignatory codes (CHPN / CHPL)

- Need: BORÇNAMA Review row 5.1 (signatory passport date) opened Add placeholder. Officers could not find CompanySignatory tokens. The native list grouped them as **Signatory** and hid **CHPN** / **CHPL** because those codes were already on sibling comma parts of the same yellow span. Keyboard search also failed (options start with short codes, not `CompanySignatory`).
- Cause: Remaining choices used **parent** short codes. Compound 5.1 / 5.2 / 5.3 share one span, so sibling Signatory passport tokens disappeared from the date row. Native `<select>` has no filter on related BO name.
- Fix: Filter box matches short code, labels, `CompanySignatory`, and **Authorized signatory**. Compound Add **replaces that part** (`ApplyPartCodes`) and lists unused codes for **this part** so CHPN / CHPL / CHPD stay visible. Group label is **Authorized signatory**.
- Officer: Restart, hard-refresh. Click the date row, type `CompanySignatory` or `CHPD` in the filter, pick **CHPD** (issue date — not person **PPED**).
- Cross-skill: visa2026-user-report-templates

### 2026-09-02 — Downloaded şahsy photo token wraps and Preview shows `{{IMAGE:Person_Photo}}`

- Need: Catalog Preview filled the photo after Create from yellow marks. After **download template** + **Add existing template**, SAHSY KAGYZ_t showed the raw token in the photo box (CHECK chip).
- Cause: `{{IMAGE:Person_Photo}}` is too long for the 35×45mm photo cell. Word wraps it (`Person_Phot` / `o}}`), often with a newline or a second paragraph. The injector only matched one paragraph and `[\w]+`, so the split token stayed literal.
- Fix: Injector matches whitespace/soft-hyphen wraps and concatenates table-cell paragraphs. Extract normalizes `IMAGE:` keys. Scan again emits short `{{IMAGE:PPH}}` (injector still maps to `Person_Photo`). Seed şahsy_kagyz layout is unchanged.
- Officer: Restart, hard-refresh, Preview the re-added template — the photo should fill without re-uploading. New Create-from-yellow-marks templates get `{{IMAGE:PPH}}` so a later download is less likely to wrap.
- Cross-skill: visa2026-user-report-templates · visa2026-resminamalar

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
