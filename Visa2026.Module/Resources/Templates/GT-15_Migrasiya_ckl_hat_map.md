# Report Map: GT-15_Migrasiya_ckl_hat

| Field | Value |
|-------|-------|
| **Status** | Approved |
| **Map version** | 1.0.0 |
| **Basename** | `GT-15_Migrasiya_ckl_hat` |
| **Template file(s)** | `GT-15_Migrasiya_ckl_hat.docx` |
| **Format** | Word (user report seed) |
| **Primary reference image** | `GT-15_Migrasiya_ckl_hat.png` |

**Layout family:** **L3** — plain ministry letter (no Çalık letterhead): **Energetika ministrligi** → **Döwlet migrasiýa gullugy**; static **Ministr** / **A.Saparow** signatory.

**Siblings:** **`GT-15_Elyasow_ckl`** / **`GT-15_Elyasow_ckl_only`** — Turkmenenergo addressee + dynamic company-head signatory; **`Sanaw_ckl*`** — personnel list seeds (same **App_Inv** / **App_Inv_And_WP** + GT-15 visibility).

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | GT-15 Migrasiya Çalık hat |
| **Validation root** (`UserReportBoType`) | **`Application`** |
| **Template family** | **`AppScalar`** — one letter per application |
| **Applicable application types** | **`App_Inv`**, **`App_Inv_And_WP`** (same as **`Sanaw_ckl`**) |
| Applicable project contracts | **`GT-15`** — `NameTm` contains `GT-15` |
| Visibility criteria | `null` |
| Sort order (seed) | `65` |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** |
| **Collection binding** | **none** |
| **Item inclusion rule** | N/A |
| **Photo pipeline (Word)** | **none** |
| **Determinism statement** | Same application + template bytes + map 1.0.0 ⇒ same letter; footer always **Ministr** / **A.Saparow** |

**Dynamic fields only:** §6 tokens on **`Application`**. **No** `FullApplicationNumber`, **no** `ApplicationDateText`, **no** Goşundy block, **no** `Application_CompanyHead_*`.

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `GT-15_Migrasiya_ckl_hat.png` | **Primary** — addressee **Döwlet migrasiýa gullugy** (right); urgency *Gyssagly tertipde!* (top left); GT-15 şertnama body; request **1 (bir)** / **1 (bir) aý** / **iki gezeklik** çakylyk; **Bellik**; signatory **Ministr** / **A.Saparow** |

| Region | Map ID | Content |
|--------|--------|---------|
| Top left | U1 | **`{{ds.Urgency_NameTm}}`** |
| Body §2 | B2a–d | Count, period, category (§6) |
| Footer | SIG-L / SIG-R | **Static:** `Ministr` / `A.Saparow` |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `GT-15_Migrasiya_ckl_hat.docx` |
| Logical copies per application | **`1`** |
| Page / sheet breaks | **no** |
| Empty item list behavior | Letter still generates (counts may be `0` if no items — verify app data) |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper | **A4 portrait** |
| Structure summary | Urgency (optional line) → addressee block → **B1** (GT-15 narrative) → **B2** (request) → **B3** (haýyş) → **Bellik** → signatory |
| Static regions | **R1**, **B1**, **B3**, **Bellik**, signatory (§8) |
| Dynamic regions | **U1**, **B2a–d** only |
| Typography | Times New Roman; justified body; italic urgency |

---

## §6 Placeholder contract (master table)

Prefix **`ds.`** on **`Application`**. Type each token in **one Word run** where possible.

| ID | Region | Static label | Placeholder token (exact) | BO property | Data type | Golden value (scan) | Notes |
|----|--------|--------------|---------------------------|-------------|-----------|---------------------|-------|
| U1 | Urgency | — | `{{ds.Urgency_NameTm}}` | `Urgency.NameTm` | string | `Gyssagly tertipde !` | Shown when application has urgency |
| B2a | Body §2 | — | `{{ds.TotalPersonCount}}` | `TotalPersonCount` | int | `1` | Inside request sentence |
| B2b | Body §2 | — | `{{ds.TotalPersonCountText}}` | `TotalPersonCountText` | string | `bir` | `({{ds.TotalPersonCountText}})` after count |
| B2c | Body §2 | — | `{{ds.VisaPeriod_NameTm}}` | `VisaPeriod.NameTm` | string | `1 (bir) aý` | Before *möhlet bilen* |
| B2d | Body §2 | — | `{{ds.VisaCategory_NameTm}}` | `VisaCategory.NameTm` | string | `iki gezeklik` | Before *çakylyk* clause |

**No** other `{{ds.*}}` keys. **Not used:** `FullApplicationNumber`, `ApplicationDateText`, `ProjectContract_Description`, `Application_CompanyHead_*`, row loops.

### B2 — static wrapper (**çakylyk** request)

Only **B2a–d** are placeholders; lead-in and suffix are static in Word:

