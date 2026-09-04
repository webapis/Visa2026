# Application progress — migration service SLA (Phase 2)

Locked design for migration-service working-day SLA on the instance ListView and case workspace. Ministry leg SLA remains on profile `MinistrySlaDays` / snapshots; this document covers **migration only**.

## Configuration

Source of truth: **`ApplicationProfile.MigrationSlaDays`** (wizard **Process & SLA**).

The retired tenant lookup `ApplicationMigrationSlaProfile` and `ApplicationType.MigrationSlaProfile` were dropped by `ApplicationMigrationSlaProfileDropSchemaUpdater`. Do not re-add the catalog.

## Runtime SLA

Helper: **`ApplicationMigrationSlaHelper`** (max days from `ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays`).

| Event | Detail |
|-------|--------|
| Clock starts | Latest step is `PROCESS_STARTED` (includes direct-to-migration cases) |
| Clock ends | Terminal: `PROCESS_ISSUED`, `PROCESS_REJECTED`, `PROCESS_CANCELLED` |
| Working days | Mon–Fri via `WorkingDaysHelper` |
| Overdue | Working days &gt; `MigrationSlaDays` |

ListView columns on the instance (mirror ministry):

- `WorkingDaysInMigrationStep`
- `MigrationSlaStatement`

Row color: **`ProgressSlaAppearanceCode`** — ministry SLA or migration SLA wins when active; same `APP_PROGRESS_SLA_WARNING` / `APP_PROGRESS_SLA_OVERDUE` registry keys.

Case workspace **SLA & deadlines** uses the same max days via `ApplicationWorkspaceSlaDashboardBuilder`.

## Validation

| Rule | Behavior |
|------|----------|
| Progress save | **Block** moving to `PROCESS_STARTED` when `ApplicationProfile.MigrationSlaDays` ≤ 0 |

## Tests

- `ApplicationMigrationSlaHelperTests`
- `ApplicationProfileConfigurationResolverTests` (migration days)

```powershell
dotnet build Visa2026.Module/Visa2026.Module.csproj -c Debug
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --filter "FullyQualifiedName~Sla|FullyQualifiedName~Migration"
```

## Related

- [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md) — transition graph
- Ministry SLA — `ApplicationProgressSlaHelper`, contract / profile snapshots
- Skill: `.cursor/skills/visa2026-application-profile/SKILL.md` (profile days) · `.cursor/skills/visa2026-application-progress/SKILL.md` (transitions)
