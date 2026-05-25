# Report Map: Sanaw_ckl

| Field | Value |
|-------|-------|
| **Status** | Approved |
| **Map version** | 1.0.3 |
| **Basename** | `Sanaw_ckl` |
| **Template file(s)** | `Sanaw_ckl.docx` |
| **Format** | Word (user report seed) |
| **Primary reference image** | `Sanaw_ckl.png` |

**Layout family:** **T1** — landscape 14-column *Daşary ýurt raýatlarynyň sanawy* (same column set as Xtra `AppItemInvSanawBaseReport`). Cross-check: **`FormTemplates/App_Change_Inv_item_map.md`**.

**Not** the legacy seed **`Sanaw_uzt.docx`** (`App_Visa_and_WP_Ext`).

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Sanaw ckl — Daşary ýurt raýatlarynyň sanawy (Çalık) |
| **Validation root** (`UserReportBoType`) | **`ApplicationItem`** |
| **Template family** | **`ItemRows`** — one `.docx` per application; table data row repeats per person |
| **Applicable application types** | **`App_Inv`**, **`App_Inv_And_WP`** |
| Applicable project contracts | **`GT-15`** — `NameTm` contains `GT-15` (same filter pattern as **`GT-15_Elyasow_ckl`**) |
| Visibility criteria | `null` |
| Sort order (seed) | `59` (proposed; after **Forma 16**) |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** (zip from application detail) |
| **Collection binding** | **`rows`** — synthetic; `UserReportMergeDataHelper.BuildSanawyStyleRows` |
| **Item inclusion rule** | All active non-deleted **`ApplicationItems`** (`GetActiveApplicationItems`) |
| **Photo pipeline (Word)** | **none** |
| **Determinism statement** | Same application + same items + same template bytes + map 1.0.2 ⇒ same table rows and signatory (order by query / `ApplicationItemName` sort in helper path) |

**Runtime row keys:** `BuildSanawyRowDictionary` in **`UserReportMergeDataHelper.cs`** (must match §6 table rows).

**Footer scalars:** `Application.CompanyHead` via **`Application_CompanyHead_PositionTm`** / **`Application_CompanyHead_FullName`** (`Application` and **`ApplicationItem`** `[NotMapped]` aliases). Resolved at merge when Resminamalar passes **`Application`** as bind root (same as **`GT-15_Elyasow_ckl`**).

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `Sanaw_ckl.png` | **Primary** — Çalık Enerji letterhead; title *Daşary ýurt raýatlarynyň sanawy*; 14-column table; row 1 golden sample (Gümüş Yakup); footer signatory + stamp |

