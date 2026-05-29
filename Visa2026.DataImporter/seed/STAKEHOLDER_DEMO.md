# Stakeholder demo seed data

Demonstrate how each **application type** looks in the UI and which **Word / Excel** reports (Resminamalar) apply.

## Scope

| Item | Count / note |
|------|----------------|
| Application types (Module) | 36 (`ApplicationTypeConfigurationSeed.Data.cs`) |
| Active scenarios (index) | **Shared + 36 demo scenarios** (one per application type) |
| Archived | `scenarios/_archive/legacy-state-dashboard/` — state bell / ministry duplicates |

## Import

```powershell
# App running (lookups seeded), then:
dotnet run --project Visa2026.DataImporter
```

## Authoring rules

1. **One scenario per application workflow** you need in demos (not multiple ministry/state variants).
2. **`ApplicationItems` columns** must match `ApplicationType.Show*` flags (`ApplicationTypeConfigurationCatalog.json` in Module). Importer strips hidden columns; `--validate-seed` reports violations.
3. **`ShowApplicationItems=false`** → use **`BusinessTrips`** (not `ApplicationItems`). Registration/check-in data uses **`ApplicationItems`** with `ShowRegistrations` columns (legacy `Registrations` sheet is obsolete).
4. **No obsolete fields:** `Filter`, `Company` on Applications/Persons, `ApplicationTypeFilter` — see `docs/DEPRECATED.md`.
5. Lookups must match Module JSON catalogs (`seed/README.md`).

## Tooling

```powershell
dotnet run --project Visa2026.DataImporter -- --validate-seed
dotnet run --project Visa2026.DataImporter -- --prune-seed
# after ApplicationType Show* changes: edit ApplicationTypeConfigurationCatalog.json, restart app (updater syncs DB)
```

## Scenario index map

Full **ApplicationType → file** map: `application-type-manifest.yaml`.

New demo files (`28`–`36`): `Demo_App_*` scenarios using application numbers `056`–`064` (`4/-056` … `4/-064`).
