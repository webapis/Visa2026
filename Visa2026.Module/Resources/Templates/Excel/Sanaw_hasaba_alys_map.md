# Report Map: Sanaw_hasaba_alys (Excel)

| Field | Value |
|-------|-------|
| **Status** | Implemented |
| **Map version** | 1.0.2 |
| **Basename** | `Sanaw_hasaba_alys` |
| **Template file(s)** | `Excel/Sanaw_hasaba_alys.xlsx` |
| **Format** | Excel (user report seed) |
| **Primary reference image** | `Excel/Sanaw_hasaba_alys.png` |

**Layout family:** **ItemRows** — `ExcelMergeMode.ItemList`; one workbook per application; template **data row** copies per **`ApplicationItem`**.

**Xtra sibling:** **`RegistrationListReport`** — same 11-column *hasaba almak* list layout (`Gelmeginiň maksady`, `Wiza maglumatlary`, separate passport number / expiry).

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Hasaba almak sanawy (Excel) — Daşary ýurt raýatlarynyň sanawy |
| **Validation root** (`UserReportBoType`) | **`ApplicationItem`** |
| **Template family** | **`ItemRows`** |
| **Applicable application types** | All with **`ApplicationType.ShowRegistrations = true`** (same as **Registration List Report** / **Forma 16**) |
| Applicable project contracts | `null` |
| Visibility criteria | `[Application.ApplicationType.ShowRegistrations]` |
| Sort order (seed) | `70` (proposed; after **Wiza ýatyrmak sanaw** `69`) |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** |
| **Collection binding** | **`rows`** — `UserReportMergeDataHelper.BuildExcelItemListRowDictionary` |
| **Item inclusion rule** | Active non-deleted **`ApplicationItems`** |
| **Photo pipeline** | **N/A — Excel only** |
| **Determinism statement** | Same application + items + template bytes + map 1.0.0 ⇒ same filled rows and footer scalars |

**Purpose column:** **`Registration_GelmeginMaksadyTm`** — mirrors **`RegistrationListReport`** / **`Forma_16`** (employee position vs family-member sponsor line).

**Visa column:** **`CurrentVisa`** only (single line); not **`CancelVisa_*Block`** (cancel-visa sanaw uses stacked current+next).

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `Excel/Sanaw_hasaba_alys.png` | **Primary** — title **B1**; headers row **2** (**B2–L2**); golden data row **3** (Yetkin / Didem); signatory row **6** |

