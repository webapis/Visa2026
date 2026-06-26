# Lookup comparison — VisaIssuedPlace

**Compared:** 2026-06-21  
**Legacy source:** `VISA2015` — `dbo.VisaIssuedPlace` (`IssuedPlaceOfVisaL`) + `Visa.VisaIssuedPlace` FK  
**Target source:** [`visa-issued-place.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/visa-issued-place.json)

**Scope:** Çalik transactional **`Visa`** rows (permits-and-visas wave). `Application.VisaIssuedPlace` deferred to Application discovery.

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy lookup table** | 37 rows total; **22** distinct labels used on live visas |
| **Visa coverage** | **6,041 / 6,041** have VisaIssuedPlace FK; **0** null |
| **Visa2026 catalog** | **16** global rows (`NameTm`; default **Aşgabat şäher howa menzilindäki MGP**) |
| **Layer 3** | `IssuedPlaceOfVisaL` → `NameTm` (+ explicit aliases; `LookupCatalogMatchHelper` ASCII-fold) |
| **Mapped visa rows** | **6,023** (14 legacy labels with approved targets) |
| **Unmapped visa rows** | **18** (8 rare embassy / border labels — **skip row**) |
| **Verdict** | **Approved** for Visa import with `skip_row` on unmapped labels |

---

## Legacy values used on Visa (Çalik / VISA2015)

| Legacy IssuedPlaceOfVisaL | Visa rows | Visa2026 NameTm | Match |
|---------------------------|-----------|-----------------|-------|
| Aşgabat şäheri | 2,853 | Aşgabat şäheri | ✓ identity |
| Aşgabat şäher howa menzilindäki MGP | 2,799 | Aşgabat şäher howa menzilindäki MGP | ✓ identity |
| Daşoguz G.Ý | 121 | Daşoguz G.Ý | ✓ identity |
| Türkmenbaşy H.M. | 65 | Türkmenbaşy howa menzilindäki MGP | ✓ abbrev H.M. |
| Ankara | 62 | Ankara | ✓ identity |
| Stambul | 58 | Stambul | ✓ identity |
| T-abat H.M. | 18 | Türkmenabat Howa Menzili | ✓ abbrev |
| Farap G.Y. | 17 | Farap MGP | ✓ same locality |
| Howdan MGP | 17 | Howdan MGP | ✓ identity |
| BERLİN | 4 | Berlin | ✓ case / İ |
| Garabogaz | 4 | Garabogaz GY | ✓ short label |
| Farap MGP | 3 | Farap MGP | ✓ identity |
| Waşington | 1 | Waşington | ✓ identity |
| Kiýew | 1 | Kiýew | ✓ identity |
| DELİ | 5 | — | ✗ skip row |
| Pekin | 3 | — | ✗ skip row |
| Serhetabat | 2 | — | ✗ skip row |
| London | 2 | — | ✗ skip row |
| Kazan | 2 | — | ✗ skip row |
| Taşkent | 2 | — | ✗ skip row |
| Mary H.M. | 1 | — | ✗ skip row |
| BAKU | 1 | — | ✗ skip row |

**Total:** 6,041 visa rows = 6,023 mapped + 18 skip.

---

## Visa2026 catalog — unused on Visa data

These seeded rows have **0** Çalik visa references (acceptable; may appear on Application wave):

| NameTm | IsDefault |
|--------|-----------|
| Wena | false |
| Astana | false |
| Moskwa | false |

---

## Mapping rule (layer 3)

Resolve `VisaIssuedPlace` OData FK by matching **`NameTm`** after `LookupCatalogMatchHelper.NormalizeKey` (Turkmen ASCII fold + lowercase).

Identity labels need no YAML entry unless legacy spelling differs. **Explicit aliases** required for:

| Legacy IssuedPlaceOfVisaL | Target NameTm |
|---------------------------|---------------|
| Türkmenbaşy H.M. | Türkmenbaşy howa menzilindäki MGP |
| T-abat H.M. | Türkmenabat Howa Menzili |
| Farap G.Y. | Farap MGP |
| BERLİN | Berlin |
| Garabogaz | Garabogaz GY |

**Unmapped labels (skip row at import):** DELİ, Pekin, Serhetabat, London, Kazan, Taşkent, Mary H.M., BAKU — **18 visas** (~0.3%). Do **not** default to catalog `IsDefault` (would mis-state embassy issuance).

**Null VisaIssuedPlace on visa row:** none on Çalik; if encountered elsewhere, `skip_row`.

---

## Application wave note

`VisaIssuedPlace` also appears on Application / ApplicationItem. This audit used **`Visa.VisaIssuedPlace`** only. Re-validate at Application discovery (additional legacy labels may appear).

---

## Import gate

- Blocks **Visa** `importConfirmed` until approved (with BorderZoneName).
- **Approved 2026-06-21** — mappings in `lookup-translations.yaml` (`scope: Visa`).
- Artifacts: `lookup-comparisons/VisaIssuedPlace.md`, `lookup-comparisons/VisaIssuedPlace.yaml`.
