# Lookup comparison — VisaType

**Compared:** 2026-06-26  
**Legacy source:** `VISA2015` — `dbo.VisaType` → `dbo.IVisaType_Data` (+ `Visa.VisaType` FK)  
**Target source:** [`visa-type.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/visa-type.json)

**Scope:** Çalik transactional **`Visa`** rows (permits-and-visas wave). Application/ApplicationItem visa-type fields deferred to Application discovery.

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy lookup table** | 6 `VisaType` rows; **5** `IVisaType_Data` buckets used on live visas |
| **Visa coverage** | **6,041 / 6,041** active visas have VisaType FK (0 null) |
| **Distinct legacy categories on data** | **5** (`TypeOfVisaL`: WP, BS, FM, GL, EX) + optional `mgCode` |
| **Visa2026 catalog** | **19** global rows (`LocalizationKey`; `PdfForm_Code` int in seed JSON) |
| **Layer 3** | Composite key `TypeOfVisaL:mgCode` → `LocalizationKey` (approved below) |
| **Verdict** | **Approved** for Visa import — 5 legacy buckets; **GL→OF** (Gulluk/official visa, not passport PG) |

---

## Legacy values used on Visa (Çalik / VISA2015)

| Legacy TypeOfVisaL | mgCode | Visa rows | Visa2026 LocalizationKey | Visa2026 NameTm (abbrev) | Match |
|--------------------|--------|-----------|--------------------------|---------------------------|-------|
| WP | 11 | 3,993 | **WP** | WP-Işçi Wiza | ✓ default catalog row |
| BS | 14 | 1,652 | **BS1** | BS1-İşerwürlik | ✓ mgCode 14 = BS1 `PdfForm_Code` |
| FM | *(null)* | 317 | **FM** | FM-Maşgala | ✓ family visa |
| GL | *(null)* | 58 | **OF** | OF-Gulluk | ✓ legacy **GL** = Gulluk/**official** visa (≠ passport GL→PG) |
| EX | 10 | 21 | **EX** | EX-Çykyş | ✓ exit visa |

**Total:** 6,041 rows.

---

## Visa2026 catalog rows **not** used on Çalik Visa data

TR1, TR2, HM, DP, PR1, PR2, HL, DR, IN, ST, SP1, SP2, BS2, TU — present in seed for other visa categories / PDF forms; no Çalik `Visa` FK hits in VISA2015.

**Note:** `TR2` shares `PdfForm_Code` **14** with `BS1`; legacy data never uses `TypeOfVisaL=TR` — resolve by **LocalizationKey** from composite key, not `PdfForm_Code` alone.

---

## Mapping rule (layer 3)

Match key: `"{TypeOfVisaL}:{mgCodeOrEmpty}"` where null/empty `mgCode` is an empty suffix after the colon.

| Legacy key | Target LocalizationKey | Notes |
|------------|------------------------|-------|
| `WP:11` | WP | Worker visa — 66% of rows |
| `BS:14` | BS1 | Business / entrepreneurship (`mgCode` 14) |
| `FM:` | FM | Family — null `mgCode` |
| `GL:` | OF | Official / Gulluk service visa — 58 rows; spot-check in Excel preview |
| `EX:10` | EX | Exit visa |

OData resolve: `VisaType` by `LocalizationKey` (prefer over `PdfForm_Code` when ambiguous).

Default when lookup misses: **WP** (`IsDefault: true` in catalog).

---

## Distinction from PassportType

| Legacy code | On **Passport** (approved 2026-06-26) | On **Visa** (this audit) |
|-------------|----------------------------------------|---------------------------|
| GL | → **PG** (Gulluk passport) | → **OF** (Gulluk/**official** visa) |
| AD | → **P** (national passport) | — not used on Visa |

Do not reuse PassportType `lookup-translations` entries for Visa.

---

## Application wave note

`VisaType` also appears on Application / ApplicationItem in the application wave. This audit used **`Visa.VisaType`** only. Re-validate Application discovery; expect the same five buckets on Çalik data.

---

## Import gate

- Blocks **Visa** `importConfirmed` until approved (with VisaCategory + VisaIssuedPlace).
- **Approved 2026-06-26** — mappings recorded in `lookup-translations.yaml` (`scope: Visa`).
- Comparison artifacts: `lookup-comparisons/VisaType.md`, `lookup-comparisons/VisaType.yaml`.
