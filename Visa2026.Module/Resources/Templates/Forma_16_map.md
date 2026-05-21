# Report Map: Forma_16

| Field | Value |
|-------|-------|
| **Status** | Approved |
| **Map version** | 1.0.0 |
| **Basename** | `Forma_16` |
| **Template file(s)** | `Forma_16.docx` |
| **Format** | Word |
| **Primary reference image** | `Forma_16.png` |

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Forma 16 — registration certificate |
| **Validation root** (`UserReportBoType`) | `ApplicationItem` |
| **Template family** | `ItemRows` |
| **Applicable application types** | Confirm before seed: `App_Reg_Check_In`, `App_Reg_Check_In_Internal`, `App_Reg_Check_Out`, `App_Reg_Check_Out_Internal`, `App_Reg_ext`, `App_Reg_Info_Change_Address`, `App_Reg_Info_Change_Passport`, `App_Reg_Info_Change_Visa` |
| Applicable project contracts | `null` (TBD) |
| Visibility criteria | `null` (TBD) |
| Sort order (seed) | TBD |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | `Application` |
| **Collection binding** | `rows` (synthetic; one row dict per `ApplicationItem`) |
| **Item inclusion rule** | All active non-deleted `ApplicationItems` on the application (`GetActiveApplicationItems`) |
| **Photo pipeline (Word)** | `IMAGE` post-merge — `WordUserReportImageInjector` |
| **Determinism statement** | Same application + same items + same `Forma_16.docx` bytes + map 1.0.0 ⇒ same merged output (item order = `ApplicationItemName` sort in query) |

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `Forma_16.png` | **Primary** — filled Daşary ýurt raýatlaryny bellige alyş namasy (Yetkin Didem sample) |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `Forma_16.docx` |
| Logical copies per application | `N` = count of active `ApplicationItems` |
| Page / sheet breaks between items | `yes` — `{{:s:}}{{:PageBreak}}` after each full form |
| Empty item list behavior | One empty form body (no rows) — confirm in test |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | A4 portrait |
| Structure summary | Title → photo top-right → §1–§15 → inspector band → 3 tables |
| Static regions | All Turkmen labels and table headers (§8) |
| Dynamic regions | Value lines and table data cells (§6) |
| Typography notes | Serif; values on underlines / dotted leaders |

---

## §6 Placeholder contract (master table)

Wrap **entire form** in `{{#ds.rows}}` … `{{/ds.rows}}`. Tokens below are **exact** strings inside the loop unless noted.

