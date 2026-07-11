# Turkmenistan geography reference (SQLite)

**Purpose:** Offline **Region ↔ City** ground truth for VISA2014 → Visa2026 import conflict resolution.

**File:** `turkmenistan-geography.db`  
**Editable seed:** `geography-overrides.json` (Wikipedia/OSM corrections + aliases)  
**Base rows:** built from Module `region.json` + `city.json`, then overrides applied.

## Policy

1. If legacy **Region+City** matches this DB → keep legacy Region.
2. If legacy Region disagrees with this DB for that city name → **use the DB Region** when resolving City / writing AddressOfResidence.
3. If city is unknown here → fall back to Visa2026 catalog matcher only.

## Rebuild

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --rebuild-visa2014-geography-db
```

Optional: `--output path\to\turkmenistan-geography.db`

## Schema

- `region` — code (AS/BN/AH/MR/DZ/LB), name_tm
- `city` — name_tm, name_key, region_code, status (current|abolished|historical), notes, source
- `city_alias` — alternate spellings / legacy labels

Not a replacement for XAF `Region`/`City` lookup catalogs — import reference only.