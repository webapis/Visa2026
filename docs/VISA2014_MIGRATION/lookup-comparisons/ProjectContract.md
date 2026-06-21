# Lookup comparison — ProjectContract

**Compared:** 2026-06-21  
**Legacy source:** `VISA2015` — `dbo.Contract` (+ `Person.Contract`, `Application.Contract` FK)  
**Target source:** [`tenant/project-contract.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/project-contract.json) (tenant catalog)

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy shape** | `NumberOfContract` **nvarchar(100)** short code + `ContentOfContract` (long text) + `AppliedMinistery` FK (single ministry) |
| **Person coverage** | **2,410 / 2,569** active persons have `Contract` FK — **all employees**; **159 family members** null (expected) |
| **Application coverage** | **2,391** applications with `Contract` FK (audit when Application discovery runs) |
| **Lookup table bloat** | **82** total rows, **19** active — **5** active rows unused on Person/Application |
| **Visa2026 dev seed** | **3** sample rows (`GT-15` ×2, `Şatlyk-1`) — **zero overlap** with legacy prod codes |
| **Layer 3 today** | Approved in `lookup-translations.yaml` — all legacy `NumberOfContract` → `GT-15` |
| **Verdict** | **Approved (experimental pilot)** — all **15** legacy codes → tenant **`GT-15`** (Gap Insaat legacy data on Calik Enerji tenant seed) |

---

## Visa2026 catalog (target — current dev seed only)

| Code | NameTm (abbrev.) | Ministry legs | In legacy prod data? |
|------|------------------|---------------|----------------------|
| GT-15 | GT-15 — (2 ylalaşyk: türkmenenergo > energetika) | 2 | **No** |
| GT-15 | GT-15 — (4 ylalaşyk: …) | 4 | **No** |
| Şatlyk-1 | Şatlyk‑1 — (1 ylalaşyk: türkmengaz) | 1 | **No** |

**Tenant catalog** — match key in manifest: **`NameTmTitle`** (unique `NameTm` per row; multiple rows may share `Code` in greenfield seeds). Legacy prod uses **`NumberOfContract`** as the officer-facing short code → map to Visa2026 **`Code`**.

---

## Legacy values used on Person (primary comparison)

| Legacy `NumberOfContract` | Person rows | Match | Import note |
|---------------------------|------------:|-------|-------------|
| KGF-13811 | 1,087 | **approved** | → **GT-15** (pilot remap) |
| ARM-2462 | 323 | **approved** | → **GT-15** |
| TLP-13678 | 294 | **approved** | → **GT-15** |
| FIZ-2464 | 163 | **approved** | → **GT-15** |
| YAN-640 | 140 | **approved** | → **GT-15** |
| ?MRK-1609-050 | 127 | **approved** | → **GT-15** |
| ESTETIK-938 | 67 | **approved** | → **GT-15** |
| ONK-421 | 66 | **approved** | → **GT-15** |
| ATP-744 | 55 | **approved** | → **GT-15** |
| DH2-422 | 30 | **approved** | → **GT-15** |
| PDR-420 | 27 | **approved** | → **GT-15** |
| HKB-2465 | 21 | **approved** | → **GT-15** |
| END-14339 | 6 | **approved** | → **GT-15** |
| Elektron ?14999 | 2 | **approved** | → **GT-15** |

**Application-only (same remap):** `TAPT-314` → **GT-15** (5 applications).

**Totals:** 2,410 employee Person rows — all map to **GT-15** for pilot; 159 family members null contract (`allow_null`).

---

## Legacy-only (active contract rows not on Person/Application)

| Legacy `NumberOfContract` | Active lookup row | On Person/App? | Import note |
|---------------------------|------------------:|----------------|-------------|
| Infuzion-11754 | Yes | No | Ignore for layer 3 |
| Kambin | Yes | No | Ignore |
| MOR-17757. | Yes | No | Ignore |
| ? WAS-323 | Yes | No | Ignore (encoding) |
| TAPT-314 | Yes | **Application only** (5 apps) | → **GT-15** (same pilot remap) |

---

## Application cross-check (Person wave + future Application wave)

**Union of distinct `NumberOfContract` on Person ∪ Application:** **15** codes (14 on Person + **TAPT-314** application-only).

| Code | Person | Application | Notes |
|------|-------:|------------:|-------|
| TAPT-314 | 0 | 5 | Must seed for Application import |
| ?MRK-1609-050 | 127 | 0 | Person-only in sample |
| *(others)* | yes | yes | Same code on both BOs |

Re-use **one** tenant `ProjectContract` row per `NumberOfContract` for Person and Application FK resolution.

---

## Ministry legs (secondary — blocks ApplicationProgress)

Legacy: **one** `AppliedMinistery` per contract. Visa2026: **ordered `MinistryLegs[]`** referencing `ApprovingMinistry.ShortNameTm`.

| Legacy ministry cluster (AppliedMinistery) | Contract codes (count) |
|---------------------------------------------|-------------------------|
| Health / Derman senagaty (`566A0530-…`) | ARM, FIZ, YAN, ESTETIK, ONK, DH2, PDR, END, MOR (unused) |
| Turkmenhimiya (`476E17C0-…`) | KGF-13811 |
| Türkmendeniz (`FEF33FBD-…`) | TLP-13678 |
| Şalyk Enerji coordinator (`20A01963-…`) | ?MRK-1609-050, ? WAS-323, TAPT-314 |
| President's Office directorate (`53A712B9-…`) | ATP-744 |
| Ashgabat city (`B46AF0E2-…`) | HKB-2465 |
| Industry / Senagat (`244E8E11-…`) | Elektron ?14999 |

**Suggestion:** seed each contract with **1 leg** (sequence 1) after **ApprovingMinistry** audit maps legacy `TitleOfMinisteryL` → `ShortNameTm`. Full multi-leg routes are a **post-migration** refinement unless legacy data proves otherwise.

---

## Target-only (Visa2026 dev seed not in legacy prod)

| Visa2026 Code | Match | Import note |
|---------------|-------|-------------|
| GT-15 | **target_only** | Dev/demo seed — keep or remove per tenant; not referenced by VISA2015 Person data |
| Şatlyk-1 | **target_only** | Same |

---

## Decisions needed (before approval)

*(Superseded — see Approved decisions below.)*

---

## Approved decisions (2026-06-21)

| legacy (`NumberOfContract`) | target (`Code`) |
|-----------------------------|-----------------|
| *(all 15 distinct codes)* | **GT-15** |

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-21 | Approved experimental remap → GT-15 (Gap Insaat / Calik Enerji pilot) |
| 2026-06-21 | Initial comparison — 15 legacy codes, 0 target overlap; E1 tenant seed required |
