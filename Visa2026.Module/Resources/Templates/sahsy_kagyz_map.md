# Report Map: sahsy_kagyz

| Field | Value |
|-------|-------|
| **Status** | Approved |
| **Map version** | 1.0.4 |
| **Basename** | `sahsy_kagyz` |
| **Template file(s)** | `sahsy_kagyz.docx` |
| **Format** | Word |
| **Primary reference image** | `sahsy_kagyz.png` |

**Layout family:** **`ItemRows`** — one **ŞAHSY KAGYZY** page per active **`ApplicationItem`** (same Resminamalar zip pattern as **`Forma_16`**).

**Template state:** Placeholders placed in `sahsy_kagyz.docx` (2026-05-28). Before **Extract** / **Validate**, confirm §7 loop tokens and **SIG-CO** (see authoring checklist below).

---

## Dynamic vs static rule (authoritative)

On **`sahsy_kagyz.png`**, only content shown with **yellow background** is **dynamic** (`{{ds.rows.*}}` or `{{IMAGE:…}}`). **All other** text — title, field labels, declaration paragraph, **Türkmenistanda öňki işlän ýerleri** (blank underline), date line, **M.Ý.**, signature line — stays **static** literal Turkmen in Word. **Do not** add placeholders outside yellow regions.

### Yellow-highlighted regions → placeholders

| Scan (yellow) | Map ID | Placeholder |
|---------------|--------|-------------|
| Portrait (top-right) | P-IMG | `{{IMAGE:Person_Photo}}` |
| `Hilmi Erol` | F01 | `{{ds.rows.Person_FullName}}` |
| `16.05.1980 ý., TUR, Türkiýe/Üsküdar` | F02–F02c | `{{ds.rows.Person_DateOfBirthText}}` + static ` ý., ` + `{{ds.rows.Person_CountryOfBirthCode}}` + static `, ` + `{{ds.rows.Person_BirthPlace}}` |
| `TUR` (Raýatlygy) | F03 | `{{ds.rows.Person_NationalityCode}}` |
| `U20352559` | F04a | `{{ds.rows.Passport_Number}}` |
| `20.06.2018` (berlen senesi) | F04b | `{{ds.rows.Passport_IssueDateText}}` + static ` ý., ` |
| `20.06.2028` (möhleti) | F04c | `{{ds.rows.Passport_ExpirationDateText}}` + static ` ý.` |
| _(full passport line)_ | F04 | F04a + static `, ` + F04b + F04c — see §6 assembly |
| `11402573788` | F05 | `{{ds.rows.Passport_PersonalNumber}}` |
| `Ýokary` (bilimi / education level) | F06a | `{{ds.rows.Education_LevelTm}}` |
| `TUR` (okan ýeri — country) | F06b | `{{ds.rows.Education_CountryCode}}` |
| `Gündogar mediterian uniwersiteti` (institution) | F06c | `{{ds.rows.Education_InstitutionName}}` |
| _(full education line)_ | F06 | F06a + static `, ` + F06b + static `, ` + F06c — see §6 assembly |
| `elektrik-elektronika inženerçiligi` | F07 | `{{ds.rows.Education_SpecialtyTm}}` |
| `Taslamanyň dolandyryş müdiri` | F08 | `{{ds.rows.Position_PositionTm}}` |
| `ayaly-Firuza Mine Erol …` | F10 | `{{ds.rows.SahsyKagyz_FamilyStatusText}}` |
| `TUR, Tatlısu mah. …` | F11 | `{{ds.rows.Person_ForeignAddressWithCountry}}` |
| `«Çalık Enerji Sanayi ve Ticaret A.Ş»` | SIG-CO | static `«` + `{{ds.rows.Application_SponsorName}}` + static `»` |
| `Türkmenistandaky Şahamçasynyň müdiri` | SIG-T | `{{ds.rows.Application_CompanyHead_PositionTm}}` |
| `Mehmet ÇIRAK` | SIG-N | `{{ds.rows.Application_CompanyHead_FullName}}` |

