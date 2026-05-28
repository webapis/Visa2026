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
