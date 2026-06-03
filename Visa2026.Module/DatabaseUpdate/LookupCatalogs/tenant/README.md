# Tenant / company-specific lookup catalogs

Rows here are **not** shared across all Visa2026 installations. They belong to one deploying organization.

**Multi-row catalogs:** Position, Specialty, EducationInstitution, Department, Ministry, ProjectContract, **BorderZoneName**, **WorkPermittedLocationName**, **Lodging**.

| JSON file | Entity | Used by |
|-----------|--------|---------|
| `border-zone-name.json` | `BorderZoneName` | `ApplicationItem.BorderZoneLocation`, `Visa.BorderZoneLocation` (comma-separated multi-select) |
| `work-permitted-location-name.json` | `WorkPermittedLocationName` | `ApplicationItem.WorkPermittedLocations`, `WorkPermitItem.WorkPermittedLocations` |
| `lodging.json` | `Lodging` | `AddressOfResidence` / registration lodging (`FullAddress`) |

See [`docs/COMMA_SEPARATED_MULTI_SELECT.md`](../../../../docs/COMMA_SEPARATED_MULTI_SELECT.md).

**Organization singletons** (one row per deployment; special sync — not inferred from filename):

| JSON file | Entity |
|-----------|--------|
| `company-profile.json` | `CompanyProfile` |
| `application-numbering.json` | `ApplicationNumberingProfile` |
| `authorized-signatory.json` | `AuthorizedSignatory` |
| `authorized-representative.json` | `AuthorizedRepresentative` |

Full behavior: [`docs/LOOKUP_ORGANIZATION_SINGLETONS.md`](../../../../docs/LOOKUP_ORGANIZATION_SINGLETONS.md).

- `manifest.json` here is merged with global `LookupCatalogs/manifest.json` on app startup.
- JSON files load from embedded resources (this repo) and/or `{AppBase}/LookupCatalogs/tenant/` on disk.

For a new customer deployment, replace these files with that customer's data.

General lookup architecture: [`docs/LOOKUP_SEEDING.md`](../../../../docs/LOOKUP_SEEDING.md).
