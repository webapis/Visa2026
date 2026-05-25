# Report Map: Sanaw_ckl_ministr_saparov

| Field | Value |
|-------|-------|
| **Status** | Approved |
| **Map version** | 1.0.0 |
| **Basename** | `Sanaw_ckl_ministr_saparov` |
| **Template file(s)** | `Excel/Sanaw_ckl_ministr_saparov.xlsx` |
| **Format** | Excel (user report seed) |
| **Primary reference image** | `Excel/Sanaw_ckl_ministr_saparov.png` |

**Layout family:** **ItemRows** — same table contract as **`Excel/Sanaw_ckl.xlsx`**; **static ministry signatory** (not `Application.CompanyHead`).

**Sibling:** **`Sanaw_ckl (Excel)`** — dynamic footer `{{ds.Application_CompanyHead_*}}` (Çalık branch manager).

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Sanaw ckl (Excel) — Ministr / Saparow |
| **Validation root** (`UserReportBoType`) | **`ApplicationItem`** |
| **Template family** | **`ItemRows`** |
| **Applicable application types** | **`App_Inv`**, **`App_Inv_And_WP`** (same as **`Sanaw_ckl (Excel)`**) |
| Applicable project contracts | **`GT-15`** — `NameTm` contains `GT-15` |
| Visibility criteria | `null` |
| Sort order (seed) | `64` |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** |
| **Collection binding** | **`rows`** — `UserReportMergeDataHelper.BuildExcelItemListRowDictionary` |
| **Item inclusion rule** | All active non-deleted **`ApplicationItems`** |
| **Photo pipeline (Word)** | **N/A — Excel only** |
| **Determinism statement** | Same application + items + template bytes + map 1.0.0 ⇒ same rows; footer always **Ministr** / **A.Saparow** (never company head) |

**Footer:** **static literals only** — no `{{ds.Application_CompanyHead_*}}` tokens.

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `Excel/Sanaw_ckl_ministr_saparov.png` | **Primary** — 14-column table; row **5** placeholders; row **7** static **Ministr** (left) and **A.Saparow** (right) |

| Scan (footer) | Map ID | Content |
|---------------|--------|---------|
| Row **7** (~**G7**) | SIG-L | **Static:** `Ministr` |
| Row **7** (~**K7**) | SIG-R | **Static:** `A.Saparow` |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `Sanaw_ckl_ministr_saparov.xlsx` |
| Logical copies per application | **`1`** workbook, **N** data rows |
| Page / sheet breaks between items | **N/A** |
| Empty item list behavior | Empty data row; static footer unchanged |

---

## §5 Page / sheet layout

Same row geometry as **`Sanaw_ckl_map.md`**: title row **2**; headers **B4–O4** (14 data columns); loop row **5**; signatory row **7**.

| Row | Role |
|-----|------|
| 2 | Title — `Daşary ýurt raýatlarynyň sanawy` |
| 4 | Column headers |
| 5 | **`{{#ds.rows}}`** + **`{{.…}}`** (§6) |
| 7 | **Static** signatory (§8) — **not** merged from BO |

**v1:** Do not merge cells on row **5**. **Single worksheet** only (first tab merged).

---

## §6 Placeholder contract (master table)

