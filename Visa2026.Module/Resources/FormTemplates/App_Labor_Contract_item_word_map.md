# Map: Zähmet şertnamasy (Labor Contract) — Word Version

**Status:** ✅ Implemented — `MakeLaborContractTemplate()` redesigned per map; `AppLaborContractItemReportDef` cross-cutting; preview `labor-contract` passes.

---

## Report Identity

| Field | Value |
| --- | --- |
| **Word ReportDef class** | `AppLaborContractItemReportDef` |
| **XtraReport class** | `AppLaborContractItemReportV2` (reference for field mapping) |
| **Template filename** | `App_Labor_Contract_Item.docx` |
| **Embedded resource** | `Visa2026.Module.Resources.App_Labor_Contract_Item.docx` |
| **Display name (Tm)** | `Zähmet şertnamasy — Şahsy` |
| **Application type** | All (cross-cutting) — visible for any application with items |
| **Reference scan** | `App_Labor_Contract_item.png` |

---

## Data

| Field | Value |
| --- | --- |
| **Root data type (BO)** | `Application` (controller entry point) |
| **Row data type** | `ApplicationItem` (per-person contract) |
| **Row source** | `application.ApplicationItems` (all non-deleted items) |
| **Applicable types** | All (cross-cutting) — `ApplicableApplicationTypeNames` is empty array |
| **IsApplicable rule** | `application.ApplicationItems?.Any() == true` — visible if application has any items |
| **Output mode** | One `.docx` per person, combined in zip download |
| **Fill method** | `FillListForm` with `{{#ds.rows}}` loop + `{{:s:}}{{:PageBreak}}` between items |

---

## Page Setup

| Property | Value |
| --- | --- |
| **Layout family** | **F2** — Sectioned item contract |
| **Paper** | A4 Portrait |
| **Margins** | Tighter margins (~756 twips L/R, 600 twips T, 800 twips B) — fit one page |
| **Font** | Times New Roman — 13pt bold title, 12pt bold section headers, 11pt body |
| **Goal** | Single A4 page per person (like scan) |

---

## Layout Structure (7 Sections + Signatures)

### Title Block
| Element | Style | Static/Dynamic |
|---------|-------|----------------|
| `ZÄHMET ŞERTNAMASY` | 13pt bold, centered | Static |
| `Aşgabat şäheri` | 11pt bold, left-aligned | Static |

### Intro Paragraph (Dynamic, Justified)
**Template:** `"{{Application_SponsorName}}" Türk kärhanasynyň Türkmenistandaky şahamçasynyň Müdiri {{Application_SponsorSignatory}} bilen mundan beýläk "IŞ BERIJI" diýip atlandyrylýan, beýleki tarapyndan "IŞGÄR" diýip atlandyrylýan **{{Person_FullName}}** arasynda zähmet şertnamasy baglaşyldy. IŞGÄR **{{Position_PositionTm}}** wezipesine işe kabul edilýär.`

**Note:** Employee name and position are **bold** within justified paragraph.

### Section 1: Iş berijiniň borçlary
| Element | Content |
|---------|---------|
| Header | `1. Iş berijiniň borçlary` — 12pt bold |
| Body (static) | 1.1–1.4 bullet clauses, 11pt, justified, indented |

**Static text (from XtraReport):**
```
1.1. Hünärine görä iş bilen üpjün etmelidir.
1.2. Her aý aýlyk zähmet hakyny bellenilen güni tölemelidir.
1.3. Hereket edýän Türkmenistanyň Zähmet baradaky kanunlar kodeksine laýyklykda kesgitlenen möhletde ýyllyk zähmet rugsadyny bermelidir.
1.4. Şertnamanyň möhleti boýunça hereket edýän Türkmenistanyň Zähmet baradaky kanunlar kodeksine laýyklykda iş üçin oňaýly şertleri örtänmeli, sosial goraglary we beýleki kepillikleri bermelidir.
```

### Section 2: Işgäriň borçlary
| Element | Content |
|---------|---------|
| Header | `2. Işgäriň borçlary` — 12pt bold |
| Body (static) | 2.1–2.5 clauses, 11pt, justified, indented |

**Static text:**
```
2.1. Bu şertnama laýyklykda tabşyrylan işi etmeli.
2.2. Kärhanadaky içerki düzgüne, tehniki we önümçilik tertibine tabyn bolmaly, zähmet howpsuzlygy we zähmeti goramak düzgünlerini berjaý etmeli.
2.3. Öz iş ýerini, kärhana degişli bolan iş enjamlaryny, abzallary arassa saklamaly we seresaplylyk bilen ulanyp olaryň abatlagyny gazanmaly.
2.4. Kärhananyň iş syrlaryny aýan etmeli däldir.
2.5. Işleýän bölüminiň ýolbaşçysynyň tabşyryklaryny we öz zähmet borçlaryny ak ýürek bilen ýerine ýetirmelidir.
```

