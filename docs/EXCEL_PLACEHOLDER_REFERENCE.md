# Excel user report placeholders

Excel templates use the **same token syntax** as Word user templates (`{{ds.…}}`, `{{#ds.rows}}`, `{{.…}}`). Property names match **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`**.

## List templates (v1)

Used from **Resminamalar** on **Application** with **`ExcelMergeMode = ItemList`**.

| Area | Validated against | Example |
|------|-------------------|---------|
| Header cells | `Application` | `{{ds.FullApplicationNumber}}`, `{{ds.ApplicationDateText}}` |
| Loop row | `ApplicationItem` | `{{.Person_FullName}}`, `{{.Passport_Number}}` |

### Loop row rules (Phase 0 spike)

1. Put **`{{#ds.rows}}`** in **any cell on the template data row** (the row that will be copied for each `ApplicationItem`).
2. Optional: put **`{{/ds.rows}}`** on the **next row**; that row is **deleted** after merge.
3. **Do not merge cells** in the template data row (v1).
4. Row tokens use the **`{{.PropertyName}}`** prefix (same as Word `{{#ds.rows}}` sections).
5. Non-deleted items only (`IsDeleted = false`).

### Seed layout (`Personnel_List_Seed.xlsx`)

| Row | Content |
|-----|---------|
| 1–2 | Application header placeholders |
| 4 | Column titles |
| 5 | `{{#ds.rows}}` + `{{.RowNumber}}`, `{{.Person_FullName}}`, … |
| 6 | `{{/ds.rows}}` (removed after merge) |

## Single-item templates

Planned for **v1.1** (`ExcelMergeMode.SingleItem`).

## Tools

- **`tools/ExcelTemplateSpike`** — `generate-seed` writes `Resources/Templates/Excel/Personnel_List_Seed.xlsx`; `merge` runs ClosedXML merge against sample data.
