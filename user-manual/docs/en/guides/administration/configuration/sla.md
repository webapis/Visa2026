---
title: SLA settings
slug: administration/configuration/sla
locale: en
tier: 8
guideStatus: draft
bo: ApplicationProfile
navPath: Application Profiles
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

Working-day service-level days are set on each **Application Profile**, not under Configuration.

| Setting | Where | Applies when |
|---------|-------|--------------|
| **Ministry SLA days** | Configure profile → **Process & SLA** | Each ministry review leg |
| **Migration SLA days** | Configure profile → **Process & SLA** | Migration service step (`PROCESS_STARTED`) |

!!! tip "Officer view"
    Officers record progress in [Track application progress](../../applications/progress.md). SLA days define when the system flags delays — they do not auto-advance workflow.

## Edit SLA days

1. Open **Application Profiles** and **Configure** the template.
2. On **Process & SLA**, set **Ministry SLA days** and **Migration SLA days**.
3. **Save**.

New cases that pick this profile use those days. In-flight cases keep the live profile values (config lock may block edits after office preparation).

## Working days vs calendar days

| Setting area | Day type |
|--------------|----------|
| Profile ministry / migration SLA | **Working** days |
| Document expiration alerts | **Calendar** days (separate guide) |

## Common problems

| Problem | What to do |
|---------|------------|
| Cannot advance to migration | Set **Migration SLA days** greater than 0 on the profile |
| SLA warning never appears | Confirm the case is on the migration step and days are set on the profile |
| Different ministries need different SLA | Legs share the profile **Ministry SLA days** |
