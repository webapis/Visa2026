# Learnings (append-only): User report templates (Word / Excel seeds)

Purpose: capture Resminamalar / DocxTemplater / Extract–Validate / **`ItemRows`** pitfalls from user-seeded templates under **`Resources/Templates/`**. Agents **read before** debugging merge or placeholder work on a similar template; **append after** a resolved incident.

Keep **`SKILL.md`** stable; **promote** into `SKILL.md` only when the same lesson has recurred.

## How to use

**Before** `ItemRows` merge errors, invalid Extract counts, or new registration/list seeds: skim **## Entries**.

**After** fix is verified in app (Resminamalar OK + Validate green): append one entry (date, template, symptom, root cause, fix, prevent) using the template below.

```markdown
### YYYY-MM-DD — <Basename>.docx (family: ItemRows | …)

- **Symptom**:
- **Root cause**:
- **Fix**:
- **Prevent**:
```

---

## Entries

### 2026-09-02 — Image tokens are `{{IMAGE:Person_Photo}}` not `{{IMAGE:PPH}}`

- **Symptom**: Catalog short code **PPH** wrote `{{IMAGE:PPH}}`. Preview cleared the token instead of injecting `Person.Photo`.
- **Root cause**: `WordUserReportImageInjector` looks up photos by `Person_Photo`. `[\w]+` captures `PPH` as a different key.
- **Fix**: `BuildWordToken` for `IsImage` uses `CanonicalPath`. `TemplateTokenSyntax.TryGetShortCode` maps `Person_Photo` → **PPH**. Injector falls back `PPH` → `Person_Photo`. Create from yellow marks writes the canonical token when a sample portrait is in the Word body.
- **Prevent**: Do not emit `{{IMAGE:PPH}}` on new Word templates. Do not edit seed `.docx` layout. Excel still rejects image tokens.

### 2026-09-01 — `{{ds.PVFM}}` is invalid on ApplicationProfileInstance

- **Symptom**: Create from yellow marks Preview blocked Approve: `Person_VisaApplicationFamilyMembersText` (and other Person row fields) not found on ApplicationProfileInstance.
- **Root cause**: Row-only catalog codes written as `{{ds.CODE}}` bind on the instance. They belong on `ApplicationRosterMergeLine` as `{{.CODE}}` (letters without `{{#ds.rows}}` still fill via first-roster promotion).
- **Fix**: `BuildWordToken` honors catalog Row/Header; scan rewriter turns leftover `{{ds.PVFM}}` into `{{.PVFM}}`.
- **Prevent**: Do not add Person_* getters to ApplicationProfileInstance to silence scan validation.

### 2026-09-01 — `Person_VisaApplicationFamilyMembersText` / **PVFM**

- **Symptom**: Officers needed the employee visa family block in Create from yellow marks / the placeholder picker. Only **SKFM** (`SahsyKagyz_FamilyStatusText`) existed, which is the formatted Maşgala ýagdaýy line.
- **Root cause**: `Person.VisaApplicationFamilyMembersText` had no catalog short code or roster merge getter.
- **Fix**: Catalog **PVFM** → `Person_VisaApplicationFamilyMembersText`. Merge line reads the employee (or sponsor for family-member rows). Sanawy + Şahsy row dicts include the key. Excel header `wiza üçin maşgala` → PVFM. Labels are the officer editor caption, not **Maşgala ýagdaýy**.
- **Prevent**: Do not reuse **SKFM** for the raw stored lines. Do not edit şahsy_kagyz.docx; officers type `{{ds.rows.Person_VisaApplicationFamilyMembersText}}` or `{{.PVFM}}` where they need the raw block.

### 2026-09-01 — Person catalog tokens PCBT / PMNM / PMST / PNTM / PSEF / PSEP

- **Symptom**: Sanaw uses `Person_CountryOfBirthTm`; Forma 16 uses sponsoring-employee name/position; middle name, marital status, and nationality name existed on the merge line but not in the picker.
- **Root cause**: Catalog had codes (`PCBC`, `PNAT`) and first/last name only.
- **Fix**: **PCBT**, **PMNM**, **PMST**, **PNTM**, **PSEF**, **PSEP**. Sanawy/Forma 16 row dicts include the keys. Excel birth column prefers PCBT (name) not PCBC (code). **PSEF** is roster sponsor, not header **SPFNM**. **PNTM** tk-TM is **Raýatlyk ady** (not **Raýatlygy**) so Sanaw nationality **code** column stays **PNAT**.
- **Prevent**: Do not add HireDate / Email / Age until a merge getter exists. Do not reuse **SPFNM** for the family-member sponsor row.

