# Lookup comparison — MaritalStatus

**Compared:** 2026-06-21  
**Legacy source:** `VISA2015` — `dbo.MaritalStatus` (+ `Person.MaritalStatus` FK)  
**Target source:** [`marital-status.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/marital-status.json)

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy shape** | Hybrid: `Status` **int** (0–5) + `StatusL` **nvarchar(500)** family narrative (unique per lookup row) |
| **Person coverage** | **2,569 / 2,569** active persons have a MaritalStatus FK |
| **Lookup table bloat** | **1,965** active `MaritalStatus` rows but only **6** distinct `Status` int values; **~1,582** distinct `StatusL` prefixes for `Status=0` alone |
| **Visa2026 catalog** | **7** fixed global rows (Turkmen `Code` / `NameTm`) |
| **Layer 3 today** | `values[]` approved — Status int → Visa2026 `Code` (2026-06-21) |
| **Verdict** | **Approved** — coarse **`Status` int** maps to Visa2026; **`StatusL`** → family text, not catalog |

---

## Visa2026 catalog (target — all rows)

| Code / NameTm | LocalizationKey | PdfForm_Code | In legacy Person data? |
|---------------|-----------------|--------------|----------------------|
| Sallah | Single | 1 | Yes — via `Status=1` |
| Çaga | Minor | 0 | Yes — via `Status=5` |
| Aýrylşan | Divorced | 3 | Yes — via `Status=4` |
| Öýlenen | Married | 2 | Partial — via `Status=2`, `3`; **`Status=0` narrative rows likely married** |
| Durmuşa Çykmadyk | WidowedNotRemarried | 1 | **No** distinct legacy `Status` bucket on Person |
| Durmuşa Çykan | WidowedRemarried | 2 | **No** distinct legacy `Status` bucket on Person |
| Dul | Widow | 4 | **No** distinct legacy `Status` bucket on Person |

---

## Legacy values used on Person (primary comparison)

These are the **six** legacy categories that matter for import (not all 1,965 lookup rows).

| Legacy (VISA2015) | Visa2026 (target) | Person rows | Match | Import note |
|-------------------|-------------------|------------:|-------|-------------|
| **`Status=0`**, `mgCode=2` (typical) — `StatusL` = long **family block** (spouse + children names/DOB/country), e.g. *Ayaly: … Çagalary: …* | **Öýlenen** (Married) | **1,543** | **approved** | Map FK to **Married**; copy `StatusL` → `Person.VisaApplicationFamilyMembersText`. **Do not** import 1,600+ narrative lookup rows. |
| **`Status=0`**, other `mgCode` (1, 3) | **Öýlenen** (Married) | **18** | **approved** | Same as above. |
| **`Status=1`**, `mgCode=1` — `StatusL` often `"."` or *Sallah* | **Sallah** (Single) | **707** | **approved** | Strong match to Visa2026 **Single**. |
| **`Status=2`**, `mgCode=2` — `StatusL` *Adamsy …* (husband line) | **Öýlenen** (Married) | **117** | **approved** | Married semantics. |
| **`Status=3`**, `mgCode=1` — `StatusL` married spouse/child lines | **Öýlenen** (Married) | **41** | **approved** | Married semantics. |
| **`Status=4`**, `mgCode=3` — `StatusL` *Ayrylysan* / divorced narrative | **Aýrylşan** (Divorced) | **66** | **approved** | Strong match to **Divorced**. |
| **`Status=5`**, empty `mgCode` — `StatusL` *Çaga* / child line | **Çaga** (Minor) | **76** | **approved** | Strong match to **Minor**. |
| **`Status=5`**, `mgCode=1` | **Çaga** (Minor) | **1** | **approved** | Same. |

**Totals:** 2,569 Person rows — 5 `duplicate_merged` persons excluded from other export tallies still have MS FK.

---

## Target-only (Visa2026 catalog not seen as legacy `Status` int on Person)

| Visa2026 (target) | LocalizationKey | Match | Import note |
|-------------------|-----------------|-------|-------------|
| Durmuşa Çykmadyk | WidowedNotRemarried | **target_only** | No Person rows use a dedicated legacy bucket; may be unused in VISA2015 prod or buried in `Status=0` text. Confirm with domain expert. |
| Durmuşa Çykan | WidowedRemarried | **target_only** | Same. |
| Dul | Widow | **target_only** | Same. |

---

## Legacy-only (lookup table noise — do not import)

| Legacy | Rows in `MaritalStatus` | On Person? | Import note |
|--------|------------------------:|------------|-------------|
| Unique `StatusL` narrative rows (`Status=0`) | ~1,603 lookup rows | Yes (via FK) | **Not** Visa2026 catalog entries — free-text family composition. Collapse to coarse marital status + optional `VisaApplicationFamilyMembersText`. |
| Duplicate Oids per same `Status` int | Many | Yes | Resolve by **`Status` + `mgCode`**, not legacy Oid. |

---

## Approved decisions (2026-06-21)

1. **Approved coarse map** `Status` int → Visa2026 `Code`:

   | Legacy `Status` | Approved Visa2026 `Code` |
   |-----------------|--------------------------|
   | 0 | Öýlenen |
   | 1 | Sallah |
   | 2 | Öýlenen |
   | 3 | Öýlenen |
   | 4 | Aýrylşan |
   | 5 | Çaga |

2. **`StatusL` narrative:** copied to `VisaApplicationFamilyMembersText` in Excel preview; optional at OData import.

3. **Widow/widower variants** in Visa2026 with **zero** legacy `Status` hits: leave unused in target; no import action.

4. Layer 3 in `lookup-translations.yaml` keyed on **`Status` int as string** (`"0"`–`"5"`) — not `StatusL`.

---

## Recommended decisions (historical — superseded by approval above)

---

## SQL (repeat audit)

```sql
SELECT ms.Status, ms.mgCode, COUNT(*) AS person_count,
       MIN(LEFT(ms.StatusL, 60)) AS sample_statusL
FROM dbo.Person p
INNER JOIN dbo.MaritalStatus ms ON p.MaritalStatus = ms.Oid
WHERE p.GCRecord IS NULL
GROUP BY ms.Status, ms.mgCode
ORDER BY person_count DESC;
```

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-21 | **Approved** Status int → Code map; Status=0 → Öýlenen; layer 3 in lookup-translations.yaml |
| 2026-06-21 | Initial comparison — corrected legacy model (Status int + StatusL narrative) |
