# Duplicate lookup preview — local PostgreSQL `visa2026`

Generated from live DB after VISA2015 import. **No merges applied.**

Dashboard symptoms match these **near-duplicate** catalog rows (different `NameTm` spellings from legacy), not exact clones.

## ProjectContract (73 active)

| Group | NameTm | Person | App | Active visas (view) | Suggested |
|-------|--------|------:|---:|--------------------:|-----------|
| **KYC** | KYC (Kiyanlı **Kobmine** …) | ~~515~~ | ~~72~~ | ~~319~~ | **MERGED 2026-07-27** → Kombine |
| **KYC** | KYC (Kiyanlı **Kombine** Elektrik Santrali projesi) | **675** | **94** | **392** | KEEP (applied) |
| **CS-1** | CS-1 **Şatlık** | 68 | 81 | 52 | KEEP (Turkmen Ş) |
| **CS-1** | CS-1 **Shatlık** | 75 | 6 | 10 | MERGE → CS-1 Şatlık |
| **CS-1 family** | CSK-1 Shatlık | 7 | — | 1 | REVIEW (keep separate unless you want one Şatlık family) |
| **CS-1 family** | Şatlyk-1 | 6 | 9 | 0 | REVIEW |
| **KYM** | KYM (Kiyanlı yedek parça-servis ve bakım projesi) | 191 | 11 | 7 | KEEP |
| **KYM** | KYM(Kiyanlı yedek parça-servis ve bakım projesi) | 7 | 9 | 2 | MERGE → spaced `KYM (` |
| **KYM** | KYM | 0 | 0 | 0 | Soft-delete / unused |
| Other token families | 1604-TOP / 1604-TOP gulluk pasport; TRS / TRS proje; Halka LOT 7; AWAZA / Awaza DES… | varies | | | REVIEW (may be intentional)

Exact `NameTm` duplicates: **0**. Chart split is Ordinal grouping on distinct labels.

## Subcontractor (129 active) — matches dual “Çalık Enerji” bars

| NameTm | Hex (UTF-8) | Default | Person refs | Suggested |
|--------|-------------|---------|------------:|-----------|
| **Çalyk Enerji** | `…616c796b…` (y) | yes | **2083** | KEEP |
| **Çalık Enerji** | `…616cc4b16b…` (ı) | no | **365** | MERGE → Çalyk Enerji |
| Çalik Enerji | `…616c696b…` (i) | no | 5 | MERGE → Çalyk Enerji |
| Çalık Enerji̇ (extra char) | ends `c4b1` | no | 1 | MERGE → Çalyk Enerji |
| Çalık Enenrji (typo) | — | no | 1 | MERGE → Çalyk Enerji |
| Çalık enerji-Sakir… (phone noise) | — | no | 1 | REVIEW |
| Çalık Dijital ve… | — | no | 1 | KEEP separate (different company) |

## How to re-run

```powershell
.\scripts\visa2014-migration\Preview-DuplicateProjectContractSubcontractor.ps1 -Profile Local
```

## Next

Approve which groups to merge (especially KYC keeper = majority+rename vs correct-spelling row), then add `-Apply` merge for Postgres (repoint Person/Application FKs, soft-delete extras).