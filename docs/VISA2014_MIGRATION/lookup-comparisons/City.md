# Lookup comparison — City (AddressOfResidence scope)

**Compared:** 2026-06-27  
**Legacy source:** `VISA2015` — `dbo.ŞeherEtrap` (+ `Address.ŞeherEtrap` FK)  
**Target source:** [`city.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/city.json) (`matchKey`: NameAndRegion)

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy city FK** | **2,753 / 4,083** rows have ŞeherEtrap FK |
| **Distinct city labels on data** | ~22 composite (region + city name + mgCode) |
| **Primary match** | **`ŞeherEtrap.mgCode`** = Visa2026 **`PdfForm_Code`** when present |
| **Secondary match** | Normalized **`ŞeherEtrapL`** + parent Region → `NameTm` + Region |
| **Verdict** | **Approved with normalization** for AddressOfResidence import |

---

## Top legacy cities (by AddressOfResidence row count)

| Region mgCode | Legacy ŞeherEtrapL | city mgCode | Rows | Visa2026 NameTm |
|---------------|-------------------|-------------|-----:|-----------------|
| BN | Türkmenbaşy etraby | BN15 | 873 | Türkmenbaşy etraby |
| AH | Akbugday etraby | AH48 | 576 | Akbugdaý etraby |
| MR | Mary etraby | MR36 | 548 | Mary etraby |
| AS | Asgabat şäheri | AS69 | 324 | Aşgabat şäheri |
| MR | Mary şäheri | MR19 | 214 | Mary şäheri |
| BN | Serdar etraby | (null) | 63 | Serdar etraby |
| LB | Türkmenabat şäheri | LB18 | 40 | Türkmenabat şäheri |
| DZ | Dasoguz şäheri | DZ56 | 36 | Daşoguz şäheri |

Spelling normalization: legacy ASCII/Turkish variants (`Akbugday`, `Asgabat`, `Dasoguz`) → Visa2026 Turkmen diacritics.

---

## Mapping rule

1. When **`ŞeherEtrap.mgCode`** present → resolve City by **`PdfForm_Code`**.
2. Else **`CityByName`** translation table (legacy label → target `NameTm`).
3. Else infer from **`AddressLine`** for Aşgabat hotel/patent patterns.
4. Parent **Region** required on target row (`NameAndRegion` match at OData time).

---

## Import gate

- Approved **2026-06-27** for AddressOfResidence discovery — `City` + `CityByName` in `lookup-translations.yaml`.
