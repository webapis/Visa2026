# Lookup comparison — ProjectContract (Çalik Energi / VISA2015)

**Status:** audit complete (2026-06-26)  
**Legacy source:** `--legacy-source calik-energi`  
**Legacy database:** **`VISA2015`** on `localhost\SQLEXPRESS` (VISA2014 is the app/repo name only)  
**Legacy table:** `dbo.Contract` (`NumberOfContract`, `ContentOfContract`, `AppliedMinistery` → `dbo.AppliedMinistery`)  
**FK usage:** `dbo.Person.Contract`, `dbo.Application.Contract`  
**Target:** [`tenant/project-contract.calik-energi.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/project-contract.calik-energi.json) (Çalik, 73 rows) · greenfield demo [`tenant/project-contract.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/project-contract.json) (3 rows)  
**OData resolve:** `Visa2014ODataLookupResolver.ResolveProjectContract` matches **`Code` only**  
**Tenant manifest match key:** **`NameTmTitle`** (unique `NameTm` per row; greenfield seed allows duplicate `Code`)

**Gap İnşaat:** separate path — 15 codes → `GT-15` in [`lookup-translations.gap-insaat.yaml`](../lookup-translations.gap-insaat.yaml) and [`ProjectContract.md`](ProjectContract.md). **Do not apply to Çalik.**

---

## Summary

| Metric | Value |
|--------|------:|
| Active `Contract` rows (`GCRecord IS NULL`) | **95** |
| Distinct `NumberOfContract` in `Contract` table | **83** |
| Max `LEN(NumberOfContract)` | **56** |
| Distinct codes on **Person** FK | **60** |
| Person rows with contract FK | **2,950** |
| Distinct codes on **Application** FK | **34** |
| Application rows with contract FK | **3,845** |
| **Union** distinct codes (Person ∪ Application) | **73** |
| Codes in `Contract` but unused on Person/Application | **10** |
| `Contract` rows with `AppliedMinistery` set | **42** / 95 |
| Tenant seed rows (`project-contract.json`) | **3** (`GT-15` ×2, `Şatlyk-1` ×1) |
| Seed `Code` overlap with Çalik union codes | **0** exact (`Satlyk-1` ≈ `Şatlyk-1` spelling only) |
| `GT-15` in Çalik legacy data | **none** |

### Strategy recommendation

| Option | Verdict |
|--------|---------|
| Gap-style blanket remap → `GT-15` | **Reject** for Çalik (obsolete; wrong tenant semantics) |
| **`identityPassThrough`:** legacy `NumberOfContract` → Visa2026 `ProjectContract.Code`** | **Recommended** (already in [`lookup-translations.calik-energi.yaml`](../lookup-translations.calik-energi.yaml)) |
| **Pre-import tenant catalog seed** from legacy **73** union codes (plus ministry legs where known) | **Required before OData import** — current 3-row demo seed cannot resolve Çalik FKs |
| Map to `NameTm` instead of `Code` | **Not recommended** — resolver and YAML target **`Code`**; use `ContentOfContract` / generated `NameTm` when seeding catalog rows |
| `unmappedPolicy: allow_null` alone | **Insufficient** — would drop contract on nearly all employees/applications |

**Ministry:** Legacy stores a single `AppliedMinistery` on ~44% of contract rows (`dbo.AppliedMinistery.TitleOfMinistery`). Visa2026 uses **`MinistryLegs`** on `ProjectContract`. Seed generation should map ministry where FK exists; do not block identity pass-through on Code.

---

## Overlap vs tenant seed

| Tenant seed `Code` | In Çalik union (73)? | Notes |
|--------------------|----------------------|-------|
| `GT-15` | **No** | Demo/greenfield only; Gap remap is a different company |
| `Şatlyk-1` | **No** (legacy uses `Satlyk-1`) | 3 Person refs; also `CS-1 Satlik` (70), `CS-1 Shatlik` (7), `CSK-1 Shatlik` (14) |

---

## Şatlyk / Shatlik / Satlik variants (Çalik)

| Legacy `NumberOfContract` | Person | Application | Total refs |
|---------------------------|-------:|------------:|-----------:|
| `CS-1 Satlik` | 70 | 0 | 70 |
| `CSK-1 Shatlik` | 9 | 5 | 14 |
| `CS-1 Shatlik` | 7 | 0 | 7 |
| `Satlyk-1` | 3 | 0 | 3 |

**Orphan contract row (not on Person/Application):** `CSH-1 Shatlik`.

**Duplicate Oids per trimmed code:** `?12/24` (4), `CS-1 Shatlik` (3), `CSK-1 Shatlik` (2), and several pairs — id-map on **trimmed `NumberOfContract`**, not legacy `Oid`.