| ID | Region | Static label (literal) | Placeholder token (exact) | BO property | Data type | Golden value (scan) | Notes |
|----|--------|------------------------|---------------------------|-------------|-----------|---------------------|-------|
| P-IMG | Photo | — | `{{IMAGE:Person_Photo}}` | `Person_Photo` | byte[] | (portrait) | fixed cell size in Word |
| F01 | §1 | Familiýasy, ady, atasynyň ady | `{{ds.rows.Person_FullName}}` | `Person_FullName` | string | Yetkin Didem | |
| F02 | §2 | Raýatlygy | `{{ds.rows.Person_NationalityCode}}` | `Person_NationalityCode` | string | TUR | |
| F03 | §3 | Doglan senesi | `{{ds.rows.Person_DateOfBirthText}}` | `Person_DateOfBirthText` | string | 18.01.1977 | |
| F04a | §4 | Pasportynyň belgisi | `{{ds.rows.Passport_Number}}` | `Passport_Number` | string | U36556957 | |
| F04b | §4 | (expiry) | `{{ds.rows.Passport_ExpirationDateText}}` | `Passport_ExpirationDateText` | string | 20.05.2034 | space between F04a/b |
| F05a | §5 | Doglan ýeri, ýurdy | `{{ds.rows.Person_CountryOfBirthCode}}` | `Person_CountryOfBirthCode` | string | TUR | |
| F05b | §5 | | `{{ds.rows.Person_BirthPlace}}` | `Person_BirthPlace` | string | Türkiye/Gaziantep | static ` / ` between |
| F06 | §6 | Jynsy | `{{ds.rows.Person_GenderTm}}` | `Person_GenderTm` | string | Aýal | |
| F07 | §7 | Öý salgysy | `{{ds.rows.Person_ForeignAddress}}` | `Person_ForeignAddress` | string | TUR, Emek mahallesi… | |
| F08 | §8 | Gelmeginiň maksady | `{{ds.rows.Travel_PurposeOfTravelTm}}` | `Travel_PurposeOfTravelTm` | string | Türkmenistandaky şahamça müdiriniň orunbasary-Ali Enes Yetkin-ayaly | v1; Xtra uses IIF — phase-2 combined field optional |
| F09 | §9 | Türkmenistanda bolýan ýeri | `{{ds.rows.Address_FullAddress}}` | `Address_FullAddress` | string | Aşgabat… jaý-86, öý-114 | |
| F10a | §10 | Wizanyň derejesi, görnüşi we № | `{{ds.rows.Visa_CategoryTm}}` | `Visa_CategoryTm` | string | FM | |
| F10b | §10 | | `{{ds.rows.Visa_TypeTm}}` | `Visa_TypeTm` | string | köp gezeklik | |
| F10c | §10 | | `{{ds.rows.Visa_Number}}` | `Visa_Number` | string | A1688318 | static ` №` before number |
| F11 | §11 | Wizanyň berlen ýeri (ýurdy) | `{{ds.rows.Visa_IssuedPlaceTm}}` | `Visa_IssuedPlaceTm` | string | Aşgabat şäher howa menzilindäki MGP | |
| F12a | §12 | Wizanyň berlen senesi we möhleti | `{{ds.rows.Visa_StartDateText}}` | `Visa_StartDateText` | string | 20.01.2026-de | |
| F12b | §12 | | `{{ds.rows.Visa_ExpirationDateText}}` | `Visa_ExpirationDateText` | string | 06.07.2026 çenli | static ` — ` between |
| F13 | §13 | Giren wagty | `{{ds.rows.Travel_DateText}}` | `Travel_DateText` | string | 20.01.2026 | |
| F14 | §14 | Giren ýeri | `{{ds.rows.Travel_CheckPointTm}}` | `Travel_CheckPointTm` | string | Aşgabat şäher howa menzilindäki MGP | |
| F15a | §15 | Kabul edýän edara… | `{{ds.rows.Application_SponsorName}}` | `Application_SponsorName` | string | Çalyk Enerji… şahamçasy | |
| F15b | §15 | (salgy) | `{{ds.rows.Application_CompanyAddress}}` | `Application_CompanyAddress` | string | Aşgabat ş., Bitarap Türkmenistan şaýoly 538 | |
| M1 | Dolduran | Dolduran edara | `TDMG` (static) or `{{ds.rows.Application_MigrationServiceCode}}` | `Application_MigrationServiceCode` | string | TDMG | confirm static vs dynamic |
| M2 | Dolduran | wagty | `{{ds.rows.Application_RegistrationDateText}}` | `Application_RegistrationDateText` | string | 20.01.2026 | |
| M3 | Inspector | Barlan gözegçi | _manual_ | — | — | empty | signature |
| T1-1 | Table1 col1 | Hasaba alan… edarasy | `{{ds.rows.Application_MigrationServiceCode}}` | `Application_MigrationServiceCode` | string | TDMGAS | |
| T1-2 | Table1 col2 | Hasaba alyş, uzaldyş belgisi | _manual_ | — | — | empty | |
| T1-3 | Table1 col3 | Hasaba alnan… wagty | `{{ds.rows.Application_RegistrationDateText}}` | `Application_RegistrationDateText` | string | 20.01.2026 | |
| T1-4 | Table1 col4 | Möhleti | `{{ds.rows.Visa_ExpirationDateText}}` | `Visa_ExpirationDateText` | string | 06.07.2026 | |
| T1-5 | Table1 col5 | Esas (belgisi we wagty) | `{{ds.rows.Application_FullNumber}}` | `Application_FullNumber` | string | 1/-120 | date prefix in scan: 20.01.2026 |
| T1-6 | Table1 col6 | Jogapkär işgäriň… | _manual_ | — | — | empty | |
| T2-1 | Table2 col1 | Türkmenistanyň çäginde… | `{{ds.rows.Address_FullAddress}}` | `Address_FullAddress` | string | (same as F09) | |
| T2-2 | Table2 col2 | Gelen, giden ýeri | `{{ds.rows.Travel_CheckPointTm}}` | `Travel_CheckPointTm` | string | MGP | |
| T2-3 | Table2 col3 | Kabul edýän edara… | `{{ds.rows.Application_SponsorName}}` | `Application_SponsorName` | string | Çalyk Enerji… | |
| T3-1 | Table3 | Pasportynyň möhleti | `{{ds.rows.Passport_IssueDateText}}` + ` – ` + `{{ds.rows.Passport_ExpirationDateText}}` | passport dates | string | 20.05.2024 – 20.05.2034 | |
| T3-2 | Table3 | Milleti | `{{ds.rows.Person_NationalityCode}}` | `Person_NationalityCode` | string | TUR | |
| T3-3 | Table3 | Hasapdan aýyrmak… | _manual_ | — | — | empty | |
| T3-4 | Table3 | Başga bellikler | _manual_ | — | — | empty | |
| T3-5 | Table3 | Esas we ýazgyň wagty | _manual_ | — | — | empty | |

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | yes |
| `{{/ds.rows}}` | yes |
| `{{#ds.ApplicationItems}}` | no |
| `{{/ds.ApplicationItems}}` | no |
| `{{:s:}}{{:PageBreak}}` | yes (between items when N>1) |
| `{{IMAGE:…}}` | `Person_Photo` |

