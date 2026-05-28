# Report Map: 433_gurlusyk_ckl

| Field | Value |
|-------|-------|
| **Status** | Implemented |
| **Map version** | 1.0.1 |
| **Basename** | `433_gurlusyk_ckl` |
| **Template file(s)** | `Excel/433_gurlusyk_ckl.xlsx` |
| **Format** | Excel (user report seed) |
| **Primary reference image** | `Excel/433_gurlusyk_ckl.png` |

**Layout family:** **ItemRows** — `ExcelMergeMode.ItemList`; one workbook per application; data row **4** repeats per `ApplicationItem`.

**Sibling:** `Excel/433_gurlusyk_uzt.xlsx` (**Gurlusyk**) — WP/visa-extension visibility. This **ckl** variant is for **çakylyk** and uses application-level visa period/category on the **Möhleti** column (not `Visa_DurationFrequencyBlock`).

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Gurlusyk ckl — Ylalaşylan daşary ýurt raýatlarynyň sanawy |
| **Validation root** (`UserReportBoType`) | **`ApplicationItem`** |
| **Template family** | **`ItemRows`** |
| **Applicable application types** | **`App_Inv_And_WP`** |
| Applicable project contracts | **`GT-15`** — `NameTm` contains `GT-15` |
| Visibility criteria | `null` |
| Sort order (seed) | `66` |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** |
| **Collection binding** | **`rows`** — `UserReportMergeDataHelper.BuildExcelItemListRowDictionary` |
| **Item inclusion rule** | All active non-deleted `ApplicationItems` |
| **Photo pipeline (Word)** | **N/A — Excel only** |
| **Determinism statement** | Same application + same items + same template bytes + map 1.0.1 ⇒ same rows; footer always static |

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `Excel/433_gurlusyk_ckl.png` | **Primary** — A4 landscape personnel sanawy; header + 14 columns; footer `Ministr` / `A.Saparow` |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `433_gurlusyk_ckl.xlsx` |
| Logical copies per application | `1` workbook with N rows |
| Page / sheet breaks between items | N/A — row insert/copy |
| Empty item list behavior | Template data row remains (merged to blanks) |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | A4 landscape |
| Structure summary | Ministry header row **1** → title row **2** → table headers row **3** → **data row 4** (loop) → footer row **5** |
| Static regions | Header/title labels; footer signatory literals |
| Dynamic regions | Row **4** item cells (§6) |
| Typography notes | Wrap text on `Gelmeginiň maksady` and `Möhleti we gezekligi` columns; top-align row 4 |

---

## §6 Placeholder contract (master table)

Row **4** is the template data row. Column **C4** carries the loop marker.

| ID | Region | Static label (literal) | Placeholder token (exact) | BO property | Data type | Golden value (scan) | Notes |
|----|--------|-------------------------|---------------------------|-------------|-----------|---------------------|-------|
| LOOP | C4 | — | `{{#ds.rows}}` | — | control | — | Required |
| C01 | D4 | № | `{{.RowNumber}}` | `RowNumber` | int | `1` | |
| C02 | E4 | Familiýasy | `{{.Person_LastName}}` | `Person_LastName` | string | — | |
| C03 | F4 | Ady | `{{.Person_FirstName}}` | `Person_FirstName` | string | — | |
| C04 | G4 | Doglan senesi we ýeri | `{{.Person_DateOfBirthText}} {{.Person_BirthPlace}}` | DOB + place | string | — | |
| C05 | H4 | Jynsy | `{{.Person_GenderTm}}` | `Person_GenderTm` | string | — | |
| C06 | I4 | Raýatlygy | `{{.Person_NationalityCode}}` | `Person_NationalityCode` | string | — | |
| C07 | J4 | Pasport belgisi we möhleti | `{{.Passport_Number}} {{.Passport_ExpirationDateText}}` | passport | string | — | |
| C08 | K4 | Bilimi we okan ýeri | `{{.Education_LevelAndInstitutionTm}}` | education | string | — | |
| C09 | L4 | Bilimine görä hünäri | `{{.Education_SpecialtyTm}}` | `Education_SpecialtyTm` | string | — | |
| C10 | M4 | Wezipesi | `{{.Position_PositionTm}}` | `Position_PositionTm` | string | — | |
| C11 | N4 | Gelmeginiň maksady | `{{.WorkDuty_Description}}` | `WorkDuty_Description` | string | — | |
| C12 | O4 | Çagyrýan Tarap | `{{.Application_SponsorName}}` | `Application_SponsorName` | string | — | |
| C13 | P4 | Möhleti we gezekligi | `Çakylyk {{.Application_VisaPeriod_NameTm}}, {{.Application_VisaCategory_NameTm}}` | application visa period + category | string | — | **CKL difference** vs `433_gurlusyk_uzt.xlsx` |

