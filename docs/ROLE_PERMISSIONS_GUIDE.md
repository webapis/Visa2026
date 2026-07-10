# Role Permissions Guide

Reference for adjusting XAF security permissions in `Visa2026.Module/DatabaseUpdate/Updater.cs`.

**Agent workflow:** [`.cursor/skills/visa2026-security-access/SKILL.md`](../.cursor/skills/visa2026-security-access/SKILL.md) (symptom triage, verify as `User`, cross-links to deploy/E2E skills).

---

## How Permissions Work

The app uses **"Deny all by default"** policy. Every permission must be explicitly granted.  
Roles are managed in `Updater.cs` → `CreateAdminRole()`, `CreateUserRole()`, `CreateUsersReadOnlyRole()`, `CreateVisaOfficeRole()`, `CreateDefaultRole()`.

| Role | Purpose |
|------|---------|
| **Administrators** | Super administrator (`IsAdministrative = true`) — full bypass |
| **Users** | Case officers — applications, people, operational workflows |
| **UsersReadOnly** | Parallel-period case officers — view/search only (legacy sync window) |
| **VisaOffice** | Organization / tenant configuration + Resminamalar templates |
| **Default** | Self-service profile, password, culture |

**Tenant users:** `LookupCatalogs/tenant/tenant-users.json` — per-deployment officer accounts (`Updater` + `TenantUserSeedUpdater`). Syncs the **exact** role list on every DB update (adds missing roles, **removes** roles not listed); does not change passwords on existing accounts. During legacy parallel period use **`UsersReadOnly`** instead of `Users` + `VisaOffice`; restore full roles on cutover.

There are two places to set permissions:

| Location | Purpose |
|---|---|
| Inside `if (userRole == null)` block | Runs only when role is **first created** (fresh DB) |
| Outside the block (using `Ensure*` helpers) | Runs on **every startup** — covers existing roles too |

> **Rule of thumb:** Any new permission grant must go **outside** the `if (userRole == null)` block using an `Ensure*` helper so it is applied to existing production roles on next deploy.

---

## Available Helper Methods

### `EnsureNavigationPermission`
Grants or updates a navigation item permission for an existing role.
```csharp
EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/People", SecurityPermissionState.Allow);
```

### `EnsureTypePermission<T>`
Adds a single-operation type permission only if **no permission record** yet exists for that type.  
Use for **new types** not yet in the role at all.
```csharp
EnsureTypePermission<ReportDataV2>(userRole, SecurityOperations.Read, SecurityPermissionState.Allow);
```

### `EnsureReadOnlyPermission<T>`
Enforces **Read=Allow, Write/Create/Delete=null** on an existing permission row.  
If no row exists, creates one with Read only.  
Use when you want to lock a type to read-only for an existing role.
```csharp
EnsureReadOnlyPermission<EducationLevel>(userRole);
```

### `EnsureReadWriteCreatePermission<T>`
Sets `Read=Allow`, `Write=Allow`, `Create=Allow`, `Delete=null` on an existing permission row.  
If no row exists, it creates one.  
Use when you want **Read + Write + Create but no Delete** on an existing role.
```csharp
EnsureReadWriteCreatePermission<EducationInstitution>(userRole);
```

---

## Common Operations Reference

| Constant | Value | Meaning |
|---|---|---|
| `SecurityOperations.Read` | `"Read"` | View records |
| `SecurityOperations.Write` | `"Write"` | Edit existing records |
| `SecurityOperations.Create` | `"Create"` | Create new records |
| `SecurityOperations.Delete` | `"Delete"` | Delete records |
| `SecurityOperations.ReadWriteAccess` | `"Read;Write"` | Read + Edit |
| `SecurityOperations.FullAccess` | `"Read;Write;Create;Delete;Navigate"` | Everything |

Combined string for Read+Write+Create (no Delete):
```csharp
private static readonly string ReadWriteCreateWithoutDelete =
    $"{SecurityOperations.Read};{SecurityOperations.Write};{SecurityOperations.Create}";
```

---

## Current VisaOffice Role Permissions

