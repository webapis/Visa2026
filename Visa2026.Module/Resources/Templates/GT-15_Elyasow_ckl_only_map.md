# Report Map: GT-15_Elyasow_ckl_only

| Field | Value |
|-------|-------|
| **Status** | Implemented (seed registered) |
| **Map version** | 1.0.1 |
| **Basename** | `GT-15_Elyasow_ckl_only` |
| **Template file(s)** | `GT-15_Elyasow_ckl_only.docx` |
| **Format** | Word (user report seed) |
| **Primary reference image** | `GT-15_Elyasow_ckl_only.png` |

**Layout family:** **L2** — same stepped ministry letter as **`GT-15_Elyasow_ckl`**. **Difference:** static Turkmen asks only for **çakylyk** (invitation) resmileşdirme — **not** *çakylyk we iş rugsatnamasy*. Placeholder set matches authored **`GT-15_Elyasow_ckl_only.docx`** (verified extract).

**Sibling:** **`GT-15_Elyasow_ckl`** — dual çakylyk + iş rugsatnamasy; keep both seeds if both layouts are required.

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | GT-15 Elyasow Çalık — çakylyk only (Turkmenenergo) |
| **Validation root** (`UserReportBoType`) | **`Application`** |
| **Template family** | **`AppScalar`** — one letter per application |
| **Applicable application types** | **`App_Inv`** only (Application for Invitation) |
| Applicable project contracts | **`GT-15`** — `NameTm` contains `GT-15` |
| Visibility criteria | `null` |
| Sort order (seed) | `60` |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** |
| **Collection binding** | **none** |
| **Item inclusion rule** | N/A (no row loop) |
| **Photo pipeline (Word)** | **none** |
| **Determinism statement** | Same application + template bytes + map 1.0.0 ⇒ same letter (same keys as **`GT-15_Elyasow_ckl`** except **G1** pattern) |

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `GT-15_Elyasow_ckl_only.png` | **Primary** — Çalık letterhead; № **5/-752**; **06.05.2026 ý.**; *Gyssagly tertipde!*; **Hormatly Durdy Baýjanowiç!**; B2 **1 (bir)** / **1 (bir) aý** çakylyk only; Goşundy **1-pasport** / **(1-maglumatlary)**; signatory **Mehmet ÇIRAK** |

**Dynamic rule:** Same as **`GT-15_Elyasow_ckl_map.md`** — only §6 tokens are merged; ministry addressee, salutation, GT-15 **B1**, wrappers, and Goşundy labels stay **static** in Word.

### Dynamic regions (authored docx + scan)

| Map ID | Placeholder | Golden (scan) |
|--------|-------------|---------------|
| H1 | `{{ds.FullApplicationNumber}}` | `5/-752` |
| H2 | `{{ds.ApplicationDateText}}` + static ` ý.` | `06.05.2026` |
| U1 | `{{ds.Urgency_NameTm}}` | `Gyssagly tertipde !` |
| B2a–b | `{{ds.TotalPersonCount}}` `({{ds.TotalPersonCountText}})` | `1 (bir)` |
| B2c | `{{ds.VisaPeriod_NameTm}}` | `1 (bir) aý` |
| B2d | `{{ds.VisaCategory_NameTm}}` | _(from application — e.g. single-entry label)_ |
| G1 | `{{ds.TotalPersonCount}}` only | `1` before `-pasport…` (**no** Turkmen word on line 1) |
| G2 | `{{ds.TotalPersonCount}}` | `1` inside `(…maglumatlary)` |
| SIG-L / SIG-R | `Application_CompanyHead_*` | Position + **Mehmet ÇIRAK** |

**Not used on this template (vs `GT-15_Elyasow_ckl`):** **`{{ds.TotalPersonCountText}}`** on **Goşundy line 1** (G1b).

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `GT-15_Elyasow_ckl_only.docx` |
| Logical copies per application | **`1`** |
| Page / sheet breaks between items | **no** |
| Empty item list behavior | N/A |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | **A4 portrait** (Inv+WP letter) |
| Structure summary | Letterhead → ref/date → addressee → urgency → salutation → **B1** → **B2** → **B3** → Goşundy → signatory |
| Static regions | **R1**, **S1**, **B1**, **B3**, Goşundy labels, B2/B3 wrappers (§8) |
| Dynamic regions | §6 tokens only |
| Typography notes | Match **`GT-15_Elyasow_ckl.docx`** / **`App_Inv_And_WP_Letter.docx`** |

