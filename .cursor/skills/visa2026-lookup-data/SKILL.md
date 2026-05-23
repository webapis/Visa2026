---
name: visa2026-lookup-data
description: >-
  Maintains Visa2026 lookup/reference data: ApplicationType Show* visibility flags,
  SelectionCode, bulk catalogs (Country, City) via lookup.xlsm, ModuleUpdater deploy
  sync, DataImporter seed/sync, LOOKUPS.md as read-only reference. Use when changing
  ApplicationType visibility, lookup.xlsm, LOOKUPS.md, LookupSeeder, Application/ApplicationItem
  Appearance criteria tied to ApplicationType, or redeploy lookup corrections to other environments.
disable-model-invocation: false
---

# Visa2026: lookup data maintenance

## Source-of-truth contract

| Data | Source of truth | Applied to DB |
|------|-----------------|---------------|
| **ApplicationType** (especially all **`Show*`** flags, `SelectionCode`, `Category`, `LifecycleStage`, `DurationInDays`, `PdfForm_Code`, display names) | **`ApplicationTypeConfigurationSeed`** + **`ApplicationTypeConfigurationUpdater`** in `Visa2026.Module/DatabaseUpdate/` | **Every app startup / deploy** via XAF DB update — **`Show*` always overwritten** from seed |
| **SelectionCode** only (if not yet folded into configuration seed) | `ApplicationTypeSelectionCodeSeed` + `ApplicationTypeSelectionCodeUpdater` | Deploy (fills empty codes; configuration updater owns full row when present) |
| **Bulk catalogs** (Country, Gender, City, …) | `Visa2026.Module/DatabaseUpdate/LookupCatalogs/*.json` + `manifest.json` | **Every deploy** via `LookupCatalogSyncUpdater` (overwrite scalars; no deletes) |
| **Tenant / company** | `LookupCatalogs/tenant/*.json` + `tenant/manifest.json` | **Position**, **Specialty**, **EducationInstitution**, **Department**, **Ministry**, **Company**, **ProjectContract** — per deployment |
| **Human reference** | `LOOKUPS.md` at solution root | Legacy: `--dump-lookups` from xlsm; prefer regenerating from JSON (future script) |
| **`lookup.xlsm`** | Dev export only (`--export-lookup-catalogs`) | **Not** used at runtime or for seeding |

**Do not** use `LOOKUPS.md` or the XAF lookup UI as the place to fix `ApplicationType` visibility for shipped behavior.

**Canonical doc:** [docs/LOOKUP_SEEDING.md](../../../docs/LOOKUP_SEEDING.md) (architecture, global vs tenant lists, deploy behavior).

**Commands and file map:** [reference.md](./reference.md).

**Related skills:** [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) (deploy, `FORCE_XAF_DB_UPDATE`, schema), [commit-after-verify](../commit-after-verify/SKILL.md) (commit after build).

---

## When this skill applies

- Toggle visibility for `Application` / `ApplicationItem` fields via `ApplicationType.Show*`
- Add a new BO property that needs type-driven visibility
- Add or rename an `ApplicationType` (`Name` key e.g. `App_Inv`)
- Keep `lookup.xlsm` / `LOOKUPS.md` aligned after catalog changes
- Redeploy lookup corrections to dev, Docker, prod, or another company instance

---

## Decision: which workflow?

```
Changing ApplicationType Show* / workflow config?
  → § ApplicationType (code seed + updater + Appearance)

Adding/editing Country, City, Gender, … (large catalog)?
  → § Bulk lookup.xlsm

Only need to read exact seeded strings?
  → Regenerate LOOKUPS.md (§ Reference dump); do not edit LOOKUPS.md
```

---

## ApplicationType workflow (primary)

### A. New or changed UI visibility

1. **BO + Appearance** (if new field):
   - Add property on `Application.cs` or `ApplicationItem.cs`.
   - Add `[Appearance(..., Criteria = "... ApplicationType.ShowX ...")]` (or `Application.ApplicationType.ShowX` on items).
   - If new flag: add `public virtual bool ShowX { get; set; }` on `ApplicationType` in `LookupBusinessObjects.cs`.

2. **Seed** — edit `ApplicationTypeConfigurationSeed` (one row per `Name`):
   - Key: **`Name`** (stable, e.g. `App_Inv_And_WP`) — must match `LOOKUPS.md` / existing DB rows.
   - Set **all** `Show*` booleans for that type (deploy overwrites flags; partial rows must still be intentional).

