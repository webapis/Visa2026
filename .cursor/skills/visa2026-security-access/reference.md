# Visa2026 security — reference

Read when implementing or debugging permissions. Workflow: [SKILL.md](./SKILL.md).

---

## File map

| File | Role |
|------|------|
| `Visa2026.Module/DatabaseUpdate/Updater.cs` | Role creation, all `Ensure*` helpers, seeded users |
| `Visa2026.Blazor.Server/Startup.cs` | `PermissionPolicyRole`, `ApplicationUser`, `PermissionsReloadMode.NoCache` |
| `Visa2026.Module/BusinessObjects/ApplicationUser.cs` | User entity + roles collection |
| `Visa2026.Module/BusinessObjects/ReportVisibility.cs` | Per-report role visibility |
| `Visa2026.Module/Controllers/ShowReportController.cs` | Filters reports by role + `ReportVisibility` |
| `Visa2026.Module/Services/UserReports/UserReportTemplateEditAccess.cs` | `SecuritySystem.IsGranted` for templates |
| `Visa2026.Module/Services/RuntimeLogging/ApplicationRuntimeLogAdminHelper.cs` | Admin-only runtime log |
| `Visa2026.Blazor.Server/Controllers/*Controller.cs` | `[Authorize]` on download/batch/preview endpoints |
| `docs/ROLE_PERMISSIONS_GUIDE.md` | Human-readable matrix and scenarios |

---

## Helper methods (`Updater.cs`)

| Method | Behavior |
|--------|----------|
| `EnsureNavigationPermission` | Create nav row or update `NavigateState` |
| `EnsureTypePermission<T>` | Add **only if no type row** — single operation |
| `EnsureReadOnlyPermission<T>` | Read=Allow; Write/Create/Delete=Deny |
| `EnsureReadWriteOnlyPermission<T>` | Read+Write=Allow; Create/Delete=Deny |
| `EnsureReadWriteCreatePermission<T>` | Read+Write+Create=Allow; Delete=null |
| `EnsureCatalogManagePermission<T>` | Full CRUD for comma-separated catalog BOs |
| `EnsureFullAccessRecursivePermission<T>` | Full CRUD on existing or new row |
| `EnsureUserReportTemplateOfficerPermissions` | Templates + Reports nav |
| `EnsureUserFeedbackOfficerPermissions` | Create + read-own object permission |
| `EnsureApplicationRuntimeLogUserDeny` | Deny read on `ApplicationRuntimeLog` for Users |

Constants in `Updater`:

```csharp
ReadWriteCreateWithoutDelete = "Read;Write;Create"
ReadWriteCreateDelete = "Read;Write;Create;Delete"
```

`SecurityOperations.FullAccess` = `Read;Write;Create;Delete;Navigate`.

---

## Navigation path format

Paths mirror the XAF Model tree:

```
Application/NavigationItems/Items/{Group}
Application/NavigationItems/Items/{Group}/Items/{Item}
```

Examples from `CreateUserRole()`:

| Path suffix | Typical state (Users) |
|-------------|----------------------|
| `People`, `People/Items/Employees` | Allow |
| `Lookup` | Deny |
| `Application/Items/Application` (legacy combined list) | Deny |
| `Application/Items/Application_ViaMinistries` | Allow |
| `Reports/Items/UserReportTemplate` | Allow |
| `Operations/Items/UserFeedback` | Allow |
| `System/Items/ExpirationAlertRule` | Allow |

To find a path: open `Visa2026.Blazor.Server/Model.xafml` (or Module `Model.DesignedDiffs.xafml`), locate the navigation item `Id`, walk parent `NavigationItems` nodes.

---

## SQL inspection (SQL Server)

Replace `@db` with target database name.

**Roles:**

```sql
SELECT ID, Name, IsAdministrative
FROM PermissionPolicyRole;
```

**Type permissions for Users role:**

```sql
SELECT tp.TargetTypeFullName,
       tp.ReadState, tp.WriteState, tp.CreateState, tp.DeleteState
FROM PermissionPolicyTypePermissionObject tp
JOIN PermissionPolicyRole r ON r.ID = tp.RoleID
WHERE r.Name = N'Users'
ORDER BY tp.TargetTypeFullName;
```

**Navigation permissions:**

```sql
SELECT np.ItemPath, np.NavigateState
FROM PermissionPolicyNavigationPermissionObject np
JOIN PermissionPolicyRole r ON r.ID = np.RoleID
WHERE r.Name = N'Users'
ORDER BY np.ItemPath;
```

**User → roles:**

```sql
SELECT u.UserName, r.Name AS RoleName, r.IsAdministrative
FROM PermissionPolicyUser u
JOIN PermissionPolicyUserUsers_PermissionPolicyRoleRoles l ON l.UsersID = u.ID
JOIN PermissionPolicyRole r ON r.ID = l.RolesID
ORDER BY u.UserName, r.Name;
```

After deploy, re-run type/nav queries to confirm `Ensure*` applied (e.g. `WriteState` = 1 for Allow).

---

## Layered security checks

1. **XAF type/navigation/object permissions** — primary UI and ObjectSpace gates.
2. **`[Authorize]`** — ASP.NET auth on Blazor API controllers (batch download, culture, feedback API). User must be logged in; BO permissions still apply inside XAF actions.
3. **`SecuritySystem.IsGranted`** — explicit checks in services/controllers (e.g. `UserReportTemplateEditAccess`).
4. **`IsAdministrative` / role name checks** — rare admin-only UI (e.g. `UserFeedbackViewController`, `ApplicationRuntimeLogAdminHelper`).

When fixing “API works in debugger but button hidden”, check layers in order.

---

## Report visibility (two mechanisms)

1. **Type Read** on `ReportDataV2` — required to load report metadata.
2. **`ReportVisibility`** rows — link report name to allowed roles; `ShowReportController` filters menu.

Officers also need **navigation** to Reports where applicable. User-defined templates use `UserReportTemplate` type permissions + `EnsureUserReportTemplateOfficerPermissions`.

---

## Object-permission template (read own rows)

From `EnsureUserFeedbackOfficerPermissions`:

1. Type level: Create=Allow, Read/Write/Delete=Deny.
2. Object level: `AddObjectPermissionFromLambda<T>(Read, criteria on current user, Allow)`.
3. Navigation: Allow Operations path, Deny Default duplicate if needed.

Use the same pattern for future “submit only / read own” features.

---

## EF Core recursive permission caveat

`AddTypePermissionsRecursively<Person>(FullAccess)` does **not** always cover every persisted child type in EF security (e.g. `FileData`, `EducationDocument`, `MedicalRecordDocument`). Comments in `CreateUserRole()` document each gap — mirror with explicit `Ensure*` when adding new file/child aggregates.

---

## Deploy note

Permission changes apply when `Updater.UpdateDatabaseAfterUpdateSchema()` runs (app startup / DB update). No separate migration for permission rows. On environments where updaters were skipped, see `docs/ENVIRONMENTS.md` (`FORCE_XAF_DB_UPDATE`).
