# Word report layout families — reference matrix

Companion to **`SKILL.md`**. Use when classifying a new report or aligning typography with an existing generator.

**OOXML note:** `w:sz` / `FontSize` values are **half-points** (e.g. `30` = 15 pt).

## Data anchors (business objects)

| Type | Path | Role |
|------|------|------|
| `Application` | `Visa2026.Module/BusinessObjects/Application.cs` | Root for **Resminamalar**; header `ds.*`; owns collections below. |
| `ApplicationItem` | `…/ApplicationItem.cs` | Per-person merge fields; typical **row** in item/sanawy lists. |
| `Registration` | `…/Registration.cs` | Registration list / movement reports; **rows** from `Application.Registrations`. |
| `BusinessTrip` | `…/BusinessTrip.cs` | Trip sanawy / letters; **rows** from `Application.BusinessTrips`. |

Placeholder names and dictionary keys must match **`docs/WORD_REPORT_GENERATION_IDEA.md`** and actual properties—if unclear, **ask the user** (see **`SKILL.md`** → *Missing information*).

**`FormTemplates/`:** ministry **scans** (static text + layout) and **`*_map.md`** (dynamic fields, widths, BO-oriented labels) — use both when designing or reviewing Word output; when paralleling an XtraReport, reconcile map + Word + `Visa2026.Module/Reports/*`.

## Inventory — embedded Word templates (`Visa2026.Module/Resources`)

Authoritative list: **`Visa2026.Module/Visa2026.Module.csproj`** (`EmbeddedResource` for `Resources\*.docx`). Group for **review batches**:

| Group | Templates (file names) |
|-------|-------------------------|
| **Business trip** | `BusinessTrip_Sanawy.docx`, `BusinessTrip_Arrival_Letter.docx`, `BusinessTrip_Departure_Letter.docx` |
| **Sanawy (app)** | `App_Sanawy_Letter.docx` |
| **Letters — registration / info** | `App_Reg_Check_In_Letter.docx`, `App_Reg_Check_In_Internal_Letter.docx`, `App_Reg_Check_Out_Letter.docx`, `App_Reg_Check_Out_Internal_Letter.docx`, `App_Reg_Ext_Letter.docx`, `App_Reg_Info_Change_Address_Letter.docx`, `App_Reg_Info_Change_Passport_Letter.docx` |
| **Letters — invitation / visa / cancel / change** | `App_Inv_Letter.docx`, `App_Inv_And_WP_Letter.docx`, `App_Inv_FM_Letter.docx`, `App_Cancel_Visa_Letter.docx`, `App_Cancel_Visa_And_WP_Letter.docx`, `App_Cancel_Inv_WP_Letter.docx`, `App_Change_Passport_Letter.docx`, `App_Exit_Visa_Letter.docx`, `App_Additional_WP_Location_Letter.docx`, `App_Border_Zone_Permission_Letter.docx`, `App_Change_Inv_Letter.docx`, `App_Visa_And_WP_Ext_Letter.docx`, `App_Visa_Ext_FM_Letter.docx` |
| **Item / table forms** | `App_Cancel_Inv_WP_Item.docx`, `App_Cancel_Visa_And_WP_Item.docx`, `App_Change_Inv_Item.docx`, `App_Border_Zone_Permission_Item.docx`, `App_Inv_And_WP_Borcnama_Item.docx`, `App_Labor_Contract_Item.docx` |

Other **`Resources\*.docx`** (e.g. legacy mail-merge **`App_Reg_Check_In.docx`**) may exist in the project file—confirm on disk when tracing consumers.

**Review progress:** **`review-status.md`** (Pending / In review / Completed / Blocked) — one row per Resminamalar template.

## Family codes (use in Phase 1 / `learnings.md`)

| Code | Name | Page | Typical margins (twips L/R, T/B) | Primary body (half-pt) | Generator / pattern in `tools/GenerateTemplates/Program.cs` | Example `*ReportDef` / template |
|------|------|------|----------------------------------|-------------------------|---------------------------------------------------------------|----------------------------------|
| **L1** | Simple portrait letter (migration service) | A4 portrait | 1800 / 1440 | 30 (15 pt) headers & justified body | `MakeSimpleLetterTemplate` | `App_Reg_*_Letter`, many `App_*_Letter` using that helper |
| **L2** | Ministry letter (Group A) | A4 portrait | 1800 / 1440 | 30 body; urgency line 30 italic | `MakeGroupALetterTemplate` | `App_Inv_Letter`, `App_Inv_And_WP_Letter`, … |
| **L3** | Letter variants (custom header/body blocks) | A4 portrait | Usually 1800 / 1440 | 30 or 24 per block | Other `Make*Letter` / inline builders in same file | FM, exit visa, specialized letters |
| **T1** | Landscape personnel sanawy | A4 landscape | Per map / column math | Table cells often **24** (12 pt) | `BusinessTrip` grid + `MakeSanawyTemplate` (App 14-col) | `BusinessTripSanawyReportDef`, `AppSanawyLetterReportDefBase` |
| **F1** | Statutory / ministry **item** form | A4 portrait | Tighter (e.g. 1020 L/R) | 20–28 mix; captions **14** | `MakeBorcnamaTemplate` | `App_Inv_And_WP_Borcnama_Item` |
| **F2** | Long structured **item** (sections) | A4 portrait | Per template | Section titles vs body | `MakeLaborContractTemplate` | `App_Labor_Contract_Item` |
| **T2** | Compact **item** table (row loop) | portrait or landscape | Per map | Cell lines **24** typical | Inline column arrays + `MakeCellParagraphs` | Cancel/change/border item `.docx` |

## Defaults (unless the **FormTemplates** scan forces otherwise)

- **Font:** Times New Roman (`RunFonts` Ascii + HighAnsi).
- **Page:** A4 — `PageSize` width **11906**, height **16838** twips (portrait unless **T1**).
- **New report:** Pick the **closest family** and **reuse** the same helper or copy its margins/font sizes before introducing one-off numbers.

## When a report does not fit any row

Document in **`learnings.md`** and add a **new code** (e.g. **F3**) here in the same table style after the template ships.
