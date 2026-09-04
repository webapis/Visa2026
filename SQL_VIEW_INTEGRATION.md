# Integration Guide: PostgreSQL views as business objects (Visa2026)

How to map a PostgreSQL view to an XAF business object in this project. Visa2026 is **PostgreSQL only** — the former SQL Server path (`SqlViewsUpdater` + plain `SqlViews/*.sql`) was removed with the `ApplicationItem` hard-remove; see [`docs/DEPRECATED.md`](docs/DEPRECATED.md).

## 1. Write the view SQL

Views live in `Visa2026.Module/SqlViews/` as `<view_name>.postgres.sql`, embedded via `<EmbeddedResource>` in `Visa2026.Module.csproj` (keep the `LogicalName` pattern `Visa2026.Module.SqlViews.<leaf>`). Start the script with `DROP VIEW IF EXISTS <name>;` then `CREATE VIEW <name> AS …`, and quote identifiers to match XAF/EF names (`"ApplicationProfileInstances"`, `"GCRecord"`).

Roster-based views must read the M2M roster — `ApplicationProfileInstancePeople` plus `ApplicationProfileInstancePersonResolvedLinks` (by `LinkKind`). Shared CTEs live in `ReportDashboardPostgresRosterSql`; a view whose whole body is defined there is redirected by `ReportDashboardSqlViewResource.Load`, so its `.postgres.sql` file is only a placeholder.

## 2. Create the view at deploy and at startup

Two places, because `ModuleUpdater` is skipped when `ModuleInfo` is already current:

- **`ReportDashboardPostgresViewsUpdater`** (registered in `Module.GetModuleUpdaters`) — add a `CreateView…()` method, either inline SQL or `ExecuteEmbeddedPostgresView("<leaf>.postgres.sql")`.
- **`ReportDashboardPostgresViewsHealSql`** — add the view to the matching array (`BaseViews`, `WrapperViews`, `StandaloneViews`, `RosterCascadeViews`, …) so a host start recreates it when it is missing or its definition is stale. Ordering matters: base views before wrappers.

Non-dashboard views follow the same shape (e.g. `ApplicationWorkspacePostgresViewsSql`).

## 3. Define the business object

Create the class in `Visa2026.Module/BusinessObjects`. It needs a `[Key]` property (XAF uses it for the open-record link) and must be read-only in the UI. Do **not** add `[DomainComponent]` — Domain Components are banned in this project (`.cursor/rules/visa2026-no-domain-components.mdc`).

```csharp
[NavigationItem("Reports")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdEmployeeSummary
{
    [Key]
    public Guid ID { get; set; }

    public string FullName { get; set; }
}
```

## 4. Map it in the DbContext

In `Visa2026DbContext.OnModelCreating`, map to the view so EF never creates a table:

```csharp
public DbSet<VwRdEmployeeSummary> VwRdEmployeeSummaries { get; set; }

modelBuilder.Entity<VwRdEmployeeSummary>()
    .ToView("vw_rd_employee_summary")
    .HasKey(t => t.ID);
```

Use the exact case of the view name. Unquoted PostgreSQL identifiers fold to lower case, so `vw_*` views are lower case while quoted `"View_*"` views keep their casing.

## 5. Troubleshooting

- **Row click does not open a DetailView:** the `[Key]` column must be unique in the view; duplicate IDs make EF and XAF behave unpredictably. For a view without a single unique column use `HasKey(v => new { v.ColumnA, v.ColumnB })`, but a single GUID key navigates better.
- **`42P01 relation … does not exist`:** the view SQL references a dropped table, or a base view is created after the wrapper that selects from it.
- **`2BP01 cannot drop column … because other objects depend on it`:** drop the dependent views first (see the `pg_depend` loop in `VisaIssuingApplicationSchemaSql`), then let the startup heal recreate them.
- **Column missing after a schema change:** the live view is stale. Add a sentinel-column check to the heal so it recreates the view instead of relying on a manual `psql` run.

## 6. Server-side calculated values

Prefer computing the value **inside the view** (`CASE`, date arithmetic such as `("ExpirationDate")::date - CURRENT_DATE`) so sorting and filtering stay in SQL. If a table column needs a server-side value, create the PostgreSQL function alongside the view DDL and map it with `HasComputedColumnSql` using PostgreSQL syntax — the old SQL Server scalar functions (`fn_CalculateDaysRemaining`, `fn_GetVisaRegistrationState`) were removed and have no PostgreSQL replacement yet.