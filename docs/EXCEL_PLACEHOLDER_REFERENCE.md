# Excel user report placeholders

Excel templates use the **same token syntax** as Word user templates (`{{ds.…}}`, `{{#ds.rows}}`, `{{.…}}`). Property names match **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`**.

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

## Single-item templates

Planned for **v1.1** (`ExcelMergeMode.SingleItem`).

## Tools

- **`tools/ExcelTemplateSpike`** — `test-gurlusyk`, `build-433-ek`, `test-433-ek` (see `Resources/Templates/Excel/README.md`).
