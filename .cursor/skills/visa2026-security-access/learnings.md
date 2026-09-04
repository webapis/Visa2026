# visa2026-security-access — learnings

Append-only. Read before permission work; append after **verified** fixes (User role, not Admin-only).

- **2026-08-14 — VisaOffice could not delete Application Profile templates** — `EnsureReadWriteCreatePermission` leaves Delete null — `EnsureFullAccessRecursivePermission` on `ApplicationProfile` + nested legs/templates/state settings (`Updater.cs`). Catalog still blocks delete when any `ApplicationProfileInstance` is linked.

## Seeded from docs/ROLE_PERMISSIONS_GUIDE.md

- **2026-04 — `Show in Report` hidden for Users in production** — `ReportDataV2` / `ReportVisibility` only in creation block — `EnsureTypePermission<ReportDataV2>` and `EnsureTypePermission<ReportVisibility>` outside block (`Updater.cs`).
- **2026-04 — `EducationInstitution` / `Specialty` not editable for Users** — `PermissionSettingHelper.SetTypePermission` did not persist Allow on existing rows — `EnsureReadWriteCreatePermission<T>` helper (`Updater.cs`).
- **2026-04 — `EducationLevel` / `Country` should stay read-only for Users** — existing rows retained Write from older grants — `EnsureReadOnlyPermission<T>` strips Write/Create/Delete (`Updater.cs`).

---

## New entries

- **2026-08 — Report Dashboard Invitation/Registration/WP Overview Total 0 for officers** — data present in `vw_rd_*` but Users/UsersReadOnly/VisaOffice lacked Read on Invitation/Registration/WorkPermit/Education/Position/Passport/`VwRdProject` view BOs (only Visa + Application + PersonSearch views were granted) — secured ObjectSpace returned empty panels — expanded `EnsureReportDashboardOfficerPermissions` to all `VwRd*` types (`Updater.cs`, `docs/ROLE_PERMISSIONS_GUIDE.md`); apply on existing DBs via app restart after deploy (or one-shot SQL insert into `PermissionPolicyTypePermissionObject`).
- **2026-06 — Visa office configuration role** — officers need company/signatory/representative/numbering + project contracts + templates without super admin — added **`VisaOffice`** role with `EnsureVisaOfficeConfigurationPermissions` + `EnsureVisaOfficeNavigationPermissions`; seeded user `VisaOffice` (`Updater.cs`). Users role unchanged (shared template access).
- **2026-06 — Admin-only Operations screens** — `ApplicationRuntimeLog` + `StateNotifications` nav must not appear for Users/VisaOffice — `EnsureAdminOnlyOperationsDeny`; header state-notification badge gated like runtime log (`StateNotificationHeaderBadge.razor`).
- **2026-07 — Users cannot add/edit `VisaPeriod` from Application lookup** — type was Read-only in `CreateUserRole` with no `Ensure*` upgrade — `ReadWriteCreateWithoutDelete` in creation block + `EnsureReadWriteCreatePermission<VisaPeriod>` for existing roles; Lookup nav still denied (`Updater.cs`, `docs/ROLE_PERMISSIONS_GUIDE.md`).
- **2026-07 — Migration deadline blank for VisaOfficer / Users; Admin OK** — `ApplicationMigrationSlaProfile` was only on VisaOffice (full access); deny-by-default blocked `ApplicationType.MigrationSlaProfile` for officers so `ApplicationMigrationSlaHelper.Resolve` returned empty — `EnsureReadOnlyPermission<ApplicationMigrationSlaProfile>` on Users + UsersReadOnly (creation + Ensure); no Configuration nav (`Updater.cs`, `docs/ROLE_PERMISSIONS_GUIDE.md`).
- **2026-07 — UsersReadOnly (reader officers) need process-tracking ListView columns** — same SLA/status types as Users must be readable (Migration/Approval deadline & working days, Current status) — shared `EnsureApplicationProcessTrackingReadPermissions` for Users (`officerCanWriteProgress: true`) and UsersReadOnly (`false`); docs UsersReadOnly section (`Updater.cs`, `docs/ROLE_PERMISSIONS_GUIDE.md`).
- **2026-08-05 — Calik tenant officers still read-only after IIS cutover** — `tenant-users.json` assigned `UsersReadOnly` (parallel-period lockdown) — switched `tumar`, `gulshat`, `arzygul` to `Default` + `Users`; `TenantUserSeedUpdater` syncs roles on next app startup / deploy (`tenant-users.json`, `docs/ROLE_PERMISSIONS_GUIDE.md`).
- **2026-08-05 — Users cannot Mark incomplete / Mark complete on Person** — missing `PersonIncompleteMarkOptions` type grant + explicit member Write on `IsDataIncomplete` / `Incomplete*` fields for deny-by-default popup + save — `PersonIncompleteDataOfficerPermissions.Ensure` on Users role (`PersonIncompleteDataOfficerPermissions.cs`, `Updater.cs`).
- **2026-08-05 — Users Report Dashboard permissions not upgraded on existing DBs** — `EnsureTypePermission` only adds missing types; officers with stale Deny/empty Navigate on `VwRd*` or Read-only `ReportDashboardHost` saw zeros or blocked ListView — `ReportDashboardOfficerPermissions.Ensure` upgrades Read+Navigate on all 44 view BOs and Read/Write/Create on host (`ReportDashboardOfficerPermissions.cs`, `Updater.cs`); restart app after deploy.