```text
Agzalan şertnama esasynda, Türkiýe Respublikasynyň "Çalık Enerji Sanayi ve Ticaret A.Ş" kompaniýasyna degişli bolan sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany daşary ýurt raýatyna {{ds.VisaPeriod_NameTm}} möhlet bilen {{ds.VisaCategory_NameTm}} çakylyk resmileşdirilmegine meselesi ýüze çykyar.
```

**Not** *çakylyk we iş rugsatnamasy* (contrast **`GT-15_Elyasow_ckl`**).

---

## §7 Loop and control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **no** |
| `{{/ds.rows}}` | **no** |
| `{{ds.Application_CompanyHead_PositionTm}}` | **no** |
| `{{ds.Application_CompanyHead_FullName}}` | **no** |
| `{{IMAGE:…}}` | **no** |
| `{{:s:}}{{:PageBreak}}` | **no** |

---

## §8 Static text inventory

**No** `{{ds.*}}` in these regions.

### R1 — addressee (right block)

```text
Türkmenistanyň Döwlet
migrasiýa gullugyna
```

### B1 — body paragraph 1 (GT-15 şertnama — Energetika opening)

```text
Türkmenistanyň Energetika ministrligi, Türkmenistanyň Prezidentinin 28.10.2023ý. seneli, 754 belgili kararyna laýyklykda, ministrligiň "Türkmenenergo" döwlet elektroenergetika korporasiýasy bilen Türkiýe Respublikasynyň "Çalık Enerji Sanayi ve Ticaret A.Ş" kompaniýasynyň arasynda "Balkan welaýatynyň Türkmenbaşy etrabynda kuwwatlylygy 1574 MWt bolan utgaşykly dolanyşykda işleýän elektrik stansiýasyny we ony energoulgama birikdirmek üçin gerek bolan elektrik geçiriji ulgamlary gurmak hem-de bar bolan döwlet elektrik stansiýalary üçin zerur bolan ätiýaçlyk şaýlaryny satyn almak" hakyndaky 01.12.2023ý. seneli, GT-15 belgili şertnama baglaşyldy.
```

### B3 — body paragraph 3 (haýyş)

```text
Şunun bilen baglanyşykly, goşundyda görkezilen daşary ýurt raýatynyň resminamasyny bellenilen tertipde resmileşdirmek meselesinde ýardam bermegiňizi haýyş edýäris.
```

### Bellik — responsibility note

```text
Bellik: Daşary ýurt raýatlarynyň Türkmenistana gelmeginiň, bolmagynyň we gitmeginiň düzgünlerini berjaý edmegine "Çalık Enerji Sanayi ve Ticaret A.Ş." kompaniýasy we Türkmenistanyň Energetika ministrligi doly jogapkärçilik çekýärler.
```

### Signatory — static (no placeholders)

| Side | Text (exact) |
|------|----------------|
| Left | `Ministr` |
| Right | `A.Saparow` |

---

## §9 Photos / images

N/A — Word only; no `{{IMAGE:…}}`.

---

## §10 Excel merge

N/A — Word only.

---

## §11 Deterministic verification

| Step | Action |
|------|--------|
| 1 | **`GT-15_Migrasiya_ckl_hat.docx`** wired per §6 — **done** |
| 2 | **Extract** + **Validate** on **`Application`** (5 placeholders) |
| 3 | Seed **GT-15_Migrasiya_ckl_hat** sort **65**; DEBUG reload |
| 4 | **`App_Inv`** or **`App_Inv_And_WP`** + GT-15 → **Resminamalar** → compare §12 to scan |
| 5 | Signatory stays **Ministr** / **A.Saparow** (not `CompanyHead`) |

---

## §12 Golden sample values (from scan)

| ID | Key / region | Golden value |
|----|--------------|--------------|
| U1 | `Urgency_NameTm` | `Gyssagly tertipde !` |
| B2a | `TotalPersonCount` | `1` |
| B2b | `TotalPersonCountText` | `bir` |
| B2c | `VisaPeriod_NameTm` | `1 (bir) aý` |
| B2d | `VisaCategory_NameTm` | `iki gezeklik` |
| SIG-L | static | `Ministr` |
| SIG-R | static | `A.Saparow` |

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **`Sanaw_ckl` / Excel variants** | Same **`App_Inv`**, **`App_Inv_And_WP`** + GT-15 — list vs this **hat** |
| **`GT-15_Elyasow_ckl_only`** | Also çakylyk-only wording; addressee **Turkmenenergo**, dynamic signatory |
| **Code-backed migration letter** | **`App_Visa_And_WP_Ext_GT15_Calik_Migration`** — different type (`App_Visa_and_WP_Ext`); uses `MigrationService_NameTm`, `ProjectContract_Description`, `CompanyHead` |
| **Register** | **`Visa2026.Module.csproj`** embed + **`UserReportTemplateUpdater`** |

---

## §14 Waiver

N/A — scan **`GT-15_Migrasiya_ckl_hat.png`** provided.

---

## §15 Changelog

| Map version | Date | Notes |
|-------------|------|-------|
| 1.0.0 | 2026-05-21 | Initial map from scan + authored **`GT-15_Migrasiya_ckl_hat.docx`**; **Approved**. |