| Scan (footer) | Map ID | Placeholder |
|---------------|--------|-------------|
| `Türkmenistandaky Şahamçasynyň müdiri` (left) | SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` |
| `Mehmet Çırak` (right) | SIG-R | `{{ds.Application_CompanyHead_FullName}}` |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `Sanaw_hasaba_alys.xlsx` |
| Logical copies per application | **`1`** workbook; **N** rows = active items |
| Page / sheet breaks | **N/A** — ClosedXML row copy |
| Empty item list | Header + footer; template data row empty |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | **A4 landscape** (match **`RegistrationListReport`**) |
| Structure summary | Title **B1** → headers **2** → loop row **3** → optional `{{/ds.rows}}` row **4** → signatory **6** |
| Static regions | Title *Daşary ýurt raýatlarynyň sanawy*; column headers **B2–L2** |
| Dynamic regions | **B3–L3** (§6); footer **B6** / **I6** (§6) |
| Typography notes | **Wrap text** on **J3** (maksady), **K3** (wiza maglumatlary), **L3** (address) |

### Sheet row map (authored `.xlsx`)

| Row | Role |
|-----|------|
| 1 | Title **B1** (static) |
| 2 | Column headers **B2–L2** — static literals §8 |
| 3 | **Template data row** — `{{#ds.rows}}` in **A3**; `{{.…}}` in **B3–L3** |
| 4 | Optional `{{/ds.rows}}` (deleted after merge) |
| 6 | Footer signatory — **SIG-L** **B6**, **SIG-R** **I6** |

**v1:** Do **not** merge cells on row **3**.

---

## §6 Placeholder contract (master table)

Author **row 3** only for item fields. Excel list convention: **`{{.PropertyName}}`**. Loop in **A3**.

| ID | Cell | Static header (row 2) | Placeholder token (exact) | BO property | Golden value (scan row 3) | Notes |
|----|------|------------------------|---------------------------|-------------|---------------------------|-------|
| LOOP | A3 | — | `{{#ds.rows}}` | — | — | Required |
| C01 | B3 | № | `{{.RowNumber}}` | `RowNumber` | `1` | |
| C02 | C3 | Familiýasy | `{{.Person_LastName}}` | `Person_LastName` | `Yetkin` | |
| C03 | D3 | Ady | `{{.Person_FirstName}}` | `Person_FirstName` | `Didem` | |
| C04 | E3 | Doglan senesi | `{{.Person_DateOfBirthText}}` | `Person_DateOfBirthText` | `01.01.1980` | Date only (not birth place) |
| C05 | F3 | Jynsy | `{{.Person_GenderTm}}` | `Person_GenderTm` | `Aýal` | |
| C06 | G3 | Raýatlygy | `{{.Person_NationalityCode}}` | `Person_NationalityCode` | `TUR` | |
| C07 | H3 | Pasportynyň (… resminamanyň) belgisi | `{{.Passport_Number}}` | `Passport_Number` | `U36556957` | Separate column |
| C08 | I3 | Pasportynyň (… resminamanyň) möhleti | `{{.Passport_ExpirationDateText}}` | `Passport_ExpirationDateText` | `20.05.2030` | |
| C09 | J3 | Gelmeginiň maksady | `{{.Registration_GelmeginMaksadyTm}}` | `Registration_GelmeginMaksadyTm` | `Türkmenistandaky şahamça müdiriniň orunbasary-Ali Enes Yetkin-ň aýaly` | Wrap text |
| C10 | K3 | Wiza maglumatlary | `{{.Visa_Number}} {{.Visa_TypeTm}} {{.Visa_StartDateText}} {{.Visa_ExpirationDateText}}` | `CurrentVisa` | `A1688318 FM 20.02.2026 06.07.2026` | Single line; spaces between tokens |
| C11 | L3 | Türkmenistandaky salgysy | `{{.Address_FullAddress}}` | `Address_FullAddress` | `Aşgabat şäheriniň 1958-nji (Andalyp) köçesi jaý-86, öý-114` | Wrap text |

**Not used:** `{{.Visa_DurationFrequencyBlock}}` (multiline — Gurlusyk layout); **`CancelVisa_*Block`** (cancel-visa sanaw only).

### Footer — application scalars

| ID | Region | Placeholder token (exact) | BO property | Golden value |
|----|--------|---------------------------|-------------|--------------|
| SIG-L | **B6** | `{{ds.Application_CompanyHead_PositionTm}}` | `Application_CompanyHead_PositionTm` | `Türkmenistandaky Şahamçasynyň müdiri` |
| SIG-R | **I6** | `{{ds.Application_CompanyHead_FullName}}` | `Application_CompanyHead_FullName` | `Mehmet Çırak` |

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **yes** — **A3** |
| `{{/ds.rows}}` | optional — row **4** |
| `{{ds.Application_CompanyHead_PositionTm}}` | **yes** — SIG-L |
| `{{ds.Application_CompanyHead_FullName}}` | **yes** — SIG-R |
| `{{IMAGE:…}}` | **no** |

---

## §8 Static text inventory

- **B1 (title):** `Daşary ýurt raýatlarynyň sanawy`
- **Row 2 headers (B2–L2):** `№`, `Familiýasy`, `Ady`, `Doglan senesi`, `Jynsy`, `Raýatlygy`, `Pasportynyň (şahsyýetini tassyklaýan resminamasynyň) belgisi`, `Pasportynyň (şahsyýetini tassyklaýan resminamasynyň) möhleti`, `Gelmeginiň maksady`, `Wiza maglumatlary`, `Türkmenistandaky salgysy`
- **Letterhead / branding:** as designed in **`Sanaw_hasaba_alys.xlsx`** (if present above title)

---

## §9 Photos / images

N/A — Excel v1 (text merge only).

---

## §10 Excel merge

| Field | Value |
|-------|-------|
| Applicable | **yes** |
| **ExcelMergeMode** | **`ItemList`** |
| **Template data row** | **3** |
| **Loop marker** | **A3** — `{{#ds.rows}}` |
| **Header row** | **2** |
| **Row tokens** | §6 — `BuildExcelItemListRowDictionary` (+ `Registration_GelmeginMaksadyTm`, `Visa_TypeTm`) |
| **Footer tokens** | `{{ds.Application_CompanyHead_*}}` |
| **Engine** | **`ExcelReportGenerator`** |
| **Local test** | `dotnet run --project tools/ExcelTemplateSpike -- scan-sanaw-hasaba-alys` · `patch-sanaw-hasaba-alys` · `test-sanaw-hasaba-alys` |

**Authoring:** Microsoft Excel or **`patch-sanaw-hasaba-alys`** (ClosedXML). Do **not** repack `.xlsx` via zip (see **`Excel/README.md`**).

---

## §11 Deterministic verification

| Step | Action |
|------|--------|
| 1 | Wire **`Sanaw_hasaba_alys.xlsx`** row **3** + footer **B6**/**I6** per §6–§7 — **done** |
| 2 | **Extract** + **Validate** on **`ApplicationItem`** — **passed** (updater reload) |
| 3 | Embed + **`UserReportTemplateUpdater`** — **Hasaba almak sanawy (Excel)**, sort **70** — **done** |
| 4 | Registration application → **Resminamalar** — **passed** (John Doe + Tom Doe sample) |
| 5 | **2** rows = active items; **J3** employee position vs FM `position-John Doe-ogly`; **K3** visa line; footer signatory merged |

---

## §12 Golden sample values

| ID | Golden value |
|----|--------------|
| C01 | `1` |
| C02 | `Yetkin` |
| C03 | `Didem` |
| C04 | `01.01.1980` |
| C05 | `Aýal` |
| C06 | `TUR` |
| C07 | `U36556957` |
| C08 | `20.05.2030` |
| C09 | `Türkmenistandaky şahamça müdiriniň orunbasary-Ali Enes Yetkin-ň aýaly` |
| C10 | `A1688318 FM 20.02.2026 06.07.2026` |
| C11 | `Aşgabat şäheriniň 1958-nji (Andalyp) köçesi jaý-86, öý-114` |
| SIG-L | `Türkmenistandaky Şahamçasynyň müdiri` |
| SIG-R | `Mehmet Çırak` |

### Resminamalar QA merge (registration application, 2026-05-28)

| Row | Person | **J3** (`Registration_GelmeginMaksadyTm`) | **K3** (visa line) |
|-----|--------|-------------------------------------------|---------------------|
| 1 | John Doe (employee) | `Elektrik işleriniň hil gözegçiliginiň elektrik inženeri` | `WP-Işçi Wiza` + dates |
| 2 | Tom Doe (FM) | `…elektrik inženeri-John Doe-ogly` | `V-FM-003 WP-Işçi Wiza` + dates |

Footer: **Türkmenistandaky şahamçasynyň müdiri** / **Mehmet Çyrak** (live **`CompanyHead`**).

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **Xtra** | **`RegistrationListReport`** — same columns; Xtra visa cell is multiline (`Char(10)`); Excel **C10** uses **one line** with spaces |
| **vs `Excel/Sanaw_ckl`** | 14-col invitation sanawy (education, wezipesi, çakylyk möhleti, foreign address) — **not** interchangeable |
| **vs `wiza_yatyrylmak_sanaw`** | 12-col cancel visa; stacked **`CancelVisa_*Block`** — **not** this layout |
| **vs `Forma_16`** | Per-person registration certificate; shares **`Registration_GelmeginMaksadyTm`** |
| **Seed name** | **`Hasaba almak sanawy (Excel)`** |
| **Register** | **`Visa2026.Module.csproj`** embed + **`UserReportTemplateUpdater`** sort **70** |

---

## §14 Authoring checklist

- [x] Create **`Sanaw_hasaba_alys.xlsx`** (11 data columns)
- [x] Co-locate **`Sanaw_hasaba_alys.png`**
- [x] **`Sanaw_hasaba_alys_map.md`** (this file) — **Implemented** v1.0.2
- [x] Placeholders in **`Sanaw_hasaba_alys.xlsx`** (row **3** + footer **B6**/**I6**)
- [x] Embed + seed updater (**`Hasaba almak sanawy (Excel)`**, sort **70**)
- [x] **Extract** / **Validate** in app
- [x] Resminamalar QA — John Doe (employee) + Tom Doe (FM); §12 QA table

## §14b Waiver

N/A

---

## §15 Changelog

| Map version | Date | Notes |
|-------------|------|-------|
| 1.0.2 | 2026-05-28 | **QA complete** — Resminamalar: 2 rows; employee vs FM **`Registration_GelmeginMaksadyTm`**; visa + footer OK. |
| 1.0.1 | 2026-05-28 | **Implemented** — embedded + **`UserReportTemplateUpdater`** seed **70**; registration app types (same as Forma 16). |
| 1.0.0 | 2026-05-28 | Initial map from scan; 11-column hasaba almak list; row **3** loop; **`patch-sanaw-hasaba-alys`**. |
