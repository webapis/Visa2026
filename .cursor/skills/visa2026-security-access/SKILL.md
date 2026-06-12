---
name: visa2026-security-access
description: >-
  Manages Visa2026 XAF security: PermissionPolicyRole grants in ModuleUpdater,
  navigation/type/member/object permissions, officer vs administrator roles,
  and fixing access-denied symptoms (hidden nav, disabled actions, save blocked,
  child BO not covered). Use when changing user roles, granting or denying access,
  ReportVisibility, object-scoped permissions, or debugging why Users role cannot
  see or edit something that works for Admin.
disable-model-invocation: false
---

# Visa2026: security and role permissions

## Scope

| Concern | Where it lives | This skill |
|---------|----------------|------------|
| **Role seed & deploy sync** | `Visa2026.Module/DatabaseUpdate/Updater.cs` → `CreateUserRole()`, `CreateAdminRole()`, `CreateDefaultRole()` | Primary edit surface |
| **Permission helpers** | `Ensure*` methods at bottom of `Updater.cs` | Use for **existing** production roles |
| **Security wiring** | `Visa2026.Blazor.Server/Startup.cs` (`PermissionPolicyRole`, `PermissionsReloadMode.NoCache`) | Read when permissions “stick” only after restart |
| **User BO** | `ApplicationUser`, `ApplicationUserLoginInfo` | Role assignment, not permission matrix |
| **Report menu filtering** | `ReportVisibility` + `ShowReportController` | Role-linked report visibility (separate from type Read) |
| **Code-level checks** | `SecuritySystem.IsGranted`, `[Authorize]` on Blazor controllers | Secondary gate after XAF permissions |

**Canonical doc:** [docs/ROLE_PERMISSIONS_GUIDE.md](../../../docs/ROLE_PERMISSIONS_GUIDE.md) — helper API, current Users matrix, known fixes.

**Implementation detail:** [reference.md](./reference.md) — file map, SQL inspection, nav paths, object-permission patterns.

**Experience log:** [learnings.md](./learnings.md) — append-only (read before work, append after verified permission fixes).

**Not this skill:**

| Symptom / task | Use instead |
|----------------|-------------|
| Unhandled 500, stack trace, `ApplicationRuntimeLog` | [visa2026-runtime-error-tracking](../visa2026-runtime-error-tracking/SKILL.md) |
| E2E login as `Admin` / `User` | [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md) |
| Deploy / restart after permission change | [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) or [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) |
| Lookup catalog data (not security) | [visa2026-lookup-data](../visa2026-lookup-data/SKILL.md) |

**Related:** [commit-after-verify](../commit-after-verify/SKILL.md) when committing permission changes.

---

## Roles and test users

| Role | `IsAdministrative` | Typical use |
|------|-------------------|-------------|
| **Administrators** | `true` | Super administrator — full bypass; do **not** verify officer access as Admin only |
| **Users** | `false` | Immigration officers — main permission matrix in `CreateUserRole()` |
| **VisaOffice** | `false` | Org singletons, project contracts, ministries, Resminamalar templates (`CreateVisaOfficeRole()`) |
| **Default** | `false` | Self-service profile / password / culture only |

Seeded accounts (`Updater.UpdateDatabaseAfterUpdateSchema`):

| User | Roles | Password (dev) |
|------|-------|----------------|
| `Admin` | Administrators | empty |
| `User`, `StandardUser` | Default + Users | empty |
| `VisaOffice` | Default + VisaOffice | empty |
| `tumar`, `gulshat`, `arzygul` | Default + Users + VisaOffice | empty (from `tenant/tenant-users.json`) |

**Verify officer fixes** by logging in as `User` or `StandardUser`, not `Admin`. Tenant officers: edit `LookupCatalogs/tenant/tenant-users.json` (see `TenantUserSeedUpdater`).

---

## Core rule: fresh DB vs production

Policy: **deny all by default** — every grant must be explicit.

```
if (userRole == null) { ... }   → runs ONLY when "Users" role is first created
Ensure*(userRole, ...)          → runs EVERY app startup — patches existing roles
```

**Any new permission for shipped environments must have an `Ensure*` call outside the `if (userRole == null)` block.**

`EnsureTypePermission<T>` only adds when **no type row exists** — it does **not** upgrade Read→Write. Use `EnsureReadWriteCreatePermission`, `EnsureReadOnlyPermission`, or `EnsureFullAccessRecursivePermission` to change existing rows.

---

## Symptom → diagnosis