### 2026-09-01 — Education institution / country / graduation year tokens

- **Symptom**: Şahsy kagyz and Sanaw use `Education_InstitutionName` and `Education_CountryCode`, but Create from yellow marks / placeholder picker only had `EGLV` / `EGIY` / `EGSP`.
- **Root cause**: Merge line already exposed institution, country code, and graduation year; catalog never listed them.
- **Fix**: Short codes **EGIN**, **EGCC**, **EGYR** (`PersonEducation` / Education). Sanawy + Şahsy row dicts include the keys. Excel header `okan ýeri` → EGIN.
- **Prevent**: When a map §6 token exists on the roster merge line, add the catalog short code in the same change.

### 2026-09-01 — `Person_PreviousWorkplacesInTurkmenistan` / `PWTM` (Şahsy kagyz F09)

- **Symptom**: Şahsy kagyz **Türkmenistanda öňki işlän ýerleri** had no merge token; map F09 was a static blank underline.
- **Root cause**: Person field existed but catalog, roster merge line, and `BuildSahsyKagyzRowDictionary` had no key.
- **Fix**: Catalog **PWTM** → `Person_PreviousWorkplacesInTurkmenistan`; merge line + sahsy/sanawy row dicts; map **1.0.5**. Word seed still needs the officer to type `{{ds.rows.Person_PreviousWorkplacesInTurkmenistan}}` on the underline (do not edit `.docx` in repo).
- **Prevent**: New Person merge fields need catalog + `ApplicationRosterMergeLine` + the row dictionary that template actually uses (`BuildSahsyKagyzRowDictionary` here).

### 2026-09-01 — Passport type / issued country / authority tokens + related-BO groups

- **Symptom**: Yellow marks for passport type, issued country, and issuing authority had no library tokens. Officers and AI saw a flat placeholder list mixed across Person, company, wekil, and passport.
- **Root cause**: Catalog had `PPN`/`PPIS`/`PPED` only. `Passport_Authority` / `Passport_CountryCode` / `Passport_CountryTm` existed on the roster merge line but were not catalogued. `PassportType` had no merge property. Manual and Azure payload were a single A–Z list.
- **Fix**: `Passport_TypeTm` + short codes `PPTP`, `PPAT`, `PPCC`, `PPCT`. Catalog `relatedBo` groups the officer Placeholder manual, Review Add-placeholder optgroups, and Azure `allowedTokensByBo`.
- **Prevent**: New tokens need `packKey` (profile gate) and `relatedBo` (manual/AI group). Do not put roster passport fields on wekil `RPPA` / signatory `CHPA`.

### 2026-09-01 — Company registration date placeholder `ACRDT`

- **Symptom**: Yellow `02.02.2009ý.` on borçnama mapped to `ApplicationDateText` because no company registration date existed.
- **Root cause**: `CompanyProfile` had no registration date; catalog had no token; scan date regex always chose `ADAT`.
- **Fix**: `CompanyProfile.RegistrationDate` + `Application_Company_RegistrationDateText` / `{{ds.ACRDT}}`. Tenant JSON `2009-02-02`.
- **Prevent**: Isolated dates next to hasaba alyş / şahamça are company registration, not application date.

### 2026-09-01 — `6aylık-BORÇNAMA_02.docx` (family: letter / loose `{{.X}}`)

- **Symptom**: Catalog Preview empty after yellow-mark Approve; wizard outline had 8 placeholders including `{{.PFN}}` and no `{{#ds.rows}}`.
- **Root cause**: DocxTemplater resolves `{{.PFN}}` on `ds` when there is no row loop; those keys were not on the root bind model.
- **Fix**: `UserReportMergeDataHelper.PromoteLooseRowTokensOntoRoot` copies first-roster values onto `ds` when extracted tokens include `.X` and no `#ds.rows`.
- **Prevent**: Scan Word letters may emit row short codes; merge must flatten first person or the template needs a loop.