### Section 3: Iş we dynç alyş düzgüni
**Static text:**
```
3.1. Iş we dynç alyş wagtynyň tertibi kärhananyň içerki düzgünine laýyklykda kesgitlenilýär.
3.2. Işgär üçin 8 (sekiz) sagatlyk iş günü we 6 (alty) günlük iş hepdesi kesgitlenilýär.
3.3. Önümçilik zerurlygy ýüze çykan wagty, Türkmenistanyň hereket edýän kanunlaryna laýyklykda işgär iş wagtyndan artyk möhlet bilen işdedilip bilner.
3.4. Aýlyk zähmet haky ştat birligine laýyklykda tölenýär.
```

### Section 4: Zähmet şertnamasynyň ýatyrylmagy
**Static text:**
```
Zähmet şertnamasy "IŞ BERIJI" tarapyndan aşakdaky ýagdaýlarda ýatyrylýar:
4.1. Zähmet şertnamasynyň möhletiniň gutarmagy;
4.2. Işleriň gutarmagy;
4.3. Iş möçberiniň azalmagy;
4.4. Işe serhoş bolup, narkotiki ýa-da zäherli maddalaryň täsiri astynda gelmegi;
4.5. Zähmet şertnamasyna ýa-da kärhananyň içerki tertip düzgünlerine laýyklykda özüne tabşyrylan borçlary işgäriň birsygyn ýerine ýetirmezligi;
4.6. Kärhana degişli emlägi ogurlamagy;
4.7. Şu şertnamada kadalaşdyrylmadyk jedelli meseleler Türkmenistanyň hereket edýän kanunlary esasynda çözülýär.
```

### Section 5: Zähmet şertnamasynyň hereket edýän möhleti
| Element | Binding |
|---------|---------|
| Header | `5. Zähmet şertnamasynyň hereket edýän möhleti` — 12pt bold |
| Date line (dynamic) | `Zähmet şertnamasy {{Contract_StartDateText}} - {{Contract_ExpirationDateText}} çenli.` — 11pt bold |
| Fallback | If dates empty: use `{{Contract_PeriodFallbackText}}` |
| Static note | Two paragraphs about contract effective date and auto-extension |

### Section 6: Türkmenistanyň döwletinde alýan aýlyk zähmet haky
| Element | Binding |
|---------|---------|
| Header | `6. Türkmenistanyň döwletinde alýan aýlyk zähmet haky` — 12pt bold |
| Salary line (dynamic) | `Aýlyk zähmet haky {{Contract_SalaryText}} {{Salary_CurrencyCode}} Türkiýada Bankyň üsti bilen hasabyna geçirilýär.` |

### Section 7: Taraplaryň gollary we salgylary
| Element | Content |
|---------|---------|
| Header | `7. Taraplaryň gollary we salgylary` — 12pt bold |
| Employer block (left) | Header + Signatory + Company + Address + signature line |
| Employee block (right) | Header + FullName + Passport number + signature line |

**Two-column layout (no table borders):**
| Column | Width | Content |
|--------|-------|---------|
| Employer | ~50% | `IŞ BERIJI:` (bold)<br>`{{Application_SponsorSignatory}}`<br>`{{Application_SponsorName}}`<br>`{{Application_CompanyAddress}}`<br>`___________________________` |
| Employee | ~50% | `IŞGÄR:` (bold)<br>`{{Person_FullName}}`<br>`Pasport belgisi: {{Passport_Number}}`<br>`___________________________` |

---

## Field Contract (Placeholder → BO Property)

| Placeholder (Word) | BO Property | Source | Notes |
|-------------------|-------------|--------|-------|
| `{{ds.rows.Application_SponsorName}}` | `ApplicationItem.Application_SponsorName` | `ApplicationItem` | Employer company name |
| `{{ds.rows.Application_SponsorSignatory}}` | `ApplicationItem.Application_SponsorSignatory` | `ApplicationItem` | Signatory person |
| `{{ds.rows.Application_CompanyAddress}}` | `ApplicationItem.Application_CompanyAddress` | `ApplicationItem` | Multi-line address |
| `{{ds.rows.Person_FullName}}` | `ApplicationItem.Person_FullName` | `ApplicationItem` | Bold in intro |
| `{{ds.rows.Position_PositionTm}}` | `ApplicationItem.Position_PositionTm` | `ApplicationItem` | Bold in intro |
| `{{ds.rows.Passport_Number}}` | `ApplicationItem.Passport_Number` | `ApplicationItem` | Employee passport |
| `{{ds.rows.Contract_StartDateText}}` | `ApplicationItem.Contract_StartDateText` | `ApplicationItem` | Section 5 |
| `{{ds.rows.Contract_ExpirationDateText}}` | `ApplicationItem.Contract_ExpirationDateText` | `ApplicationItem` | Section 5 |
| `{{ds.rows.Contract_PeriodFallbackText}}` | `ApplicationItem.Contract_PeriodFallbackText` | `ApplicationItem` | Fallback if dates empty |
| `{{ds.rows.Contract_SalaryText}}` | `ApplicationItem.Contract_SalaryText` | `ApplicationItem` | Section 6 |
| `{{ds.rows.Salary_CurrencyCode}}` | `ApplicationItem.Salary_CurrencyCode` | `ApplicationItem` | Section 6 |

