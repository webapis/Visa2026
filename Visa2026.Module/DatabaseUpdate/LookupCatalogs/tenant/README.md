# Tenant / company-specific lookup catalogs

Rows here are **not** shared across all Visa2026 installations. They belong to one deploying organization.

**Catalogs in this folder:** Position, Specialty, EducationInstitution, Department, Ministry, Company, ProjectContract.

- `manifest.json` here is merged with global `LookupCatalogs/manifest.json` on app startup.
- JSON files load from embedded resources (this repo) and/or `{AppBase}/LookupCatalogs/tenant/` on disk.

For a new customer deployment, replace these files with that customer's data.

Full architecture and workflows: [`docs/LOOKUP_SEEDING.md`](../../../../docs/LOOKUP_SEEDING.md).
