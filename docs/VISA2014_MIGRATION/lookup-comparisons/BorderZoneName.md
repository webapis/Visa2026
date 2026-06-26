# Lookup comparison — BorderZoneName

**Compared:** 2026-06-21  
**Legacy source:** `VISA2015` — `dbo.BorderZoneForVisa` bit matrix (`Visa.BorderZone` FK)  
**Target source:** [`border-zone-name.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/border-zone-name.json) (tenant catalog)

**Scope:** Çalik transactional **`Visa`** rows (permits-and-visas wave). Application / `BorderZoneForVisa` on Application deferred to Application discovery.

**Legacy label source:** `VISA2014.Module/BusinessObjects/Helper.cs` — `Get_BZ_*` Turkmen strings (Çalik `AppConfig.InTurkmen`).

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy shape** | Not a string lookup table — **8 boolean columns** on `BorderZoneForVisa` (FK from `Visa.BorderZone`) |
| **Visa coverage** | **589 / 6,041** have BorderZone FK; **5,452** null FK → **`Ýok`** |
| **All bits false** | **2** visas with FK but no zone flags → **`Ýok`** |
| **Distinct bit patterns on data** | **18** combinations |
| **Bits used on visas** | 6 of 8 (`Sarahs` never set on Çalik visas) |
| **Visa2026 catalog (before)** | **5** tenant rows — subset of zones |
| **Layer 3** | Bit column name → `BorderZoneName.NameTm`; comma-join in legacy Helper order |
| **Verdict** | **Approved** — expand tenant catalog + 1 alias (`Garabogaz` → `Garabogaz şäheri`) |

---

## Visa BorderZone FK (Çalik / VISA2015)

| Condition | Visa rows | Import `BorderZoneLocation` |
|-----------|----------:|------------------------------|
| `BorderZone` FK null | 5,452 | **`Ýok`** |
| FK set, ≥1 bit true | 587 | Comma-separated mapped `NameTm` labels |
| FK set, all bits false | 2 | **`Ýok`** |
| **Total** | **6,041** | |

`HasBorderZonePermit` when FK null: all **5,452** rows = `0` (consistent with `Ýok`).

---

## Bit usage on visas with BorderZone FK (589 rows)

| Legacy bit column | Visas with bit = 1 | Legacy Helper label (TM) | Visa2026 `NameTm` | Match |
|-------------------|-------------------:|--------------------------|-------------------|-------|
| Farap | 227 | Farap etrap | **Farap etrap** | add catalog |
| Garabogaz | 189 | Garabogaz şäher | **Garabogaz şäheri** | alias → existing |
| Serhetabat | 179 | Serhetabat etrap | **Serhetabat etrap** | add catalog |
| Tagtabazar | 150 | Tagtabazar etrap | **Tagtabazar etrap** | add catalog |
| Daşoguz | 82 | Daşoguz şäher | **Daşoguz şäher** | add catalog |
| Etrek | 4 | Etrek etrap | **Etrek etrap** | add catalog |
| Ýolöten | 1 | Ýolöten etrap | **Ýolöten etrap** | add catalog |
| Sarahs | 0 | Sarags etrap (legacy typo) | **Sarahs etraby** | existing (unused on visas) |

**Top patterns:** Farap-only (217), Garabogaz-only (143), Tagtabazar+Serhetabat (110).

---

## Visa2026 tenant catalog

**Before audit:** Serhetabat etr, Serhetabat şäheri, Garabogaz şäheri, Sarahs etraby, Sarahs etrabyna.

**Added 2026-06-21** (visa bit-matrix labels): Daşoguz şäher, Tagtabazar etrap, Serhetabat etrap, Ýolöten etrap, Farap etrap, Etrek etrap.

**Preserved** for ApplicationItem / officer UI: Serhetabat etr, Serhetabat şäheri, Sarahs etrabyna (not produced by visa bit matrix on Çalik data).

---

## Mapping rule (layer 3)

1. Join `Visa.BorderZone` → `BorderZoneForVisa` (active rows only).
2. For each **true** bit, append the mapped `NameTm` in fixed order:  
   `Daşoguz`, `Tagtabazar`, `Serhetabat`, `Ýolöten`, `Farap`, `Sarahs`, `Garabogaz`, `Etrek`.
3. Join with **comma** (Visa2026 `CommaSeparatedSelectionHelper` — legacy UI used space-concatenated Helper fragments).
4. If FK null or all bits false → **`Ýok`** (`BorderZoneSelectionHelper.NoneValue`).

| Legacy bit | Target NameTm |
|------------|---------------|
| Daşoguz | Daşoguz şäher |
| Tagtabazar | Tagtabazar etrap |
| Serhetabat | Serhetabat etrap |
| Ýolöten | Ýolöten etrap |
| Farap | Farap etrap |
| Sarahs | Sarahs etraby |
| Garabogaz | Garabogaz şäheri |
| Etrek | Etrek etrap |

OData resolve: ensure each label exists in `BorderZoneName` catalog (tenant sync on deploy).

---

## Application wave note

`BorderZoneForVisa` also links to Application / border-zone permit flows. This audit used **`Visa.BorderZone`** only. Re-validate at Application discovery.

---

## Import gate

- Last Visa lookup audit before Excel preview / `importConfirmed`.
- **Approved 2026-06-21** — mappings in `lookup-translations.yaml`; catalog rows in `border-zone-name.json`.
- Artifacts: `lookup-comparisons/BorderZoneName.md`, `lookup-comparisons/BorderZoneName.yaml`.
