# Organization singletons (lookup + runtime)

How Visa2026 keeps **one row per deployment** for company identity, signatory, visa representative, application numbering, and system settings — including JSON seeding under `LookupCatalogs/tenant/`.

**Related:** general lookup catalogs and multi-row sync rules → [`LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md).  
**Agent skill:** [`.cursor/skills/visa2026-lookup-data/SKILL.md`](../.cursor/skills/visa2026-lookup-data/SKILL.md).

---

## What counts as a singleton

| Business object | Seeded from tenant JSON? | JSON file | Match key in manifest |
|-----------------|--------------------------|-----------|------------------------|
| `CompanyProfile` | Yes | `tenant/company-profile.json` | `Name` |
| `ApplicationNumberingProfile` | Yes | `tenant/application-numbering.json` | `Name` |
| `AuthorizedSignatory` | Yes | `tenant/authorized-signatory.json` | `FullName` |
| `AuthorizedRepresentative` | Yes | `tenant/authorized-representative.json` | `FullName` |
| `SystemSettings` | **No** | — | — (`OrganizationSingletonSeedUpdater` only) |

**Not singletons** (same `tenant/` folder, normal multi-row catalogs): `position.json`, `specialty.json`, `ministry.json`, `project-contract.json`, etc. A file with only one `rows[]` entry does **not** make the entity a singleton; only the **entity type** + code paths below do.

---

## JSON and manifest do not say “singleton”

`tenant/manifest.json` entries look like any other catalog:

```json
{
  "id": "authorized-representative",
  "entity": "AuthorizedRepresentative",
  "file": "authorized-representative.json",
  "matchKey": "FullName",
  "syncMode": "OverwriteScalars"
}
```

There is **no** `"singleton": true` field on `LookupCatalogDefinition`.

The app treats four entity names as organization singletons in **`LookupCatalogEntitySync.IsOrganizationSingletonEntity`**:

- `CompanyProfile`
- `ApplicationNumberingProfile`
- `AuthorizedSignatory`
- `AuthorizedRepresentative`

To add another JSON-backed singleton you must extend that list, add `TryGetInstance` on the BO, and wire reports/settings to read it — not only add a JSON file.

---

## Deploy-time behavior (four JSON singletons)

**Updater:** `LookupCatalogSyncUpdater` → `LookupCatalogEntitySync`.

| Step | Behavior |
|------|----------|
| **Find row** | `FindOrganizationSingleton`: match by manifest `matchKey`; if no match, **reuse** the only existing populated row (so renaming `Name` / `FullName` in JSON updates in place instead of inserting a second row). |
| **Upsert** | `syncMode: OverwriteScalars` — non-key fields in JSON overwrite the DB row each deploy. |
| **Empty placeholders** | `CleanupEmptyOrganizationSingletons` removes rows with blank `Name` / `FullName` when a populated row exists. |
| **Enforce one row** | `RemoveStaleOrganizationSingletonDuplicates`: when the JSON file defines **exactly one** valid match key, keep one row (prefer the row matching that key) and **delete** all other rows for that entity. |

Multi-row catalogs **never** delete rows when keys disappear from JSON (FK safety). Singletons are the **exception**.

**`SystemSettings`:** `OrganizationSingletonSeedUpdater` runs `GetOrCreateInstance` and `OrganizationSingletonHelper.CollapseToSingleRow` so at most one settings row exists. Settings are not in `LookupCatalogs/`.

If sync does not run (DB already “current”), use **`FORCE_XAF_DB_UPDATE=true`** once — see [`ENVIRONMENTS.md`](ENVIRONMENTS.md).

---

## Runtime reads (reports, Word merge, PDF)

Templates such as **`Borcnama.docx`** do not embed JSON. Merge fields use live data, e.g. `{{ds.rows.Representative_FullName}}` → `ApplicationItem` → `AuthorizedRepresentative` in SQL.

**Helper:** `OrganizationReportHelper` → `TryGetInstance()` on each singleton BO.

**Resolver:** `OrganizationSingletonHelper.TryGet` — returns the single populated row; if duplicates still exist, picks one deterministically and logs a tracer warning until the next DB update prunes extras.

Changing `authorized-representative.json` alone is not enough: the app must **run DB updaters** so SQL is updated, then regenerate the document.

---

## Safe vs risky JSON edits

| Edit | JSON singleton | Multi-row catalog (e.g. Position) |
|------|----------------|----------------------------------|
| Change phone, address, `PositionTitleTm`, etc. **without** changing match key | Safe on redeploy | Safe on redeploy |
| Rename **`Name`** or **`FullName`** (match key) | Safe **after** singleton sync + prune (one row enforced) | Creates a **new** row; old row remains; FKs may still point at old row |
| Remove row from JSON | Row removed from DB on deploy (singleton prune) | Old DB row **kept** (by design) |

For production fixes before redeploy, admins can edit **Organization** screens (where navigation allows) or correct SQL once.

---

## Code map

| Concern | Location |
|---------|----------|
| Singleton entity list + find/prune | `Visa2026.Module/DatabaseUpdate/LookupCatalogs/LookupCatalogEntitySync.cs` |
| Updater orchestration | `Visa2026.Module/DatabaseUpdate/LookupCatalogSyncUpdater.cs` |
| System settings row | `Visa2026.Module/DatabaseUpdate/OrganizationSingletonSeedUpdater.cs` |
| Runtime `TryGetInstance` | `CompanyProfile`, `AuthorizedSignatory`, `AuthorizedRepresentative`, `ApplicationNumberingProfile`, `SystemSettings` |
| Shared resolver | `Visa2026.Module/Services/OrganizationSingletonHelper.cs` |
| Reports entry | `Visa2026.Module/Services/OrganizationReportHelper.cs` |
| Tenant seed files | `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/*.json` |
| Tenant manifest | `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/manifest.json` |

---

## Checklist: new organization singleton from JSON

1. Add BO with `TryGetInstance` / `GetOrCreateInstance` (and use `OrganizationSingletonHelper` where appropriate).
2. Add `tenant/<name>.json` with **one** `rows[]` entry and register in `tenant/manifest.json`.
3. Add entity name to `IsOrganizationSingletonEntity` and `OrganizationSingletonKeyProperty` in `LookupCatalogEntitySync`.
4. Register cleanup label in `LookupCatalogSyncUpdater.CleanupDuplicateOrganizationSingletons` if using a new key property.
5. Point reports/PDF/Word placeholders at the BO (not at JSON).
6. Document the file in this doc and [`LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md) tenant table.
