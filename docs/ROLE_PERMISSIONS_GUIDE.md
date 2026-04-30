# Role Permissions Guide

Reference for adjusting XAF security permissions in `Visa2026.Module/DatabaseUpdate/Updater.cs`.

---

## How Permissions Work

The app uses **"Deny all by default"** policy. Every permission must be explicitly granted.  
Roles are managed in `Updater.cs` → `CreateUserRole()`.

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
| `Invitation` | Full |
| `InvitationItem` | Full |

### Read + Write + Create (no Delete)
| Type | Access |
|---|---|
| `EducationInstitution` | Read, Write, Create |
| `Specialty` | Read, Write, Create |

### Read Only
| Type |
|---|
| `ApplicationTypeFilter`, `ApplicationType`, `ApplicationState`, `ApplicationLocation` |
| `CheckPoint`, `Country`, `Department`, `EducationLevel`, `Gender`, `MaritalStatus` |
| `MigrationService`, `OrganizationType`, `PassportType`, `Position`, `PurposeOfTravel` |
| `Region`, `Relationship`, `Subcontractor`, `Urgency`, `ValidityDuration` |
| `VisaCategory`, `VisaIssuedPlace`, `VisaPeriod`, `VisaType` |
| `WorkPermitLocation`, `MovementPermitLocation`, `BorderZoneLocation` |
| `Company`, `Ministry`, `ProjectContract` |
| `ReportDataV2`, `ReportVisibility` |

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
