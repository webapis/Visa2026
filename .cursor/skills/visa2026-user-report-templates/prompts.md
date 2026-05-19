# Prompts — visa2026-user-report-templates

Copy-paste any of these into Cursor chat to steer the agent toward this skill. Mention **`Resources/Templates`**, **`UserReportTemplateUpdater`**, **`EnsureTemplateExists`**, or **`EnsureExcelTemplateExists`** for best matching.

In Cursor you can also reference the skill explicitly: **`@visa2026-user-report-templates`**.

**Scope:** user-editable **Resminamalar** seeds:

| Format | Folder |
|--------|--------|
| **Word** | `Visa2026.Module/Resources/Templates/*.docx` |
| **Excel** | `Visa2026.Module/Resources/Templates/Excel/*.xlsx` |

**Out of scope:** code-backed ministry Word reports (`Resources/App_*.docx`, `IWordReportDefinition`) → **`visa2026-word-reports`**.

**Agent will not edit layout** in the repo — you author `.docx` / `.xlsx` in Office; the agent embeds, registers seeds, and maps placeholders in C#.

---

## Create a new Word seed

- `Embed Resources/Templates/MyTemplate.docx as a user report seed and register it in UserReportTemplateUpdater.`
- `Add a new EnsureTemplateExists entry for MyTemplate.docx under Resources/Templates.`
- `Register Resources/Templates/MyLetter_uzt.docx — Application root, only App_Visa_and_WP_Ext, GT-15 project contracts (NameTm contains GT-15).`

**Example with details:**

> Register `Resources/Templates/MyLetter_uzt.docx` as template name **MyLetter_uzt**, Application root, applicable application types **App_Visa_and_WP_Ext**, project contracts where NameTm contains **GT-15**, sort order 56.

---

## Create a new Excel seed

- `Embed Resources/Templates/Excel/MyList_uzt.xlsx as a user report seed via EnsureExcelTemplateExists.`
- `Register 433-style Excel list template — ApplicationItem root, ExcelMergeMode ItemList, App_WP_Ext and visa extension types.`
- `Wire Resources/Templates/Excel/MyList_uzt.xlsx into csproj and UserReportTemplateUpdater with TemplateOutputFormat Excel.`

**Example with details:**

> I added `Resources/Templates/Excel/MyList_uzt.xlsx`. Register as **MyList**, ApplicationItem root, **ExcelMergeMode.ItemList**, applicable types **App_WP_Ext**, **App_Visa_and_WP_Ext**, sort order 63. Header uses `{{ds.FullApplicationNumber}}`; data row has `{{#ds.rows}}` and `{{.Person_FullName}}` columns. Do not edit the xlsx layout in the repo.

**Excel authoring reminders (tell agent if relevant):**

- `.xlsx` only (save `.xls` as `.xlsx` first)
- Do not create `.xlsx` with ZipFile/Compress-Archive
- List row: `{{#ds.rows}}` on the data row; `{{.Property}}` per column — see **`docs/EXCEL_PLACEHOLDER_REFERENCE.md`**

---

## Update an existing seed (Word or Excel)

**Word examples:** Borcnama, Contract, Contract Inv, GT-15_Sazakow_uzt, Sanaw_uzt  
**Excel examples:** Gurlusyk (`433_gurlusyk_uzt.xlsx`), 433-ek sanawy (`433-ek_uzt.xlsx`)

- `Update the Contract Inv seed: applicable application types App_Inv_And_WP only.`
- `Add App_WP_Ext to applicable application types for the Gurlusyk Excel seed.`
- `Change Gurlusyk Excel seed excelMergeMode to ItemList and refresh description.`
- `Rename seeded template file to GT-15_Sazakow_uzt.docx and sync csproj + UserReportTemplateUpdater.`
- `Set GT-15_Elyasow_uzt seed to link all project contracts where NameTm contains GT-15.`
- `Adjust visibility criteria for the Sanaw user report template seed.`

**Visibility (property-based — do not mention Applicability mode):**

