# Application progress — migration service SLA (Phase 2)

Locked design for migration-service working-day SLA on `Application` ListView. Ministry leg SLA (Phase 1) remains on contract snapshots; this document covers **migration only** — no office-prep `IS_BEING_PREPARED` @ `AT_OFFICE` SLA yet.

## Profiles

Tenant lookup BO: **`ApplicationMigrationSlaProfile`** (`Lookup → Application → Config`).

| Property | Role |
|----------|------|
| `Code` | Stable key (seed + `ApplicationTypeConfigurationCatalog.json`) |
| `NameTm` | Officer-facing tier label |
| `MaxDaysInReview` | Max working days at migration service (source of truth) |
| `WarningDaysBeforeMax` | Early warning threshold (must be &lt; max) |

Officers may add/edit tiers in the UI. Initial seed: `tenant/application-migration-sla-profile.json` (manifest version bumped when added).

| Code | Max days | Warn before |
|------|----------|-------------|
| `UP-TO-3-DAYS` | 3 | 2 |
| `UP-TO-ONE-WEEK` | 5 | 4 |
| `UP-TO-TWO-WEEKS` | 10 | 8 |
| `UP-TO-ONE-MONTH` | 20 | 16 |
| `UP-TO-45-DAYS` | 45 | 36 |

## ApplicationType mapping

- FK: `ApplicationType.MigrationSlaProfile` (`MigrationSlaProfileId`)
- Seed: `MigrationSlaProfileCode` on every row in `ApplicationTypeConfigurationCatalog.json` (validation / cross-check)
- Deploy link sync: nested `ApplicationTypeNames` on `tenant/application-migration-sla-profile.json` via `ApplicationMigrationSlaProfileTypeLinkCatalogSync` (authoritative for FK links; runs after profiles + application types)

Draft tier mapping (business defaults):

| Tier | Application types |
|------|-------------------|
| `UP-TO-3-DAYS` | All `App_Cancel_*` (including `App_Cancell_WP`) |
| `UP-TO-TWO-WEEKS` | All `App_Reg_*`, `App_Business_Trip_*`, `App_Change_Passport`, `App_Border_Zone_Permission` |
| `UP-TO-ONE-MONTH` | Invitations, visa/WP extensions and changes, service passport, exit visa, additional WP location, etc. |

## Runtime SLA

Helper: **`ApplicationMigrationSlaHelper`** (parallel to `ApplicationProgressSlaHelper`).

| Event | Detail |
|-------|--------|
| Clock starts | Latest step is `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE` (includes `DirectToMigrationService` types) |
| Clock ends | Terminal: `PROCESS_ISSUED`, `PROCESS_REJECTED`, `PROCESS_CANCELLED` |
| Working days | Mon–Fri via `WorkingDaysHelper` (same as ministry) |
| Warning | Working days &gt; `WarningDaysBeforeMax` and ≤ `MaxDaysInReview` |
| Overdue | Working days &gt; `MaxDaysInReview` |

ListView columns on `Application` (mirror ministry):

- `WorkingDaysInMigrationStep`
- `MigrationSlaStatement`

Row color: **`ProgressSlaAppearanceCode`** — ministry SLA (`{n}_REVIEW_APPROVED`) or migration SLA wins when active; same `APP_PROGRESS_SLA_WARNING` / `APP_PROGRESS_SLA_OVERDUE` registry keys as ministry (only one SLA active at a time).

## Validation

| Rule | Behavior |
|------|----------|
| Progress save | **Block** moving to `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE` when type has no profile or `MaxDaysInReview` ≤ 0 |
| ApplicationType detail | **Warn** (non-blocking) on save when `MigrationSlaProfile` is missing |
| Profile delete | **Block** when any `ApplicationType` references the profile (`DeleteBehavior.Restrict` + controller) |

## Tests

- `ApplicationMigrationSlaHelperTests`
- `ApplicationMigrationSlaProfileCatalogLoaderTests`

```powershell
dotnet build Visa2026.Module/Visa2026.Module.csproj -c Debug
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --filter "FullyQualifiedName~Sla|FullyQualifiedName~Migration"
```

## Related

- [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md) — transition graph
- Ministry SLA — `ApplicationProgressSlaHelper`, contract leg snapshots
- Skill: `.cursor/skills/visa2026-application-progress/SKILL.md`