**Not yellow (static §8):** **ŞAHSY KAGYZY** title; all field **labels**; declaration paragraph; **Türkmenistanda öňki işlän ýerleri:** empty underline; date line `"____" … 20____ ý.`; **M.Ý.**; **goly** signature line.

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Şahsy kagyz — Daşary ýurt raýatynyň maglumatlary (proposed) |
| **Validation root** (`UserReportBoType`) | **`ApplicationItem`** |
| **Template family** | **`ItemRows`** |
| **Applicable application types** | **`App_Inv`**, **`App_Inv_And_WP`** |
| Applicable project contracts | **`null`** (all contracts) |
| Visibility criteria | `null` |
| Sort order (seed) | **`67`** (proposed) |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** |
| **Collection binding** | **`rows`** — synthetic; one row dict per `ApplicationItem` |
| **Item inclusion rule** | All active non-deleted `ApplicationItems` (`GetActiveApplicationItems`) |
| **Photo pipeline (Word)** | **`IMAGE`** post-merge — `WordUserReportImageInjector` |
| **Determinism statement** | Same application + same items + same template bytes + map 1.0.3 ⇒ same merged output |

**Signatory / sponsor on each page:** inside `{{#ds.rows}}` via **`ApplicationItem`** aliases (`Application_SponsorName`, `Application_CompanyHead_*`) — same values on every item row for one application.

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `sahsy_kagyz.png` | **Primary** — yellow = dynamic; Hilmi Erol sample (Çalık) |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `sahsy_kagyz.docx` |
| Logical copies per application | **`N`** = count of active `ApplicationItems` |
| Page / sheet breaks between items | **`yes`** — `{{:s:}}{{:PageBreak}}` after each full form when **N > 1** |
| Empty item list behavior | One empty form body (no rows) — confirm in test |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | A4 portrait |
| Structure summary | Title → **photo (yellow)** → labeled fields (yellow values only) → static declaration → **yellow signatory block** → static date / stamp / signature scaffolding |
| Static regions | Title, labels, declaration, F09 prior-work underline (empty), date / **M.Ý.** / **goly** lines |
| Dynamic regions | Yellow spans only (§6 + yellow table above) |
| Typography notes | Serif; values on underlines |

---

## §6 Placeholder contract (master table)

Wrap **entire form** (including signatory block) in `{{#ds.rows}}` … `{{/ds.rows}}`. Tokens below are **exact** strings inside the loop.

**DocxTemplater:** use `{{ds.rows.Property}}`. Type each token in **one Word run** where possible.

