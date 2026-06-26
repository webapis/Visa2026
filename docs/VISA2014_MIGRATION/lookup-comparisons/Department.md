# Lookup comparison — Department (Çalik Energi / VISA2015)

**Status:** audit started (2026-06-26) — **catalog seed pending** `department.calik-energi.json`  
**Legacy source:** `--legacy-source calik-energi`  
**Legacy table:** `dbo.Department` (`TitleOfDepartment`)  
**FK usage:** `dbo.WorkHistoryOfEmployee.Department` (2,993 active rows; **0** null FK)  
**Target:** [`tenant/department.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/department.json) (**3** demo rows)  
**OData resolve (planned):** `NameTm` after `LookupCatalogMatchHelper.NormalizeKey`

---

## Summary

| Metric | Value |
|--------|------:|
| Active `WorkHistoryOfEmployee` rows | **2,993** |
| Distinct department labels on work history | **74** |
| Visa2026 tenant catalog rows | **3** |

### Verdict

| Option | Verdict |
|--------|---------|
| Current 3-row demo seed | **Insufficient** for Çalik production |
| **Pre-import catalog seed:** DISTINCT `TitleOfDepartment` from active `WorkHistoryOfEmployee` → `Department.NameTm` | **Recommended** |
