# visa2026-security-access — learnings

Append-only. Read before permission work; append after **verified** fixes (User role, not Admin-only).

Format: `YYYY-MM-DD — symptom — cause — fix (files)`.

---

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
