# Report Map: GT-15_Elyasow_ckl

| Field | Value |
|-------|-------|
| **Status** | Implemented (seed registered) |
| **Map version** | 1.0.4 |
| **Basename** | `GT-15_Elyasow_ckl` |
| **Template file(s)** | `GT-15_Elyasow_ckl.docx` |
| **Format** | Word (user report seed) |
| **Primary reference image** | `GT-15_Elyasow_ckl.png` |

**Not** the legacy seed **`GT-15_Elyasow_uzt.docx`**. **Layout family:** **L2** (stepped ministry letter). Typography baseline: **`App_Inv_And_WP_Letter.docx`** / **`FormTemplates/App_Inv_And_WP_app_map.md`** (code-backed cross-check only).

---

## Identity

| | |
|---|---|
| **Template file** | `Visa2026.Module/Resources/Templates/GT-15_Elyasow_ckl.docx` |
| **Pipeline** | User report seed (**`visa2026-user-report-templates`**) — manual Word; **do not** regenerate from `tools/GenerateTemplates` |
| **Root BO** | **`Application`** (`UserReportBoType.Application`) |
| **Application type** | **`App_Inv_And_WP`** only (`ApplicationType.Name`) |
| **Project contract filter** | `ProjectContract.NameTm` contains **`GT-15`** (case-insensitive) — seed via `applicableProjectContractNameTmContains: "GT-15"` |
| **Legacy sibling** | `GT-15_Elyasow_uzt.docx` — different file; seeded for **`App_Visa_and_WP_Ext`** (not this map) — **do not conflate** with **ckl** |
| **Xtra cross-check** | **`AppInvAndWPReport`** + **`FormTemplates/App_Inv_And_WP_app_map.md`** + code-backed **`App_Inv_And_WP_Letter.docx`** (same request paragraph: **TotalPersonCount**, **VisaPeriod**, **VisaCategory**, “çakylyk we iş rugsatnamasy”) |
| **Display (working name)** | GT-15 — Turkmenenergo (Elýasowa) — Çalık — Çakylyk we iş rugsatnamasy hat |

---

## Reference scan

| File | Role |
|------|------|
| `GT-15_Elyasow_ckl.png` | **Primary** — co-located in **`Resources/Templates/`** with this map and `GT-15_Elyasow_ckl.docx`. Ministry sample: № **4/7-14**, date **30.04.2026**, Turkmenenergo addressee, **Adaty tertipde !**, salutation **Hormatly Durdy Baýjanowiç!**, GT-15 body, **1 (bir)** / **6 (alty) aý** / **köp gezeklik**, Goşundy, signatory **Mehmet Çırak** |

**Dynamic vs static rule (authoritative):** On **`GT-15_Elyasow_ckl.png`**, only text shown with **yellow background** is **dynamic** (`{{ds.*}}`). **All other** text — including addressee, salutation, GT-15 narrative, B2/B3 wrappers, Goşundy labels — is **static** literal Turkmen in Word. Do **not** add placeholders outside the yellow regions.

### Yellow-highlighted regions → placeholders

