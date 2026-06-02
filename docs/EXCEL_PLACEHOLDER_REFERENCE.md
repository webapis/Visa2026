# Excel user report placeholders

Excel templates use the **same token syntax** as Word user templates (`{{ds.…}}`, `{{#ds.rows}}`, `{{.…}}`). Property names and **illustrative merged output** (what users see after merge) are in **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** — use the **Example output** column and [Composite / multiline outputs](WORD_REPORT_PLACEHOLDER_REFERENCE.md#composite--multiline-outputs) for ministry Excel cells.

## List templates (v1)

Used from **Resminamalar** on **Application** with **`ExcelMergeMode = ItemList`**.

| Area | Validated against | Example |
|------|-------------------|---------|
| Header cells | `Application` | `{{ds.FullApplicationNumber}}`, `{{ds.ApplicationDateText}}` |
| Loop row | `ApplicationItem` | `{{.Person_FullName}}`, `{{.Passport_Number}}`, `{{.Education_LevelAndInstitutionTm}}`, … |

Row tokens resolve to **`ApplicationItem`** getters (see **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`**), including combined fields such as **`{{.Education_LevelAndInstitutionTm}}`** (level + institution) and **`{{.Visa_DurationFrequencyBlock}}`** (multiline visa validity + number + category — enable wrap text on that column in the template).

### Loop row rules (Phase 0 spike)

1. Put **`{{#ds.rows}}`** in **any cell on the template data row** (the row that will be copied for each `ApplicationItem`).
2. Optional: put **`{{/ds.rows}}`** on the **next row**; that row is **deleted** after merge.
3. **Do not merge cells** in the template data row (v1).
4. Row tokens use the **`{{.PropertyName}}`** prefix (same as Word `{{#ds.rows}}` sections).
5. Non-deleted items only (`IsDeleted = false`).

### Seed layouts

Ministry templates live under `Resources/Templates/Excel/` (e.g. **`433_gurlusyk_uzt.xlsx`**, **`433-ek_uzt.xlsx`**). Each uses a header row, one data row with **`{{#ds.rows}}`** plus **`{{.…}}`** column tokens, and optional **`{{/ds.rows}}`** on the following row.

| Template | **Möhleti we gezekligi** data cell | Recommended placeholder |
|----------|-----------------------------------|-------------------------|
| **Gurlusyk** (`433_gurlusyk_uzt.xlsx`) | Same column as header (e.g. **P4** on the `{{#ds.rows}}` row) | **`{{.Visa_DurationFrequencyBlock}}`** — multiline: start date, end date, `(visa no)`, category (e.g. köp gezeklik). Enable **wrap text** on the column. |
| **433-ek sanawy** (`433-ek_uzt.xlsx`) | Column **L** on the data row (e.g. **L5**) | **`{{.Visa_StartDateText}} {{.Visa_ExpirationDateText}} ({{.Visa_Number}}) {{.Visa_CategoryTm}}`** — single line |
| **Wiza ýatyrmak sanaw** (`wiza_yatyrylmak_sanaw.xlsx`) | **J–L** on data row | **`{{.CancelVisa_NumberBlock}}`**, **`{{.CancelVisa_StartDateBlock}}`**, **`{{.CancelVisa_ExpirationDateBlock}}`** — multiline: **CurrentVisa** then **NextVisa** in the **same row** (`App_Cancel_Visa` only). Enable **wrap text**. |

Both forms read the same **`ApplicationItem.CurrentVisa`** data. Prefer **`Visa_DurationFrequencyBlock`** on Gurlusyk because the ministry layout expects stacked lines, not one long line. For cancel-visa lists, use **`CancelVisa_*Block`** (not **`Visa_Number`** alone) when **NextVisa** may be set.

Do **not** use application-level tokens such as `{{ds.Application_VisaPeriod_NameTm}}` in Excel row cells — they are for Word headers or the wrong root.

If Gurlusyk still shows blanks after deploy: confirm **Extract + Validate** passes for `Visa_DurationFrequencyBlock`, ensure items have **Current visa** filled, and refresh the seed file (DEBUG updater reload, or re-upload **Template file** in Release).

## Single-item templates

Planned for **v1.1** (`ExcelMergeMode.SingleItem`).

## Tools

- **`tools/ExcelTemplateSpike`** — `test-gurlusyk`, `build-433-ek`, `test-433-ek` (see `Resources/Templates/Excel/README.md`).
