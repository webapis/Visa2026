# Report Map: wiza_yatyrylmak_sanaw (Excel)

| Field | Value |
|-------|-------|
| **Status** | Implemented |
| **Map version** | 1.0.3 |
| **Basename** | `wiza_yatyrylmak_sanaw` |
| **Template file(s)** | `Excel/wiza_yatyrylmak_sanaw.xlsx` |
| **Format** | Excel (user report seed) |
| **Primary reference image** | `Excel/wiza_yatyrylmak_sanaw.png` |

**Layout family:** **`ItemRows`** — `ExcelMergeMode.ItemList`; one workbook per application; template **data row** copies per **`ApplicationItem`**.

**Application type:** **`App_Cancel_Visa` only** (`cancel_visa`, selection **807**). Pairs with Word letter **`../wiza_yatyrylmak_hat.docx`** (sort **68**).

**Core rule:** **`CurrentVisa`** and **`NextVisa`** on the **same person** → **one Excel row**; visa columns use **`{{.CancelVisa_*Block}}`** (multiline / wrap text) — **not** a second row per visa.

**Template state:** **Implemented** — Resminamalar verified: 2 persons; stacked **CurrentVisa** + **NextVisa** in K–M; footer **`CompanyHead`** merged.

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Wiza ýatyrmak sanaw (Excel) |
| **Validation root** (`UserReportBoType`) | **`ApplicationItem`** |
| **Template family** | **`ItemRows`** |
| **Applicable application types** | **`App_Cancel_Visa`** only |
| Applicable project contracts | `null` |
| Visibility criteria | `null` |
| Sort order (seed) | `69` (proposed; after **Wiza ýatyrmak hat** `68`) |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** |
| **Collection binding** | **`rows`** — `ExcelReportGenerator` + **`BuildWizaYatyrylmakSanawExcelRowDictionary`** |
| **Item inclusion rule** | Active non-deleted **`ApplicationItems`** |
| **Photo pipeline** | **N/A — Excel only** |
| **Determinism statement** | Same application + items + template bytes + map 1.0.0 ⇒ same filled rows |

**Visa stack order:** line 1 = **`CurrentVisa`**, line 2 = **`NextVisa`** in **`CancelVisa_NumberBlock`**, **`CancelVisa_StartDateBlock`**, **`CancelVisa_ExpirationDateBlock`**.

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `Excel/wiza_yatyrylmak_sanaw.png` | **Primary** — title *Daşary ýurt raýatlarynyň sanawy*; **12** data columns (B–M); row 1 **Mehmet Akif Tarsus** with **two** visa lines in K–M; footer signatory |

