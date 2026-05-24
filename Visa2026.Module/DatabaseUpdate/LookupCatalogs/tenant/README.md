# Tenant / company-specific lookup catalogs

Rows here are **not** shared across all Visa2026 installations. They belong to one deploying organization.

**Catalogs in this folder:** Position, Specialty, EducationInstitution, Department, Ministry, Company, ProjectContract.

**`Company` column on `project-contract.json`:** matches the legacy **`Company`** lookup row **`Name`** (tenant `company.json`), which should stay aligned with **`CompanyProfile.Name`** after deploy. `ProjectContract.Company` is a legacy FK until Phase 5; runtime reports use **`CompanyProfile`** singletons.

- `manifest.json` here is merged with global `LookupCatalogs/manifest.json` on app startup.
- JSON files load from embedded resources (this repo) and/or `{AppBase}/LookupCatalogs/tenant/` on disk.

For a new customer deployment, replace these files with that customer's data.

Full architecture and workflows: [`docs/LOOKUP_SEEDING.md`](../../../../docs/LOOKUP_SEEDING.md).
