# Lookup comparison — PassportType

**Compared:** 2026-06-26  
**Legacy source:** `VISA2015` — `dbo.PassportType` (+ `Passport.PassportType` FK)  
**Target source:** [`passport-type.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/passport-type.json)

---

## Summary

| Finding | Detail |
|---------|--------|
| **Legacy lookup table** | 24 rows total; **3 active** rows used on live passports |
| **Passport coverage** | **3,684 / 3,684** active passports have PassportType FK (0 null) |
| **Distinct legacy categories on data** | **3** (`TypeOfPassportL`: AD, GL, DP) + optional `mgCode` (P, PG, PD) |
| **Visa2026 catalog** | **17** global rows (`LocalizationKey` / `PdfForm_Code`) |
| **Layer 3** | Composite key `TypeOfPassportL:mgCode` → `LocalizationKey` (approved below) |
| **Verdict** | **Approved** for Passport import — only 3 legacy buckets on Çalik data |

---

## Legacy values used on Passport (Çalik / VISA2015)

| Legacy TypeOfPassportL | mgCode | Passport rows | Active type row? | Visa2026 LocalizationKey | Visa2026 NameTm (abbrev) |
|------------------------|--------|---------------|------------------|--------------------------|---------------------------|
| AD | P | 3,383 | Yes | **P** | P - MILLI PASPORT |
| AD | *(null)* | 228 | No (soft-deleted type) | **P** | Same — national passport |
| GL | PG | 70 | Yes | **PG** | PG-GULLUK PASPORTY |
| GL | *(null)* | 2 | No | **PG** | Service passport — nearest match |
| DP | *(null)* | 1 | No | **PD** | PD-DIPLOMAT PASPORTY |

**Total:** 3,684 rows. No live passport references the active `DP` + `PD` type row; one diplomatic row points at a deleted type.

---

## Visa2026 catalog rows **not** used on Çalik Passport data

AML, APD, SH, DZ, AGL, YD, EU, BS, US, PT, LBG, YG, AUN, UN — present in seed catalog for other document types / PDF forms; no Çalik `Passport` FK hits in VISA2015.

---

## Mapping rule (layer 3)

Match key: `"{TypeOfPassportL}:{mgCodeOrEmpty}"` where null/empty `mgCode` is an empty suffix after the colon.

| Legacy key | Target LocalizationKey |
|------------|------------------------|
| `AD:P` | P |
| `AD:` | P |
| `GL:PG` | PG |
| `GL:` | PG |
| `DP:PD` | PD |
| `DP:` | PD |

OData resolve: `PassportType` by `LocalizationKey` (or `PdfForm_Code` — same values in seed).

Default when lookup misses: **P** (`IsDefault: true` in catalog).

---

## ApplicationItem note

`PassportType` also appears on ApplicationItem in the application wave. This audit used **Passport** FK only. Re-validate when Application discovery starts; expect overlap with the same three buckets.

---

## Import gate

- Added to **personWave** in `lookup-review-queue.yaml` (blocks Passport `importConfirmed` until approved).
- **Approved 2026-06-26** — mappings recorded in `lookup-translations.yaml`.
