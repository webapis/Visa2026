# Report Map: wiza_yatyrylmak_hat

| Field | Value |
|-------|-------|
| **Status** | Implemented |
| **Map version** | 1.0.3 |
| **Basename** | `wiza_yatyrylmak_hat` |
| **Template file(s)** | `wiza_yatyrylmak_hat.docx` |
| **Format** | Word |
| **Primary reference image** | `wiza_yatyrylmak_hat.png` |

**Layout family:** **`AppScalar`** — one **Çalık Enerji** branch letter per **`Application`** (Group D — national Migration Service head).

**Application type:** **`App_Cancel_Visa` only** (`cancel_visa`, selection **807**). Not for `App_Cancel_Visa_and_WP`, `App_Cancel_Visa_Ext`, or other cancellation types.

**Template state:** **Implemented** — Extract/Validate OK; Resminamalar verified (e.g. **2 (iki)** persons / **4 (dört)** visas on test application).

---

## Dynamic vs static rule (authoritative)

On **`wiza_yatyrylmak_hat.png`**, only text shown with **yellow background** is **dynamic** (`{{ds.*}}`). **All other** text — Çalık letterhead, addressee, responsibility paragraph, footer address, stamp area, handwritten annotations on the signed sample — is **static** literal Turkmen (or fixed graphics) in Word. **Do not** add placeholders outside the yellow regions.

### Yellow-highlighted regions → placeholders

| Scan (yellow) | Map ID | Placeholder |
|---------------|--------|-------------|
| `№1/-175` | H1 | `{{ds.FullApplicationNumber}}` |
| `02.02.2026 ý.` | H2 | `{{ds.ApplicationDateText}}` + static ` ý.` |
| First `1 (bir)` (person count) | B2a–b | `{{ds.TotalPersonCount}}` `({{ds.TotalPersonCountText}})` |
| Second `1 (bir)` (visa count) | B2c–d | `{{ds.CancelVisaCount}}` `({{ds.CancelVisaCountText}})` |
| `Türkmenistandaky Şahamçasynyň müdiri` | SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` |
| `Mehmet Çırak` | SIG-R | `{{ds.Application_CompanyHead_FullName}}` |

**Not yellow (static §8):** **R1** addressee, full **B2** lead-in / `wizasyny ýatyrmagyňyzy` / closing, **B3** responsibility, letterhead logo block, footer contact lines.

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | Wiza ýatyrmak hat |
| **Validation root** (`UserReportBoType`) | **`Application`** |
| **Template family** | **`AppScalar`** |
| **Applicable application types** | **`App_Cancel_Visa`** only |
| Applicable project contracts | `null` |
| Visibility criteria | `null` |
| Sort order (seed) | `68` (after **Sahsy kagyz** `67`) |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` |
| **Merge root at Resminamalar** | **`Application`** |
| **Collection binding** | **none** |
| **Item inclusion rule** | N/A — person count: **`TotalPersonCount`** (application lines). Visa count: **`CancelVisaCount`** = sum over active lines of `(CurrentVisa ? 1 : 0) + (NextVisa ? 1 : 0)` |
| **Photo pipeline (Word)** | **none** |
| **Determinism statement** | Same **`App_Cancel_Visa`** application + template bytes + map 1.0.0 ⇒ same letter bytes |

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `wiza_yatyrylmak_hat.png` | **Primary** — Çalık Enerji Turkmenistan branch sample: № **`1/-175`**, date **`02.02.2026 ý.`**, addressee **Döwlet migrasiýa gullugy** başlygy, body counts **`1 (bir)`** ×2, signatory **Mehmet Çırak** |
| `FormTemplates/App_Cancel_Visa_app.jpg` | **Secondary** — XAF predefined **`AppCancelVisaReport`** reference (№ `2/-229`, date `10.02.2026`); same layout family, different golden numbers |

**Waiver:** N/A

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `wiza_yatyrylmak_hat.docx` |
| Logical copies per application | **`1`** |
| Page / sheet breaks | **no** |
| Empty item list behavior | Letter still generates; counts may be **`0`** if no active items — verify on test app |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper | **A4 portrait** |
| Structure summary | Letterhead → **H1** / **H2** (top left) → **R1** (right) → **B2** (request) → **B3** (responsibility) → signatory table + stamp → footer |
| Static regions | Letterhead graphic, **R1**, **B2** wrappers, **B3**, footer address / web / email, stamp artwork |
| Dynamic regions | **H1**, **H2**, **B2a–d**, **SIG-L**, **SIG-R** only |
| Typography | Times New Roman; justified body paragraphs; first-line indent **720 twips** on **B2** / **B3** (match Group D reports) |

