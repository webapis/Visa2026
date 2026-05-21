# Report Map: <Basename>

<!-- §0 Document header — required -->

| Field | Value |
|-------|-------|
| **Status** | Draft \| Approved \| Implemented |
| **Map version** | 1.0.0 |
| **Basename** | `<Basename>` |
| **Template file(s)** | `<Basename>.docx` \| `Excel/<Basename>.xlsx` |
| **Format** | Word \| Excel |
| **Primary reference image** | `<Basename>.png` |

---

## §1 Identity

| Field | Value |
|-------|-------|
| Display name (in app) | |
| **Validation root** (`UserReportBoType`) | `Application` \| `ApplicationItem` |
| **Template family** | `AppScalar` \| `ItemRows` \| `ItemRoster` \| `ItemScalar` |
| **Applicable application types** | `null` (all) \| list e.g. `App_Reg_Check_In` |
| Applicable project contracts | `null` \| GT-15 substring \| names |
| Visibility criteria | `null` \| criteria string |
| Sort order (seed) | e.g. `55` |

---

## §2 Determinism specification

| Field | Value |
|-------|-------|
| **Bind model prefix** | `ds` (always for user reports) |
| **Merge root at Resminamalar** | `Application` (zip) \| `ApplicationItem` (single-item API only) |
| **Collection binding** | `none` \| `rows` \| `ApplicationItems` |
| **Item inclusion rule** | Default: all active non-deleted items via `GetActiveApplicationItems(objectSpace, application)` |
| **Photo pipeline (Word)** | `none` \| `IMAGE` post-merge (`WordUserReportImageInjector`) |
| **Determinism statement** | Same application data + same template bytes + same map version ⇒ same output bytes |

---

## §3 Reference image(s)

| File | Role |
|------|------|
| `<Basename>.png` | **Primary** — official / signed sample |
| | Secondary (optional) |

**Waiver:** N/A — _or_ who waived scan, date, alternate source.

---

## §4 Output specification (Resminamalar)

| Field | Value |
|-------|-------|
| Zip entries per template | `1` × `<Basename>.docx` \| `.xlsx` |
| Logical copies per application | `1` (AppScalar) \| `N` = item count (ItemRows / ItemRoster) |
| Page / sheet breaks between items | `yes` — `{{:s:}}{{:PageBreak}}` \| `no` \| N/A (Excel row copy) |
| Empty item list behavior | Empty doc \| single empty section \| error — specify |

---

## §5 Page / sheet layout

| Field | Value |
|-------|-------|
| Paper / workbook | A4 portrait \| … |
| Structure summary | e.g. title + §1–15 + 3 tables |
| Static regions | Labels, headers (see §8) |
| Dynamic regions | Value cells in §6 |
| Typography notes | e.g. Times New Roman 12pt |

---

## §6 Placeholder contract (master table)

| ID | Region | Static label (literal) | Placeholder token (exact) | BO property | Data type | Golden value (scan) | Notes |
|----|--------|------------------------|---------------------------|-------------|-----------|---------------------|-------|
| | | | | | | | |

---

## §7 Loop and control tokens

List **exact** strings that must appear in the template file (after Extract, must match).

| Token | Required |
|-------|----------|
| `{{#ds.rows}}` | yes \| no |
| `{{/ds.rows}}` | yes \| no |
| `{{#ds.ApplicationItems}}` | yes \| no |
| `{{/ds.ApplicationItems}}` | yes \| no |
| `{{:s:}}{{:PageBreak}}` | yes \| no |
| `{{IMAGE:…}}` | list keys: e.g. `Person_Photo` |

**Scalar-only header tokens (outside loops):** list `{{ds.…}}` if any.

---

## §8 Static text inventory

Bulleted list of **all** text that stays literal in the template (transcribed from scan). Dynamic values must **not** appear here.

-

---

## §9 Photos / images

| Field | Value |
|-------|-------|
| Word photos | yes \| no |
| Image token(s) | `{{IMAGE:Person_Photo}}` \| N/A |
| Cell / region | e.g. top-right portrait box |
| Excel images | N/A — text only in v1 |

---

## §10 Excel merge

| Field | Value |
|-------|-------|
| Applicable | yes \| **N/A — Word only** |
| **ExcelMergeMode** | `ItemList` \| `SingleItem` |
| Header row tokens | `{{ds.FullApplicationNumber}}`, … |
| Data row marker | `{{#ds.rows}}` on row N |
| Row tokens | `{{.Person_FullName}}`, … |

---

## §11 Deterministic verification

| Check | Command / action |
|-------|------------------|
| Placeholder extract | UI **Extract** or seed updater — count matches §6 rows |
| Placeholder validate | UI **Validate** — all §6 tokens valid for root BO |
| Local preview (Word) | `dotnet run --project tools/PreviewWordReports -- <preset>` — preset name: |
| Local preview (Excel) | `tools/ExcelTemplateSpike` command if exists: |
| Compare to scan | Visual diff vs `<Basename>.png` using §12 golden values |
| Map/template sync | Changing §6 ⇒ bump §0 version ⇒ user edits template ⇒ re-Extract |

---

## §12 Golden sample values (from scan)

Single source of truth for preview fixtures and QA (transcribed from **primary scan**).

| ID | Golden value |
|----|--------------|
| | |

---

## §13 Cross-check

| Artifact | Notes |
|----------|-------|
| XtraReport | name \| N/A |
| Existing seed | `UserReportTemplateUpdater` block \| none |
| Related templates | e.g. same layout as Borcnama |

---

## §14 Waiver

N/A — or scan waiver details.

---

## §15 Changelog

| Map version | Date | Author | Summary |
|-------------|------|--------|---------|
| 1.0.0 | YYYY-MM-DD | | Initial map from `<Basename>.png` |