---

## Ministry table (discovery)

| Item | Value |
|------|-------|
| Table | **`dbo.AppliedMinistery`** (legacy spelling) |
| Sample columns | `TitleOfMinistery`, `TitleOfMinisteryL`, `MinistersPosition`, `MinistersFullName` |
| Example | `12552 AST1` → Aşgabat hükümeti; `12985 LM6000` → Energetika ministrligi |

---

## Sample `ContentOfContract`

| Code | Content (abbrev.) |
|------|-------------------|
| `14306 Mary` | Energetika / Prezident 14306 — Mary, Türkmenenergo… |
| `12552 AST1` | Aşgabat hükümeti — Prezident 12552 karary… |
| `1574 -KIYANLI` | Balkan, 1574 MW CCPP and grid connection… |

---

## Full distinct codes: Person ∪ Application (73)

Sorted by total FK refs.

| Code | Person | Application | Total |
|------|-------:|------------:|------:|
| 14306 Mary | 904 | 1276 | 2180 |
| 1574 -KIYANLI | 7 | 974 | 981 |
| KYC (Kiyanli Kobmine Elektrik Santrali projesi) | 453 | 0 | 453 |
| 12552 AST1 | 216 | 159 | 375 |
| 14080 Watan | 86 | 244 | 330 |
| 13111 Derweze | 102 | 194 | 296 |
| 13542 AST2 | 54 | 189 | 243 |
| 12985 LM6000 | 109 | 81 | 190 |
| KYM (Kiyanli yedek parça-servis ve bakim projesi) | 184 | 0 | 184 |
| 13110 Akbugday | 70 | 90 | 160 |
| 1235-SERVIS MERKEZI | 11 | 126 | 137 |
| TAP | 29 | 104 | 133 |
| KYC (Kiyanli Kombine Elektrik Santrali projesi) | 126 | 0 | 126 |
| GY2022/L1-Serdar etr | 49 | 60 | 109 |
| 1862 | 0 | 94 | 94 |
| TFM | 78 | 0 | 78 |
| TRS proje | 73 | 0 | 73 |
| CS-1 Satlik | 70 | 0 | 70 |
| GKT-28.10/2024 | 0 | 69 | 69 |
| HE-2024 -Dasoguz | 24 | 24 | 48 |
| Istanbul Merkez Ofis | 45 | 0 | 45 |
| 1604-TOP | 2 | 42 | 44 |
| HG-0819 -MARY1 | 14 | 30 | 44 |
| GFM | 25 | 0 | 25 |
| TAP-serhetyaka | 12 | 7 | 19 |
| Hibrit | 18 | 0 | 18 |
| KYT (Kiyanli iletim hatlari ve salt sahalari projesi) | 18 | 0 | 18 |
| HE-1820/L7 | 0 | 17 | 17 |
| ?12/24 | 0 | 17 | 17 |
| SERGI | 15 | 0 | 15 |
| CSK-1 Shatlik | 9 | 5 | 14 |
| Türkmen himiya | 14 | 0 | 14 |
| 15/592 | 1 | 12 | 13 |
| Asgabat merkez ofis | 13 | 0 | 13 |
| Garabogaz | 12 | 0 | 12 |
| 9915 | 9 | 2 | 11 |
| Merkez ofis | 11 | 0 | 11 |
| TRM-1574 MW Kiyanli | 9 | 0 | 9 |
| TRS | 7 | 1 | 8 |
| CS-1 Shatlik | 7 | 0 | 7 |
| KYM(Kiyanli yedek parça-servis ve bakim projesi) | 7 | 0 | 7 |
| Serdar etr | 7 | 0 | 7 |
| tmgaz | 0 | 7 | 7 |
| 25.04.2012 | 5 | 0 | 5 |
| Ahal & Dasoguz Ziyaret | 5 | 0 | 5 |
| ASB-350 | 2 | 2 | 4 |
| AWAZA | 4 | 0 | 4 |
| KIYANLY 1574 MW CCPP LOJISTIK | 4 | 0 | 4 |
| MB-01/22 | 0 | 4 | 4 |
| 1604-TOP gulluk pasport | 0 | 3 | 3 |
| 1862-NGIZ | 0 | 3 | 3 |
| Halka LOT 7 | 3 | 0 | 3 |
| HE-1820/7 | 0 | 3 | 3 |
| KYM | 3 | 0 | 3 |
| LM6000 Bakim | 3 | 0 | 3 |
| Satlyk-1 | 3 | 0 | 3 |
| Teklip grubu | 3 | 0 | 3 |
| ... | 1 | 1 | 2 |
| 1188-"Türkmenhimiya" | 0 | 2 | 2 |
| Dasoguz ziyarety | 2 | 0 | 2 |
| Suw rezerwuarlar | 2 | 0 | 2 |
| Türkmenenergo DEK | 2 | 0 | 2 |
| 10 MWt bolan gün we yel utgasdyrylan elektrik stansiyasy | 0 | 1 | 1 |
| 12ý476 | 1 | 0 | 1 |
| Awaza DES ve Newruz salt sahalari | 1 | 0 | 1 |
| ÇOGANLI MERKEZ DEPO | 1 | 0 | 1 |
| Dolandyryjylar genesinin baslygynyn orunbasary | 1 | 0 | 1 |
| GY2022/L1 | 0 | 1 | 1 |
| Halka LOT 7 projesi | 1 | 0 | 1 |
| Mary Galkinsh Petrofac | 1 | 0 | 1 |
| Özbegistan | 1 | 0 | 1 |
| TOP-24.01.2020.,1604 | 0 | 1 | 1 |
| TRS-319 | 1 | 0 | 1 |