| ID | Region | Static label (literal) | Placeholder token (exact) | BO property | Data type | Golden value (scan) | Notes |
|----|--------|------------------------|---------------------------|-------------|-----------|---------------------|-------|
| P-IMG | Photo | — | `{{IMAGE:Person_Photo}}` | `Person_Photo` | byte[] | (portrait) | **Yellow** — top-right; post-merge injector |
| F01 | Name | Familiýasy, ady, atasynyň ady | `{{ds.rows.Person_FullName}}` | `Person_FullName` | string | Hilmi Erol | **Yellow** |
| F02 | Birth | Doglan senesi we ýeri | `{{ds.rows.Person_DateOfBirthText}}` | `Person_DateOfBirthText` | string | 16.05.1980 | **Yellow** — static **` ý., `** after |
| F02b | Birth | (country + place) | `{{ds.rows.Person_CountryOfBirthCode}}` | `Person_CountryOfBirthCode` | string | TUR | **Yellow** — static **`, `** before place |
| F02c | Birth | | `{{ds.rows.Person_BirthPlace}}` | `Person_BirthPlace` | string | Türkiýe/Üsküdar | **Yellow** |
| F03 | Citizenship | Raýatlygy | `{{ds.rows.Person_NationalityCode}}` | `Person_NationalityCode` | string | TUR | **Yellow** |
| F04a | Passport | Pasportyň belgisi | `{{ds.rows.Passport_Number}}` | `Passport_Number` | string | U20352559 | **Yellow** — passport number only |
| F04b | Passport | berlen senesi | `{{ds.rows.Passport_IssueDateText}}` | `Passport_IssueDateText` | string | 20.06.2018 | **Yellow** — `CurrentPassport.IssueDate` as `dd.MM.yyyy`; static **` ý., `** after token |
| F04c | Passport | möhleti | `{{ds.rows.Passport_ExpirationDateText}}` | `Passport_ExpirationDateText` | string | 20.06.2028 | **Yellow** — `CurrentPassport.ExpirationDate` as `dd.MM.yyyy`; static **` ý.`** after token |
| F04 | Passport | Pasportyň belgisi, berlen senesi we möhleti (assembled) | `{{ds.rows.Passport_Number}}`, `{{ds.rows.Passport_IssueDateText}}`, `{{ds.rows.Passport_ExpirationDateText}}` | (F04a–c) | string | U20352559, 20.06.2018 ý., 20.06.2028 ý. | **Word order:** `F04a` + static `, ` + `F04b` + static ` ý., ` + `F04c` + static ` ý.` |
| F05 | Personal ID | Şahsy belgisi | `{{ds.rows.Passport_PersonalNumber}}` | `Passport_PersonalNumber` | string | 11402573788 | **Yellow** |
| F06a | Education | bilimi (education level) | `{{ds.rows.Education_LevelTm}}` | `Education_LevelTm` | string | Ýokary | **Yellow** — `CurrentEducation.EducationLevel.NameTm` |
| F06b | Education | okan ýeri — country | `{{ds.rows.Education_CountryCode}}` | `Education_CountryCode` | string | TUR | **Yellow** — `CurrentEducation.EducationCountry.Code` |
| F06c | Education | okan ýeri — institution | `{{ds.rows.Education_InstitutionName}}` | `Education_InstitutionName` | string | Gündogar mediterian uniwersiteti | **Yellow** — institution name (NameTm-first) |
| F06 | Education | Bilimi, okan ýeri (assembled) | `{{ds.rows.Education_LevelTm}}`, `{{ds.rows.Education_CountryCode}}`, `{{ds.rows.Education_InstitutionName}}` | (F06a–c) | string | Ýokary, TUR, Gündogar mediterian uniwersiteti | **Word order:** `F06a` + static `, ` + `F06b` + static `, ` + `F06c` |
| F07 | Specialty | Hünäri | `{{ds.rows.Education_SpecialtyTm}}` | `Education_SpecialtyTm` | string | elektrik-elektronika inženerçiligi | **Yellow** |
| F08 | Position | Wezipesi | `{{ds.rows.Position_PositionTm}}` | `Position_PositionTm` | string | Taslamanyň dolandyryş müdiri | **Yellow** |
| F09 | Prior work TM | Türkmenistanda öňki işlän ýerleri: | _none_ | — | — | _(blank)_ | **Not yellow** — leave empty underline; no placeholder |
| F10 | Family | Maşgala ýagdaýy | `{{ds.rows.SahsyKagyz_FamilyStatusText}}` | `SahsyKagyz_FamilyStatusText` | string | ayaly-Firuza Mine Erol 23.05.1985ý. TUR., gyzy-Nil Erol 03.07.2014ý. TUR. | **Yellow** — `Person.FamilyMembers` inline format or manual visa text |
| F11 | Foreign address | Daşary ýurtdaky ýaşaýan anyk salgysy | `{{ds.rows.Person_ForeignAddressWithCountry}}` | `Person_ForeignAddressWithCountry` | string | TUR, Tatlısu mah. … | **Yellow** |
| SIG-CO | Signatory | (company name) | `«{{ds.rows.Application_SponsorName}}»` | `Application_SponsorName` | string | Çalık Enerji Sanayi ve Ticaret A.Ş | **Yellow** — guillemets **static** in Word as `«` … `»` around token |
| SIG-T | Signatory | (position) | `{{ds.rows.Application_CompanyHead_PositionTm}}` | `Application_CompanyHead_PositionTm` | string | Türkmenistandaky Şahamçasynyň müdiri | **Yellow** |
| SIG-N | Signatory | (name) | `{{ds.rows.Application_CompanyHead_FullName}}` | `Application_CompanyHead_FullName` | string | Mehmet ÇIRAK | **Yellow** |

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **yes** — wraps full page including signatory |
| `{{/ds.rows}}` | **yes** |
| `{{#ds.ApplicationItems}}` | no |
| `{{/ds.ApplicationItems}}` | no |
| `{{:s:}}{{:PageBreak}}` | **yes** (between items when N>1) |
| `{{IMAGE:…}}` | `Person_Photo` |

**Scalar-only header tokens (outside `rows`):** **none** — all yellow fields inside `{{#ds.rows}}`

---

## §8 Static text inventory

- **ŞAHSY KAGYZY** (title, centered)
- Field labels: **Familiýasy, ady, atasynyň ady**; **Doglan senesi we ýeri**; **Raýatlygy**; **Pasportyň belgisi, berlen senesi we möhleti**; **Şahsy belgisi**; **Bilimi, okan ýeri**; **Hünäri**; **Wezipesi**; **Türkmenistanda öňki işlän ýerleri:** (label + **empty** underline — not yellow); **Maşgala ýagdaýy**; **Daşary ýurtdaky ýaşaýan anyk salgysy**
- Declaration: *Daşary ýurt raýatlary barada galp maglumatlary görkezilen ýagdaýynda Türkmenistanyň kanunçylygyna laýyklykda doly jogapkärçiligi çekýärin.*
- **SIG-D:** date line `"____" _______________ 20____ ý.` (blank — not yellow)
- **SIG-S:** `M.Ý.` (not yellow)
- **SIG-G:** `____________________ goly` (not yellow)

