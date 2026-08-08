---
title: Create an application
slug: applications/create
locale: en
tier: 4
guideStatus: draft
bo: Application
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
  - applications/overview
  - applications/application-profiles
  - employee/register
screenshotsVersion: "2026.08-preview"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/APPLICATION_PROFILE_PLAN.md
  - docs/APPLICATION_PROGRESS_STATE_VALIDATION.md
---

# Create an application

This guide walks through creating a new **application** header — the collective case file for a ministry or migration request.

**New in Visa2026:** **New** on an Applications list opens the **Application Profile** picker (not a blank form). Read [Application profiles — how configuration works](application-profiles.md) for the full model; this page is the short checklist.

You add people on the **Application items** tab in a separate guide.

**Two header lists:** create on **Applications (via ministry)** or **Applications (direct migration)** — see [Applications — ministry and direct migration](overview.md).

!!! tip "Prerequisites"
    Sign in ([login](../../getting-started/login.md)), know the shell ([navigation](../../getting-started/navigation.md)), and have person master data ready ([Register an employee](../employee/register.md)).

!!! info "Screenshots (preview)"
    Legacy screenshots below show the **Application Type Code** form. Current builds use the **profile picker** first — images will be refreshed without E2E in a later manual pass. Follow on-screen labels.

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with create access | Ask your supervisor if **New** is missing |
| Correct **workflow list** | Ministry route vs direct migration |
| A matching **Application Profile** | Maintained under **Configuration** by VisaOffice — visible in the picker |

| List you open | Use when the procedure… |
|---------------|-------------------------|
| **Applications (via ministry)** | Goes through ministry approval before the migration service |
| **Applications (direct migration)** | Goes directly to the migration service |

## Step 1 — Open the correct Applications list

1. Sign in to Visa2026.
2. Expand **Applications** in the left menu.
3. Select **Applications (via ministry)** **or** **Applications (direct migration)**.
4. Wait for the list to load.

The toolbar should show **New** and **Refresh**.

![Applications group in the left menu](../../../assets/screenshots/v2026.08/en/navigation-step-02-left-menu.png)

## Step 2 — New → choose Application Profile

1. On the list toolbar, select **New**.
2. On **Choose Application Profile**, select the row for your procedure (name, code, related-to, route).
3. Select **Use profile (live link)**.
4. Wait for the **application detail form** to open.

**Application Profile** on the form is read-only. Defaults from the profile may already fill **Visa Type**, **Project Contract**, **Urgency**, and other header fields.

!!! warning "Profile cannot be changed after save"
    If you picked the wrong profile, stop before **Save** and return to the list — or ask your supervisor after save.

!!! note "Legacy Application Type Code"
    During migration you may still see **Application Type (Deprecated)** filled automatically from the profile. Officers should not rely on typing a three-digit code on a blank form — use the picker.

## Step 3 — Fill header fields

Enter values using on-screen labels. Fields **appear or hide** based on the profile configuration.

| Field | When officers set it |
|-------|----------------------|
| **Application Date** | Usually today |
| **Project Contract** | When required for this profile |
| **Urgency** | Processing priority |
| **Visa Period** / **Visa Category** / **Visa Type** | When the profile shows them |
| **Migration Service** | Direct-migration procedures |
| **Border Zone Location** | When required |
| **Business trip** / city fields | When the profile is business-trip related |

Optional **manual numbering** is behind **Show optional fields** — normal files use automatic numbering on save.

## Step 4 — Save the application

1. Review header values.
2. Select **Save**.
3. Wait until save completes.

On first save, Visa2026 typically:

- Assigns **Application Number** and **Full Application Number**
- Leaves **Progress history** empty until you add rows ([Track application progress](progress.md))

If **Save** fails, read the validation message — required visible fields must be filled.

![Application saved — header example (legacy screenshot)](../../../assets/screenshots/v2026.08/en/application-create-step-03-saved-header.png)

## Step 5 — Confirm and add people

1. Return to the Applications list or use **Save and Close**.
2. Find the row by **Full Application Number**.
3. Open **Application items** → [Add application items](add-items.md).

## Common problems

| Problem | What to do |
|---------|------------|
| **New** does not show picker | Deployment may be on legacy flow — use type code or ask IT |
| Empty picker | Ask VisaOffice to activate profiles under **Configuration → Application Profiles** |
| Wrong profile on list | Open the other route list (ministry vs direct migration) |
| Missing header fields | They may not apply to this profile — see [Application profiles](application-profiles.md) |

## What to read next

- [Application profiles — how configuration works](application-profiles.md)
- [Applications overview](overview.md)
- [Add application items](add-items.md)
- [Track application progress](progress.md)