---

## Word-Specific Details

| Aspect | Value |
|--------|-------|
| **Embedded resource** | `Visa2026.Module.Resources.App_Labor_Contract_Item.docx` |
| **Generator method** | `MakeLaborContractTemplate()` in `tools/GenerateTemplates/Program.cs` |
| **Preview preset** | `labor-contract` (to be added to `PreviewWordReports/Program.cs`) |
| **DocxTemplater pattern** | `{{#ds.rows}}` … `{{:s:}}{{:PageBreak}}` … `{{/ds.rows}}` |
| **Section body indent** | 360 twips (~0.25 in) first-line indent for 1.x–4.x clauses |
| **Intro paragraph** | 720 twips first-line indent, fully justified, bold name/position |

---

## Static vs Dynamic

| Region | Type | Notes |
|--------|------|-------|
| Title + City | Static | No placeholders |
| Intro paragraph | **Dynamic** | Bold interpolated name + position |
| Sections 1–4 body | Static | All clause text fixed |
| Section 5 date line | **Dynamic** | Start/end dates or fallback |
| Section 5 note | Static | Auto-extension clause |
| Section 6 salary | **Dynamic** | Salary amount + currency |
| Section 7 signatures | **Dynamic** | Both columns data-driven |

---

## Scan vs Current Word Template Differences

| Aspect | Scan/XtraReport | Current Word (`MakeLaborContractTemplate`) | Action |
|--------|-----------------|------------------------------------------|--------|
| **Title alignment** | Centered | Centered | ✓ Match |
| **City** | Left-aligned, bold | Left-aligned, bold | ✓ Match |
| **Intro format** | Bold name + position within justified text | Plain text (no bold) | **Fix needed** |
| **Section headers** | 12pt bold | 12pt bold | ✓ Match |
| **Body clauses** | Justified, indented 20F padding | Justified, some indent | Tweak spacing |
| **Section 5 dates** | Bold inline format | Bold inline | ✓ Match |
| **Section 6 salary** | One line format | One line | ✓ Match |
| **Signatures** | Two-column, no borders, signature lines | Tab-based layout | **Redesign needed** |
| **Page fit** | One A4 per person | May overflow | **Verify with longest data** |

---

## Required BO Properties (Verification)

All properties already exist on `ApplicationItem` (per `AppLaborContractItemReportV2`):

| Property | Status |
|----------|--------|
| `Application_SponsorName` | ✅ exists |
| `Application_SponsorSignatory` | ✅ exists |
| `Application_CompanyAddress` | ✅ exists |
| `Person_FullName` | ✅ exists |
| `Position_PositionTm` | ✅ exists |
| `Passport_Number` | ✅ exists |
| `Contract_StartDateText` | ✅ exists |
| `Contract_ExpirationDateText` | ✅ exists |
| `Contract_PeriodFallbackText` | ✅ exists |
| `Contract_SalaryText` | ✅ exists |
| `Salary_CurrencyCode` | ✅ exists |

---

## Preview Dump Data (from Scan)

Preset values for `PreviewWordReports` (transcribed from `App_Labor_Contract_item.png`):

| Key | Sample Value |
|-----|--------------|
| `Application_SponsorName` | `Çalık Holding A.Ş.` |
| `Application_SponsorSignatory` | `Mehmet Ali Çalık` |
| `Application_CompanyAddress` | `Aşgabat şäheri, Arçabil şaýoly 58. tel: 12-34-56` |
| `Person_FullName` | `Azat Berdimuhamedow` |
| `Position_PositionTm` | `Inžener` |
| `Passport_Number` | `A12345678` |
| `Contract_StartDateText` | `01.01.2026` |
| `Contract_ExpirationDateText` | `31.12.2026` |
| `Contract_SalaryText` | `5.000` |
| `Salary_CurrencyCode` | `USD` |

---

## Open Points / Design Decisions

1. **Intro paragraph bold formatting:** DocxTemplater doesn't support inline bold within a single `{{}}`. Options:
   - Split into multiple runs (static + bold dynamic + static)
   - Use rich text content control (more complex)
   - Recommendation: Three-part intro: static start + `{{Person_FullName}}` (bold) + static middle + `{{Position_PositionTm}}` (bold) + static end

2. **Signature block layout:** Replace tab-based layout with proper two-column table (borderless) matching XtraReport positioning.

3. **Spacing:** Verify 10F spacer rows between sections match scan density.

4. **One-page constraint:** Test with longest realistic strings (long address, long name) to ensure signature block doesn't overflow.

---

## Approval Checklist

- [ ] Data types confirmed: `Application` root, `ApplicationItem` rows
- [ ] All placeholders mapped to existing BO properties
- [ ] Static text matches ministry scan wording
- [ ] Layout family F2 confirmed
- [ ] One-page-per-person requirement understood
- [ ] Preview dump data transcribed from scan
- [ ] Open points resolved (intro bold, signature layout)

**Approver:** Please review and confirm to proceed with Word template redesign in `MakeLaborContractTemplate()` and `AppLaborContractItemReportDef` updates.
