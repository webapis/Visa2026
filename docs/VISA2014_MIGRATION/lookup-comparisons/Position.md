# Lookup comparison — Position (Çalik Energi / VISA2015)

**Status:** audit started (2026-06-26) — **catalog seed pending** `position.calik-energi.json`  
**Legacy source:** `--legacy-source calik-energi`  
**Legacy table:** `dbo.Position` (`TitleOfPosition`)  
**FK usage:** `dbo.WorkHistoryOfEmployee.Position` (2,993 active rows)  
**Target:** [`tenant/position.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/position.json) (**259** rows)  
**OData resolve (planned):** `NameTm` after `LookupCatalogMatchHelper.NormalizeKey`

---

## Summary

| Metric | Value |
|--------|------:|
| Active `WorkHistoryOfEmployee` rows | **2,993** |
| Distinct position labels on work history | **1,579** |
| Visa2026 tenant catalog rows | **259** |

### Verdict

| Option | Verdict |
|--------|---------|
| Current 259-row tenant seed only | **Insufficient** for full Çalik position history import |
| **Pre-import catalog seed:** DISTINCT `TitleOfPosition` from active `WorkHistoryOfEmployee` → `Position.NameTm` | **Recommended** (mirror `education-institution.calik-energi.json`) |

### Related

- **ActualPosition** (Visa2026-only): same legacy title → `ActualPosition.Name` via find-or-create on OData (not tenant JSON).
