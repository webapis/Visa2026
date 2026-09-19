---
name: visa2026-template-scan
description: >-
  Improve Create from yellow marks (Word/Excel): yellow-highlighted .docx/.xlsx → library
  placeholders → Approve. Officers submit wizard screenshots; agent compares, fixes TemplateScan,
  appends learnings. Not Convert (value-match), not PNG/JPG/PDF (retired), not Resminamalar ZIP,
  not #visa-preview-slot inside the wizard. Read learnings.md first; append after verified fixes.
  User prompts: prompts.md.
disable-model-invocation: false
---

# Visa2026 — Create from yellow marks

**Mission:** Generate **Word and Excel** merge templates from **yellow-marked `.docx` / `.xlsx`** (OpenXML highlight / yellow cell fill → library placeholders → officer Approve). **PNG/JPG/PDF uploads are retired.**

**Officer label:** **Create from yellow marks** (not “Create from scan”).  
**User prompts:** [prompts.md](./prompts.md) (`@visa2026-template-scan`).

## Screenshot feedback loop

Officers submit **wizard step screenshots** + optional **catalog Preview** + the **original yellow-marked Office file**. Treat as a regression pack; append learnings after every run.

| Step | Judge |
|------|--------|
| **1 Upload** | `.docx`/`.xlsx` only; yellow requirements; case hints |
| **2 Review** | Mapped tokens vs yellow only; no bogus gaps. Left pane is the **uploaded Word/Excel as pdf.js pages** (no browser PDF chrome), numbered `#` on the letter matching Detected fields. **Click a Detected fields row** to highlight it and **add one or more library placeholders** on that same yellow mark (compound spans). **Lock** a reviewed yellow. Optional checkboxes **Unidentified yellows remain** / **Some placeholders are wrong**, then **Remap unmarked** (locked Short codes stay; hints steer AI). Optional **Ask AI** docks chat with that mark’s context and may send the Review page plus an officer **PNG/JPG** to Azure. Outline fallback if convert fails. Not `#visa-preview-slot` |
| **3 Generate** | Token writer on **copy**; **strip all yellow** markup; diff gate |
| **4 Preview** | Generated Office copy as PDF in the modal (not `#visa-preview-slot`). Outline fallback |
| **5 Done** | Saved; correct TemplateKind |
| **Catalog Preview** | Real page/sheet; **no yellow highlighter** left on filled text |

## Agent workflow