```
Navigation group/item missing for officer?
  → Navigation permission (Allow or Deny) — see Updater EnsureNavigationPermission block
  → Path must match Model node: Application/NavigationItems/Items/...

Action visible but disabled / Save fails with security?
  → Type permission (Read/Write/Create/Delete) on BO or child type
  → EF: child rows (FileData, EducationDocument, …) often need explicit grants — not covered by parent recursive alone

Works for Admin, not for User?
  → Almost always missing Ensure* for Users role — Admin bypasses matrix

Report missing from Resminamalar / Show in Report?
  → ReportDataV2 Read + ReportVisibility rows + ShowReportController
  → UserReportTemplate: EnsureUserReportTemplateOfficerPermissions

Custom controller endpoint 401/403?
  → [Authorize] on Blazor controller + type permission for underlying BO

Object visible only for own rows?
  → Object permission (criteria / lambda) — see UserFeedback pattern in Updater
```

---

## Change workflow

### 1. Classify the grant

| Need | Fresh role (`if null`) | Existing roles (`Ensure*`) |
|------|------------------------|----------------------------|
| New BO, read only | `AddTypePermissionsRecursively<T>(Read, Allow)` | `EnsureTypePermission<T>(..., Read, Allow)` |
| Read + edit + create, no delete | `AddTypePermissionsRecursively<T>(ReadWriteCreateWithoutDelete, Allow)` | `EnsureReadWriteCreatePermission<T>` |
| Read + edit only | custom in creation block | `EnsureReadWriteOnlyPermission<T>` |
| Read only (strip write) | `AddTypePermissionsRecursively<T>(Read, Allow)` | `EnsureReadOnlyPermission<T>` |
| Full CRUD / recursive children | `AddTypePermissionsRecursively<T>(FullAccess, Allow)` | `EnsureFullAccessRecursivePermission<T>` |
| Comma-separated catalog popup CRUD | `AddTypePermissionsRecursively<T>(ReadWriteCreateDelete, Allow)` | `EnsureCatalogManagePermission<T>` |
| Nav item show/hide | `AddNavigationPermission(path, Allow/Deny)` | `EnsureNavigationPermission(role, path, state)` |
| Own rows only | `AddObjectPermissionFromLambda<T>(...)` | mirror in dedicated `Ensure*OfficerPermissions` helper |

### 2. Edit `Updater.cs`

- Put creation-block grants **inside** `if (userRole == null)`.
- Put production sync **after** the block, before `return userRole`.
- For cross-cutting bundles (templates, feedback, runtime log deny), add or extend named helpers (`EnsureUserReportTemplateOfficerPermissions`, etc.).

### 3. Build and verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Restart app (F5 or deploy). `UpdateDatabaseAfterUpdateSchema()` applies role patches on startup.

1. Log in as **User** (not Admin).
2. Confirm nav, open detail, save, delete (if denied by design).
3. If E2E covers the flow: `dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest` with officer login where relevant.

### 4. Document

- Update [docs/ROLE_PERMISSIONS_GUIDE.md](../../../docs/ROLE_PERMISSIONS_GUIDE.md) permission tables if the Users matrix changed materially.
- Append verified incident to [learnings.md](./learnings.md).

---

## Common pitfalls

1. **Only inside `if (userRole == null)`** — works on greenfield DB, fails on production.
2. **`EnsureTypePermission` for upgrades** — skips when row exists; use the correct `EnsureRead*` / `EnsureFull*` helper.
3. **Testing as Admin** — false positive; always verify Users role.
4. **Child types** — `FileData`, aggregated document rows, placeholder rows often need explicit grants (see comments in `CreateUserRole()`).
5. **Navigation Deny overrides** — explicit `EnsureNavigationPermission(..., Deny)` hides items even when type Read exists (e.g. Lookup group, legacy Application list).
6. **Delete required** — some flows need delete (e.g. `UserReportPlaceholder` extract); `ReadWriteCreateWithoutDelete` is insufficient.

---

## When to use object / member permissions

Use **type** permissions first. Add **object** or **member** permissions when:

- Officers may create but only read **their own** rows (`UserFeedback`).
- Officers may edit only specific members on self (`Default` role password / culture).
- Administrators need a type denied at type level but allowed by criteria (audit trail pattern on `Default`).

Follow existing lambda/criteria style in `CreateDefaultRole()` and `EnsureUserFeedbackOfficerPermissions`.

---

## Quick links

- Updater: `Visa2026.Module/DatabaseUpdate/Updater.cs`
- Guide: `docs/ROLE_PERMISSIONS_GUIDE.md`
- SQL / paths / helpers: [reference.md](./reference.md)