| Scan (footer) | Map ID | Placeholder |
|---------------|--------|-------------|
| `Türkmenistandaky Şahamçasynyň müdiri` | SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` |
| `Mehmet Çırak` | SIG-R | `{{ds.Application_CompanyHead_FullName}}` |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `Sanaw_ckl.docx` |
| Logical copies per application | **`1`** document containing **all** application items as table rows |
| Page / sheet breaks between items | **no** — single table grows (contrast **`Forma_16`** ItemRows + page break) |
| Empty item list behavior | Empty table body (header + footer remain) |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | **A4 landscape** (match Xtra sanawy / `REPORT_STANDARDS.md`) |
| Structure summary | Letterhead → centered title → **14-column table** (header row + repeating data row) → signatory band (position, stamp, name) |
| Static regions | Title, all column headers, embedded **stamp** image (§8) |
| Dynamic regions | **Table data row** (§6 C01–C14) + **footer signatory** (§6 SIG-L / SIG-R) |
| Typography notes | Times New Roman; table text small (~6–7 pt in Xtra); centered cells; word wrap in cells |

**Word table authoring:** Put **`{{#ds.rows}}`** / **`{{/ds.rows}}`** on the **template data row only** (not the header row). Prefer one table row cloned by DocxTemplater.

### Band / region map (footer)

| ID | Region | Dynamic? | Content |
|----|--------|----------|---------|
| SIG-L | Signatory title (left) | **Yes** | **`{{ds.Application_CompanyHead_PositionTm}}`** |
| SIG-M | Stamp (center) | **No** | Embedded picture in Word (§8) |
| SIG-R | Signatory name (right) | **Yes** | **`{{ds.Application_CompanyHead_FullName}}`** |

Layout: borderless **two-column** signatory table per Inv+WP Word standard (**`App_Inv_And_WP_app_map.md`** / **`GT-15_Elyasow_ckl`**): title wraps in left cell; name right-aligned in right cell on the same band as line 1 of the title.

---

## §6 Placeholder contract (master table)

Use **`{{ds.rows.PropertyName}}`** inside the data row (same as **`Sanaw_uzt.docx`** / **`Contract_Inv.docx`**). **`{{.PropertyName}}`** also works inside **`{{#ds.rows}}`**.

Type each token in **one Word run** per cell where possible.

| ID | Col | Static header (literal) | Placeholder token(s) (exact) | BO property | Golden value (row 1 scan) | Notes |
|----|-----|-------------------------|------------------------------|-------------|---------------------------|-------|
| C01 | 1 | № | `{{ds.rows.RowNo}}` | `RowNo` | `1` | Auto-increment in merge |
| C02 | 2 | Familiýasy | `{{ds.rows.Person_LastName}}` | `Person_LastName` | `Gümüş` | |
| C03 | 3 | Ady | `{{ds.rows.Person_FirstName}}` | `Person_FirstName` | `Yakup` | |
| C04 | 4 | Doglan senesi we ýeri | `{{ds.rows.Person_DateOfBirthText}}` + static `, ` + `{{ds.rows.Person_CountryOfBirthTm}}` + static `/` + `{{ds.rows.Person_BirthPlace}}` | DOB + country + place | `31.08.1997, Türkiye/ İzmit` | Match Xtra `Char(10)` as comma + slash in Word |
| C05 | 5 | Jynsy | `{{ds.rows.Person_GenderTm}}` | `Person_GenderTm` | `Erkek` | |
| C06 | 6 | Raýatlygy | `{{ds.rows.Person_NationalityCode}}` | `Person_NationalityCode` | `TUR` | |
| C07 | 7 | Pasport belgisi we möhleti | `{{ds.rows.Passport_Number}}` + static `, ` + `{{ds.rows.Passport_ExpirationDateText}}` | passport no + expiry | `U22553507, 01.11.2029` | |
| C08 | 8 | Bilimi we okan ýeri | `{{ds.rows.Education_LevelTm}}` + static `, ` + `{{ds.rows.Education_InstitutionName}}` | education | `Ýokary, Awrasya uniwersiteti` | |
| C09 | 9 | Bilimine görä hünäri | `{{ds.rows.Education_SpecialtyTm}}` | `Education_SpecialtyTm` | `Mehanika inženerçiligi` | |
| C10 | 10 | Wezipesi | `{{ds.rows.Position_PositionTm}}` | `Position_PositionTm` | Turbalar we sepleýji… inženeri | |
| C11 | 11 | Möhleti we gezekligi | static `Çakylyk ` + `{{ds.rows.Application_VisaPeriod_NameTm}}` + static `, ` + `{{ds.rows.Application_VisaCategory_NameTm}}` | app visa period + category | `Çakylyk 6 (alty) aý, köp gezeklik` | Prefix **Çakylyk** static per scan |
| C12 | 12 | Türkmenistandaky salgysy | `{{ds.rows.Address_FullAddress}}` | `Address_FullAddress` | Balkan wel. T-başy-Garabogaz… Çalyk Enerji UÝJ | |
| C13 | 13 | Daşary ýurtdaky salgysy | `{{ds.rows.Person_ForeignAddressCountryCode}}` + static `, ` + `{{ds.rows.Person_ForeignAddress}}` | foreign address | `TUR, Cumhuriyet mah. sevgi cad.… Menemen/İzmir` | |
| C14 | 14 | Barjak serhet ýakasy | `{{ds.rows.Application_BorderZoneLocation_NameTm}}` | `Application_BorderZoneLocation_NameTm` | `Ýok` | Default **Ýok** when unset on item |

### Footer — application scalars (outside `{{#ds.rows}}`)

Prefix **`ds.`**; bind root at merge = **`Application`** (`Application.CompanyHead`).

| ID | Placeholder token (exact) | `Application` source | Golden value (scan) | Notes |
|----|---------------------------|----------------------|---------------------|-------|
| SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` | `CompanyHead.Position.NameTm` | `Türkmenistandaky Şahamçasynyň müdiri` | Borderless two-column signatory table per Inv+WP Word standard |
| SIG-R | `{{ds.Application_CompanyHead_FullName}}` | `CompanyHead.FullName` | `Mehmet Çırak` | Right cell |

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **yes** — on template **data** table row |
| `{{/ds.rows}}` | **yes** |
| `{{#ds.ApplicationItems}}` | **no** |
| `{{/ds.ApplicationItems}}` | **no** |
| `{{:s:}}{{:PageBreak}}` | **no** |
| `{{IMAGE:…}}` | **no** |

**Scalar-only tokens (outside `rows`):**

| Token | Required |
|-------|----------|
| `{{ds.Application_CompanyHead_PositionTm}}` | **yes** — SIG-L |
| `{{ds.Application_CompanyHead_FullName}}` | **yes** — SIG-R |

---

## §8 Static text inventory

- **Title (centered):** `Daşary ýurt raýatlarynyň sanawy`
- **Column headers (row 1 of table):** exact Turkmen labels from scan §6 table header column
- **C11 prefix:** `Çakylyk ` (before visa period/category placeholders)
- **Footer — center (SIG-M):** Çalık Enerji **stamp** (embedded image in Word — not merged)
- **Letterhead / footer address block:** static graphics and contact lines as designed in `Sanaw_ckl.docx`

---

## §9 Photos / images

| Field | Value |
|-------|-------|
| Word photos | **No** merge photos in table |
| Image token(s) | N/A |
| Cell / region | Footer **stamp** = static picture in template only |

---

## §10 Excel merge

N/A — Word only. Excel sibling: **`Excel/Sanaw_ckl_map.md`** + **`Excel/Sanaw_ckl.xlsx`** (same visibility; **`ItemList`** + `{{.…}}` row tokens).

---

## §11 Deterministic verification

| Step | Action |
|------|--------|
| 1 | User authors **`Sanaw_ckl.docx`** data row per §6 + footer SIG-L/SIG-R + §7 |
| 2 | **Extract** + **Validate** (expect **`Person_LastName`** / **`RowNo`** → valid on **`ApplicationItem`**) |
| 3 | Embed + **`UserReportTemplateUpdater`** seed (after map **Approved**) |
| 4 | **`App_Inv`** or **`App_Inv_And_WP`** + GT-15 application → **Resminamalar** → compare table to **`Sanaw_ckl.png`** |
| 5 | Row count = non-deleted **ApplicationItems** count |

---

## §12 Golden sample values

### Table row 1 (from scan)

| Property | Dump value |
|----------|------------|
| `RowNo` | `1` |
| `Person_LastName` | `Gümüş` |
| `Person_FirstName` | `Yakup` |
| `Person_DateOfBirthText` | `31.08.1997` |
| `Person_CountryOfBirthTm` | `Türkiye` |
| `Person_BirthPlace` | `İzmit` |
| `Person_GenderTm` | `Erkek` |
| `Person_NationalityCode` | `TUR` |
| `Passport_Number` | `U22553507` |
| `Passport_ExpirationDateText` | `01.11.2029` |
| `Education_LevelTm` | `Ýokary` |
| `Education_InstitutionName` | `Awrasya uniwersiteti` |
| `Education_SpecialtyTm` | `Mehanika inženerçiligi` |
| `Position_PositionTm` | `Turbalar we sepleýji turbageçiriji elementleriň montaž işleriniň inženeri` |
| `Application_VisaPeriod_NameTm` | `6 (alty) aý` |
| `Application_VisaCategory_NameTm` | `köp gezeklik` |
| `Address_FullAddress` | `Balkan wel. T-başy-Garabogaz awtomobil ýol-ň 6-7-nji km. günbatar tarapynda ýerleşýän Çalyk Enerji UÝJ` |
| `Person_ForeignAddressCountryCode` | `TUR` |
| `Person_ForeignAddress` | `Cumhuriyet mah. sevgi cad. B blok N-34 iç kapı N-23 Menemen/İzmir` |
| `Application_BorderZoneLocation_NameTm` | `Ýok` |

### Footer signatory (application scalars)

| Key | Dump value |
|-----|------------|
| `Application_CompanyHead_PositionTm` | `Türkmenistandaky Şahamçasynyň müdiri` |
| `Application_CompanyHead_FullName` | `Mehmet Çırak` |

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **Xtra** | `AppItemInvSanawBaseReport` — 14 columns; same keys as **`BuildSanawyRowDictionary`** |
| **User merge** | `UserReportGenerator` → `BuildSanawyStyleRows` when placeholders include **`Person_LastName`** or **`RowNo`** |
| **vs `Sanaw_uzt.docx`** | **uzt** = `App_Visa_and_WP_Ext`; **ckl** = **`App_Inv`** + **`App_Inv_And_WP`** + GT-15 |
| **vs `GT-15_Elyasow_ckl`** | Same SIG-L / SIG-R tokens and Inv+WP signatory table layout |
| **Removed** | Code-backed **`App_Sanawy_Letter.docx`** — use **`Sanaw_ckl`** (Resminamalar) for GT-15 Inv+WP |
| **Column 14** | User-report sanawy uses **`Application_BorderZoneLocation_NameTm`** (not `WorkPermit_WorkPermittedLocations` on change-inv Xtra) |
| **Signatory source** | `Application.CompanyHead` → **`Application_CompanyHead_*`** |
| **Register seed** | **`Visa2026.Module.csproj`** embed + **`UserReportTemplateUpdater`** template **Sanaw_ckl**, sort order **59** |

---

## §14 Waiver

N/A — scan **`Sanaw_ckl.png`** provided.

---

## §15 Changelog

| Version | Date | Notes |
|---------|------|-------|
| 1.0.3 | 2026-05-21 | Visibility **`App_Inv`** + **`App_Inv_And_WP`** (seed + map). |
| 1.0.2 | 2026-05-21 | Placeholders in **`Sanaw_ckl.docx`**; seed registered (sort 59). **Approved**. |
| 1.0.1 | 2026-05-21 | **SIG-L / SIG-R:** dynamic footer aligned with **`GT-15_Elyasow_ckl_map.md`** (`Application_CompanyHead_*`). |
| 1.0.0 | 2026-05-21 | Initial map from scan; template **`Sanaw_ckl.docx`** (placeholders pending). Co-located under **`Resources/Templates/`**. |