1. Read [learnings.md](./learnings.md) (newest first) + Scenarios.
2. Classify vs Convert / Resminamalar / preview-slot / user-report-templates.
3. Re-read [`docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md).
4. Implement in `Visa2026.Module/Services/TemplateScan/` + thin Blazor `TemplateScan*`.
5. Verify: `dotnet test … --filter FullyQualifiedName~TemplateScan`.
6. Append learnings; promote repeated issues to Scenarios.

## Canonical docs

| Doc | Topic |
|-----|--------|
| [`docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md) | Product locks |
| [`docs/TEMPLATE_AI_SCAN_UI_FLOW.md`](../../../docs/TEMPLATE_AI_SCAN_UI_FLOW.md) | Wizard flow |
| [`docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md) | Contracts |

**Related:** [application-profile](../visa2026-application-profile/SKILL.md) · [resminamalar](../visa2026-resminamalar/SKILL.md) · [preview-slot](../visa2026-preview-slot/SKILL.md) · [user-report-templates](../visa2026-user-report-templates/SKILL.md) · Convert specs (value-match, not yellow).

## Output maturity

| Format | Status |
|--------|--------|
| **Word `.docx`** | Shipped — yellow → tokens on copy → strip all yellow markup |
| **Excel `.xlsx`** | Shipped foundation — yellow fill → tokens → strip yellowish fills |

## Scenarios

| Symptom | First step | Owner |
|---------|------------|--------|
| Sanaw #13 TUR + street maps only to Foreign address (PFAD), country missing | Comma in the yellow = two placeholders: **PFAC** then **PFAD**. Stop F5, rebuild, **Analyze** | **This skill** |
| After × on compound 11.1, picking Visa period (item) on 11.2 jumps to another code | Hidden parts keep empty slots. Stop F5, rebuild, hard-refresh. × leftover 11.1, then Add **AVPRD** on 11.2 | **This skill** |
| Lock on compound 11.2 / 11.3 shows Part instead of Visa period / category | Add placeholders first (chips must show AVPRD/AVCAT). Lock keeps Short codes. Stop F5, rebuild, hard-refresh | **This skill** |
| Business-trip letter dates map to ADAT / duration maps to TPCNT | **BTSD** / **BTED** (`-den` / `-ne` stay in Word). **BTDCNT** / **BTDCTX** for `N (words) gün`. From/to: **BTFRG** **BTFCT** **BTTRG** **BTTCT**. Purpose **BTPRP**. Sanaw destination **BTAD**. Restart, Analyze | **This skill** + user-report-templates |
| Sanaw destination address empty as **11.1** / cannot find trip address like Address_FullAddress | One yellow = **BTAD** (`BusinessTripAddress_FullAddress`, region+city+street). Search `BTAD` / `business trip address` / `Address_FullAddress`. Do not split on commas. Stop F5, rebuild, **Analyze** | **This skill** + user-report-templates |
| Review Add cannot find From Region / From City / To Region / To City | Filter those names or **BTFRG** **BTFCT** **BTTRG** **BTTCT**. They sit in the **Application** group (not “district”). Restart, rebuild, Analyze | **This skill** + user-report-templates |
| Review Add cannot find Purpose / Maksady | Filter `Purpose`, `Maksady`, or **BTPRP**. Instance free-text Purpose, not RGEL / BusinessTripPurpose lookup. Restart, rebuild, Analyze | **This skill** + user-report-templates |
| Yellow `1` and `2` not in Detected fields (`bir`/`iki` only) | Word often yellows the words, not the digits. Analyze now adds leading `1`/`2`: **TPCNT** then **BTDCNT**. Stop F5, rebuild, **Analyze** (do not Remap unmarked first) | **This skill** |
| Yellow `15` / `20` ink with no `#` (`on bäş` / `ýigrimi` only) | Same count-pair rule for multi-digit. Analyze adds **TPCNT**/**BTDCNT**. Review script `?v=tasmarks8` places `#` on digits after space-fold. Stop F5, rebuild, hard-refresh, **Analyze** | **This skill** |
| Isolated yellow `2` next to `(iki) gün` missing; From Region preview empty | Digit after person `1 (bir)` is **BTDCNT**, not a second TPCNT. Rebuild, Analyze. Fill From region / From city so **BTFRG** prints | **This skill** + user-report-templates |
| Review left pane missing a `#` (e.g. 12) or `#8` sits on list `1.` instead of yellow `1 sany` | Digit marks skip PDF list numbers (`1. Daşary…`); unmatched AFNUM still paints the OpenXML box. Script `?v=tasmarks15`. Stop F5, rebuild, hard-refresh Review | **This skill** |
| Cover letter Goşundy `1 sany` maps to TPCNT (same as person count) | Enclosure list ≠ person count. Header codes are one-shot (like Excel columns). Stop F5, rebuild, **Analyze** | **This skill** |
| Şahsy **#4 Nationality** sits on Şahsy belgis or Bilimi, not **Raýatlygy TUR** | Birth/address `TUR` pin first; leftover **#4** sits in the gap on **Raýatlygy**. Script `?v=tasmarks14`. Stop F5, rebuild, hard-refresh Review | **This skill** |
| Review `#` on one line jump (10 12 11 14) | Numbers follow left-to-right on the letter (then the Detected list). Restart, hard-refresh Review | **This skill** |
| Word Review `#` order lost on a table (Excel sanaw is fine) | Word table marks snap to the cell grid like Excel. Script `?v=tasmarks10`. Stop F5, rebuild, hard-refresh Review | **This skill** |
| Word sanaw names/birth unmapped (Excel headers were fine) | Analyze uses the **column caption** above the yellow (`Familiýasy` → PLN, `Ady` → PFNM, `Doglan senesi we ýeri` → PDBT/PCBT/PBPL). Stop F5, rebuild, **Analyze** | **This skill** |
| Word yellow cell has no Detected row / no `#` (`TUR`, `Ýok`, signatory name) | Cell shading counts as yellow. Duplicate short text still gets a table `#`. Stop F5, rebuild, hard-refresh Review, **Analyze** | **This skill** |
| Review `#` empty boxes in whitespace / stacked on letterhead | Word still paints PDF text (table cell % is a snap hint, not a ghost box). Rebuild, hard-refresh Review | **This skill** |
| Preview shows `{{IMAGE:Person_Photo}}` in the photo box after Add existing template | Word wrapped the long token in the photo cell. Restart, hard-refresh Preview. New Generate uses `{{IMAGE:PPH}}` | **This skill** + user-report-templates |
| Inserted sample photo not mapped | Body portrait (not a tiny icon) → `{{IMAGE:PPH}}` on Generate (`Person_Photo` still injects). Yellow still required for text values. Restart, Analyze | **This skill** |
| Review placeholders dropped Person photo after Open yellow file | Restore re-pins `{{IMAGE:PPH}}` onto the live body portrait (do not yellow-highlight the picture). Continue replaces the sample photo. Re-Approve once. Restart | **This skill** |
| Create template should be one Project contract or all via-ministry cases | **This profile only**: **All contracts** or **this case’s** Project contract (not the full catalog). Shared catalog has no contract filter. Stop F5, rebuild | **This skill** |
| Add existing template has no Project contract on via-ministry cases | Same **This profile only** dropdown as yellow marks. Stop F5, rebuild. Pick **All contracts** or one Project contract, then Add to profile | **This skill** + application-profile |
| Add existing template lands on **CHECK** (placeholders not validated) | Stop F5, rebuild, Add existing again. Extract/Validate now runs like yellow-marks Approve. Already-saved CHECK rows: add the file again or Extract placeholders under Configuration | **This skill** + resminamalar |
| Comma yellow guessed from the wrong catalog group (e.g. TUR → PNAT on an Education line) | Printed **label** picks the group first (`Bilimi` → Education), then each comma part is guessed inside that group (`EGLV`, `EGCC`, `EGIN`). Restart, Analyze | **This skill** |
| Review Add placeholder missing Signatory / CompanySignatory (CHPN, CHPL, CHPD, CHPE) | Filter box: type `CompanySignatory` or `CHPE`. Group is **Authorized signatory**. Compound parts no longer hide sibling Signatory codes. Restart, hard-refresh | **This skill** |
| Review Add placeholder cannot find education (level, institution, specialty) | Filter box: type `education`, `EGLV`, `EGIN`, `EGSP`, or `speciality`, then **Add placeholder…**. Cancel-visa still offers Education tokens; People & links Education tile stays hidden. Restart, hard-refresh | **This skill** |
| Review Add placeholder cannot find birth place | Filter `birth place` / `birthplace` / `PBPL` / `Doglan`. List shows **Birth place — PBPL** (not country of birth **PCBT**). Restart, hard-refresh | **This skill** |
| PBPL Preview / Review sample shows country (Türkiye) | **PBPL** is city only. **PCBT** is country. Rebuild, hard-refresh Preview. Person.BirthPlace `Türkiye/Gaziantep` prints Gaziantep | **This skill** + user-report-templates |
| Review Add placeholder missing codes used on other templates | List is the full catalog for Word/Excel (not profile tiles, not Header vs Row). Filter the name or Short code. Restart, hard-refresh | **This skill** |
| Review cannot add a placeholder on an unidentified yellow (Part) | Unmapped rows show Filter + **Add placeholder…** again (native list). Type in Filter, then pick from Add. Restart, hard-refresh | **This skill** |
| Change-invitation letter invitation number / dates | Invitation group: **INVN** / **INVS** / **INVE**. One invitation per case — `{{ds.INVN}}` in the paragraph. Several InvitationItems (people) share that header | **This skill** + user-report-templates |
| Review date 5.1 (`19.02.2034ý.`) has no Signatory passport expiration | `AuthorizedSignatory.PassportExpirationDate` + catalog **CHPE**. Fill expiration in Configuration. Restart, Analyze | **This skill** + user-report-templates |
| Review missed yellows / remap wiped reviewed Short codes | Lock the reviewed row (or **Lock mapped**). Tick **Unidentified yellows remain** and/or **Some placeholders are wrong**, then **Remap unmarked**. Locked rows stay. Not Preview **Regenerate**. Restart, hard-refresh | **This skill** |
| Review has extra 10.1 / 10.2 rows on one yellow | Row **×** hides that part; remaining token stays on the span. Last × drops the mark so Generate leaves printed text. Hard-refresh | **This skill** |
| Azure Ask AI dumps dates on the selected WP mark | Chat now sends Review page + optional PNG/JPG. Attach a photo of line 12. Restart, hard-refresh | **This skill** |
| Review Add placeholder is hard to find / need 12.3 on the same yellow | Select the row. Short column: Filter + **Add placeholder…**. Pick a code — appends a sibling. Hard-refresh | **This skill** |
| This profile only letter appears on Shared | Approve wrote a merge backing `UserReportTemplate`. Rebuild; open **This profile**. Shared hides names that exist only as this-profile nested rows | **This skill** + resminamalar |
| After **Approve — save to profile**, template missing from Resminamalar ZIP selection / Shared tab | **This profile only** lands on **This profile** (checkbox list), not Shared. Stop F5, rebuild, Approve, Close — new row is checked. Shared is only **Save to = Shared catalog** | **This skill** + resminamalar |
| Catalog Preview of a sanaw shows only one of several selected people | Stop F5, rebuild. Invitation yellow-marks copies clone rows at Preview. Direct-to-migration seeded **SANAW_WIZANY_UZTURMEK** already loops. Keep all header chips selected | **This skill** + resminamalar |
| Invitation yellow-marks sanaw Preview fails; visa-extension seeded sanaw is fine | Stop F5, rebuild. Preview This-profile Dasary Word/Excel — one row per person. No Re-Approve | **This skill** + resminamalar |
| Sanaw Preview lists people but **№** / record number is blank | Stop F5, rebuild. Preview again — 1, 2, 3. Review **RNUM** can stay. No Re-Approve | **This skill** + resminamalar |
| Word roster Approve does not add a catalog row (Excel does) | Same Word name overwrote the existing This-profile sanaw. Stop F5, rebuild, **Create** again — new Word row is `…_2`. Review placeholders still replaces the same name | **This skill** |
| Analyze crashes `Specified part does not exist in the package` | Word ZIP lists a missing related part. Rebuild; Analyze again. If it still fails, Word **Save As** `.docx` | **This skill** |
| PNG/JPG/PDF rejected | Expected — use yellow-marked Word/Excel | **This skill** |
| Yellow not detected | Word Text Highlight Color / Excel solid yellow fill | **This skill** |
| Wrong tokens / compound split | `ScanYellowHighlightTokenResolver` + catalog ShortCodes | **This skill** + user-report-templates |
| Wekil slot mapped to `{{.PFN}}` / catalog Preview fills a roster person | Printed caption `ygtyýarly wekili` → `{{ds.RPFN}}` (`AuthorizedRepresentative`). Isolated names stay `PFN`. Restart Analyze | **This skill** |
| Review maps company hasaba alyş date to `ADAT` / `ApplicationDateText` | `CompanyProfile.RegistrationDate` + `{{ds.ACRDT}}`. Nearby `hasaba alyş` / `şahamça` → not letter date. Restart, Analyze, set Company Registration Date in Configuration | **This skill** + user-report-templates |
| Review 11.1 / *Türkmenistandaky salgysy* mapped to company `ACADR` | Person residence on the case is **`ADRS`** (`Address_FullAddress` from People & links Address). Restart, Analyze, hard-refresh | **This skill** + user-report-templates |
| `42703: column c.RegistrationDate does not exist` | Host-start heal `CompanyProfileRegistrationDateSchemaSql`. Restart app (ModuleInfo already current skips XAF schema). Then Analyze | **This skill** |
| Review left pane is HTML text, not the Word page | Office→PDF via pdf.js pages (`TemplateScanOfficePdfPreview`). Hard-refresh, Analyze again. Not `#visa-preview-slot` | **This skill** |
| Review shows Chrome/Edge PDF toolbar or thumbnail sidebar | pdf.js canvases, not an iframe. Hard-refresh CSS/JS | **This skill** |
| Hired-person / any left-label yellow guessed from value shape only | `ScanGuessingPatternRegistry` + `ScanSurroundPlaceholderPattern`: caption, left field label, inline prose, letter regex, Excel header. Restart, Analyze | **This skill** |
| Yellow text contains a comma but Review is one row | Comma = combination candidate. Left label + parenthetical under the line (`hasaba alnan belgisi, senesi…`) guide each part. Review shows **6.1 / 6.2 / 6.3** with separate preview borders. Generate still writes one span. Restart, Analyze, hard-refresh | **This skill** |
| Excel `05.04.1989, TUR, Fatih` shows as 4/5/6 not 6.1/6.2/6.3; Fatih unmapped | All-comma birth cells map **PDBT, PCBT, PBPL**. Visual renumber keeps **N.1 / N.2 / N.3**. Stop F5, rebuild, Analyze | **This skill** |
| Review lost numbered marks / row click does not highlight the letter | Numbered overlays + sticky row select (`ActiveFieldId`). Click a Detected fields row | **This skill** |
| Review preview stays portrait for a landscape Word/Excel | Outline reads `sectPr`/`PageSetup`. Hard-refresh CSS, Analyze again. Not `#visa-preview-slot` | **This skill** |
| Review has no left document / no `#` on fields | Office outline + `ScanReviewFieldOrder` (top→bottom). Not `#visa-preview-slot`. Restart Analyze | **This skill** |
| Need to remap a saved Resminamalar template | Catalog **Review placeholders** restores the last approved mapping. Use **Remap unmarked** only to re-guess unlocked rows. Not desktop **Edit template**. Not `#visa-preview-slot` | **This skill** + resminamalar |
| Review placeholders shows `{{.PFN}}` instead of the yellow letter | Left page is the mapped catalog file — yellow original missing or overwritten. Upload the yellow Word/Excel (saved Short codes stay). Restart, then Approve once to store SourceFile | **This skill** |
| Approve — save disabled after placeholder edits on `_F16` | Profile is locked. Approve now updates this file. Restart. Rename only if you want a new catalog copy | **This skill** |
| Continue then Back to field list shifted locked Short codes | Generate re-anchors by OpenXML key / same-paragraph slot, never sample text. Mapped `{{.AVCAT}}, {{.PFN}}, {{.PPN}}` clusters split 1:1 onto sibling yellows (duplicate PFN is allowed). **Back to field list** restores the Review list from before Continue. Re-Approve `_F16` once. Restart | **This skill** |
| Review placeholders wiped locks / remapped everything | Open restores `ReviewPlanJson` (no Analyze). Remap unmarked only if you want a re-guess. Re-Approve once so the snapshot is stored. Restart | **This skill** |
| Preview: 0 placeholders / “No yellow-marked spans could be written” | Review had tokens but Generate lost Word spans — restart, Analyze, Continue | **This skill** |
| Preview skips `CHFN`/`RPFN`: overlapping spans | Duplicate yellow of the same name in one paragraph — restart, Analyze, Generate | **This skill** |
| Word letter catalog Preview fails after Approve | Row tokens `{{.PFN}}` without `{{#ds.rows}}` — restart, re-Approve | **This skill** + resminamalar |
| Cover letter Preview empty (AFNUM/ADAT/MSRV/TPCNT) while ACPOS/CHFN fill | Scan defaulted Both → per-person Word. Rebuild; Preview the same row. Re-Approve so the letter is Application header (one file, not `ERDOGAN Arzu`) | **This skill** + resminamalar |
| Catalog Preview of Yuztutma is one identical page per person | Header-only `{{ds.*}}` Word was still generated once per selected person. Rebuild; Preview — **one page**. Forma 16 / şahsy stay per person | **This skill** + resminamalar |
| Excel Review `#` squares stacked at the page corner, not on yellow cells | pdf.js now places Excel marks from cell geometry on the printed table (not sample text `1` / `TUR`). Hard-refresh Review | **This skill** |
| Passport-change Excel sanaw Preview fills only the latest booklet in both stacked tables | Kiçirak table remaps to `PRPN`; merge expands both tables and overlays previous onto `{{.PPN}}`. Person needs two linked passports. Restart, Preview | **This skill** + user-report-templates |
| Cancel-visa letter maps visa `1 (bir)` to TPCNT | Nearby *wizasy ýatyrmak* → `CVCNT`/`CVCTX`. Person *daşary ýurt raýaty* stays TPCNT. Restart, Analyze | **This skill** + user-report-templates |
| Cancel invitation Excel missing stacked number/dates | **CINB** / **CISB** / **CIEB**. Filter `Cancel invitation AS numbers`, `issued dates`, `expiration dates`. Columns *Çakylygyň belgisi* / *resmileşdirilen* / *möhleti* map on Analyze. Restart | **This skill** + user-report-templates |
| WP count `3` mapped but `üç` unidentified / CancelWPCountText missing | Separate yellows: digit → `CWCNT`, words → `CWCTX`. Add list filter `CWCTX` / `CancelWPCountText`. Restart, Analyze | **This skill** |
| Review #10 Gap — cannot Add placeholder (e.g. Mehmet Çırak) | Unmapped Office yellows stay fields. Gaps also show Add and promote on pick (`CHFN`). Restart, Analyze, click the row | **This skill** |
| Cancel WP Excel AS-№ maps to CWNB / WPNM | *AS-№* → **CWAB** (`ASNumber`). *Tassyknama belgisi* → **CWNB**. Restart, Analyze | **This skill** + user-report-templates |
| Cancel WP Excel #9 Hereket edýän çägi has no Add code | **CWLB** (cancel stack) / **WPLC** (current) = `WorkPermitItem.WorkPermittedLocations`. Analyze maps the column to CWLB. Filter `CWLB`, `hereket`, or `Work Permitted Locations`. Restart, Analyze | **This skill** + user-report-templates |
| Excel last column *Goşulmaly hereket çäkleri* maps to WPLC/CWLB or is missing | **AWPLC** = Case summary Work permit location (same on every row). Analyze maps that caption to AWPLC. Filter `AWPLC` / `goşulmaly`. Not WPLC/CWLB | **This skill** + user-report-templates |
| Review Add cannot find linked Work permit item (number, AS, valid to) | Current item: **WPNM** **WPAS** **WPST** **WPED** **WPLC**. Cancel stack stays CWNB/CWAB/CWSB/CWEB/CWLB. Filter `work permit item` / `valid to` / `WPAS`. *Rugsat edilen möhleti* → WPED. Restart, Analyze | **This skill** + user-report-templates |
| Excel roster gaps (names, TUR, …) | Column header + manual inference (`ScanExcelYellowResolver`); not case value match | **This skill** |
| Preview blocking `{{ds.PVFM}}` / `Person_* not found on ApplicationProfileInstance` | Şahsy yellow classified Header wrote `{{ds.CODE}}` for Row-only Person tokens. Continue / Regenerated writes `{{.PVFM}}` (`PDBT`/`PCBT`/`PBPL`/`PFWC` same). Restart, hard-refresh | **This skill** |
| Review shows `{{ds.PLN}}` / Approve blocks `not found on ApplicationProfileInstance` | Row-only codes now stay `{{.PLN}}` even on Header yellow. Analyze again after restart | **This skill** |
| Azure ambiguous guess needs more context | Payload sends role/description + nearby snippet — not the Office file | **This skill** |
| Clarification chat disabled | Needs `TemplateAiScan` AI provider (optional); Analyze does not | **This skill** |
| Config lock | May add **new** templates; Review placeholders may **update the same file** | application-profile |
| Excel catalog Preview blank; pane titled `report_….docx` | Nested Resminamalar keys — Excel bytes converted as Word PDF | **resminamalar** |
| Diff gate fail on Generate | Span addresses; fingerprints **ignore** yellow strip | **This skill** + Convert writer |
| Yellow remains after Approve / catalog Preview | `StripAllYellowMarkup` / `StripAllYellowFills` after write; re-Approve old templates | **This skill** |

## Scope

| In | Out |
|----|-----|
| Yellow-marked `.docx` / `.xlsx` | PNG / JPG / PDF (retired) |
| OpenXML yellow → tokens → Approve | Convert value-match modal |
| Wizard Review/Preview pdf.js pages inside the modal | `#visa-preview-slot` inside wizard |
| | Resminamalar ZIP |

## Locked rules

1. Separate from Convert (yellow marks ≠ instance value match).
2. Yellow only → placeholders; library tokens only.
3. Preserve source Office layout (token writer on copy).
4. **Yellow is scan markup only** — after Generate, strip **all** highlighter/yellow fill from the saved copy (not only substituted runs). Unmapped leftovers (e.g. `6 (alty)` when only VCAT mapped) must not survive catalog Preview.
5. Officer Approve required.
6. Wizard Review/Preview shows the Office file as **pdf.js pages inside the modal** (not `#visa-preview-slot`, not the browser PDF viewer chrome). Numbered marks + row highlight stay. HTML outline is fallback only.
7. Config lock allows **new** templates and **file updates** on an existing catalog name (Review placeholders). Renaming still creates a copy. Catalog scope / applicability stay locked.
8. **Layout-specific guessing patterns** (not one letter): **Official letter** (`№`, date, urgency, `N (words)`, `N (words) aý`, gezeklik); **caption under the line** (Borçnama); **left field label** (Şahsy kagyzy); **inline prose** (Zähmet şertnamasy); **Excel column header** (sanaw). Immediate surround + value shape still combine. Comma in a yellow highlight is a combination candidate (Review **6.1 / 6.2 / 6.3**; Generate writes one span).
9. Resminamalar **Review placeholders** reopens the last **approved** mapping (`ReviewPlanJson` + yellow `SourceFile`). It does **not** re-guess. Remap unmarked is the only re-guess. Config lock does **not** block Approve of the same name — only the Word/Excel file and snapshot are replaced.

## Pipeline

```text
Upload .docx/.xlsx (or Resminamalar Review placeholders)
  → Ingest → ScanOfficeYellowExtractor + body pictures (`{{IMAGE:PPH}}`) (else library {{…}} clusters)
  → Merge/split → Yellow gate (token-backed plans skip the no-yellow fail)
  → Review / optional Clarification
  → ITemplateTokenWriter → StripAllYellow* → diff gate → Extract/Validate → Outline → Approve
```

## Triage

| Layer | Look at |
|-------|---------|
| UI | `TemplateScanDialog.razor`, Resminamalar **Create from yellow marks** / **Review placeholders** |
| Yellow | `ScanOfficeYellowExtractor`, `ScanYellowHighlight*` |
| Generate | `TemplateScanOrchestrator` Office path, `ITemplateTokenWriter`, `StripAllYellowMarkup` / `StripAllYellowFills` |
| Tests | `Visa2026.Module.Tests/TemplateScan/` |

```powershell
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --filter "FullyQualifiedName~TemplateScan"
```