3. **Updater** — `ApplicationTypeConfigurationUpdater`:
   - Match existing row by `Name`; create if missing.
   - **Always assign `Show*` from seed** (overwrite DB).
   - Register in `Module.cs` updater list if new class.

4. **SelectionCode** — if ministry code changes, update the same configuration row or `ApplicationTypeSelectionCodeSeed` consistently.

5. **Verify**
   - `dotnet build Visa2026.slnx -c Debug`
   - Local: start app (or Docker recreate) so updaters run; open Application detail for affected `Name`.
   - Optional: `dotnet run --project Visa2026.DataImporter -- --dump-lookups` → diff `LOOKUPS.md` Application Type section.

6. **Do not** rely on DataImporter to push `Show*` to an existing database (`LookupSeeder` skips non-empty tables).

### B. New ApplicationType row

Same as §A, plus:

- Add seed row with full flag set and metadata (`Category`, `LifecycleStage`, `NameTm`, `PdfForm_Code`, …).
- Update `ApplicationTypeDevelopmentReadiness` / `PROMPT_APPLICATION_TYPE_READINESS.md` if the type is gated for selection.
- Reports / Word / user templates: grep `ApplicationType.Name` and update criteria or template links.
- After seed/updater exist: export or sync `lookup.xlsm` ApplicationType sheet **from** seed (§ Excel sync) so greenfield import stays aligned.

### C. Deploy to existing environments

1. Merge seed/updater changes; build image or publish Module.
2. Restart app (XAF runs `UpdateDatabaseAfterUpdateSchema`).
3. If DB is “current” but updaters did not run: **`FORCE_XAF_DB_UPDATE=true`** once — see [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md) and [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md).
4. Smoke-test one Application per changed `Name`.
5. **Do not** ask the user to edit lookups in the Blazor UI for routine flag fixes.

---

## Bulk catalog JSON workflow

1. Edit `Visa2026.Module/DatabaseUpdate/LookupCatalogs/{id}.json` (see `manifest.json` for entity + match key).
2. Deploy / restart app → `LookupCatalogSyncUpdater` upserts and overwrites scalars.
3. **New database:** start Blazor Server once (updaters run), then `dotnet run --project Visa2026.DataImporter -- --import-yaml-only`.
4. **One-time migration from Excel:** `dotnet run --project Visa2026.DataImporter -- --export-lookup-catalogs` (requires `lookup.xlsm` in DataImporter output dir).

`data.yaml` scenario imports: lookup **names** must match seeded JSON / `LOOKUPS.md` exactly (Turkmen strings).

---

## Excel sync (ApplicationType sheet)

**Direction:** seed (code) → Excel, not Excel → production truth.

Until an export tool exists:

- After changing `ApplicationTypeConfigurationSeed`, manually update the **ApplicationType** sheet in `lookup.xlsm` **or** track a follow-up task to add `tools/ExportApplicationTypeToLookupXlsm`.
- Never maintain divergent Excel-only `Show*` values that are not in the seed.

---

## Agent checklist (copy per task)

```
- [ ] Classified change: ApplicationType vs bulk catalog
- [ ] Appearance / new Show* on BO if needed
- [ ] ApplicationTypeConfigurationSeed row(s) updated
- [ ] ApplicationTypeConfigurationUpdater overwrites Show* (policy confirmed)
- [ ] Related SelectionCode / readiness / reports / templates checked (grep ApplicationType.Name)
- [ ] dotnet build Visa2026.slnx
- [ ] Deploy/updater verified (not manual lookup UI; not LOOKUPS.md edit)
- [ ] LOOKUPS.md regenerated if catalog or reference dump needed
- [ ] lookup.xlsm ApplicationType sheet aligned if greenfield parity required
```

---

## Anti-patterns

| Do not | Do instead |
|--------|------------|
| Edit `LOOKUPS.md` as source | Edit seed; `--dump-lookups` for reference |
| Fix `Show*` only in Excel for deployed systems | Seed + deploy updater |
| Expect `--seed-lookups-only` to update existing ApplicationType rows | Configuration updater on app start |
| Edit ApplicationType in Blazor lookup UI for product fixes | Seed in git |
| Add Appearance without seed flag | Add `Show*` on BO + seed row for every type |

---

## Regenerate seed data from LOOKUPS.md

After bulk-editing `lookup.xlsm`, refresh `LOOKUPS.md` (`--dump-lookups`), then:

```powershell
.\scripts\local\Generate-ApplicationTypeConfigurationSeed.ps1
```

Commit `ApplicationTypeConfigurationSeed.Data.cs` with the script output.