### Band / region map

| ID | Region | Dynamic? | Notes |
|----|--------|----------|-------|
| H1 | Reference | **Yes** | `№` + **`{{ds.FullApplicationNumber}}`** |
| H2 | Date | **Yes** | **`{{ds.ApplicationDateText}}`** + ` ý.` |
| R1 | Addressee | **No** | §8 **R1** (same four lines as **ckl**) |
| U1 | Urgency | **Yes** | **`{{ds.Urgency_NameTm}}`** |
| S1 | Salutation | **No** | §8 **S1** |
| B1 | Body §1 | **No** | §8 **B1** (same GT-15 şertnama paragraph as **ckl**) |
| B2 | Body §2 | **Partial** | §8 **B2-only** wrapper; yellow-style spans = §6 B2a–d |
| B3 | Body §3 | **No** | §8 **B3** (same as **ckl**) |
| G1 | Goşundy 1 | **Partial** | **`{{ds.TotalPersonCount}}`-pasport…** only (no G1b) |
| G2 | Goşundy 2 | **Partial** | **`Goşundy ({{ds.TotalPersonCount}} …)`** |
| SIG-L / SIG-R | Signatory | **Yes** | Same Inv+WP two-column table as **ckl** |

---

## §6 Placeholder contract (master table)

Prefix **`ds.`** on **`Application`**. Type each token in **one Word run** where possible.

