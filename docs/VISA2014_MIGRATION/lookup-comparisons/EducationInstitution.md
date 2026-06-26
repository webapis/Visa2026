# Lookup comparison — EducationInstitution (Çalik Energi / VISA2015)

**Status:** audit complete (2026-06-26) — **catalog seeded** `education-institution.calik-energi.json` (**1,471** rows, manifest v21)  
**Legacy source:** `--legacy-source calik-energi`  
**Legacy table:** `dbo.EducationInstitution` (`TitleOfIEducationInstitution`)  
**FK usage:** `dbo.Education.EducationInstitution` (3,133 active education rows)  
**Target:** [`tenant/education-institution.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/education-institution.json) (953 rows)  
**OData resolve (planned):** `NameTm` after `LookupCatalogMatchHelper.NormalizeKey` (Turkmen ASCII fold)

**Machine-readable:** [EducationInstitution.yaml](./EducationInstitution.yaml) · full gap list: [`analysis.json`](../../../Visa2026.DataImporter/legacy/visa2014/preview-export/_education-lookup-gap/analysis.json)

---

## Summary

| Metric | Value |
|--------|------:|
| Active `Education` rows | **3,133** |
| Distinct institution labels on education data | **1,471** |
| Active rows in `EducationInstitution` table | **2,022** |
| Visa2026 tenant catalog rows | **953** |
| **Mapped education rows** (normalize match) | **1,037** (33%) |
| **Unmapped education rows** | **2,096** (67%) |
| Unmapped distinct labels | **1,334** |
| Spelling aliases (legacy ≠ catalog `NameTm`, same norm key) | **31** labels |
| Catalog duplicate norm keys | **3** |

### Verdict

| Option | Verdict |
|--------|---------|
| Identity pass-through only (current 953-row seed) | **Insufficient** — loses **2,096** education rows |
| `skip_row` on unmapped | **Reject** for Çalik education wave |
| **Pre-import catalog seed:** all **1,471** distinct labels from active `Education` → `EducationInstitution.NameTm` | **Recommended** (mirror `project-contract.calik-energi.json`) |
| Explicit YAML aliases for Istanbul/İstanbul, case, etc. | **Optional** if seed uses exact legacy strings; else add ~31 alias rows |

After seeding **`education-institution.calik-energi.json`** (union of legacy DISTINCT + keep existing catalog rows not in legacy), expect **~100%** education row coverage with identity `NameTm` match.

---

## Top unmapped labels (by education row count)

| Legacy TitleOfIEducationInstitution | Education rows |
|-------------------------------------|---------------:|
| Manisa tehniki ýörite orta hünärmenlik mekdebi | 89 |
| Bilim Ministrligi Ankara ş. Gayybana ýörite orta hünärmenlik mekdebi | 79 |
| Anadolu Uniwersiteti, Senagat ýörite orta hünärmenlik mekdebi | 46 |
| Yıldız Tehniki Uniwersiteti | 46 |
| Gazi Uniwersiteti Polatly sosiologiýa hünärmentlik ýokary okuw jaýy… | 23 |
| Kojaeli Uniwersiteti | 20 |
| Garadeniz Tehniki Uniwersiteti | 19 |
| Erciyes Uniwersiteti | 18 |
| Fırat Uniwersiteti | 16 |
| Dokuzynjy Sentýabr Uniwersiteti | 13 |

These are valid legacy lookup strings absent from the current tenant seed (Turkish universities / vocational schools).

---

## Spelling aliases (already in catalog under different casing)

| Legacy label | Education rows | Target NameTm |
|--------------|---------------:|---------------|
| Istanbul Tehniki uniwersiteti | 38 | İstanbul Tehniki Uniwersiteti |
| Sakarya Uniwersiteti | 15 | Sakarya Uniwersiteti |
| Selçuk uniwersiteti | 13 | Selçuk Uniwersiteti |
| Mustafa Kemal uniwersiteti | 12 | Mustafa Kemal Uniwersiteti |
| Dokuz Eylül uniwersiteti | 8 | Dokuz Eylül Uniwersiteti |
| Istanbul Uniwersiteti | 8 | İstanbul Uniwersiteti |

If catalog seed uses **exact legacy strings**, aliases are unnecessary. If seed normalizes to catalog canonical `NameTm`, add layer-3 alias entries.

---

## Mapping rule (layer 3)

1. **Primary:** `TitleOfIEducationInstitution` (trimmed) → `EducationInstitution.NameTm` (identity after catalog seed).
2. **Resolve:** OData FK by normalized `NameTm` (`Visa2014CatalogMatchHelper.NormalizeKey`).
3. **unmappedPolicy:** `skip_row` only for rows that fail after seed deploy — not acceptable before seed.

---

## Import gate

- **Audit complete 2026-06-26** — blocked until **tenant catalog seed** from legacy DISTINCT.
- Next: generate `education-institution.calik-energi.json` + manifest entry → Excel preview → `importConfirmed`.
