# Multi-company legacy import

## Naming (important)

| Term | Meaning |
|------|---------|
| **VISA2014** | Legacy **application** and git repo; CLI flags (`--import-visa2014`) and folder `legacy/visa2014/` |
| **VISA2015** | Legacy **SQL Server database** name (`Database=VISA2015` in connection strings) |
| **Not used** | There is no database named `VISA2014` or `VISA2025` in this migration |

Visa2026 is **single-tenant per deployment** (one `CompanyProfile`, one set of tenant lookup catalogs). Legacy production data from the old VISA system is imported **per company**:

| Company | Legacy SQL DB (attached name) | CLI `--legacy-source` |
|---------|--------------------------------|----------------------|
| **Çalik Enerji** (current focus) | **`VISA2015`** on `.\SQLEXPRESS` | `calik-energi` (default) |
| **Gap İnşaat** (future) | separate attach / snapshot | `gap-insaat` |

The SQL database **name** (`VISA2015`) is not the company name — Çalik production data may live in a database still named `VISA2015` on disk. Use **`--legacy-source`** to pick company mapping, not the database name alone.

Shared catalogs (Gender, Country, MaritalStatus, Relationship) live in [`lookup-translations.yaml`](lookup-translations.yaml). Company-specific files are **merged on top** (later file wins for the same `targetCatalog`).

**Canonical machine-readable profiles:** [`legacy-sources.yaml`](../../Visa2026.DataImporter/legacy/visa2014/legacy-sources.yaml)

---

## Rules

1. **One legacy database → one Visa2026 deployment** — do not mix Gap and Çalik rows in the same target database without explicit multi-tenant design (not supported).
2. **Separate id-maps** — `id-maps/calik-energi/Person.json` vs `id-maps/gap-insaat/Person.json`.
3. **Separate preview workbooks** — `Person-preview.calik-energi.xlsx` vs `Person-preview.gap-insaat.xlsx`.
4. **`importConfirmed`** is per entity **and** per legacy source (see `order.yaml` `legacySource` + notes).
5. **ProjectContract** differs by company:
   - **Çalik:** `identityPassThrough` — legacy `NumberOfContract` → same Visa2026 `Code` (tenant catalog).
   - **Gap:** experimental GT-15 remap when importing Gap legacy into a Calik-style tenant seed (see `lookup-translations.gap-insaat.yaml`).

---

## CLI

```powershell
# Çalik (default) — VISA2015 attached on SQLEXPRESS
dotnet run --project Visa2026.DataImporter -c Debug -- `
  --export-visa2014-preview --entity Person

# Explicit source
dotnet run --project Visa2026.DataImporter -c Debug -- `
  --export-visa2014-preview --entity Person --legacy-source calik-energi

# Gap snapshot (separate VISA2015 .mdf when available)
dotnet run --project Visa2026.DataImporter -c Debug -- `
  --export-visa2014-preview --entity Person --legacy-source gap-insaat

# Override connection
dotnet run --project Visa2026.DataImporter -c Debug -- `
  --export-visa2014-preview --entity Person --connection "Server=.\SQLEXPRESS;Database=VISA2015;User Id=ReadOnlyUser;TrustServerCertificate=True"
```

Environment variables:

- `VISA2014_LEGACY_SOURCE` — `calik-energi` | `gap-insaat`
- `VISA2014_SQL_CONNECTION` — full connection string override
- `VISA2014_SQL_PASSWORD` — password for `ReadOnlyUser` (or any `User Id=` in connection string). **Set in Windows user environment — never commit.**

Default SQL auth in `legacy-sources.yaml`: `User Id=ReadOnlyUser` on `VISA2015`; password injected from `VISA2014_SQL_PASSWORD` at runtime.

OData import uses the same `--legacy-source` flag (`--import-visa2014`).

---

## Target database (local dev)

Çalik pilot target: **LocalDB** `(localdb)\mssqllocaldb`, database **`Visa2026`** — configured in `Visa2026.Blazor.Server/appsettings.Development.json`. Start Blazor.Server before OData import.

---

## Attach legacy database (SSMS)

1. Detach old company DB if replacing files on the same instance.
2. Attach `.mdf` / `.ldf` as database **`VISA2015`** (standard legacy catalog name).
3. Refresh Object Explorer (F5); confirm database name matches `legacy-sources.yaml`.
4. Re-run schema snapshot + Person Excel preview before `importConfirmed`.

---

## Related

- [IMPORT_PLAN_AND_STRATEGY.md](IMPORT_PLAN_AND_STRATEGY.md)
- [LOOKUP_RESOLUTION_STRATEGY.md](LOOKUP_RESOLUTION_STRATEGY.md)
- [migration-status.yaml](migration-status.yaml)