Tenant configuration (`LookupCatalogs/tenant/*.json`) and user report templates. Seeded user: **`VisaOffice`** (`Default` + `VisaOffice`). Does **not** include case processing (Application, Person, …).

### Read + Write (no Create/Delete) — organization singletons

| Type |
|------|
| `CompanyProfile`, `AuthorizedSignatory`, `AuthorizedRepresentative`, `ApplicationNumberingProfile` |

### Read + Write + Create (no Delete)

| Type |
|------|
| `ProjectContract`, `FileData` |
| `UserReportTemplate`, `UserReportTemplateApplicationType`, `UserReportTemplateProjectContract` |

### Full recursive (templates)

| Type | Access |
|------|--------|
| `UserReportPlaceholder` | Read, Write, Create, Delete (extract placeholders) |
| `ApplicationMigrationSlaProfile` | Full (migration SLA tiers + link application types) |
| `SystemSettings` | Full (singleton upload size limits) |
| `ExpirationAlertRule` | Read + Write (edit warn/extension day counts; rows seeded) |

### Read only

| Type | Notes |
|------|-------|
| `OrganizationType`, `ReportDataV2`, `ReportVisibility`, `PdfFormMapping` | |
| `ApplicationType` | Link popup on migration SLA profile detail |

### Navigation (allow)

| Area |
|------|
| **Configuration** — company, signatory, representative, numbering, project contracts, ministries, migration SLA profiles, document expiration alerts, upload limits |
| **Reports** — `UserReportTemplate` |

---

## Current Users Role Permissions

### Full Access
| Type | Access |
|---|---|
| `Person` | Full |
| `Application` | Full |
| `ApplicationItem` | Full |
| `Passport` | Full |
| `Visa` | Full |
| `Registration` | Full |
| `Invitation` | Read, Write, Create |
| `InvitationItem` | Read, Write, Create |
| `BorderZone` | Read, Write, Create |
| `BorderZoneItem` | Read, Write, Create |

### Read + Write + Create (no Delete)
| Type | Access |
|---|---|
| `EducationInstitution` | Read, Write, Create |
| `Specialty` | Read, Write, Create |
| `Subcontractor` | Read, Write, Create |
| `VisaPeriod` | Read, Write, Create |
| `Rejection` | Read, Write, Create |
| `RejectionItem` | Read, Write, Create |
| `WorkPermit` | Read, Write, Create |
| `WorkPermitItem` | Read, Write, Create |

### Read Only
| Type |
|---|
| `ApplicationTypeFilter`, `ApplicationType`, `ApplicationState`, `ApplicationLocation` |
| `ApplicationMigrationSlaProfile` | Read only (Migration deadline column via `ApplicationType.MigrationSlaProfile`; no Configuration nav) — also via `EnsureApplicationProcessTrackingReadPermissions` |
| `CheckPoint`, `Country`, `Department`, `EducationLevel`, `Gender`, `MaritalStatus` |
| `MigrationService`, `OrganizationType`, `PassportType`, `Position`, `PurposeOfTravel` |
| `Region`, `Relationship`, `Urgency`, `ValidityDuration` |
| `VisaCategory`, `VisaIssuedPlace`, `VisaType` |
| `WorkPermitLocation`, `MovementPermitLocation`, `BorderZoneLocation` |
| `Company`, `ProjectContract` |
| `ExpirationAlertRule` | Read only (runtime state evaluators; no Configuration nav) |
| `ReportDataV2`, `ReportVisibility` |

---

## Current UsersReadOnly Role Permissions

Parallel-period / reader officers (`UsersReadOnly` — same navigation as Users for Applications, People, etc.; no Configuration / Reports / Operations). All business types are **read-only**.

### Process-tracking reads (same columns as Users)

Shared helper `EnsureApplicationProcessTrackingReadPermissions` — required for Application ListView:

| Column / data | Types |
|---|---|
| Migration deadline / working days | `ApplicationMigrationSlaProfile` (via `ApplicationType`) |
| Approval deadline / working days | `ApplicationApprovalLegSnapshot`, `ApprovingMinistry`, `ApprovalLegProfile` (+ legs) |
| Current status | `ApplicationProgress`, `ApplicationState`, `ApplicationLocation`, `MigrationService` |
| Project/Contract | `ProjectContract`, `ProjectContractApprovalLegProfile` |

