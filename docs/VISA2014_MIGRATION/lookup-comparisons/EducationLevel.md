# Lookup comparison — EducationLevel

**Compared:** 2026-06-26  
**Legacy source:** `VISA2015` — `dbo.EducationLevel` (+ `Education.EducationLevel` FK)  
**Target source:** [`education-level.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/education-level.json)

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy lookup table** | 24 rows total; **3 active buckets** on live education rows |
| **Education coverage** | **3,133 / 3,133** active education rows have EducationLevel FK (0 null) |
| **Visa2026 catalog** | **5** global rows (`LocalizationKey` / `PdfForm_Code`) |
| **Layer 3** | `mgCode` → `LocalizationKey` via `PdfForm_Code` (approved below) |
| **Verdict** | **Approved** for Education import — only 3 legacy buckets on Çalik data |

---

## Legacy values used on Education (Çalik / VISA2015)

| Legacy TitleOfEducationLevel | mgCode | Education rows | Visa2026 LocalizationKey | Visa2026 NameTm |
|------------------------------|--------|----------------|--------------------------|-----------------|
| Yokary | 2 | 1,496 | **Higher** | Ýokary |
| Ýörite Orta | 1 | 937 | **SpecialSecondary** | Ýörite Orta |
| Orta | 5 | 700 | **Secondary** | Orta |

**Total:** 3,133 active education rows.

---

## Mapping rule (layer 3)

Primary match: **`EducationLevel.mgCode`** = Visa2026 **`PdfForm_Code`** (= `LocalizationKey` numeric code in seed).

| Legacy mgCode | Target LocalizationKey |
|---------------|------------------------|
| 1 | SpecialSecondary |
| 2 | Higher |
| 5 | Secondary |

Default when lookup misses: **SpecialSecondary** (`IsDefault: true` in catalog).

OData resolve: `EducationLevel` by `LocalizationKey`.

---

## Import gate

- Blocks Education `importConfirmed` until approved in `lookup-review-queue.yaml`.
- **Approved 2026-06-26** — mappings recorded in `lookup-translations.yaml`.
