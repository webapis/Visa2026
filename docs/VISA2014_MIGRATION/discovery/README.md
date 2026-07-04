# Per-BO discovery dossiers

Discovery is **atomic per Business Object** and follows **dependency order** — the same sequence as OData import.

**Legacy source of truth:** database **`VISA2015`** (`visa2014-sql-local`). The VISA2014 repo (`visa2014-readonly-files`) is for additional context only — if code ≠ database, map from SQL.

**Import gate:** complete Phase 1 discovery, approve [IMPORT_PLAN_AND_STRATEGY.md](../IMPORT_PLAN_AND_STRATEGY.md), then **human confirmation** (`importConfirmed: true`) before import code or OData load.

**Experience:** read [learnings.md](../../.cursor/skills/visa2014-to-visa2026-import/learnings.md) before work; append after verified sessions.

**Canonical order:** [`Visa2026.DataImporter/legacy/visa2014/order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) → `entities[]`

Do not maintain a separate discovery queue. When adding a BO, append or insert it in `order.yaml` at the correct dependency position (parents before children).

## Files

| File | Role |
|------|------|
| [`order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) | **Dependency-ordered** entity list (discovery + import) |
| [`_template.yaml`](./_template.yaml) | Copy when adding a new BO |
| `{TargetODataEntity}.yaml` | One dossier per BO |
| [`../entity-inventory.yaml`](../entity-inventory.yaml) | Summary index — sync when dossier closes |
| [`../table-mappings.yaml`](../table-mappings.yaml) | Layer 1 — legacy table → OData entity |
| [`../lookup-translations.yaml`](../lookup-translations.yaml) | Layer 3 — lookup value map |
| [`../property-gap-registry.yaml`](../property-gap-registry.yaml) | Cross-BO gap + dedupe summary |
| [`../migration-status.yaml`](../migration-status.yaml) | **Workstreams, issues, lookup audit** — [`../STATUS.md`](../STATUS.md) dashboard |
| [`../../Visa2026.DataImporter/legacy/visa2014/field-maps/`](../../Visa2026.DataImporter/legacy/visa2014/field-maps/) | Layer 2 — column maps |
| [`../schema-snapshot.md`](../schema-snapshot.md) | Bootstrap only — global table index |

## Pick next BO (dependency order)

1. Bootstrap complete (`order.yaml` → `bootstrapOnce`).
2. Walk **`entities`** in **array order** (already a topological sort).
3. Select the **first** entry where:
   - `discoveryStatus` ∉ `complete`, `blocked`, `skip`
   - every name in `dependsOn` has `discoveryStatus` ∈ `complete`, `blocked`, `skip`
4. If a dependency is `blocked`, stop downstream unless waived in that dependency's dossier `mapping.notes`.
5. Set `discoveryStatus: in_progress` on dossier + `order.yaml` entry.

## Atomic session workflow

1. Pick next BO using rules above (one `in_progress` at a time).
2. Complete dossier checklist (or `blocked` / `skip` with reason).
3. Sync `entity-inventory.yaml`, `property-gap-registry.yaml`, `migration-status.yaml`, and `order.yaml` `discoveryStatus`.
4. **Do not import yet** — export **Excel preview** (Phase 1c), then wait for Phase **1b** (`importConfirmed: true`) after workbook review.
5. Only then pick the next BO in dependency order (or proceed to pilot if confirmed).

**Data quality (required before dossier `complete`):** layers **1–3** mapping + gaps, dedupe, defaults — [VISA2014_MIGRATION.md § Mapping protocol](../VISA2014_MIGRATION.md).
