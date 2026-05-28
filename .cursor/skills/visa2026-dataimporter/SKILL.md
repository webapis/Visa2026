---
name: visa2026-dataimporter
description: Run and maintain Visa2026.DataImporter seed/scenario import (seed layout, preflight visibility check, validate/prune seed, clear/sync scenario workflows). Use when the user mentions Visa2026.DataImporter, seeding scenarios, data.yaml/seed/scenarios, clear-scenario, sync-scenario, validate-seed, prune-seed, or needs a safe repeatable import workflow.
disable-model-invocation: true
---

# Visa2026.DataImporter

## Quick start (local workstation)

- Ensure **`Visa2026.Blazor.Server`** has been started at least once for the target DB so Module updaters seed lookup catalogs + application types.
- Use the launcher (preferred):
  - `scripts/local/Run-DataImporter.ps1`

## Import modes

### Default import (recommended)

- Runs the **visibility preflight** against the live server `ApplicationType` rows.
- Imports `seed/scenarios.index.yaml` by default (or another seed path).

Command:

```powershell
dotnet run --project Visa2026.DataImporter
```

### Clear and re-import one scenario

Use when you changed scenario yaml content and want a clean refresh.

```powershell
dotnet run --project Visa2026.DataImporter -- --clear-scenario <ScenarioName>
```

### Sync (PATCH) one scenario

Use only for small edits where upsert keys exist and you do not want deletes.

```powershell
dotnet run --project Visa2026.DataImporter -- --sync-scenario <ScenarioName>
```

## Safety checks

### Visibility preflight (blocks import)

Before importing, DataImporter verifies that `seed/application-type-visibility.json` matches the **live** server `ApplicationType` `Show*` flags.

If it fails:
- If the server is correct, regenerate the JSON:
  - `scripts/local/Export-ApplicationTypeSeedVisibility.ps1`
- If the JSON is correct, ensure Module updaters applied (start the app; update DB).

Escape hatch (debug only):

```powershell
dotnet run --project Visa2026.DataImporter -- --skip-visibility-preflight
```

## Seed maintenance (YAML hygiene)

### Validate (no changes)

```powershell
dotnet run --project Visa2026.DataImporter -- --validate-seed
```

### Prune (rewrites scenario yaml files)

Use after changing `ApplicationType.Show*` behavior to remove now-hidden fields from `seed/scenarios/*.yaml`.

```powershell
dotnet run --project Visa2026.DataImporter -- --prune-seed
```

## Seed layout rules

- Edit scenarios under `Visa2026.DataImporter/seed/scenarios/*.yaml`
- Keep `seed/scenarios.index.yaml` as the ordered list.
- Archived / non-demo scenarios belong under `seed/scenarios/_archive/` and must not be listed in the index.

