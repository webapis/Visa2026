# Report Map: Sanaw_ckl (Excel)

| Field | Value |
|-------|-------|
| **Status** | Implemented |
| **Map version** | 1.0.2 |
| **Basename** | `Sanaw_ckl` |
| **Template file(s)** | `Excel/Sanaw_ckl.xlsx` |
| **Format** | Excel (user report seed) |
| **Primary reference image** | `Excel/Sanaw_ckl.png` |

**Layout family:** **ItemRows** — `ExcelMergeMode.ItemList`; one workbook per application; row **5** copies per `ApplicationItem`.

**Word sibling:** **`../Sanaw_ckl.docx`** + **`../Sanaw_ckl_map.md`** (v1.0.3) — same 14-column sanawy contract and visibility; Excel uses **`{{.Property}}`** row tokens and **`RowNumber`** (not `RowNo`).

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Sanaw ckl (Excel) — Daşary ýurt raýatlarynyň sanawy (Çalık) |
| **Validation root** (`UserReportBoType`) | **`ApplicationItem`** |
| **Template family** | **`ItemRows`** |
| **Applicable application types** | **`App_Inv`**, **`App_Inv_And_WP`** |
| Applicable project contracts | **`GT-15`** — `NameTm` contains `GT-15` |
| Visibility criteria | `null` |
| Sort order (seed) | `63` (proposed; after **433-ek sanawy** `62`) |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** (zip from application detail; Excel generator loads items from DB) |
| **Collection binding** | **`rows`** — synthetic; `UserReportMergeDataHelper.BuildExcelItemListRowDictionary` (extends **`BuildSanawyRowDictionary`**) |
| **Item inclusion rule** | All active non-deleted **`ApplicationItems`** (`GetActiveApplicationItems`) |
| **Photo pipeline (Word)** | **N/A — Excel only** |
| **Determinism statement** | Same application + same items + same template bytes + map 1.0.1 ⇒ same filled rows and footer scalars |

**Runtime row keys:** **`BuildExcelItemListRowDictionary`** in **`UserReportMergeDataHelper.cs`** — must match §6 (includes **`RowNumber`** alias for column №).

**Footer scalars:** **`Application.CompanyHead`** via **`{{ds.Application_CompanyHead_PositionTm}}`** / **`{{ds.Application_CompanyHead_FullName}}`** (resolved from **`Application`** in **`ExcelReportGenerator.BuildHeaderData`**).

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `Excel/Sanaw_ckl.png` | **Primary** — Excel layout scan: title row 2; headers row 4 (cols **B–N**); golden data row 5 (Arda Özer); signatory row 7 |