---

## §6 Field contract — exact `{{ds.*}}` tokens

**Only** the **yellow-highlighted** spans on **`wiza_yatyrylmak_hat.png`** may use placeholders. **No** row loops, **no** `ProjectContract_*`, **no** `Urgency_*`, **no** Goşundy block.

Type each token in **one Word run** where possible. Prefix is always **`ds.`** on **`Application`**.

| ID | Placeholder token (exact) | `Application` source | Golden value (scan) | Notes |
|----|---------------------------|----------------------|---------------------|-------|
| H1 | `{{ds.FullApplicationNumber}}` | `FullApplicationNumber` | `1/-175` | Static `№` **before** token in Word |
| H2 | `{{ds.ApplicationDateText}}` | `ApplicationDateText` (`dd.MM.yyyy`) | `02.02.2026` | Static ` ý.` **after** token |
| B2a | `{{ds.TotalPersonCount}}` | `TotalPersonCount` | `1` | First count — foreign nationals |
| B2b | `{{ds.TotalPersonCountText}}` | `TotalPersonCountText` | `bir` | `({{ds.TotalPersonCountText}})` after B2a |
| B2c | `{{ds.CancelVisaCount}}` | `CancelVisaCount` | `1` | Second count — visas to cancel (per-line **`CurrentVisa`** / **`NextVisa`**) |
| B2d | `{{ds.CancelVisaCountText}}` | `CancelVisaCountText` | `bir` | `({{ds.CancelVisaCountText}})` after B2c |
| SIG-L | `{{ds.Application_CompanyHead_PositionTm}}` | `CompanyHead.Position.NameTm` | `Türkmenistandaky Şahamçasynyň müdiri` | Left cell — borderless two-column signatory table |
| SIG-R | `{{ds.Application_CompanyHead_FullName}}` | `CompanyHead.FullName` | `Mehmet Çırak` | Right cell |

### B2 — static wrapper (Turkmen, match scan)

Keep this **static** skeleton in Word; only the **yellow** spans become placeholders:

```text
Hatymyzyň goşundysynda görkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany daşary ýurt raýatynyň {{ds.CancelVisaCount}} ({{ds.CancelVisaCountText}}) sany wizasyny ýatyrmagyňyzy Sizden haýyş edýäris.
```

Golden merged sentence (single-line sample on scan): **`… 1 (bir) sany daşary ýurt raýatynyň 1 (bir) sany wizasyny ýatyrmagyňyzy …`**

**Multi-line example:** 2 persons, line A has **`CurrentVisa`** + **`NextVisa`**, line B has **`CurrentVisa`** only → **`TotalPersonCount`** `2`, **`CancelVisaCount`** `3` → **`2 (iki) sany daşary ýurt raýatynyň 3 (üç) sany wizasyny …`**

### B3 — static responsibility (do not merge)

Match **`AppBaseReport.RtfResponsibility`** / **`AppCancelVisaReport`**:

```text
Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligini kompaniýamyz öz üstüne alýar.
```

---

## §7 Control tokens

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | **No** — AppScalar letter |
| `{{:s:}}{{:PageBreak}}` | **No** |
| `{{IMAGE:…}}` | **No** |

---

## §8 Static text inventory

Transcribe **literally** into Word (from **`wiza_yatyrylmak_hat.png`**). **No** `{{ds.*}}` on these regions.

### Letterhead (top)

Company block: **ÇALIK ENERJİ TURKMENISTAN BRANCH** (logo + text) — use company Word background / header graphic as for other Çalık **`AppScalar`** seeds; **not** merged from BO.

### R1 — addressee (right block)

```text
Türkmenistanyň Döwlet migrasiýa gullugynyň başlygyna
```

### B2 — static fragments (inside §6 wrapper)

Already embedded in §6 skeleton: lead-in **`Hatymyzyň goşundysynda görkezilen sanawdaky`**, middle **`sany daşary ýurt raýatynyň`**, **`sany wizasyny ýatyrmagyňyzy`**, closing **`Sizden haýyş edýäris.`**

### Footer (bottom right, printed)

```text
Bitarap Turkmenistan shayoly 538
Ashgabat - Turkmenistan
www.calikenerji.com
info@calikenerji.com
```

**Do not** placeholder handwritten approval date/signature on the archival scan.

