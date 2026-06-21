# VISA2014 → Visa2026 — lookup resolution strategy

**Principle:** **Do not import lookup tables from `VISA2015`.** Visa2026 catalogs are already seeded (Module updaters / `LookupCatalogs/*.json`). Migration only **translates** legacy FK values → existing target rows at transactional import time.

**Machine-readable map:** [`lookup-translations.yaml`](lookup-translations.yaml) (layer 3)

**Related:** [LOOKUP_SEEDING.md](../LOOKUP_SEEDING.md) · [visa2026-lookup-data skill](../../.cursor/skills/visa2026-lookup-data/SKILL.md) · [LookupCatalogMatchHelper.cs](../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/LookupCatalogMatchHelper.cs) · [STATUS.md](STATUS.md)

---

## What we do vs do not do

| Do | Do not |
|----|--------|
| Read legacy lookup **values** from `VISA2015` (SQL DISTINCT / FK join) | POST new lookup rows from legacy during migration (default) |
| Map legacy string/code → Visa2026 `Name` / `Code` in `lookup-translations.yaml` | Copy legacy lookup tables into Visa2026 DB |
| Resolve OData FK using **target** catalog via translated key | Assume legacy string equals target string |
| Investigate gaps and record a **decision** per unmapped value | Silently drop or guess ministry/legal values |

**Exception (rare, human-approved):** add a **missing** target catalog row via normal Visa2026 seed path (`LookupCatalogs/*.json` + updater), then reference it in `lookup-translations.yaml` — not ad-hoc OData POST of lookups during `--import-visa2014`.

---

## Workflow per catalog (repeat for each `lookupCatalog` in field-maps)

```text
1. INVENTORY legacy   — DISTINCT values actually used in transactional data (not whole legacy table)
2. INVENTORY target   — OData GET or Module JSON for Visa2026 catalog
3. CLASSIFY each legacy value (see table below)
4. DOCUMENT           — lookup-translations.yaml values[] + unmappedPolicy
5. VERIFY             — 100% coverage of DISTINCT legacy values used by BOs being imported
```

Run **before** transactional import for that BO (`importConfirmed`). Phase **2 — Lookup value audit** in [VISA2014_MIGRATION.md](../VISA2014_MIGRATION.md).

---

## Step 1 — Legacy inventory (source of truth: SQL)

For each catalog field on the current BO:

```sql
-- Template: replace table/column/join for the transactional BO
SELECT DISTINCT l.<display_column> AS legacy_value, COUNT(*) AS row_count
FROM dbo.<TransactionalTable> t
INNER JOIN dbo.<LegacyLookup> l ON t.<FkColumn> = l.Oid
WHERE t.GCRecord IS NULL  -- if applicable
GROUP BY l.<display_column>
ORDER BY row_count DESC;
```

Also note:

- **Legacy duplicate rows** — same meaning, different `Oid` or spelling (see step 3b)
- **Unused legacy rows** — in lookup table but not referenced by transactional data → **ignore** for migration

---

## Step 2 — Target inventory (authoritative: Visa2026)

| Source | Use |
|--------|-----|
| `Visa2026.Module/DatabaseUpdate/LookupCatalogs/<catalog>.json` | Canonical codes/names for global catalogs |
| `ApplicationTypeConfigurationCatalog.json` | ApplicationType `Name` keys (`App_Inv`, …) |
| OData GET on dev (`Visa2026DbDev`) after Blazor startup | Verify runtime rows match JSON |

Match property is usually **`Code`** (Country, Gender) or **`Name`** (Department, ApplicationType) — set `targetMatchProperty` per catalog in `lookup-translations.yaml`.

---

## Step 3 — Classify each legacy value

### 3a. Match type

| Class | Example | Action |
|-------|---------|--------|
| **A — Exact** | Legacy `TUR` → target `Code=TUR` | Add `legacy: TUR` → `target: TUR` (or auto-resolve if rule documented) |
| **B — Normalized** | Legacy `Ayal` → target `Aýal` | Map explicitly; importer may use `LookupCatalogMatchHelper.NormalizeKey` for **suggest**, human confirms in YAML |
| **C — Consolidate** | Two legacy Gender rows both `Erkek` | One `legacy` entry per **distinct string used in data**; ignore extra legacy Oids |
| **D — Semantic remap** | Legacy code `INV` → target `App_Inv` | Explicit `values[]` row with note |
| **E — Gap** | Legacy value has **no** target row | Decision required (3c) |
| **F — Not a lookup** | Legacy `MaritalStatus.Status` free text | Not layer 3 — field `mismatch` / `allow_null` / custom (see Person dossier) |

### 3b. Legacy duplicate values