---

## How to Add a New Permission

### Scenario A — New type, fresh + existing roles
Add inside the creation block AND use an `Ensure*` helper outside:
```csharp
// Inside if (userRole == null):
userRole.AddTypePermissionsRecursively<MyNewType>(SecurityOperations.Read, SecurityPermissionState.Allow);

// Outside (for existing roles):
EnsureTypePermission<MyNewType>(userRole, SecurityOperations.Read, SecurityPermissionState.Allow);
```

### Scenario B — Read/Write/Create on a type (no Delete)
```csharp
// Inside if (userRole == null):
userRole.AddTypePermissionsRecursively<MyNewType>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);

// Outside (for existing roles):
EnsureReadWriteCreatePermission<MyNewType>(userRole);
```

### Scenario C — Navigation item
```csharp
// Inside if (userRole == null):
userRole.AddNavigationPermission(@"Application/NavigationItems/Items/MyGroup/Items/MyItem", SecurityPermissionState.Allow);

// Outside (for existing roles):
EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/MyGroup/Items/MyItem", SecurityPermissionState.Allow);
```

---

## After Changing Permissions

1. Commit and push to `droplet` branch.
2. Deploy and restart the app — `UpdateDatabaseAfterUpdateSchema()` runs automatically on startup.
3. Log in with a `Users` role account and verify the change.
4. If the role already exists on production and the permission isn't applying, check that the `Ensure*` call is **outside** the `if (userRole == null)` block.

---

## Known Issues & Fixes

| Date | Issue | Fix |
|---|---|---|
| Apr 2026 | `Show in Report` hidden for `Users` role in production | Added `EnsureTypePermission<ReportDataV2>` and `EnsureTypePermission<ReportVisibility>` outside creation block |
| Apr 2026 | `EducationInstitution` and `Specialty` not editable for `Users` role | Added `EnsureReadWriteCreatePermission<T>` helper; `PermissionSettingHelper.SetTypePermission` did not correctly persist Allow states |
| Apr 2026 | `EducationLevel` and `Country` enforced as read-only for `Users` role | Added `EnsureReadOnlyPermission<T>` helper to explicitly strip Write/Create/Delete from existing roles |
| Jun 2026 | Visa office staff need org singleton + project contract + template screens without full admin | Added **`VisaOffice`** role (`CreateVisaOfficeRole`, `EnsureVisaOfficeConfigurationPermissions`, `EnsureVisaOfficeNavigationPermissions`); seeded user `VisaOffice` |
| Jun 2026 | Runtime log + state-notification inbox visible to officers | **`EnsureAdminOnlyOperationsDeny`** on Users / VisaOffice — super administrators only; state inbox remains a future-release prototype |
| Jun 2026 | Visa office cannot open migration SLA profiles or system settings | **`EnsureFullAccessRecursivePermission`** for `ApplicationMigrationSlaProfile` and `SystemSettings` on VisaOffice; nav allow for those items; `ApplicationType` read for link popup |
| Jul 2026 | Migration deadline empty for VisaOfficer / Users; Admin OK | **`EnsureReadOnlyPermission<ApplicationMigrationSlaProfile>`** on Users + UsersReadOnly (creation block + Ensure); officers need Read to resolve `ApplicationType.MigrationSlaProfile` for ListView SLA text |
| Jul 2026 | UsersReadOnly (reader) must see process-tracking columns | Shared **`EnsureApplicationProcessTrackingReadPermissions`** for Users + UsersReadOnly (migration/approval SLA, progress state/location, snapshots); Users keep write on progress/snapshots |
| Jun 2026 | Document expiration thresholds under System nav for all Users | **`ExpirationAlertRule`** moved to **Configuration**; VisaOffice read/write + nav; Users read-only without System nav ([`DOCUMENT_EXPIRATION_ALERT_CONFIGURATION.md`](DOCUMENT_EXPIRATION_ALERT_CONFIGURATION.md)) |