---

## §9 Photos / images

| Field | Value |
|-------|-------|
| Word photos | **No** |
| Image token(s) | N/A |
| Company stamp | **Static** artwork in Word (not merged) |

---

## §10 Excel merge

N/A — Word only.

---

## §11 Deterministic verification

| Step | Action |
|------|--------|
| 1 | Author **`wiza_yatyrylmak_hat.docx`** from this map; embed in **`Visa2026.Module.csproj`**; register in **`UserReportTemplateUpdater`** (`App_Cancel_Visa`, sort **68**) |
| 2 | **Extract** + **Validate** on **User Report Template** row — only §6 tokens |
| 3 | Open **`App_Cancel_Visa`** application with at least one active item; set **`FullApplicationNumber`**, date, counts, company head |
| 4 | **Resminamalar** → compare to **`wiza_yatyrylmak_hat.png`**: yellow regions merged; non-yellow unchanged vs §8 |
| 5 | Optional: **`tools/PreviewWordReports`** preset with §12 keys |

---

## §12 Golden sample values (dynamic merge only)

| Key | Dump value |
|-----|------------|
| `FullApplicationNumber` | `1/-175` |
| `ApplicationDateText` | `02.02.2026` |
| `TotalPersonCount` | `1` |
| `TotalPersonCountText` | `bir` |
| `CancelVisaCount` | `1` |
| `CancelVisaCountText` | `bir` |
| `Application_CompanyHead_PositionTm` | `Türkmenistandaky Şahamçasynyň müdiri` |
| `Application_CompanyHead_FullName` | `Mehmet Çırak` |

---

## §13 Cross-check

| Topic | Decision |
|-------|----------|
| **vs `App_Cancel_Visa_Letter.docx`** | Code-backed L1 letter (`AppCancelVisaLetterReportDef`) — same §6 keys; uses `ApplicationDate` key internally but user-report merge should use **`ApplicationDateText`** + static ` ý.` |
| **vs `AppCancelVisaReport` (XtraReport)** | Same **B2** sentence (person count twice for visa-only cancel); fixed **R1** recipient |
| **vs `App_Cancel_Visa_and_WP`** | Different type — uses **`CancelPersonCount*`** / **`CancelWPCount*`**; see **`App_Cancel_Visa_and_WP_app_map.md`** |
| **vs `hasaba_almak_hat`** | Both Çalık **`AppScalar`** letters; **hasaba** = **`App_Reg_Check_In`** only |
| **Person count** | **`TotalPersonCount`** / **`TotalPersonCountText`** (lines on application) |
| **Visa count** | **`CancelVisaCount`** / **`CancelVisaCountText`** — sum per line: +1 if **`CurrentVisa`**, +1 if **`NextVisa`** (excludes deleted lines) |
| **vs legacy XtraReport** | **`AppCancelVisaReport`** still duplicates **`TotalPersonCount`** for both slots — align when that report is updated |
| **Register seed** | **`UserReportTemplateUpdater`** — **Wiza ýatyrmak hat**, sort **68**, **`App_Cancel_Visa`** |

---

## §14 Authoring checklist (post-map)

- [x] Create **`wiza_yatyrylmak_hat.docx`** on Çalık letterhead (copy styling from **`App_Cancel_Visa_Letter.docx`** or signed scan)
- [x] Place §6 tokens only in yellow regions; §8 static text verbatim
- [x] Signatory: borderless two-column table (**SIG-L** / **SIG-R**), room for stamp between columns
- [x] Co-locate **`wiza_yatyrylmak_hat.png`**
- [x] Embed resource + seed updater
- [x] **Extract** / **Validate** in app (User Report Template row)
- [x] Resminamalar test on **`App_Cancel_Visa`**

---

## §15 Changelog

| Version | Date | Notes |
|---------|------|-------|
| 1.0.3 | 2026-05-28 | **Implemented** — Resminamalar QA passed (person vs visa counts). |
| 1.0.2 | 2026-05-28 | **Approved**; **`wiza_yatyrylmak_hat.docx`** authored; embedded + seed (sort 68). |
| 1.0.1 | 2026-05-28 | **B2c–d:** visa count uses **`CancelVisaCount`** / **`CancelVisaCountText`** (per-line **`CurrentVisa`** + **`NextVisa`**). |
| 1.0.0 | 2026-05-28 | Initial map from user scan; **`App_Cancel_Visa`** only; **`AppScalar`**; eight dynamic tokens. |