### Footer — static (no placeholders)

| Region | Static text |
|--------|-------------|
| E5 | `Ministr` |
| L5 | `A.Saparow` |

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | yes |
| `{{/ds.rows}}` | no |
| `{{IMAGE:…}}` | no |

---

## §8 Static text inventory

- Header/title block (row 1–2) per scan
- Column headers (row 3) per scan
- Footer: `Ministr` / `A.Saparow`
- `Çakylyk ` prefix in the Möhleti column

---

## §9 Photos / images

N/A — Excel only.

---

## §10 Excel merge

| Field | Value |
|-------|-------|
| Applicable | yes |
| **ExcelMergeMode** | `ItemList` |
| Header row tokens | none (static) |
| Data row marker | `{{#ds.rows}}` on row **4** |
| Row tokens | `{{.…}}` per §6 |

---

## §11 Deterministic verification

| Check | Command / action |
|-------|------------------|
| Layout scan | `dotnet run --project tools/ExcelTemplateSpike -- scan-gurlusyk-ckl` |
| Merge test | `dotnet run --project tools/ExcelTemplateSpike -- test-gurlusyk-ckl` |
| UI validate | Resminamalar → Extract + Validate (root `ApplicationItem`) |

---

## §12 Golden sample values

### Resminamalar QA merge (App_Inv_And_WP + GT-15, 2026-05-28)

| ID | Golden value |
|----|--------------|
| C01 | `1` |
| C02 | `Arslan` |
| C03 | `Kemal` |
| C04 | `24.09.1982 Ankara` |
| C05 | `Erkek` |
| C06 | `TUR` |
| C07 | `TR5548821 15.06.2030` |
| C08 | `Ýokary, Manchester Uniwersiteti` |
| C09 | `Mehanik` |
| C10 | *(blank in sample)* |
| C11 | *(long paragraph in sample; see screenshot)* |
| C12 | `Çalyk Enerji Sanayi we Ticaret A.Ş. Türkiýe tarapyndan Türkmenistanyn şahamçasynyň` |
| C13 | `Çakylyk 6 (alty) aý, köp gezeklik` |
| SIG-L | `Ministr` |
| SIG-R | `A.Saparow` |

---

## §13 Cross-check

| Artifact | Notes |
|----------|-------|
| `433_gurlusyk_uzt.xlsx` | Same columns and row tokens; different visibility and Mohleti token |
| `Sanaw_ckl.xlsx` | Also uses `Çakylyk {{.Application_VisaPeriod_NameTm}}, {{.Application_VisaCategory_NameTm}}` concept |

---

## §14 Waiver

N/A — scan provided.

---

## §15 Changelog

| Map version | Date | Notes |
|-------------|------|-------|
| 1.0.1 | 2026-05-28 | **Implemented** — Resminamalar merge verified (screenshot QA). |
| 1.0.0 | 2026-05-28 | Initial map for `433_gurlusyk_ckl.xlsx` from scan; CKL-specific Mohleti token. |

