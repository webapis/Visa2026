# Organization catalogs (2026-09-03, revised)

Company / Authorized Signatory / Authorized Representative are **multi-row catalogs**, not singletons. Each Application Profile Instance **relates** to one row of each. Officers pick at create; they can change the relation later on Case workspace Organization.

**Status:** shipped 2026-09-03 (slice **10z2** live FKs; **10z3** Configuration page + **+ New**; **10z4** inline + New / Edit on create and case).

## Locked for this set

| Decision | Choice |
|----------|--------|
| Singleton? | **No.** Many Company / Signatory / Representative rows. Application numbering stays a singleton. |
| Instance storage | **Live FKs** on the Application Profile Instance |
| Create | **Choose Organization** step: three dropdowns, pre-filled from tenant Default. Gear (top-right) reveals **+ New** and **Edit** (same catalog modal as Configuration) |
| Default | **Tenant-wide** one Default per list. Pre-fills the next new application. **Make default** on create, case Organization, and Configuration lists |
| Case Organization | Overview is read-only. **Edit** shows dropdowns plus **+ New** / pencil on each column. Field values stay a read-only preview. Configuration lists remain for bulk management |
| Merge | Resminamalar / Word / PDF read the **selected catalog records** on generate. Editing a catalog row updates **every case that selected it** |
| Other cases | Unchanged unless they selected the **same** catalog row |

This replaces the 2026-09-03 per-instance **copied scalars** letterhead model.

## Screens

| File | State |
|------|--------|
| [application-profile-instance-create-choose-organization-prototype.png](./application-profile-instance-create-choose-organization-prototype.png) | Create: Choose Organization (3 dropdowns, Default, Make default; gear reveals **+ New / Edit**) |
| [application-profile-instance-organization-overview-prototype.png](./application-profile-instance-organization-overview-prototype.png) | Case Overview **Organization** — selected records, section **Edit** only |
| [application-profile-instance-organization-edit-prototype.png](./application-profile-instance-organization-edit-prototype.png) | Case Organization Edit — dropdown per column, **+ New / pencil**, read-only fields |
| [application-profile-organization-catalogs-prototype.png](./application-profile-organization-catalogs-prototype.png) | Configuration → Organization catalogs (+ New, search, Default, pencil) |

## Not in this set

- Application numbering remains one row
- Per-Application-Profile defaults (rejected: tenant-wide Default)
- Editing Name / Passport as **instance scalars** (rejected: + New / Edit opens the shared catalog record; that still updates every case that selected it)