| ID | Placeholder token (exact) | `Application` source | Golden value (scan) | Notes |
|----|---------------------------|----------------------|---------------------|-------|
| H1 | `{{ds.FullApplicationNumber}}` | `FullApplicationNumber` | `5/-752` | |
| H2 | `{{ds.ApplicationDateText}}` | `ApplicationDateText` | `06.05.2026` | Static ` ý.` after token |
| U1 | `{{ds.Urgency_NameTm}}` | `Urgency.NameTm` | `Gyssagly tertipde !` | Scan shows *Gyssagly* (not *Adaty*) |
| B2a | `{{ds.TotalPersonCount}}` | `TotalPersonCount` | `1` | Request sentence |
| B2b | `{{ds.TotalPersonCountText}}` | `TotalPersonCountText` | `bir` | `({{ds.TotalPersonCountText}})` after count |
| B2c | `{{ds.VisaPeriod_NameTm}}` | `VisaPeriod.NameTm` | `1 (bir) aý` | |
| B2d | `{{ds.VisaCategory_NameTm}}` | `VisaCategory.NameTm` | _(app lookup)_ | Merged from application |
| G1 | `{{ds.TotalPersonCount}}` | `TotalPersonCount` | `1` | **Only digit** before `-pasport kopiýalary,` — **no** `TotalPersonCountText` on this line |
| G2 | `{{ds.TotalPersonCount}}` | `TotalPersonCount` | `1` | Inside `(…maglumatlary)` |
| SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` | `CompanyHead.Position.NameTm` | `Türkmenistandaky şahamçasynyň müdiri` | Scan uses lowercase *şahamçasynyň* |
| SIG-R | `{{ds.Application_CompanyHead_FullName}}` | `CompanyHead.FullName` | `Mehmet ÇIRAK` | |

**No** `{{ds.ProjectContract_*}}`, **`{{ds.Company_Name}}`**, or row loops.

### B2 — static wrapper (**çakylyk only** — diff vs `GT-15_Elyasow_ckl`)

Only **B2a–d** are placeholders; rest is static:

```text
Hatyňyzyň goşundysyna görkezilen Türkiýe Respublikasynyň "Çalık Enerji Sanayi ve Ticaret A.Ş" kompaniýasyna degişli bolan sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany daşary ýurt raýatyna {{ds.VisaPeriod_NameTm}} möhlet bilen {{ds.VisaCategory_NameTm}} çakylyk resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```

**`GT-15_Elyasow_ckl`** ends with **`çakylyk we iş rugsatnamasynyň resmileşdirilmegine`** — do **not** use that suffix on **ckl_only**.

### G1 — Goşundy line 1 (**diff vs ckl**)

```text
Goşundy: 1. {{ds.TotalPersonCount}}-pasport kopiýalary,
```

**Not:** `{{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}})-pasport…` (that pattern is **ckl** only).

### G2 — Goşundy line 2

```text
2. Goşundy ({{ds.TotalPersonCount}} daşary ýurt raýatynyň maglumatlary)
```

### B3 — static responsibility (same as **ckl**)

```text
Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan gitmeginiň düzgünleriniň berjaý edilmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.
```

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **no** |
| `{{:s:}}{{:PageBreak}}` | **no** |

---

## §8 Static text inventory

Same blocks as **`GT-15_Elyasow_ckl_map.md`** §8 unless noted.

### R1 — addressee (unchanged vs **ckl**)

```text
"Turkmenenergo" Döwlet
elektroenergetika
korporasiýasynyň başlygy
D. Elýasowa
```

### S1 — salutation (unchanged)

```text
Hormatly Durdy Baýjanowiç!
```

### B1 — body paragraph 1 (unchanged GT-15 şertnama)

Same literal paragraph as **`GT-15_Elyasow_ckl_map.md`** §8 **B1** (Prezident karary, GT-15 şertnama 01.12.2023ý.).

### Static diff summary vs `GT-15_Elyasow_ckl`

| Region | `GT-15_Elyasow_ckl` | **`GT-15_Elyasow_ckl_only`** |
|--------|---------------------|------------------------------|
| **B2 closing** | `… çakylyk **we iş rugsatnamasynyň** resmileşdirilmegine …` | `… **çakylyk** resmileşdirilmegine …` |
| **G1** | `… {{count}} ({{countText}})-pasport …` | `… {{count}}-pasport …` only |
| **Urgency (sample)** | Often *Adaty tertipde !* | Scan: *Gyssagly tertipde !* (still **`Urgency_NameTm`**) |

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
| 1 | **Extract** + **Validate** — same keys as §6; confirm **no** orphan `TotalPersonCountText` on G1 if map says digit-only |
| 2 | **`App_Inv`** + GT-15 project contract |
| 3 | **Resminamalar** → compare to **`GT-15_Elyasow_ckl_only.png`**; B2 must **not** mention iş rugsatnamasy |
| 4 | Side-by-side with **`GT-15_Elyasow_ckl`** output on same app — only B2/G1 static wording should differ |

---

## §12 Golden sample values (dynamic merge)

| Key | Dump value |
|-----|------------|
| `FullApplicationNumber` | `5/-752` |
| `ApplicationDateText` | `06.05.2026` |
| `Urgency_NameTm` | `Gyssagly tertipde !` |
| `TotalPersonCount` | `1` |
| `TotalPersonCountText` | `bir` |
| `VisaPeriod_NameTm` | `1 (bir) aý` |
| `VisaCategory_NameTm` | _(application)_ |
| `Application_CompanyHead_PositionTm` | `Türkmenistandaky şahamçasynyň müdiri` |
| `Application_CompanyHead_FullName` | `Mehmet ÇIRAK` |

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **vs `GT-15_Elyasow_ckl`** | **`App_Inv_And_WP`** + dual permit B2; **ckl_only** = **`App_Inv`** + çakylyk-only B2/G1 |
| **vs `GT-15_Elyasow_uzt.docx`** | Different file / **`App_Visa_and_WP_Ext`** |
| **vs removed `App_Inv_Letter.docx`** | Replaced by user seed **GT-15_Elyasow_ckl_only** for **`App_Inv`** |
| **Register seed** | **`Visa2026.Module.csproj`** embed + **`UserReportTemplateUpdater`** **GT-15_Elyasow_ckl_only**, sort **60** |

---

## §14 Waiver

N/A — scan **`GT-15_Elyasow_ckl_only.png`** provided.

---

## §15 Changelog

| Version | Date | Notes |
|---------|------|-------|
| 1.0.1 | 2026-05-21 | **Approved**; visibility **`App_Inv`** only + GT-15; placeholders confirmed in Word. |
| 1.0.0 | 2026-05-21 | Initial map from scan; sibling **`GT-15_Elyasow_ckl`**; template **`GT-15_Elyasow_ckl_only.docx`** authored (placeholders extracted). |
