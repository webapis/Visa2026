# Document expiration alert configuration (Phase A)

Officer-configurable **calendar-day** thresholds before `ExpirationDate` for expiring-soon ListView states and extension-application warnings.

## Where it lives

| Piece | Location |
|-------|----------|
| Business object | `Visa2026.Module/BusinessObjects/ExpirationAlertRule.cs` |
| Configuration filter (6 families) | `DocumentExpirationAlertConfigurationKeys.cs` |
| Tenant seed (all runtime keys incl. BorderZone) | `DatabaseUpdate/LookupCatalogs/tenant/expiration-alert-rules.json` |
| Runtime loader | `ExpirationAlertRuleHelper` → `StateEvaluationSettings` → `*StateEvaluator` |

**Navigation:** **Configuration → Document expiration alerts** (`[NavigationItem("Configuration")]`).

## Configuration scope (Phase A)

Six seeded rows editable in the Configuration list:

- Passport, Visa, WorkPermitItem, AddressOfResidence, MedicalRecord, Invitation

**Not in Configuration UI:** BorderZone and any future keys — remain in JSON seed for runtime only.

### Fields

| Field | Applies to |
|-------|------------|
| `ExpiringSoonDays` | All six families |
| `ExtensionApplicationRequiredDays` | Visa and WorkPermitItem only (hidden + cleared on save for others) |

`BusinessObjectKey` is hidden; officers use `DisplayName` only.

List/detail views: **no New / Delete** (model updater + controllers).

## Seeding

`expiration-alert-rules.json` uses **`InsertOnly`** sync — deploy adds missing keys; officer edits to day counts are not overwritten.

Defaults: **30** warn days; **90** extension-application days for Visa and WorkPermitItem.

## Roles

| Role | Type permission | Navigation |
|------|-----------------|------------|
| **VisaOffice** | Read + Write (no create/delete) | Configuration → ExpirationAlertRule |
| **Users** | Read only | No screen (runtime evaluators still read rules) |
| **Administrators** | Full | Configuration (implicit) |

Users with **Users + VisaOffice** (e.g. `tumar`) get write via VisaOffice when roles are merged.

## Related docs

- [`BO_STATE_TEMPORAL_TYPES.md`](BO_STATE_TEMPORAL_TYPES.md) — `DaysRemaining` vs migration SLA working days
- [`ROLE_PERMISSIONS_GUIDE.md`](ROLE_PERMISSIONS_GUIDE.md) — permission helpers
