# Lookup comparison — Region (AddressOfResidence scope)

**Compared:** 2026-06-27  
**Legacy source:** `VISA2015` — `dbo.Region` (+ `Address.Region` FK on `AddressOfResidence` rows)  
**Target source:** [`region.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/region.json)

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy lookup table** | 50 rows total; **6 mgCode buckets** used on live address rows |
| **AddressOfResidence coverage** | **2,752 / 4,083** rows have Region FK; **1,331** infer from `AddressLine` prefix |
| **Visa2026 catalog** | **6** global welaýat rows (`LocalizationKey` = `PdfForm_Code`) |
| **Layer 3** | `Region.mgCode` → `LocalizationKey` (identity) |
| **Verdict** | **Approved** for AddressOfResidence import |

---

## Legacy mgCode usage (Çalik / active AddressOfResidence)

| mgCode | Legacy NameOfRegion (sample) | Rows | Visa2026 NameTm |
|--------|------------------------------|-----:|-----------------|
| BN | 2 Balkan welayaty | 967 | Balkan welaýaty |
| MR | 5 Mary | 779 | Mary welaýaty |
| AH | 1 Ahal | 578 | Ahal welaýaty |
| AS | 0 Asgabat | 340 | Aşgabat şäheri |
| LB | 4 Lebap | 40 | Lebap welaýaty |
| DZ | 3 Dasoguz | 36 | Daşoguz welaýaty |

---

## Mapping rule

Primary: **`Region.mgCode`** = Visa2026 **`LocalizationKey`** / **`PdfForm_Code`**.

Fallback when FK null: infer mgCode from **`AddressLine`** prefix (`Balkan wel.`, `Mary wel.`, `s. Asgabat`, …).

OData resolve: `Region` by `LocalizationKey`.

---

## Import gate

- Approved **2026-06-27** for AddressOfResidence discovery — recorded in `lookup-translations.yaml`.
