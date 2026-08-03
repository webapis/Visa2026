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

---

## Near-duplicate / keeper rule (2026-08-01)

Human-eye duplicates in `city.json` (same Region) break Address Of Residence UI: `Lodging`/`City` cascade filters by exact `City` row, so a short alias (e.g. **Ak bugdaý**) shows empty Lodging while catalog sites point at **Akbugdaý etraby** (`AH48`).

| Rule | Detail |
|------|--------|
| **Keeper** | Prefer row with **`PdfForm_Code`**, else higher AoR/Lodging usage, else longer official `… etraby` / `… şäheri` title |
| **Do not merge** | Distinct **etraby** vs **şäheri** seats that share a stem (e.g. Baharly etraby / Baharly şäheri) — keep both |
| **Do not auto-merge** | Long admin-prefix labels that match **two** keepers (ambiguous) — leave `KeepBoth` until human picks |
| **Review workbook** | `Export-AddressCityHumanReview.ps1 -ViaSsh` → `preview-export/AddressCity-HumanReview.xlsx` |
| **Apply** | `Apply-AddressCityHumanReviewDecisions.ps1 -FillEmptyKeepBoth [-ApplyProdHealViaSsh]` |
| **Applied (Calik)** | Removed aliases **Ak bugdaý**, **Akbudaý etraby** → keeper **Akbugdaý etraby**; `CityByName` aliases added; manifest **v9** |
| **KeepBoth (human)** | Remaining NearDuplicates in workbook (etraby/şäheri seats + ambiguous admin-prefix rows) — **KeepBoth**; catalog stays at **87** cities |
