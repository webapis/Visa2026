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

- **2026-06 — Visa office configuration role** — officers need company/signatory/representative/numbering + project contracts + templates without super admin — added **`VisaOffice`** role with `EnsureVisaOfficeConfigurationPermissions` + `EnsureVisaOfficeNavigationPermissions`; seeded user `VisaOffice` (`Updater.cs`). Users role unchanged (shared template access).
- **2026-06 — Admin-only Operations screens** — `ApplicationRuntimeLog` + `StateNotifications` nav must not appear for Users/VisaOffice — `EnsureAdminOnlyOperationsDeny`; header state-notification badge gated like runtime log (`StateNotificationHeaderBadge.razor`).