| Goal | Say in prompt |
|------|----------------|
| All application types | `applicable application types: null / empty` |
| Specific types only | `applicable application types: App_Inv_And_WP, App_WP_Ext` |
| GT-15 contracts | `applicableProjectContractNameTmContains: GT-15` |
| Extra rule | `visibility criteria: …` (criteria expression) |

---

## Placeholder lookup

**Word + shared property names:**

- `Which placeholder should I use for [field] in a user report template?`
- `What is the {{ds.*}} token for [business data] on an Application-root template?`
- `What row placeholder should I use inside {{#ds.rows}} for [column]?`
- `Which placeholder for visa period in an ApplicationItem-root template like Contract Inv?`

**Excel list templates:**

- `Which {{.…}} column token for Education_LevelAndInstitutionTm in Gurlusyk Excel?`
- `How do I place {{#ds.rows}} in 433-ek_uzt.xlsx?`
- `Excel user template header placeholder for application date?`

See **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`**, **`docs/EXCEL_PLACEHOLDER_REFERENCE.md`**, **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`**.

---

## Implement a missing placeholder

Works for **both** Word and Excel (same `Application` / `ApplicationItem` properties).

- `Add a missing placeholder for [business data] to the user report template system.`
- `{{ds.VisaPeriod_NameTm}} fails validation on Contract Inv — fix validation and BO mapping.`
- `Add {{.Visa_CategoryTm}} for Excel 433-ek list — update BuildExcelItemListRowDictionary if needed.`
- `Add the row key for [field] in BuildLaborContractRowDictionary for Contract Word templates.`

---

## After authoring — what’s left?

- `I finished the Word template with placeholders — what else for Resminamalar?` (see **After authoring** in `SKILL.md`)
- `Checklist after creating Excel user report template`

## Validate / troubleshoot

- `Extract + Validate placeholders for Resources/Templates/Borcnama.docx.`
- `Validate Resources/Templates/Excel/433_gurlusyk_uzt.xlsx placeholders in UI.`
- `Why is Gurlusyk not in the Resminamalar zip?` (visibility + application type + active flag)
- `ClosedXML NullReferenceException loading seeded xlsx` (often broken OOXML from zip-folder packaging)

---

## General / skill activation

- `user report template seed`
- `Resources/Templates embedded template`
- `Resources/Templates/Excel xlsx seed`
- `UserReportTemplateUpdater` / `EnsureTemplateExists` / `EnsureExcelTemplateExists`
- `visa2026-user-report-templates skill`

---

## Full prompt template — Word

```
Use visa2026-user-report-templates.

I added Resources/Templates/[FileName].docx.

- Template name in app: [Name]
- Root BO: [Application | ApplicationItem]
- Application types: [all | App_Inv_And_WP, …]
- Project contracts: [none | GT-15 via NameTm | …]
- Visibility criteria: [empty | expression]
- Sort order: [number]

Do not edit the Word file layout — only csproj embed, UserReportTemplateUpdater, and placeholder/BO work if needed.
```

---

## Full prompt template — Excel

```
Use visa2026-user-report-templates.

I added Resources/Templates/Excel/[FileName].xlsx (saved from Excel, not zipped).

- Template name in app: [Name]
- Root BO: ApplicationItem
- ExcelMergeMode: ItemList
- Application types: [App_WP_Ext, App_Visa_and_WP_Ext, …]
- Project contracts: [none | GT-15 | …]
- Visibility criteria: [empty | expression]
- Sort order: [number]
- Header placeholders: {{ds.…}}
- Data row: {{#ds.rows}} + {{.Column}} per ministry column

Do not edit xlsx layout in repo — only csproj, EnsureExcelTemplateExists, and C# row keys if a column is new.
```

---

## Related skills

| Task | Skill |
|------|--------|
| Code-backed Word reports, `GenerateTemplates`, ministry scan parity | **`visa2026-word-reports`** |
| Predefined XtraReports in `Visa2026.Module/Reports` | **`report-predefined-xaf`** |
| Commit after build | **`commit-after-verify`** |
