# Tenant / company-specific lookup catalogs

Rows here are **not** shared across all Visa2026 installations. They belong to one deploying organization.

**Catalogs in this folder:** Position, Specialty, EducationInstitution, Department, Ministry, **CompanyProfile**, ProjectContract.

**`company-profile.json`** seeds the organization singleton (replaces legacy `company.json`). **`project-contract.json`** matches contracts by **Name** or **Code** (`CodeOrName`); there is no company FK on `ProjectContract` after Phase 5.

- `manifest.json` here is merged with global `LookupCatalogs/manifest.json` on app startup.
- JSON files load from embedded resources (this repo) and/or `{AppBase}/LookupCatalogs/tenant/` on disk.

For a new customer deployment, replace these files with that customer's data.

Full architecture and workflows: [`docs/LOOKUP_SEEDING.md`](../../../../docs/LOOKUP_SEEDING.md).