| ID | Region | Static label (literal) | Placeholder token (exact) | BO property | Data type | Notes |
|----|--------|------------------------|---------------------------|-------------|-----------|-------|
| LOOP | A5 | — | `{{#ds.rows}}` | — | control | |
| C01 | B5 | № | `{{.RowNumber}}` | `RowNumber` | int | |
| C02 | C5 | Familiýasy | `{{.Person_LastName}}` | `Person_LastName` | string | |
| C03 | D5 | Ady | `{{.Person_FirstName}}` | `Person_FirstName` | string | |
| C04 | E5 | Doglan senesi we ýeri | `{{.Person_DateOfBirthText}}, {{.Person_CountryOfBirthTm}}/{{.Person_BirthPlace}}` | DOB + country + place | string | |
| C05 | F5 | Jynsy | `{{.Person_GenderTm}}` | `Person_GenderTm` | string | |
| C06 | G5 | Raýatlygy | `{{.Person_NationalityCode}}` | `Person_NationalityCode` | string | |
| C07 | H5 | Pasport belgisi we möhleti | `{{.Passport_Number}}, {{.Passport_ExpirationDateText}}` | passport | string | |
| C08 | I5 | Bilimi we okan ýeri | `{{.Education_LevelTm}}, {{.Education_InstitutionName}}` | education | string | |
| C09 | J5 | Bilimine görä hünäri | `{{.Education_SpecialtyTm}}` | `Education_SpecialtyTm` | string | **Not** `Education_SpecialityTm` |
| C10 | K5 | Wezipesi | `{{.Position_PositionTm}}` | `Position_PositionTm` | string | |
| C11 | L5 | Möhleti we gezekligi | `Çakylyk {{.Application_VisaPeriod_NameTm}}, {{.Application_VisaCategory_NameTm}}` | visa period + category | string | Literal **`Çakylyk`** prefix |
| C12 | M5 | Türkmenistandaky salgysy | `{{.Address_FullAddress}}` | `Address_FullAddress` | string | |
| C13 | N5 | Daşary ýurtdaky salgysy | `{{.Person_ForeignAddressCountryCode}}, {{.Person_ForeignAddress}}` | foreign address | string | |
| C14 | O5 | Barjak serhet ýakasy | `{{.Application_BorderZoneLocation_NameTm}}` | `Application_BorderZoneLocation_NameTm` | string | Default **Ýok** when unset |

### Footer — static (no placeholders)

| ID | Region | Static text (exact) | Placeholder |
|----|--------|---------------------|-------------|
| SIG-L | ~G7 | `Ministr` | **none** |
| SIG-R | ~K7 | `A.Saparow` | **none** |

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **yes** — **A5** |
| `{{/ds.rows}}` | optional — row **6** |
| `{{ds.Application_CompanyHead_PositionTm}}` | **no** |
| `{{ds.Application_CompanyHead_FullName}}` | **no** |
| `{{IMAGE:…}}` | **no** |

**Scalar-only header tokens:** none.

---

## §8 Static text inventory

- **Row 2:** `Daşary ýurt raýatlarynyň sanawy`
- **Row 4 headers:** same 14-column set as **`Sanaw_ckl.xlsx`** (through **Barjak serhet ýakasy**)
- **Row 7 signatory:** `Ministr` (left), `A.Saparow` (right) — **always literal**
- **L5 prefix:** `Çakylyk `

---

## §9 Photos / images

N/A — Excel only.

---

## §10 Excel merge

| Field | Value |
|-------|-------|
| Applicable | **yes** |
| **ExcelMergeMode** | **`ItemList`** |
| **Template data row** | **5** |
| **Loop marker** | **A5** |
| **Footer** | Static — merge skips cells with no `{{` |
| **Local test** | `scan-sanaw-ckl-ministr` · `test-sanaw-ckl-ministr` |

---

## §11 Deterministic verification

| Step | Action |
|------|--------|
| 1 | Placeholders in **`Sanaw_ckl_ministr_saparov.xlsx`** per §6 — **done** |
| 2 | **Extract** + **Validate** (`ApplicationItem`) |
| 3 | Seed **Sanaw_ckl_ministr_saparov (Excel)** sort **64** |
| 4 | Same visibility as **`Sanaw_ckl (Excel)`** → Resminamalar |
| 5 | Footer remains **Ministr** / **A.Saparow** regardless of `CompanyHead` on application |

---

## §12 Golden sample values

Use **`Sanaw_ckl_map.md`** §12 row values for table columns; footer fixed:

| ID | Golden value |
|----|--------------|
| SIG-L | `Ministr` |
| SIG-R | `A.Saparow` |

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **`Sanaw_ckl (Excel)`** | Same types + GT-15; dynamic Çalık signatory |
| **This template** | Ministry-style static signatory for filing variant |
| **Seed name** | **`Sanaw_ckl_ministr_saparov (Excel)`** |

---

## §14 Waiver

N/A

---

## §15 Changelog

| Map version | Date | Notes |
|-------------|------|-------|
| 1.0.0 | 2026-05-21 | Initial map; clone of **`Sanaw_ckl.xlsx`** with static **Ministr** / **A.Saparow** footer. **Approved**. |