### Active `Contract` codes not referenced (10)

`1099`, `12/24`, `12/24 T-himiya`, `CSH-1 Shatlik`, `HE-2024`, `Kiyanli 220 kV ve Goranmak 110 kV GIS Bakim ve Egitim`, `KYC-KYM-CS-1`, `Mary-3`, `SAS - 1000424087   TRS - 319`, `Tapi`

---

## Re-audit SQL (repeatable)

```sql
SELECT COUNT(*) FROM dbo.Contract WHERE GCRecord IS NULL;
SELECT COUNT(DISTINCT LTRIM(RTRIM(NumberOfContract))) FROM dbo.Contract WHERE GCRecord IS NULL;
SELECT MAX(LEN(LTRIM(RTRIM(NumberOfContract)))) FROM dbo.Contract WHERE GCRecord IS NULL;

SELECT LTRIM(RTRIM(c.NumberOfContract)) AS code, COUNT(*) AS cnt
FROM dbo.Person p INNER JOIN dbo.Contract c ON p.Contract = c.Oid
WHERE p.GCRecord IS NULL AND c.GCRecord IS NULL
GROUP BY LTRIM(RTRIM(c.NumberOfContract)) ORDER BY cnt DESC;

SELECT LTRIM(RTRIM(c.NumberOfContract)) AS code, COUNT(*) AS cnt
FROM dbo.Application a INNER JOIN dbo.Contract c ON a.Contract = c.Oid
WHERE a.GCRecord IS NULL AND c.GCRecord IS NULL
GROUP BY LTRIM(RTRIM(c.NumberOfContract)) ORDER BY cnt DESC;

SELECT COUNT(DISTINCT code) FROM (
  SELECT LTRIM(RTRIM(c.NumberOfContract)) AS code FROM dbo.Person p INNER JOIN dbo.Contract c ON p.Contract = c.Oid WHERE p.GCRecord IS NULL AND c.GCRecord IS NULL
  UNION
  SELECT LTRIM(RTRIM(c.NumberOfContract)) FROM dbo.Application a INNER JOIN dbo.Contract c ON a.Contract = c.Oid WHERE a.GCRecord IS NULL AND c.GCRecord IS NULL
) u;
```

---

## Next steps (documentation only)

1. ~~Generate tenant catalog with **73** rows~~ **Done** — `project-contract.calik-energi.json` (2026-06-26).
2. **Deploy before OData import:** copy `project-contract.calik-energi.json` → `{AppBase}/LookupCatalogs/tenant/project-contract.json`, bump `tenant/manifest.json` `version`, set `FORCE_XAF_DB_UPDATE=true` once, restart app.
3. Keep **`identityPassThrough: true`**; add `values[]` aliases only if product wants `Satlyk-1` → `Şatlyk-1`.
4. Gap **GT-15** remap stays in `gap-insaat` only.

### Ministry mapping gaps (Visa2026 catalog has 4 ministries only)

Legacy `AppliedMinistery` titles with **no** `ApprovingMinistry` match — seeded with **Energetika** fallback (SLA 10/8):

| Legacy title (sample) | Rows affected |
|-----------------------|---------------|
| AŞGABAT ŞÄHER HÄKIMLIGINE | 12552 AST1, 13542 AST2, … |
| Türkmenhimiya (döwlet konserni) | MB-01/22, 1188, ?12/24 |
| TNGIZ / Türkmennebit | 1862, 15/592, TRS, 25.04.2012 |
| TÜRKMENISTANYŇ DOKMA SENAGATY MINISTRLIGI | 12ý476 |

Contracts **without** `AppliedMinistery` FK (31 codes) also use Energetika fallback. Revisit after ministry catalog expansion if approval routes matter.
