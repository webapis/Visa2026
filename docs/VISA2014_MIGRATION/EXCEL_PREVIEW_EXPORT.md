# VISA2014 → Visa2026 — Excel preview export

**Purpose:** Export **consolidated, import-ready** legacy data to **Excel** so reviewers can see **exactly what would be loaded** into Visa2026 **before** OData import or `importConfirmed: true`.

This is **not** a raw SQL dump. The export uses the **same transform pipeline** as import (dedupe → column map → lookup translation → defaults) but writes **`.xlsx`** instead of POST/PATCH.

**Status:** specified — CLI **`--export-visa2014-preview`** planned (not implemented until strategy approved).

**Related:** [IMPORT_PLAN_AND_STRATEGY.md](./IMPORT_PLAN_AND_STRATEGY.md) · [field-maps/](../../Visa2026.DataImporter/legacy/visa2014/field-maps/) · [import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md)

---

## When to use

| Phase | Action |
|-------|--------|
| After **discovery** `complete` for a BO | Generate preview workbook from `field-maps/{Entity}.yaml` |
| Before **`importConfirmed: true`** | Reviewer opens Excel; spot-check values, skips, dedupe |
| After mapping change | Re-export; compare row counts and sample rows |
| Before pilot OData | Confirm preview row count ≈ expected import count |

**Does not require:** Blazor running, OData, or target DB writes. **Does require:** read access to **`VISA2015`** and a complete field-map for the entity.

---

## Pipeline (same as import, different sink)

```text
VISA2015 SQL extract
  → dedupe (field-map deduplication)
  → column transforms (field-map fields[])
  → lookup translation (lookup-translations.yaml)
  → apply target-only defaults
  → classify each row (import | skip | duplicate_merged)
  → write Excel (no OData)
```

If the preview looks wrong, **fix mapping YAML** and re-export — do not POST to Visa2026 to “see what happens”.

---

## Planned CLI

```powershell
# From repo root (after implementation)
dotnet run --project Visa2026.DataImporter -- `
  --export-visa2014-preview `
  --entity Person `
  --output Visa2026.DataImporter/legacy/visa2014/preview-export/Person-preview.xlsx

# Optional
#   --connection "Server=localhost\SQLEXPRESS;Database=VISA2015;..."
#   --max-rows 5000          # cap for dev; omit for full export
#   --include-skipped        # rows on _Skipped sheet (default true)
#   --verbose
```

Connection string: env **`VISA2014_SQL_CONNECTION`** or documented local default (`VISA2015` on `localhost\SQLEXPRESS`). Never commit prod credentials.

---

## Workbook layout

One **`.xlsx`** per entity export (or `--entity all` for multi-sheet workbook — TBD in implementation).

### Main sheet — `{Entity}` (e.g. `Person`)

| Column kind | Header | Content |
|-------------|--------|---------|
| **Target** | Visa2026 **property names** from `field-maps/{Entity}.yaml` `fields[].target` | Values **after** transform + lookup translation (what OData would receive) |
| **Audit** (optional, recommended) | `_legacyRowId`, `_legacyTable`, `_dedupeGroupId`, `_importAction` | Traceability; `_importAction` ∈ `import` · `skip` · `duplicate_merged` |

Column order: audit columns first (if enabled), then target properties in field-map order.

Lookup fields show **translated target** values (catalog `Name`/`Code`), not raw legacy strings.

### Sheet `_Skipped`

Rows where `missingBehavior: skip_row` or `unmappedPolicy` blocked the row.

| Column | Meaning |
|--------|---------|
| `_legacyRowId` | Legacy PK / OID |
| `_reason` | e.g. `unmapped_lookup:Department`, `required_null:PassportNumber` |
| Legacy + target columns | As available for debugging |

### Sheet `_UnmappedLookups`

Distinct legacy lookup values encountered that have **no** row in `lookup-translations.yaml` (feeds layer 3 completion).

### Sheet `_DedupeSummary`

One row per duplicate **group**: keys, member count, canonical `_legacyRowId`, `canonicalRule` applied.

### Sheet `_Meta` (optional)

Export timestamp, entity name, field-map path, row counts (legacy / after dedupe / import / skipped), connection database name (`VISA2015`).

---

## Field-map `export` section

Each [`field-maps/{Entity}.yaml`](../../Visa2026.DataImporter/legacy/visa2014/field-maps/_template.yaml) may include:

```yaml
export:
  sheetName: Person              # main sheet name
  includeAuditColumns: true
  auditColumns:
    - _legacyRowId
    - _legacyTable
    - _dedupeGroupId
    - _importAction
  lastExportedAt: null           # set by tool after successful export
  lastExportPath: null             # relative path under preview-export/
```

Sync **`discovery/{Entity}.yaml`** checklist `excel_preview_exported: true` when a reviewer accepts the export for confirmation.

---

## Review checklist (before `importConfirmed`)

Open the preview workbook and verify:

- [ ] Row count on `{Entity}` sheet matches expected production volume (after dedupe)
- [ ] Upsert key columns populated and unique where expected (`PassportNumber`, etc.)
- [ ] Lookup columns show **Visa2026** catalog values, not legacy codes
- [ ] `_Skipped` empty or every row explained in mapping / `unmappedPolicy`
- [ ] `_UnmappedLookups` empty or waived in `lookup-translations.yaml`
- [ ] `_DedupeSummary` groups and canonical picks match business rules
- [ ] No surprise empty required target columns (fix `propertyGaps.targetOnly` defaults)

Record export path in dossier `importConfirmation.reviewNotes`.

---

## Output location and git

| Path | Git |
|------|-----|
| `Visa2026.DataImporter/legacy/visa2014/preview-export/` | Folder tracked; **`*.xlsx` gitignored** (PII) |
| [`preview-export/README.md`](../../Visa2026.DataImporter/legacy/visa2014/preview-export/README.md) | Tracked — explains folder |

Do not commit preview workbooks. Share via secure channel if officers review offline.

---

## Relation to seed Excel (`data.yaml`)

| | Seed import (`data.yaml`) | VISA2014 preview export |
|---|---------------------------|-------------------------|
| Direction | Excel → OData (Visa2026) | VISA2015 SQL → Excel |
| Purpose | Dev/stakeholder demo data | **Prod migration review** |
| Mapping | `ExcelMappings.cs` | `legacy/visa2014/field-maps/*.yaml` |
| Tooling | Existing DataImporter scenarios | **`--export-visa2014-preview`** (planned) |

Preview workbooks are **not** fed back through `--import-scenario` unless you explicitly build a one-off bridge (out of scope for v1).

---

## Implementation notes (for `--export-visa2014-preview`)

Reuse from existing DataImporter where possible:

| Piece | Reuse |
|-------|--------|
| Excel write | Same stack as lookup export (`ExcelDataReader` ecosystem or `ClosedXML` / `DocumentFormat.OpenXml` — pick one at implement time) |
| Transform logic | Shared library with `--import-visa2014` (single code path for preview + load) |
| SQL extract | Parameterized queries from field-map `source.table` + `import.rowFilter` |

**Rule:** preview and OData import **must** call the same transform function — no duplicate mapping logic in Excel-only code.

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-20 | Initial Excel preview export spec (consolidated transform → xlsx before import) |