| Scan (footer) | Map ID | Placeholder |
|---------------|--------|-------------|
| `Türkmenistandaky Şahamçasynyň müdiri` (cell **C7**) | SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` |
| `Mehmet Çırak` (cell **K7**) | SIG-R | `{{ds.Application_CompanyHead_FullName}}` |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `Sanaw_ckl.xlsx` |
| Logical copies per application | **`1`** workbook with **N** data rows (one per item) |
| Page / sheet breaks between items | **N/A** — ClosedXML row insert/copy |
| Empty item list behavior | Template data row remains with empty merged values (header + footer unchanged) |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | **A4 landscape** (match Word **`Sanaw_ckl.docx`** / Xtra sanawy) |
| Structure summary | Row **2**: centered title · rows **3–4**: table header (labels **B4–N4**) · row **5**: **template data row** (loop) · row **7**: signatory band |
| Static regions | Title *Daşary ýurt raýatlarynyň sanawy*; column headers; optional letterhead graphics above row 2 |
| Dynamic regions | **Row 5** cells **B5–N5** (§6) · **C7**, **K7** (SIG-L / SIG-R) |
| Typography notes | Enable **wrap text** on data columns (especially **L** Möhleti, **M**/**N** addresses); top-align row 5 |

### Sheet row map

| Row | Role |
|-----|------|
| 2 | Title (static) — merged across table width |
| 4 | Column headers (**B4–N4**) — static literals from §8 |
| 5 | **Template data row** — `{{#ds.rows}}` + `{{.…}}` per §6 |
| 6 | Optional `{{/ds.rows}}` (row deleted after merge if present) |
| 7 | Footer signatory (**SIG-L** / **SIG-R**) |

**v1 rule:** Do **not** merge cells on row **5**.

---

## §6 Placeholder contract (master table)

Author **row 5** only for item fields. Use **`{{.PropertyName}}`** (Excel list convention). Column **A5** holds the loop opener.

| ID | Region | Static label (literal) | Placeholder token (exact) | BO property | Data type | Golden value (scan row 5) | Notes |
|----|--------|------------------------|---------------------------|-------------|-----------|---------------------------|-------|
| LOOP | A5 | — | `{{#ds.rows}}` | — | control | — | Required loop marker |
| C01 | B5 | № | `{{.RowNumber}}` | `RowNumber` / `RowNo` | int | `1` | Excel merge uses **`RowNumber`** key |
| C02 | C5 | Familiýasy | `{{.Person_LastName}}` | `Person_LastName` | string | `Özer` | |
| C03 | D5 | Ady | `{{.Person_FirstName}}` | `Person_FirstName` | string | `Arda` | |
| C04 | E5 | Doglan senesi we ýeri | `{{.Person_DateOfBirthText}}, {{.Person_CountryOfBirthTm}}/{{.Person_BirthPlace}}` | DOB + country + place | string | `22.06.1981, Türkiýe/İskenderun` | Scan shows space before country; use comma + slash per Word **`Sanaw_ckl_map.md`** |
| C05 | F5 | Jynsy | `{{.Person_GenderTm}}` | `Person_GenderTm` | string | `Erkek` | |
| C06 | G5 | Raýatlygy | `{{.Person_NationalityCode}}` | `Person_NationalityCode` | string | `TUR` | |
| C07 | H5 | Pasport belgisi we möhleti | `{{.Passport_Number}}, {{.Passport_ExpirationDateText}}` | passport | string | `S36333641, 15.11.2033` | |
| C08 | I5 | Bilimi we okan ýeri | `{{.Education_LevelTm}}, {{.Education_InstitutionName}}` | education | string | `Ýörite orta, Iskenderun Demir Polat ýörite orta hünärmen mekdebi` | Or single **`{{.Education_LevelAndInstitutionTm}}`** if one cell |
| C09 | J5 | Bilimine görä hünäri | `{{.Education_SpecialtyTm}}` | `Education_SpecialtyTm` | string | `Jemgyýetçilik ylymlary, İş tejribesi: Enjam önümçiligi we montažy` | |
| C10 | K5 | Wezipesi | `{{.Position_PositionTm}}` | `Position_PositionTm` | string | `Suwasty turba geçiriji işleriniň gözegçisi` | |
| C11 | L5 | Möhleti we gezekligi | `Çakylyk {{.Application_VisaPeriod_NameTm}}, {{.Application_VisaCategory_NameTm}}` | app visa period + category | string | `Çakylyk 1 (bir) aý, iki gezeklik` | Literal prefix **`Çakylyk`** (same as Word §6 C11) |
| C12 | M5 | Türkmenistandaky salgysy | `{{.Address_FullAddress}}` | `Address_FullAddress` | string | `Balkan wel. T-başy-Garabogaz awtomobil ýol-ň 6-7-nji km. günbatar tarapynda ýerleşýän Çalyk Enerji UÝJ` | |
| C13 | N5 | Daşary ýurtdaky salgysy | `{{.Person_ForeignAddressCountryCode}}, {{.Person_ForeignAddress}}` | foreign address | string | `TUR, Pazarcı mah. Meltem sok. Elsa Sea Suit sitesi B N-4 iç kapı N-33 Gazipaşa/Antalya` | Or **`{{.Person_ForeignAddressWithCountry}}`** |

**Column 14 (Barjak serhet ýakasy):** present on Word **`Sanaw_ckl.docx`**; **not** in this Excel layout (headers **B4–N4** = 13 data columns + loop col **A**). Add column **O** + **`{{.Application_BorderZoneLocation_NameTm}}`** only if ministry requires parity with Word.

### Footer — application scalars (outside loop)

| ID | Region | Static label | Placeholder token (exact) | BO property | Golden value | Notes |
|----|--------|--------------|---------------------------|-------------|--------------|-------|
| SIG-L | C7 | (position title) | `{{ds.Application_CompanyHead_PositionTm}}` | `Application_CompanyHead_PositionTm` | `Türkmenistandaky Şahamçasynyň müdiri` | Replace static text in **C7** |
| SIG-R | K7 | (name) | `{{ds.Application_CompanyHead_FullName}}` | `Application_CompanyHead_FullName` | `Mehmet Çırak` | Replace static text in **K7** |

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **yes** — cell **A5** (recommended) |
| `{{/ds.rows}}` | **optional** — row **6** (deleted after merge) |
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

- **Row 2 (title):** `Daşary ýurt raýatlarynyň sanawy`
- **Row 4 headers (B4–N4):** `№`, `Familiýasy`, `Ady`, `Doglan senesi we ýeri`, `Jynsy`, `Raýatlygy`, `Pasport belgisi we möhleti`, `Bilimi we okan ýeri`, `Bilimine görä hünäri`, `Wezipesi`, `Möhleti we gezekligi`, `Türkmenistandaky salgysy`, `Daşary ýurtdaky salgysy`
- **C11 cell literal prefix:** `Çakylyk ` (before visa period/category placeholders)
- **Letterhead / branding:** as designed in **`Sanaw_ckl.xlsx`** (static graphics above title if present)

---

## §9 Photos / images

N/A — **Excel only** (v1 text merge; no inline images).

---

## §10 Excel merge

| Field | Value |
|-------|-------|
| Applicable | **yes** |
| **ExcelMergeMode** | **`ItemList`** |
| **Template data row** | **5** |
| **Loop marker cell** | **A5** — `{{#ds.rows}}` |
| **Header row** | **4** (static labels only) |
| **Row tokens** | `{{.RowNumber}}`, `{{.Person_LastName}}`, … per §6 |
| **Footer tokens** | `{{ds.Application_CompanyHead_PositionTm}}`, `{{ds.Application_CompanyHead_FullName}}` |
| **Engine** | **`ExcelReportGenerator`** + **`BuildExcelItemListRowDictionary`** |
| **Local test** | `dotnet run --project tools/ExcelTemplateSpike -- scan-sanaw-ckl` · `test-sanaw-ckl` |

**Placeholders in `Sanaw_ckl.xlsx`:** wired per §6 (2026-05-21). Spike merge: **0** leftover `{{` tokens, **2** data rows from sample app.

---

## §11 Deterministic verification

| Step | Action |
|------|--------|
| 1 | **`Sanaw_ckl.xlsx`** row **5** + footer per §6–§7 — **done** |
| 2 | **Extract** + **Validate** on **`ApplicationItem`** (UI or DEBUG updater reload) |
| 3 | Seed **Sanaw_ckl (Excel)** embedded — **`UserReportTemplateUpdater`** sort **63** |
| 4 | **`App_Inv`** or **`App_Inv_And_WP`** + GT-15 → **Resminamalar** — **passed** (John Doe / GT-15 sample, 2026-05-21) |
| 5 | Row count = non-deleted **ApplicationItems**; **L** = `Çakylyk …`; **O** = border zone (e.g. **Ýok**) |

---

## §12 Golden sample values

### Design-time scan (row 5 template preview)

| ID | Golden value |
|----|--------------|
| C01 | `1` |
| C02 | `Özer` |
| C03 | `Arda` |
| C04 | `22.06.1981, Türkiýe/İskenderun` |
| C05 | `Erkek` |
| C06 | `TUR` |
| C07 | `S36333641, 15.11.2033` |
| C08 | `Ýörite orta, Iskenderun Demir Polat ýörite orta hünärmen mekdebi` |
| C09 | `Jemgyýetçilik ylymlary, İş tejribesi: Enjam önümçiligi we montažy` |
| C10 | `Suwasty turba geçiriji işleriniň gözegçisi` |
| C11 | `Çakylyk 1 (bir) aý, iki gezeklik` |
| C12 | `Balkan wel. T-başy-Garabogaz awtomobil ýol-ň 6-7-nji km. günbatar tarapynda ýerleşýän Çalyk Enerji UÝJ` |
| C13 | `TUR, Pazarcı mah. Meltem sok. Elsa Sea Suit sitesi B N-4 iç kapı N-33 Gazipaşa/Antalya` |

### Resminamalar QA merge (John Doe, `App_Inv` + GT-15, 2026-05-21)

| ID | Golden value |
|----|--------------|
| C01 | `1` |
| C02 | `Doe` |
| C03 | `John` |
| C04 | `15.06.1985, Birleşen Patyşalyk/London` |
| C05 | `Erkek` |
| C06 | `GBR` |
| C07 | `AB1234567, 10.03.2029` |
| C08 | `Ýokary, Mançester Uniwersiteti` |
| C09 | `Elektrik Inženeri` |
| C10 | `Agyr ýük göteriji kranlar (gantry) boýunça jogapkäri` |
| C11 | `Çakylyk 1 (bir) aý, köp gezeklik` |
| C12 | `Bitarap Türkmenistan şaýoly 538, Aşgabat` |
| C13 | `GBR, 12 Baker Street, London` |
| C14 | `Ýok` |

### Footer

| ID | Golden value |
|----|--------------|
| SIG-L | `Türkmenistandaky Şahamçasynyň müdiri` |
| SIG-R | `Mehmet Çırak` |

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **Word `Sanaw_ckl`** | Same columns and BO keys; Word uses `{{ds.rows.*}}` / **`RowNo`**; Excel uses **`{{.…}}`** / **`RowNumber`** |
| **Xtra** | `AppItemInvSanawBaseReport` — 14 columns |
| **433-ek / Gurlusyk Excel** | Same **`ItemList`** merge; **ckl** = invitation **Çakylyk** column, not visa dates |
| **Visibility** | **`App_Inv`**, **`App_Inv_And_WP`** + GT-15 (match Word map v1.0.3) |
| **Seed name** | **`Sanaw_ckl (Excel)`** — distinct from Word seed **`Sanaw_ckl`** |
| **Sibling Excel** | **`Sanaw_ckl_ministr_saparov (Excel)`** — same visibility; static **Ministr** / **A.Saparow** footer |
| **Register** | **`Visa2026.Module.csproj`** embed `Resources.Templates.Excel.Sanaw_ckl.xlsx` + updater sort **63** |

---

## §14 Waiver

N/A — scan **`Excel/Sanaw_ckl.png`** provided.

---

## §15 Changelog

| Map version | Date | Notes |
|-------------|------|-------|
| 1.0.2 | 2026-05-21 | **Implemented** — Resminamalar merge verified (John Doe QA). |
| 1.0.1 | 2026-05-21 | Placeholders wired; **Approved**; spike **`test-sanaw-ckl`** OK. |
| 1.0.0 | 2026-05-21 | Initial Excel map from scan; placeholders pending. |