| Scan (yellow) | Map ID | Placeholder |
|---------------|--------|-------------|
| `4/7-14` | H1 | `{{ds.FullApplicationNumber}}` |
| `30.04.2026 ý.` | H2 | `{{ds.ApplicationDateText}}` + static ` ý.` |
| `Adaty tertipde !` | U1 | `{{ds.Urgency_NameTm}}` |
| `1 (bir)` | B2a–b | `{{ds.TotalPersonCount}}` `({{ds.TotalPersonCountText}})` |
| `6 (alty) aý` | B2c | `{{ds.VisaPeriod_NameTm}}` |
| `köp gezeklik` | B2d | `{{ds.VisaCategory_NameTm}}` |
| `1 (bir)` — Goşundy line 1 | G1 | `{{ds.TotalPersonCount}}` `({{ds.TotalPersonCountText}})` |
| `1` — Goşundy line 2 | G2 | `{{ds.TotalPersonCount}}` |
| `Türkmenistandaky Şahamçasynyň müdiri` | SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` |
| `Mehmet Çırak` | SIG-R | `{{ds.Application_CompanyHead_FullName}}` |

**Not yellow (static §8):** R1 addressee, S1 salutation, B1 şertnama paragraph, all B2 lead-in/suffix, B3, `Goşundy:` / `-pasport kopiýalary,` / `daşary ýurt raýatynyň maglumatlary)`.

---

## Resminamalar output

| | |
|---|---|
| **Layout ID** | `AppScalar` (one letter per **Application**) |
| **Zip** | Single `.docx` per application when this template is selected |
| **Rows** | None — no `{{#ds.rows}}` |

---

## Band / region map (scan → Word)

**Dynamic** = yellow on scan. **Static** = no yellow.

| ID | Region | Dynamic? | Content |
|----|--------|----------|---------|
| H1 | Reference | **Yes** | Static `№` + **`{{ds.FullApplicationNumber}}`** |
| H2 | Date | **Yes** | **`{{ds.ApplicationDateText}}`** + static ` ý.` |
| R1 | Addressee | **No** | §8 **R1** — four lines Turkmenenergo / Elýasowa |
| U1 | Urgency | **Yes** | **`{{ds.Urgency_NameTm}}`** |
| S1 | Salutation | **No** | §8 **S1** |
| B1 | Body §1 | **No** | §8 **B1** |
| B2 | Body §2 | **Partial** | §8 static wrapper; **yellow only:** count, count text, period, category (§6 B2a–d) |
| B3 | Body §3 | **No** | §8 **B3** |
| G1 | Goşundy 1 | **Partial** | Static `Goşundy: 1. ` … `-pasport kopiýalary,`; **yellow:** **`{{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}})`** → `1 (bir)` |
| G2 | Goşundy 2 | **Partial** | Static `2. Goşundy (` … `maglumatlary)`; **yellow:** **`{{ds.TotalPersonCount}}`** |
| SIG-L | Signatory title | **Yes** | **`{{ds.Application_CompanyHead_PositionTm}}`** |
| SIG-R | Signatory name | **Yes** | **`{{ds.Application_CompanyHead_FullName}}`** |

---

## §6 Field contract — exact `{{ds.*}}` tokens

**Only** the **yellow-highlighted** spans on **`GT-15_Elyasow_ckl.png`** may use placeholders (see table above). **No** `{{ds.ProjectContract_*}}`, **`{{ds.Company_Name}}`**, or other keys.

Type each token in **one Word run** where possible. Prefix is always **`ds.`** on **`Application`**.

| ID | Placeholder token (exact) | `Application` source | Golden value (scan) | Notes |
|----|---------------------------|----------------------|---------------------|-------|
| H1 | `{{ds.FullApplicationNumber}}` | `FullApplicationNumber` | `4/7-14` | Include ministry prefix as stored on app |
| H2 | `{{ds.ApplicationDateText}}` | `ApplicationDateText` (`dd.MM.yyyy`) | `30.04.2026` | Add static ` ý.` **after** token in Word |
| U1 | `{{ds.Urgency_NameTm}}` | `Urgency.NameTm` | `Adaty tertipde !` | Yellow on scan — must be placeholder |
| B2a | `{{ds.TotalPersonCount}}` | `TotalPersonCount` | `1` | Inside request sentence |
| B2b | `{{ds.TotalPersonCountText}}` | `TotalPersonCountText` | `bir` | Parentheses after count: `({{ds.TotalPersonCountText}})` |
| B2c | `{{ds.VisaPeriod_NameTm}}` | `VisaPeriod.NameTm` | `6 (alty) aý` | Static ` möhlet bilen ` before category if not in lookup text |
| B2d | `{{ds.VisaCategory_NameTm}}` | `VisaCategory.NameTm` | `köp gezeklik` | Static suffix in Word: ` çakylyk we iş rugsatnamasynyň resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.` |
| G1a | `{{ds.TotalPersonCount}}` | `TotalPersonCount` | `1` | Goşundy line 1 — yellow count |
| G1b | `{{ds.TotalPersonCountText}}` | `TotalPersonCountText` | `bir` | Immediately after G1a: static ` (` … `)` → **`1 (bir)`** |
| G2 | `{{ds.TotalPersonCount}}` | `TotalPersonCount` | `1` | Goşundy line 2 — yellow digit only on scan |
| SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` | `CompanyHead.Position.NameTm` | `Türkmenistandaky` + `Şahamçasynyň müdiri` | Borderless two-column signatory table per Inv+WP Word standard |
| SIG-R | `{{ds.Application_CompanyHead_FullName}}` | `CompanyHead.FullName` | `Mehmet Çırak` | Right cell |

### B2 — static wrapper (Turkmen, match scan)

Between **B1** and **B3**, keep this **static** skeleton in Word; only the **yellow** spans become placeholders:

```text
Hatyňyzyň goşundysyna görkezilen Türkiýe Respublikasynyň "Çalık Enerji Sanayi ve Ticaret A.Ş" kompaniýasyna degişli bolan sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany daşary ýurt raýatyna {{ds.VisaPeriod_NameTm}} möhlet bilen {{ds.VisaCategory_NameTm}} çakylyk we iş rugsatnamasynyň resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```

### G1 — Goşundy line 1 (exact Word pattern)

Static prefix/suffix; **yellow** = numeric + Turkmen word (same as **B2a–b**):

```text
Goşundy: 1. {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}})-pasport kopiýalary,
```

Golden merged value: **`1 (bir)`** before `-pasport…`.

### B3 — static responsibility (do not merge)

```text
Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan gitmeginiň düzgünleriniň berjaý edilmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.
```

---

## §7 Control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **No** — AppScalar letter |
| Page break | **No** (single page typical) |

---

## §8 Static text inventory

Transcribe **literally** into Word (from **`GT-15_Elyasow_ckl.png`**). **No** `{{ds.*}}` on these regions.

### R1 — addressee (right block, four lines)

```text
"Turkmenenergo" Döwlet
elektroenergetika
korporasiýasynyň başlygy
D. Elýasowa
```

### S1 — salutation (center, bold)

```text
Hormatly Durdy Baýjanowiç!
```

### B1 — body paragraph 1 (justified, first-line indent 720 twips)

```text
Türkmenistanyň Prezidentiniň 28.10.2023ý. seneli, 754 belgili kararyna laýyklykda, Türkmenistanyň Energetika ministrliginiň "Turkmenenergo" döwlet elektroenergetika korporasiýasy bilen Türkiýe Respublikasynyň "Çalık Enerji Sanaýi ve Ticaret A.Ş" kompaniýasynyň arasynda "Balkan welaýatynyň Türkmenbaşy etrabynda kuwwatlylygy 1574 MWt bolan utgaşykly dolanyşykda işleýän elektrik stansiýasyny we ony energoulgama birikdirmek üçin gerek bolan elektrik geçiriji ulgamlary gurmak hem-de bar bolan döwlet elektrik stansiýalary üçin zerur bolan ätiýaçlyk şaýlaryny satyn almak" hakyndaky GT-15 belgili şertnama 01.12.2023ý. senesinde baglaşyldy.
```

Also static: **B3**, full **B2** lead-in and suffix (only four yellow fragments are placeholders), **Goşundy:** prefix and `-pasport…` / `maglumatlary)` suffixes.

---

## §9 Photos / images

| Field | Value |
|-------|-------|
| Word photos | **No** |
| Image token(s) | N/A |

---

## §10 Excel merge

N/A — Word only.

---

## §11 Deterministic verification

| Step | Action |
|------|--------|
| 1 | **Extract** + **Validate** on **User Report Template** row (only §6 tokens; no ministry / description keys) |
| 2 | Open **`App_Inv_And_WP`** application with GT-15 contract |
| 3 | **Resminamalar** → compare to scan: **yellow** regions merged; **non-yellow** text unchanged vs §8 |
| 4 | Optional: **`tools/PreviewWordReports`** preset with §12 dynamic keys only |

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **vs `GT-15_Elyasow_uzt.docx`** | **uzt** = older **`App_Visa_and_WP_Ext`** seed; **ckl** = **`App_Inv_And_WP`** + static ministry/GT-15 copy |
| **vs `App_Inv_And_WP_Letter.docx`** | Code-backed letter uses **`{{ds.ProjectContract_*}}`** for R1/S1/B1; **ckl** freezes those blocks in Word |
| **Person count** | **`TotalPersonCount`** / **`TotalPersonCountText`** — same as **`AppInvAndWPReport`** |
| **Visibility (seed)** | `App_Inv_And_WP` + `applicableProjectContractNameTmContains: "GT-15"` |
| **Register seed** | **`Visa2026.Module.csproj`** embed + **`UserReportTemplateUpdater`** template **GT-15_Elyasow_ckl**, sort order **58** |

---

## §12 Golden sample values (dynamic merge only)

| Key | Dump value |
|-----|------------|
| `FullApplicationNumber` | `4/7-14` |
| `ApplicationDateText` | `30.04.2026` |
| `Urgency_NameTm` | `Adaty tertipde !` |
| `TotalPersonCount` | `1` |
| `TotalPersonCountText` | `bir` |
| `VisaPeriod_NameTm` | `6 (alty) aý` |
| `VisaCategory_NameTm` | `köp gezeklik` |
| `Application_CompanyHead_PositionTm` | `Türkmenistandaky Şahamçasynyň müdiri` |
| `Application_CompanyHead_FullName` | `Mehmet Çırak` |

---

## §15 Changelog

| Version | Date | Notes |
|---------|------|-------|
| 1.0.4 | 2026-05-21 | **G1:** `TotalPersonCount` + `TotalPersonCountText` → `1 (bir)` on Goşundy line 1. Seed registered (sort 58). |
| 1.0.3 | 2026-05-21 | **Approved** by user. Yellow-only dynamic rule; **`App_Inv_And_WP`** + GT-15. |
| 1.0.2 | 2026-05-21 | **R1**, **S1**, **B1** marked **static** (§8); removed ministry / `ProjectContract_Description` placeholders from §6. |
| 1.0.1 | 2026-05-21 | **Application type** corrected to **`App_Inv_And_WP`** (was `App_Visa_and_WP_Ext`). |
| 1.0.0 | 2026-05-20 | Initial map; scan + map co-located under **`Resources/Templates/`** (user report seed). |
