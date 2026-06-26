# Lookup comparison — VisaCategory

**Compared:** 2026-06-26  
**Legacy source:** `VISA2015` — `dbo.VisaCategory` (+ `Visa.VisaCategory` FK)  
**Target source:** [`visa-category.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/visa-category.json)

**Scope:** Çalik transactional **`Visa`** rows (permits-and-visas wave). `PrefferedVisaCategory` / Application fields deferred to Application discovery.

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy lookup table** | 4 rows total; **3** used on live visas |
| **Visa coverage** | **6,040 / 6,041** have VisaCategory FK; **1** null (skip row) |
| **Distinct legacy categories on data** | **3** Turkmen labels + `mgCode` 1 / 2 / 4 |
| **Visa2026 catalog** | **3** global rows (`LocalizationKey`: Single, Double, Multiple) |
| **Layer 3** | Composite key `CategoryOfVisaL:mgCode` → `LocalizationKey` (approved below) |
| **Verdict** | **Approved** for Visa import — 1:1 match on all used buckets |

---

## Legacy values used on Visa (Çalik / VISA2015)

| Legacy CategoryOfVisaL | mgCode | CategoryOfVisa (int) | Visa rows | Visa2026 LocalizationKey | Visa2026 NameTm | Match |
|------------------------|--------|----------------------|-----------|--------------------------|-----------------|-------|
| köp gezeklik | 4 | 0 | 4,301 | **Multiple** | köp gezeklik | ✓ default |
| iki gezeklik | 2 | 2 | 1,431 | **Double** | iki gezeklik | ✓ |
| bir gezeklik | 1 | 1 | 308 | **Single** | bir gezeklik | ✓ |

**Null FK:** 1 visa (`A0794542`, Oid `BF487725-…`) — **skip row** at import (`missingBehavior: skip_row`).

**Unused legacy row:** `CategoryOfVisa=3` (`iş gezeklik`, null `mgCode`) — 0 visa references; soft-deleted catalog orphan.

---

## Visa2026 catalog

All three target rows are used. No target-only gaps.

| LocalizationKey | PdfForm_Code | IsDefault |
|-----------------|--------------|-----------|
| Multiple | 4 | **true** |
| Double | 2 | false |
| Single | 1 | false |

---

## Mapping rule (layer 3)

Match key: `"{CategoryOfVisaL}:{mgCode}"` (normalize Turkmen via `LookupCatalogMatchHelper` when matching labels).

Alternate stable key: **`mgCode` alone** (1 → Single, 2 → Double, 4 → Multiple) — identical on Çalik data; composite key kept for consistency with VisaType / field-map.

| Legacy key | Target LocalizationKey |
|------------|------------------------|
| `köp gezeklik:4` | Multiple |
| `iki gezeklik:2` | Double |
| `bir gezeklik:1` | Single |

OData resolve: `VisaCategory` by `LocalizationKey`.

Default when lookup misses: **Multiple** (`IsDefault: true`).

**Null VisaCategory on visa row:** skip import row (1 row on Çalik).

---

## Application wave note

`VisaCategory` also appears on Application / ApplicationItem and `PrefferedVisaCategory` (72 legacy rows). This audit used **`Visa.VisaCategory`** only. Re-validate at Application discovery.

---

## Import gate

- Blocks **Visa** `importConfirmed` until approved (with VisaIssuedPlace + BorderZone).
- **Approved 2026-06-26** — mappings in `lookup-translations.yaml` (`scope: Visa`).
- Artifacts: `lookup-comparisons/VisaCategory.md`, `lookup-comparisons/VisaCategory.yaml`.