**Scalar-only header tokens:** none (full form inside loop)

---

## §8 Static text inventory

- **DAŞARY ÝURT RAÝATLARYNY BELLIGE ALYŞ NAMASY** (title)
- §1–§15 field labels and parenthetical hints (full Turkmen as on `Forma_16.png`)
- *Dolduran edara*, *wagty*, *Barlan gözegçi (goly, familiýasy, ady)*
- Table 1–3 column headers (all Turkmen titles from scan)

---

## §9 Photos / images

| Field | Value |
|-------|-------|
| Word photos | yes |
| Image token(s) | `{{IMAGE:Person_Photo}}` |
| Cell / region | top-right portrait box |
| Excel images | N/A — Word only |

---

## §10 Excel merge

| Field | Value |
|-------|-------|
| Applicable | **N/A — Word only** |

---

## §11 Deterministic verification

| Check | Command / action |
|-------|------------------|
| Placeholder extract | UI Extract — token set must match §6 + §7 |
| Placeholder validate | UI Validate — `ApplicationItem` root |
| Local preview (Word) | Add `employee-photo-roster`-style preset `forma-16` when template has tokens (TBD) |
| Compare to scan | Merge one-item app; compare to `Forma_16.png` using §12 |
| Map/template sync | §6 change ⇒ bump map version ⇒ user edits docx ⇒ re-Extract |

---

## §12 Golden sample values (from scan)

| ID | Golden value |
|----|--------------|
| F01 | Yetkin Didem |
| F02 | TUR |
| F03 | 18.01.1977 |
| F04a | U36556957 |
| F04b | 20.05.2034 |
| F05a | TUR |
| F05b | Türkiye/Gaziantep |
| F06 | Aýal |
| F07 | TUR, Emek mahallesi gazi ali düşün caddesi bulvar sitesi a blok, no:49, kat:3, daire:9, şehitkamil /gaziantep |
| F08 | Türkmenistandaky şahamça müdiriniň orunbasary-Ali Enes Yetkin-ayaly |
| F09 | Aşgabat şäheriniň 1958-nji (Andalyp) köçesi jaý-86, öý-114 |
| F10a–c | FM · köp gezeklik · A1688318 |
| F11 | Aşgabat şäher howa menzilindäki MGP |
| F12a–b | 20.01.2026-de · 06.07.2026 çenli |
| F13 | 20.01.2026 |
| F14 | Aşgabat şäher howa menzilindäki MGP |
| F15a | Çalyk Enerji Sanayi ve Ticaret A.Ş. Türk kärhanasynyň Türkmenistandaky şahamçasy |
| F15b | Aşgabat ş., Bitarap Türkmenistan şaýoly 538 |
| M2 | 20.01.2026 |
| T1-1 | TDMGAS |
| T1-5 | 1/-120 |
| T3-1 | 20.05.2024 – 20.05.2034 |

---

## §13 Cross-check

| Artifact | Notes |
|----------|-------|
| XtraReport | `RegistrationForm16Report` |
| Existing seed | Not in `UserReportTemplateUpdater` |
| Related templates | `Borcnama.docx` — same `ItemRows` / `rows` family |

---

## §14 Waiver

N/A

---

## §15 Changelog

| Map version | Date | Author | Summary |
|-------------|------|--------|---------|
| 1.0.0 | 2026-05-20 | Agent + user scan | Initial standardized map from `Forma_16.png` |
| 1.0.0 | 2026-05-20 | User | Map approved for Word authoring |
