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

## Map + scan (mandatory before everything else)

- `Create Forma_16_map.md from scan — I will add Forma_16.png to Resources/Templates`
- `I added Forma_16.png — draft the map (family, placeholders, Resminamalar output) before I design the docx`
- `Approve Forma_16_map.md — ready to author Forma_16.docx placeholders per field contract`
- `Backfill Borcnama_map.md from scan — legacy seed missing map`

**Agent:** No placeholders / embed / register until **`<basename>_map.md`** (full §0–§15 per **`docs/USER_REPORT_MAP_STANDARD.md`**) + scan exist and map is **Approved**. §6 tokens are exact. See **`reference-map-contract.md`**, **`reference-deterministic-generation.md`**.

- `Backfill Borcnama_map.md to USER_REPORT_MAP_STANDARD format with scan`
- `Ensure Forma_16_map.md matches map standard §0–§15`

---

## Template family (ask user first — do not infer)

- `Register Forma_16.docx — confirm Word layout ItemRows vs ItemScalar and placeholder pattern before seeding.`
- `What template family is Borcnama? ItemRows + ApplicationItem validation — see reference-template-families.md`
- `New user report *_ItemRows.docx — one doc per application, page per ApplicationItem, {{#ds.rows}}`

**Agent must confirm:** layout ID (`AppScalar` | `ItemRows` | `ItemRoster` | `ItemScalar`), `RootBoType`, Resminamalar output, then Extract token check. See **`reference-template-families.md`**.

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

## Word photo placeholders (custom injector)

- `Which token for person photo in a user report Word template?` → **`{{IMAGE:Person_Photo}}`** (not `:img()`, not `{{.Person_Photo}}`)
- `Add employee photo roster seed — Application root, {{#ds.ApplicationItems}}, name + photo columns.`
- `Preview employee photo roster without running Blazor` → `dotnet run --project tools/PreviewWordReports -- employee-photo-roster`
- `Photo column blank in Resminamalar` → confirm **`{{IMAGE:Person_Photo}}`**, Validate passes, person has **`Person.Photo`** bytes

**Agent rules:** DocxTemplater for text only; **`WordUserReportImageInjector`** for photos. Do not add **`DocxTemplater.Images`**. See **Word photos** section in **`SKILL.md`**.

---

## Employee photo roster — experience / troubleshooting

Use when an **`ItemRoster`** template (or similar) merges names but photos fail.

**Correct template tokens:**

```
{{ds.FullApplicationNumber}}
{{#ds.ApplicationItems}}
{{.Person_FullName}}    |    {{IMAGE:Person_Photo}}
{{:s:}}{{:PageBreak}}   (optional, between items)
{{/ds.ApplicationItems}}
```

**Prompts:**

- `Resminamalar shows literal {{IMAGE:Person_Photo}} but names merge — triage user report photo injector`
- `PreviewWordReports employee-photo-roster works; in-app Word user report photo does not`
- `Register Employee photo roster sample seed — Application root, ApplicationItems loop, IMAGE:Person_Photo`
- `DocxTemplater could not replace ds.ApplicationItems.Person_FullName — fix photo roster placeholders`

**Agent checklist (store this experience):**

1. **Root BO** = **`Application`**; loop = **`{{#ds.ApplicationItems}}`**; row name = **`{{.Person_FullName}}`**; photo = **`{{IMAGE:Person_Photo}}`**.
2. **Do not** use DocxTemplater **`:img()`** / **`DocxTemplater.Images`** for user-report seeds.
3. **Resminamalar** passes **`GetActiveApplicationItems(objectSpace, application)`** into **`UserReportGenerator`** — collection rows must come from that list, not a partially loaded navigation collection.
4. After merge: **`WordUserReportMergeImageExtractor`** → **`WordUserReportImageInjector`** when template contains **`{{IMAGE:`** (clears literal token even when photo bytes are empty).
5. **Preview OK, app literal token** → rebuild **`Visa2026.Module`**, restart Blazor (DLL lock common on Windows).
6. **Extract + Validate** on template; re-seed or re-upload if DB file still has old tokens.
7. **Blank photo cell** (no literal) = no **`Person.Photo`** data — not an injector failure.

**Reference:** **`SKILL.md`** (Word photos, photo roster table, common failures), **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** (Photos).

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

Photos (if any): use {{IMAGE:Person_Photo}} in the photo cell inside {{#ds.ApplicationItems}} or {{#ds.rows}} — not :img().
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