| Scan (footer) | Map ID | Placeholder |
|---------------|--------|-------------|
| `Türkmenistandaky şahamçasynyň müdiri` | SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` |
| `Mehmet Çırak` | SIG-R | `{{ds.Application_CompanyHead_FullName}}` |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `wiza_yatyrylmak_sanaw.xlsx` |
| Logical copies per application | **`1`** workbook; **N** rows = active items |
| Page / sheet breaks | **N/A** — ClosedXML row copy |
| Empty item list | Header + footer; template row empty |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | **A4 landscape** |
| Structure summary | Title row → header row → **template data row** (loop) → signatory row |
| Static regions | Title, column headers (§8) |
| Dynamic regions | Data row **B–M** (§6); footer **SIG-L** / **SIG-R** (§6) |

### Proposed sheet row map (adjust cell addresses to match your `.xlsx`)

| Row | Role |
|-----|------|
| 2 | Title — `Daşary ýurt raýatlarynyň sanawy` (static, merged) |
| 4 | Column headers **B4–M4** (static literals §8) |
| 5 | **Template data row** — `{{#ds.rows}}` in **A5**; `{{.…}}` in **B5–M5** |
| 6 | Optional `{{/ds.rows}}` (deleted after merge) |
| 7 | Signatory — **SIG-L** (left), **SIG-R** (right) per authored template |

**v1:** Do **not** merge cells on the template data row. Enable **wrap text** on **J5** (maksady), **K5–M5** (visa blocks).

---

## §6 Placeholder contract (master table)

Author **row 5** only for item fields. Excel list convention: **`{{.PropertyName}}`**. Loop in **A5**.

| ID | Cell | Static header (row 4) | Placeholder token (exact) | BO property | Golden value (scan) | Notes |
|----|------|------------------------|---------------------------|-------------|---------------------|-------|
| LOOP | A5 | — | `{{#ds.rows}}` | — | — | Required |
| C01 | B5 | T/N (№) | `{{.RowNumber}}` | `RowNumber` | `1` | |
| C02 | C5 | Familiýasy | `{{.Person_LastName}}` | `Person_LastName` | `Tarsus` | |
| C03 | D5 | Ady | `{{.Person_FirstName}}` | `Person_FirstName` | `Mehmet Akif` | |
| C04 | E5 | Doglan senesi | `{{.Person_DateOfBirthText}}` | `Person_DateOfBirthText` | `10.01.1967` | Date only on scan |
| C05 | F5 | Jynsy | `{{.Person_GenderTm}}` | `Person_GenderTm` | `Erkek` | |
| C06 | G5 | Raýatlygy | `{{.Person_NationalityCode}}` | `Person_NationalityCode` | `TUR` | |
| C07 | H5 | Pasportynyň (… resminamanyň) belgisi | `{{.Passport_Number}}` | `Passport_Number` | `U88120878` | Separate column per authored template |
| C08 | I5 | Pasportynyň (… resminamanyň) möhleti | `{{.Passport_ExpirationDateText}}` | `Passport_ExpirationDateText` | `08.01.2028` | |
| C09 | J5 | Gelmeginiň maksady | `{{.Registration_GelmeginMaksadyTm}}` | `Registration_GelmeginMaksadyTm` | Yörite orta, Turbina desgasynda… jogapkär | Wrap text |
| C10 | K5 | Wiza belgisi | `{{.CancelVisa_NumberBlock}}` | `CancelVisa_NumberBlock` | `A1686965` + line 2 `A1675442` | **Stacked** — wrap text |
| C11 | L5 | Wiza möhleti başlanýan senesi | `{{.CancelVisa_StartDateBlock}}` | `CancelVisa_StartDateBlock` | `18.11.2025` + `08.05.2026` | **Stacked** |
| C12 | M5 | Wiza möhleti tamamlanýan sene | `{{.CancelVisa_ExpirationDateBlock}}` | `CancelVisa_ExpirationDateBlock` | `07.05.2026` + `07.11.2026` | **Stacked** |

**Not used:** `{{.Visa_Number}}`, `{{.Visa_StartDateText}}`, `{{.Visa_ExpirationDateText}}` alone (current visa only — omits **NextVisa**).

### Footer — application scalars

| ID | Region | Placeholder token (exact) | BO property | Golden value |
|----|--------|---------------------------|-------------|--------------|
| SIG-L | Footer left | `{{ds.Application_CompanyHead_PositionTm}}` | `CompanyHead.Position.NameTm` | `Türkmenistandaky şahamçasynyň müdiri` |
| SIG-R | Footer right | `{{ds.Application_CompanyHead_FullName}}` | `CompanyHead.FullName` | `Mehmet Çırak` |

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **yes** — **A5** |
| `{{/ds.rows}}` | optional — row **6** |
| `{{ds.Application_CompanyHead_PositionTm}}` | **yes** — SIG-L |
| `{{ds.Application_CompanyHead_FullName}}` | **yes** — SIG-R |
| `{{IMAGE:…}}` | **no** |

---

## §8 Static text inventory

- **Title (row 2):** `Daşary ýurt raýatlarynyň sanawy`
- **Headers (row 4, B–M):** `№`, `Familiýasy`, `Ady`, `Doglan senesi`, `Jynsy`, `Raýatlygy`, `Pasportynyň (şahsyýetini tassyklaýan resminamanyň) belgisi`, `Pasportynyň (şahsyýetini tassyklaýan resminamanyň) möhleti`, `Gelmeginiň maksady`, `Wiza belgisi`, `Wiza möhleti başlanýan senesi`, `Wiza möhleti tamamlanýan sene`

---

## §9 Photos / images

N/A — Excel v1 (no inline images).

---

## §10 Excel merge

| Field | Value |
|-------|-------|
| Applicable | **yes** |
| **ExcelMergeMode** | **`ItemList`** |
| **Template data row** | **5** (proposed) |
| **Loop marker** | **A5** — `{{#ds.rows}}` |
| **Header row** | **4** |
| **Engine** | **`ExcelReportGenerator`** + **`BuildWizaYatyrylmakSanawExcelRowDictionary`** |
| **Authoring** | Microsoft Excel only — do **not** rebuild `.xlsx` via zip/Compress-Archive (see **`Excel/README.md`**) |

---

## §11 Deterministic verification

| Step | Action |
|------|--------|
| 1 | Author **`wiza_yatyrylmak_sanaw.xlsx`** per §5–§7 |
| 2 | **Extract** + **Validate** (`ApplicationItem` + `CancelVisa_*Block`) |
| 3 | Embed + **`UserReportTemplateUpdater`** — **`EnsureExcelTemplateExists`**, sort **69** |
| 4 | **`App_Cancel_Visa`** with item (**CurrentVisa** + **NextVisa**) → **Resminamalar** — **one** row, two lines in J–L |
| 5 | Row count = person count (not **`CancelVisaCount`**) |

---

## §12 Golden sample values

| Property | Dump value |
|----------|------------|
| `RowNumber` | `1` |
| `Person_LastName` | `Tarsus` |
| `Person_FirstName` | `Mehmet Akif` |
| `Person_DateOfBirthText` | `10.01.1967` |
| `Person_GenderTm` | `Erkek` |
| `Person_NationalityCode` | `TUR` |
| `Passport_Number` | `U88120878` |
| `Passport_ExpirationDateText` | `08.01.2028` |
| `Registration_GelmeginMaksadyTm` | Yörite orta, Turbina desgasynda umumy mehaniki gurnawlary gurnamak işleri boýunça jogapkär |
| `CancelVisa_NumberBlock` | `A1686965` + newline + `A1675442` |
| `CancelVisa_StartDateBlock` | `18.11.2025` + newline + `08.05.2026` |
| `CancelVisa_ExpirationDateBlock` | `07.05.2026` + newline + `07.11.2026` |

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **vs `wiza_yatyrylmak_hat`** | Letter = Word **`AppScalar`**; sanaw = **Excel** **`ItemList`** |
| **vs `Excel/Sanaw_ckl`** | 14-col invitation layout; **not** interchangeable |
| **vs `AppCancelVisaItemReport`** | Xtra 14-col inv sanawy; this seed is **12-col** cancel-visa Excel |
| **Register seed** | **`UserReportTemplateUpdater`** — **Wiza ýatyrmak sanaw**, sort **69**, **`App_Cancel_Visa`** |

---

## §14 Authoring checklist

- [x] Create **`wiza_yatyrylmak_sanaw.xlsx`** (12 columns, row 5 loop)
- [x] Place §6 tokens (`{{#ds.rows}}`, `{{.CancelVisa_*Block}}`, …)
- [x] Co-locate **`wiza_yatyrylmak_sanaw.png`**
- [x] Embed + seed updater (sort **69**)
- [x] **Extract** / **Validate** in app
- [x] Resminamalar test on **`App_Cancel_Visa`** (with **wiza_yatyrylmak_hat**)

## §14b Waiver

N/A

---

## §15 Changelog

| Version | Date | Notes |
|---------|------|-------|
| 1.0.3 | 2026-05-28 | **Implemented** — Resminamalar QA (2 rows; dual visa lines per person). |
| 1.0.2 | 2026-05-28 | Footer **`{{ds.Application_CompanyHead_*}}`** placed in **`.xlsx`**. |
| 1.0.1 | 2026-05-28 | **Approved**; **`wiza_yatyrylmak_sanaw.xlsx`** wired (12 cols; split passport H/I); embedded + seed 69. |
| 1.0.0 | 2026-05-28 | Initial **Excel** map; stacked **CurrentVisa** + **NextVisa** per row. |
