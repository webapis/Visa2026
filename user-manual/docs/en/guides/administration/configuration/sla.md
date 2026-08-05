---
title: SLA settings
slug: administration/configuration/sla
locale: en
tier: 8
guideStatus: draft
bo: ApplicationMigrationSlaProfile
navPath: Configuration
roles: [Administrator, VisaOffice]
prerequisiteSlugs:
  - administration/configuration/overview
  - applications/progress
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/APPLICATION_PROGRESS_STATE_VALIDATION.md
---

# SLA settings

Configure **working-day** service-level thresholds for application workflow. Two **Configuration** items apply at different stages:

| Menu item | Record type | Applies when |
|-----------|-------------|--------------|
| **Ministry review SLA** | Singleton | Each **ministry review leg** (from prior step until `{n}_REVIEW_APPROVED`) |
| **Application Migration Sla Profile** | Catalog | **Migration service** step (`PROCESS_STARTED` @ `AT_MIGRATION_SERVICE`) per **application type** |

SLA values support early **warning** days and a **maximum working days** cap. Warnings must be **less than** the maximum.

!!! tip "Officer view"
    Officers record progress in [Track application progress](../../applications/progress.md). SLA settings define when the system flags delays — they do not auto-advance workflow.

## Ministry review SLA

Default SLA copied to each application when it selects an **Approval Leg Profile** (snapshot on the application).

### Edit the singleton

1. **Configuration → Ministry review SLA**.
2. Open the single row.
3. Set:
   - **Max working days** — allowed working days per ministry leg (required, > 0)
   - **Warning (working days)** — optional early warning before the max (must be < max)
4. **Save**.

Changing this record affects **new** snapshots on applications that pick an approval profile after the save. Existing in-flight applications keep their copied snapshot unless your office re-syncs them (administrator procedure).

## Application Migration Sla Profile

Per–application-type SLA for time spent at the **migration service** after ministry legs complete.

### Create or edit a profile

1. **Configuration → Application Migration Sla Profile**.
2. **New** (or open a row).
3. Enter display name (Turkmen name is the list default).
4. Set:
   - **Max working days** — optional cap at migration service
   - **Warning (working days)** — optional early warning (must be < max when both set)
5. In **Application types**, link every application type that should use this SLA tier.
6. **Save**.

### Assign types to profiles

Each **Application type** should appear on **one** migration SLA profile. If a type is missing from all profiles, migration SLA warnings may not apply for that type.

## Working days vs calendar days

| Setting area | Day type |
|--------------|----------|
| Ministry review SLA | **Working** days |
| Application Migration Sla Profile | **Working** days |
| Document expiration alerts | **Calendar** days (separate guide) |

Do not mix the two when comparing thresholds.

## Common problems

| Problem | What to do |
|---------|------------|
| Save blocked on warning days | Set **Warning** strictly **less than** **Max working days** |
| SLA warning never appears | Confirm application type is linked on the migration profile; check progress state matches migration service |
| Different ministries need different SLA | Ministry legs share the **Ministry review SLA** singleton — per-leg overrides require developer configuration |
| Officer exceeded SLA but no alert | Verify deployment enables SLA evaluation on progress views |