### 2026-05-28 — `sahsy_kagyz.docx` (family: **ItemRows**, root **`ApplicationItem`**)

- **Symptom**: Resminamalar failed (0/9): `'{{ds.rows.Person_FullName}}' could not be replaced` with context `Familiýasy, ady, atasynyň ady >> {{ds.rows.Person_FullName}} << Doglan senesi…`.
- **Root cause**: Template had **`{{ds.rows.*}}`** tokens but **no** `{{#ds.rows}}` / `{{/ds.rows}}` loop (and no `{{:s:}}{{:PageBreak}}`). DocxTemplater cannot bind `ds.rows.Property` outside a row loop. Some tokens were split across Word runs (spell-check); extractor still finds them via `InnerText`.
- **Fix** (Word): Insert `{{#ds.rows}}` before the form, `{{:s:}}{{:PageBreak}}` + `{{/ds.rows}}` before `sectPr` (own paragraphs). Rebuild embedded template in repo.
- **Fix** (code): **`BuildSahsyKagyzStyleRows`** + **`EnsureSahsyKagyzRowsWhenNeeded`** (same pattern as Forma 16).
- **Prevent**: After placing yellow placeholders, always add §7 loop tokens before Extract/Validate; confirm **`#ds.rows` count > 0** in docx XML or Extract output.

---

### 2026-05-20 — `Forma_16.docx` (family: **ItemRows**, root **`ApplicationItem`**)

- **Symptom**: Resminamalar failed: `'{{ds.rows.Person_NationalityCode}}' could not be replaced` (§2 Raýatlygy). Earlier: **65 of 93** placeholders invalid after Extract; after Word cleanup **66/66** valid and merge succeeded (TUR, photo, full form).
- **Root cause** (merge): **`{{ds.rows.*}}`** requires **`List<Dictionary<string, object>>`** (or **`List<IDictionary<string, object>>`** before `BindModel("ds", …)`). A typed POCO row type (**`RegistrationForm16MergeRow`**) did **not** bind `{{ds.rows.Property}}` like **`Contract_Inv.docx`**. If the wrong row builder runs (**`BuildLaborContractRowDictionary`**), fields above §1 that exist on labor rows still merge; **`Person_NationalityCode`** is **not** on labor rows — looks like a “new placeholder” bug but is **wrong row set**.
- **Root cause** (validation): Word **split** placeholders across runs → Extract invents fragments (e.g. `.Person_*`, partial `ds.rows`) → high invalid count until user retypes each token **in one run**.
- **Fix** (code): Revert Forma 16 rows to **`UserReportMergeDataHelper.BuildRegistrationForm16RowDictionary`**; keep **`EnsureForma16RowsWhenNeeded`** + **`IsForma16UserReportTemplate`**; cast rows to **`IDictionary<string, object>`** in **`UserReportGenerator.RenderTemplateAsync`**. Do **not** reintroduce typed row classes for **`{{ds.rows.*}}`** without proving DocxTemplater binding.
- **Fix** (Word): Retype tokens per approved **`Forma_16_map.md`** §6; **Extract → Validate** until all placeholders valid; prefer **`{{ds.rows.X}}`** or **`{{.X}}`** inside **`{{#ds.rows}}`** (both OK with dict rows).
- **Prevent**:
  - **`Person_NationalityCode`** is **not** a special BO binding — same **`ApplicationItem`** `[NotMapped]` as Xtra **`RegistrationForm16Report`** and **`BuildSanawyRowDictionary`**; add keys only in **`BuildRegistrationForm16RowDictionary`** (or sanawy/excel builders), not a one-off merge path.
  - For registration **`ItemRows`**, confirm runtime uses **`BuildRegistrationForm16StyleRows`**, not labor/sanawy unless template detection matches (see **`UserReportMergeDataHelper.IsForma16UserReportTemplate`**).
  - High invalid count after Extract → fix Word tokens first; do not add C# properties for fragments that are not real map §6 tokens.
  - Map note: **`Forma_16_map.md`** §6 — type each placeholder in a single Word run.

---
