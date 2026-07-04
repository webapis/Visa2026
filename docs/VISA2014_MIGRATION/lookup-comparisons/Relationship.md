# Lookup comparison — Relationship

**Compared:** 2026-06-21  
**Legacy source:** `VISA2015` — `dbo.Relation` (+ `Person.FamilyMemberRelation` FK)  
**Target source:** [`relationship.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/relationship.json)

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy shape** | Small catalog: `RelativeAs` **int** (0–8) + `RelativeAsL` **nvarchar(100)** display label |
| **Person coverage** | **159 / 2,569** active persons have `FamilyMemberRelation` FK (family members only; employees typically null) |
| **Lookup table bloat** | **54** total rows, **12** active — many duplicate Oids per same `RelativeAsL` (soft-deleted); **only 7** distinct labels used on Person |
| **Visa2026 catalog** | **9** fixed global rows (`NameTm` + `ReverseNameTm` + `LocalizationKey`) |
| **Layer 3 today** | Approved in `lookup-translations.yaml` (8 legacy labels → `NameTm`) |
| **Verdict** | **Approved** (2026-06-21) — all 159 Person FK rows map; **Mother** + **BrotherInLaw** added to Visa2026 catalog |

---

## Visa2026 catalog (target — all rows)

| NameTm | ReverseNameTm | LocalizationKey | In legacy Person data? |
|--------|---------------|-----------------|------------------------|
| aýaly | adamsy | Wife | Yes — legacy `ayaly` (58) |
| adamsy | aýaly | Husband | **No** Person FK (RelativeAs=8 exists in table, unused) |
| ogly | kakasy | Son | Yes — legacy `ogly` (49) + `Ogly` (1) |
| gyzy | kakasy | Daughter | Yes — legacy `gyzy` (32) |
| kakasy | ogly/gyzy | Father | Yes — legacy `kakasy` (6) |
| aýal dogany | erkek dogany | Sister | Yes — legacy `ayal dogany` (1) |
| İnisi | agasy | YoungerSister | **No** Person FK (RelativeAs=7 in table, unused) |
| baldyzy | baldyzy | Grandchild | **No** Person FK |
| (gaýny) aýalynyň ejesi | giýewisi | MotherInLaw | **No** Person FK (legacy `(gayny) ayalynyn ejesi` active, 0 hits) |

| ejesi | ejesi (Mother) | **Yes** — legacy `ejesi` (11) |
| aýal doganyň adamsy (giýewisi) | BrotherInLaw | **Yes** — legacy `Ayal doganynyn adamsy (giyewisi)` (1) |

**Note:** **Mother** and **BrotherInLaw** added to Visa2026 `relationship.json` 2026-06-21.

---

## Legacy values used on Person (primary comparison)

Match uses `LookupCatalogMatchHelper` folding (`ayaly` ↔ `aýaly`, case-insensitive).

| Legacy (VISA2015) | Visa2026 (target) | Person rows | Match | Import note |
|-------------------|-------------------|------------:|-------|-------------|
| `ayaly`, `RelativeAs=0` | **aýaly** (Wife) | **58** | **approved** | Map by normalized `RelativeAsL` → `NameTm`. |
| `ogly`, `RelativeAs=1` | **ogly** (Son) | **49** | **approved** | Same. |
| `Ogly`, `RelativeAs=1` | **ogly** (Son) | **1** | **approved** | Case variant of Son. |
| `gyzy`, `RelativeAs=2` | **gyzy** (Daughter) | **32** | **approved** | Same. |
| `kakasy`, `RelativeAs=4` | **kakasy** (Father) | **6** | **approved** | Same. |
| `ayal dogany`, `RelativeAs=6` | **aýal dogany** (Sister) | **1** | **approved** | Same. |
| `ejesi`, `RelativeAs=3` | **ejesi** (Mother) | **11** | **approved** | Added to Visa2026 catalog 2026-06-21. |
| `Ayal doganynyn adamsy (giyewisi)`, `RelativeAs=6` | **aýal doganyň adamsy (giýewisi)** (BrotherInLaw) | **1** | **approved** | Added to Visa2026 catalog 2026-06-21. |

**Totals:** 159 Person rows — **all mapped**.

---

## RelativeAs int (legacy) — do not use alone for layer 3

| RelativeAs | Typical RelativeAsL | On Person? | Notes |
|----------:|---------------------|------------|-------|
| 0 | ayaly | 58 | → Wife |
| 1 | ogly / Ogly | 50 | → Son |
| 2 | gyzy | 32 | → Daughter |
| 3 | ejesi **or** (gayny) ayalynyn ejesi | 11 (ejesi only) | **Ambiguous int** — must match on `RelativeAsL` string |
| 4 | kakasy | 6 | → Father |
| 6 | ayal dogany **or** Ayal doganynyn adamsy (giyewisi) | 2 | **Ambiguous int** |
| 7 | Inisi | 0 | → YoungerSister if ever used |
| 8 | adamsy | 0 | → Husband if ever used |

---

## Target-only (Visa2026 catalog not referenced on Person)

| Visa2026 (target) | LocalizationKey | Match | Import note |
|-------------------|-----------------|-------|-------------|
| adamsy | Husband | **target_only** | Legacy table has many `adamsy` Oids; no Person FK in prod sample. |
| İnisi | YoungerSister | **target_only** | Active legacy row; unused on Person. |
| baldyzy | Grandchild | **target_only** | Active legacy row; unused on Person. |
| (gaýny) aýalynyň ejesi | MotherInLaw | **target_only** | Active legacy row; unused on Person. |

---

## Legacy-only (lookup table noise — do not import)

| Legacy | Active lookup rows | On Person? | Import note |
|--------|-------------------:|------------|-------------|
| Duplicate Oids per same `RelativeAsL` | 42 soft-deleted duplicates | Some | Resolve by **normalized `RelativeAsL`**, not legacy Oid. |
| Unused active labels (adamsy, Inisi, baldyzy, mother-in-law) | 4 | No | No Person rows — no layer-3 entry needed unless future data appears. |

---

## Approved decisions (2026-06-21)

1. **All 7 legacy `RelativeAsL` labels** map to Visa2026 `NameTm` (ASCII-fold + explicit legacy keys in `lookup-translations.yaml`).
2. **Mother** (`ejesi`, 11 rows) — added to `relationship.json` (`LocalizationKey`: `Mother`).
3. **Brother-in-law** (`Ayal doganynyn adamsy (giyewisi)`, 1 row) — added as `BrotherInLaw` (`aýal doganyň adamsy (giýewisi)`).
4. **Layer-3 match key:** `RelativeAsL` → `NameTm`; **`allow_null`** when FK absent (2,410 employees).

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-21 | Approved; Mother + BrotherInLaw catalog rows; layer 3 complete |
| 2026-06-21 | Initial comparison; 147/159 proposed match; 12-row gap (ejesi + giyewisi) |
