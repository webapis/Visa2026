# Lookup comparison — Specialty (Çalik Energi / VISA2015)

**Status:** audit complete (2026-06-26) — **catalog seeded** `specialty.calik-energi.json` (**1,063** rows, manifest v21)  
**Legacy source:** `--legacy-source calik-energi`  
**Legacy table:** `dbo.Speciality` (`TitleOfSpeciality`) — FK column `Education.Spcialty` (legacy typo)  
**FK usage:** `dbo.Education.Spcialty` (3,133 active education rows)  
**Target:** [`tenant/specialty.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/specialty.json)  
**OData resolve (planned):** `NameTm` after `LookupCatalogMatchHelper.NormalizeKey`

**Machine-readable:** [Specialty.yaml](./Specialty.yaml) · full gap list: [`analysis.json`](../../../Visa2026.DataImporter/legacy/visa2014/preview-export/_education-lookup-gap/analysis.json)

---

## Summary

| Metric | Value |
|--------|------:|
| Active `Education` rows | **3,133** |
| Distinct specialty labels on education data | **1,063** |
| Active rows in `Speciality` table | **1,683** |
| Visa2026 tenant catalog rows | **~680** |
| **Mapped education rows** (normalize match) | **956** (31%) |
| **Unmapped education rows** | **2,177** (69%) |
| Unmapped distinct labels | **903** |
| Spelling aliases | **21** labels |
| Catalog duplicate norm keys | **0** |

### Verdict

| Option | Verdict |
|--------|---------|
| Current tenant seed only | **Insufficient** — loses **2,177** education rows |
| `skip_row` on unmapped | **Reject** |
| **Pre-import catalog seed:** all **1,063** distinct `TitleOfSpeciality` on active `Education` → `Specialty.NameTm` | **Recommended** |
| YAML aliases for engineer title casing (`Elektrik inženeri` → `Elektrik Inženeri`) | **Optional** if seed uses legacy strings verbatim |

Top unmapped specialty **Tehniki howpsuzlyk we zähmeti goramak** (401 rows) is absent from the current tenant catalog entirely — confirms seed-from-legacy is required, not alias tuning alone.

---

## Top unmapped labels (by education row count)

| Legacy TitleOfSpeciality | Education rows |
|--------------------------|---------------:|
| Tehniki howpsuzlyk we zähmeti goramak | 401 |
| Elektrik - Elektronika inzeneri | 86 |
| Elektrika | 73 |
| Mehanika inzeneri | 65 |
| Gurlusyk inzenerçiligi | 39 |
| Mehanik-inzener | 21 |
| Elektrik-Elektronik inzeneri | 20 |
| Elektrika-Elektronika inzenerçiligi | 18 |
| Elektrik-elektronika inzeneri | 17 |
| Metal tehnologiyalary | 14 |

Many unmapped labels are **near-duplicates** of seeded titles (spacing, hyphenation, capitalization) — identity seed from legacy DISTINCT avoids maintaining hundreds of manual aliases.

---

## Spelling aliases (sample)

| Legacy label | Education rows | Target NameTm |
|--------------|---------------:|---------------|
| Elektrik inženeri | 89 | Elektrik Inženeri |
| Gurlusyk inženeri | 108 | *(unmapped — not in catalog)* |
| Orta bilim | 114 | *(unmapped)* |

---

## Mapping rule (layer 3)

1. **Primary:** `TitleOfSpeciality` (trimmed) → `Specialty.NameTm` after catalog seed.
2. **Resolve:** normalized `NameTm` match on OData.
3. Legacy table name **`Speciality`**; Visa2026 entity **`Specialty`**.

---

## Import gate

- **Audit complete 2026-06-26** — blocked until **tenant catalog seed**.
- Next: generate `specialty.calik-energi.json` + manifest entry → Excel preview → `importConfirmed`.
