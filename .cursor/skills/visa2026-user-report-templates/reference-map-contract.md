# User report `*_map.md` contract (`Resources/Templates`)

**Blocking:** No **`visa2026-user-report-templates`** work until the map trio exists and the map is **user-approved** — except drafting the map from a scan.

**Canonical format:** Every `*_map.md` must follow **`docs/USER_REPORT_MAP_STANDARD.md`** (sections §0–§15, fixed order). Copy **`Templates/_map_TEMPLATE.md`**. No custom section names or reordering.

**Determinism:** **`reference-deterministic-generation.md`**.

---

## Co-located files (same basename)

| File | Required | Role |
|------|----------|------|
| **`<Basename>_map.md`** | Yes | Approved contract (§0–§15) |
| **`<Basename>.png`** / **`.jpg`** | Yes (or waiver in §14) | Primary scan |
| **`<Basename>.docx`** / **`.xlsx`** | After map **Approved** | User template |

Folders: **`Resources/Templates/`** (Word) · **`Resources/Templates/Excel/`** (Excel).

**Basename rule:** same stem for all three (e.g. `Forma_16`).

---

## Workflow (mandatory order)

1. **Scan** → **`<Basename>.<image>`**
2. **Map** → **`<Basename>_map.md`** from **`_map_TEMPLATE.md`** + scan (§6 golden column filled)
3. **Approve** → Status **Approved** in §0
4. **Author** → user edits **`<Basename>.docx`** / **`.xlsx`** to match §6 + §7 exactly
5. **Register** → embed, updater, Extract, Validate, §11 checks

---

## Section checklist (all required)

See **`docs/USER_REPORT_MAP_STANDARD.md`** for full tables. Summary:

| § | Title |
|---|--------|
| 0 | Document header (status, **map version**, files) |
| 1 | Identity (root BO, **template family**, visibility) |
| 2 | **Determinism specification** |
| 3 | Reference image(s) |
| 4 | Output specification (Resminamalar) |
| 5 | Page / sheet layout |
| 6 | **Placeholder contract** (master table — exact tokens) |
| 7 | **Loop and control tokens** |
| 8 | Static text inventory |
| 9 | Photos / images |
| 10 | Excel merge (or N/A) |
| 11 | **Deterministic verification** |
| 12 | **Golden sample values** |
| 13 | Cross-check |
| 14 | Waiver |
| 15 | Changelog |

---

## Agent rules

| Situation | Action |
|-----------|--------|
| Any skill use for basename `<B>` | Verify `<B>_map.md` + scan; map follows §0–§15 |
| Map missing sections | Complete from template — do not proceed |
| User asks placeholders | Only tokens from approved map §6 |
| Token mismatch after Extract | Fix template or map; bump map version |
| Legacy seed without map | Backfill before material changes |

---

## Legacy backfill

| Basename | Family | Map status |
|----------|--------|------------|
| `Borcnama` | `ItemRows` | **Required** — `Borcnama_map.md` + scan |
| `Contract_uzt`, `Contract_Inv` | `ItemRows` | Required |
| `hasaba_almak_hat` | `AppScalar` | Required |
| `Forma_16` | `ItemRows` | **Approved** — `Forma_16_map.md` v1.0.4 |
| `GT-15_Elyasow_ckl` | `AppScalar` | **Implemented** — seed **GT-15_Elyasow_ckl**; **`App_Inv_And_WP`** + GT-15 |
| `GT-15_Elyasow_ckl_only` | `AppScalar` | **Implemented** — **`App_Inv`** + GT-15; map v1.0.1 |
| `Sanaw_ckl` | `ItemRows` | **Implemented** — Word; **`App_Inv`**, **`App_Inv_And_WP`** + GT-15; map v1.0.3 |
| `Sanaw_ckl` (Excel) | `ItemRows` | **Implemented** — **`Excel/Sanaw_ckl_map.md`** v1.0.2; seed **Sanaw_ckl (Excel)** |
| `Sanaw_ckl_ministr_saparov` (Excel) | `ItemRows` | **Approved** — **`Excel/Sanaw_ckl_ministr_saparov_map.md`** v1.0.0; seed **Sanaw_ckl_ministr_saparov (Excel)** |
| `GT-15_Migrasiya_ckl_hat` | `AppScalar` | **Approved** — **`GT-15_Migrasiya_ckl_hat_map.md`** v1.0.0; **`App_Inv`**, **`App_Inv_And_WP`** + GT-15 |
| `Sanaw_hasaba_alys` (Excel) | `ItemRows` | **Completed** — **`Excel/Sanaw_hasaba_alys_map.md`** v1.0.2; Resminamalar QA passed |

Do not rename legacy `.docx` only for conventions; add map matching current filename.

---

## Git / embed

- **Map + scan:** git only (not `EmbeddedResource`)
- **Template:** `EmbeddedResource` in csproj

---

## Related

- **`reference-template-families.md`**
- **`reference-deterministic-generation.md`**
- **`visa2026-word-reports`** — `FormTemplates/*_map.md` (ministry code-backed; same discipline)