| Situation | Suggestion |
|-----------|------------|
| Same spelling, multiple legacy `Oid`s | One translation row; legacy FK resolves via display string, not Oid |
| Different spelling, same meaning | Multiple `legacy:` rows → same `target:` (e.g. `Ayal` and `Aýal` → `Aýal`) |
| Different spelling, **unclear** if same meaning | Flag in dossier `mapping.notes`; **block_row** until reviewer decides |
| Duplicate **target** rows in Visa2026 | Out of scope for migration — fix target catalog separately; map to chosen canonical target row |

### 3c. Gap — legacy value missing in Visa2026

Investigate each **E — Gap** with row counts (how many transactional rows affected):

| Decision | When | Action |
|----------|------|--------|
| **E1 — Add to target catalog** | Value is valid, should exist in prod Visa2026 (new ministry code, missing country) | Add row to appropriate `LookupCatalogs/*.json` (or tenant JSON), deploy updater, then add `legacy → target` |
| **E2 — Map to existing** | Legacy typo/alias; officers agree on equivalent | `values[]` only — no catalog change |
| **E3 — allow_null / skip** | Optional field; legacy noise | `unmappedPolicy: allow_null` or field `missingBehavior: allow_null` |
| **E4 — block_row** | Required field; cannot import without ministry/legal value | `unmappedPolicy: block_row`; fix data or catalog before batch |
| **E5 — Quarantine** | Needs business review | Log to import quarantine report; do not POST |

Record gap decisions in `lookup-translations.yaml` under `notes` and in dossier `mapping.notes`.

**Do not** bulk-add legacy lookup rows to Visa2026 without review — especially ApplicationType, Department, ministry enums.

---

## Step 4 — Document in `lookup-translations.yaml`

Per catalog:

```yaml
- targetCatalog: Department
  targetMatchProperty: Name
  legacy:
    table: dbo.Department
    column: NameOfDepartment   # example — confirm in discovery
    sampleQuery: | ...
  values:
    - legacy: "Legacy label"
      target: "Visa2026 Name"
      notes: "Normalized match / alias"
  unmappedPolicy: block_row   # or allow_null for optional catalogs
```

**Importer behavior:** read legacy column → find `legacy` in `values[]` → OData resolve target by `targetMatchProperty` → set FK on POST.

---

## Normalization (letter mismatch)

Visa2026 already normalizes Turkmen characters for catalog sync:

- [`LookupCatalogMatchHelper.NormalizeKey`](../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/LookupCatalogMatchHelper.cs) — fold `ý→y`, lowercase, strip diacritics

**Suggestion:**

1. **Auto-suggest** target match using `NormalizeKey(legacy)` vs `NormalizeKey(target.Code|Name)` during discovery tooling.
2. **Require explicit YAML row** for production import (no silent fuzzy match only in code).
3. Store both raw legacy string and chosen target in `values[]` for audit.

---

## Catalog priority (audit order)

| Priority | Catalogs | Why first |
|----------|----------|-----------|
| P0 | Country, Gender, ApplicationType | Person + Application everywhere |
| P1 | Department, Position, PassportType, VisaType | Employee / application items |
| P2 | Region, EducationLevel, Relationship, Contract/ProjectContract | Domain-specific |
| P3 | Rare / low row-count catalogs | As dossiers need them |

ApplicationType: always map to catalog **`Name`** (`App_Inv`), not display title — see [visa2026-lookup-data](../../.cursor/skills/visa2026-lookup-data/SKILL.md).

---

## Special cases (already seen)

| Catalog | Legacy shape | Suggestion |
|---------|--------------|------------|
| **Country** | `NameOfCountryL` (ISO alpha-3) | Map to Visa2026 `Code`; high confidence auto-match |
| **Gender** | `TypeOfGenderL` | Map `Ayal`→`Aýal`, `Erkek`→`Erkek` |
| **MaritalStatus** | Free-text `Status` | **Not layer 3** — do not import legacy rows; `allow_null` or derive from rules later |
| **ApplicationType** | Legacy vs `App_*` names | Full DISTINCT audit required before Application import |

---

## Acceptance criteria (before transactional import)

For each BO batch:

- [ ] Every `lookupCatalog` in `field-maps/{Entity}.yaml` has a `catalogs[]` entry
- [ ] Every DISTINCT legacy value **used by that BO’s data** has `values[]` row **or** documented `unmappedPolicy`
- [ ] Gap decisions (E1–E5) reviewed for values with `row_count > 0`
- [ ] No reliance on importing legacy lookup tables
- [ ] Target catalog verified on `Visa2026DbDev` via OData GET sample

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-20 | Initial strategy: target catalogs authoritative; translate only; gap/duplicate handling |