---

## §9 Photos / images

| Field | Value |
|-------|-------|
| Word photos | **yes** |
| Image token(s) | `{{IMAGE:Person_Photo}}` |
| Cell / region | top-right portrait (**yellow** on scan) |
| Excel images | **N/A — Word only** |

---

## §10 Excel merge

| Field | Value |
|-------|-------|
| Applicable | **N/A — Word only** |

---

## §11 Deterministic verification

| Check | Command / action |
|-------|------------------|
| Placeholder extract | UI **Extract** — must match §6 + §7 (yellow only) |
| Placeholder validate | UI **Validate** — `ApplicationItem` root |
| Compare to scan | No yellow literal left in merged output except static §8 |
| Map/template sync | §6 change ⇒ bump map version ⇒ re-author docx ⇒ re-Extract |

### Authoring checklist (after placeholders placed)

| Item | Required in `sahsy_kagyz.docx` |
|------|--------------------------------|
| Row loop | `{{#ds.rows}}` before first field; `{{/ds.rows}}` after signatory block |
| Multi-item | `{{:s:}}{{:PageBreak}}` at end of each form when **N > 1** |
| Company (yellow) | `«{{ds.rows.Application_SponsorName}}»` (guillemets static) |
| Split runs | Re-type any token Word split across runs if Extract misses it |

**Seed:** `UserReportTemplateUpdater` — **Sahsy kagyz**, sort **67**, `App_Inv` + `App_Inv_And_WP`. **Merge:** `BuildSahsyKagyzStyleRows` in `UserReportMergeDataHelper.cs`.

---

## §12 Golden sample values (from scan)

| ID | Golden value |
|----|--------------|
| F01 | Hilmi Erol |
| F02 | 16.05.1980 |
| F02b | TUR |
| F02c | Türkiýe/Üsküdar |
| F03 | TUR |
| F04a | U20352559 |
| F04b | 20.06.2018 |
| F04c | 20.06.2028 |
| F05 | 11402573788 |
| F06a | Ýokary |
| F06b | TUR |
| F06c | Gündogar mediterian uniwersiteti |
| F07 | elektrik-elektronika inženerçiligi |
| F08 | Taslamanyň dolandyryş müdiri |
| F10 | ayaly-Firuza Mine Erol 23.05.1985ý. TUR., gyzy-Nil Erol 03.07.2014ý. TUR. |
| F11 | TUR, Tatlısu mah. Şahin cad. Yüksel City A Blok, No:58A/5 Ümraniye/İstanbul |
| SIG-CO | Çalık Enerji Sanayi ve Ticaret A.Ş |
| SIG-T | Türkmenistandaky Şahamçasynyň müdiri |
| SIG-N | Mehmet ÇIRAK |

---

## §13 Cross-check

| Artifact | Notes |
|----------|-------|
| **`GT-15_Elyasow_ckl_map.md`** | Same **yellow-only** authoring rule |
| **`Sanaw_ckl_map.md`** | Same `Application_CompanyHead_*` sources; Sanaw uses footer **outside** `rows` (one table); **sahsy_kagyz** uses **`ds.rows.*`** inside loop (one form per item) |
| **BO** | `Education_CountryCode`, `SahsyKagyz_FamilyStatusText` on `ApplicationItem` |
| **Seed** | **Sahsy kagyz** — `UserReportTemplateUpdater` sort **67** |

---

## §14 Waiver

N/A

---

## §15 Changelog

| Map version | Date | Author | Summary |
|-------------|------|--------|---------|
| 1.0.0 | 2026-05-28 | Agent + user scan | Initial map |
| 1.0.1 | 2026-05-28 | User | **Yellow-only** rule: photo + all yellow body/footer → placeholders; signatory **SIG-CO/T/N** dynamic; F09 prior-work **not** placeholder |
| 1.0.2 | 2026-05-28 | User | Passport **issue** / **expiration** dates explicit: **F04b** `Passport_IssueDateText`, **F04c** `Passport_ExpirationDateText` in yellow table + §6 assembly row |
| 1.0.3 | 2026-05-28 | User | Education **level** explicit: **F06a** `Education_LevelTm`; split F06b/F06c in yellow table + §6 assembly row |
| 1.0.4 | 2026-05-28 | User + agent | Placeholders in docx; map **Approved**; seed + `BuildSahsyKagyzStyleRows`; BO properties added |
