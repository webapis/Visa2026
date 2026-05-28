# Seed data (scenario import)

Demo / test business data for **Visa2026.DataImporter**. Lookup catalogs and org singletons are **not** here — they come from **Visa2026.Module** on app startup (`LookupCatalogSyncUpdater`). See [docs/LOOKUP_SEEDING.md](../../docs/LOOKUP_SEEDING.md).

## Layout

| Path | Purpose |
|------|---------|
| `scenarios.index.yaml` | Ordered list of scenario fragment files |
| `scenarios/*.yaml` | One file per scenario (`order`, `name`, `anchor`, `data`, …) |

## Run import

```powershell
dotnet run --project Visa2026.DataImporter
# default: seed/scenarios.index.yaml (next to the built exe)

dotnet run --project Visa2026.DataImporter -- seed/scenarios.index.yaml
```

Override path: first positional argument, or `DATA_YAML_PATH` environment variable.

## Authoring rules

- Sheet names and column headers must match `Excelmappings.cs`.
- Lookup column values must match **Name** / **Code** in `Visa2026.Module/DatabaseUpdate/LookupCatalogs/` (and tenant JSON).
- See **SCENARIO_GUIDE.md** for anchors, application numbers (`4/-001`), and sheet order.
- **ApplicationType visibility:** only seed `ApplicationItems` columns that match `ApplicationType.Show*` for that row’s `Application Type` (see `Visa2026.Module/DatabaseUpdate/LookupCatalogs/ApplicationTypeConfigurationCatalog.json`). Registration types with `ShowApplicationItems=false` use **`Registrations`** / **`BusinessTrips`**, not `ApplicationItems`.
- **Obsolete:** do not seed `Filter`, `Company` on applications/persons, `ApplicationTypeFilter`, or deprecated sheets — see `docs/DEPRECATED.md`.

Validate / auto-prune:

```powershell
dotnet run --project Visa2026.DataImporter -- --validate-seed
dotnet run --project Visa2026.DataImporter -- --prune-seed
```

## Legacy `data.yaml`

The repo root **`data.yaml`** next to this project is **obsolete** (stub only). Edit files under **`seed/scenarios/`** instead.

To re-export fragments from an old monolithic file (one-time migration):

```powershell
dotnet run --project Visa2026.DataImporter -- --export-seed
```
