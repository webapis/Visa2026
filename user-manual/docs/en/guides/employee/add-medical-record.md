---
title: Add a medical record
slug: employee/add-medical-record
locale: en
tier: 2
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - employee/register
  - person/open-and-search
screenshotsVersion: "2026.08"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Add a medical record

This guide shows how to add a **medical record** on an existing employee. Medical records are on the Employee detail form under the **Medical Records** tab.

Application items resolve **Current Medical Record** from this nested list.

!!! tip "Prerequisites"
    the employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| **Document Number** | Unique identifier from the medical certificate |
| **Issue Date** and **Validity Duration** | Defaults often apply on a new record |

**Expiration Date** is calculated from issue date and validity duration.

## Step 1 — Open the employee

1. [Find and open the employee](../person/open-and-search.md).
2. Wait for the employee **detail form** to load.

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-medical-record-step-01-employee-detail.png)

## Step 2 — Open the Medical Records tab

1. Select the **Medical Records** tab (label may appear as **Medical records**).
2. Wait for the nested list to load.

## Step 3 — Start a new medical record

1. On the nested list toolbar, select **New Medical Record**.
2. Wait for the medical record **detail form** to open.

![New medical record detail form](../../../assets/screenshots/v2026.08/en/person-add-medical-record-step-02-medical-form-new.png)

## Step 4 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **Document Number** | Certificate or registry number |
| **Issue Date** | Confirm or set the issue date |
| **Validity Duration** | Choose duration; updates **Expiration Date** |

Attach scans under **Documents** or **Images** when your office requires file copies.

![Medical record fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-medical-record-step-03-medical-fields-filled.png)

## Step 5 — Save the medical record

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

After save, **Document Number** should match what you entered.

![Medical record saved](../../../assets/screenshots/v2026.08/en/person-add-medical-record-step-04-medical-saved.png)

!!! success "Medical record added"
    The row appears on the **Medical Records** tab for this employee.

## Step 6 — Confirm on the Medical Records tab

1. Return to the Employee detail form if needed.
2. Open the **Medical Records** tab.
3. Confirm your record appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **Expiration Date** validation | **Expiration Date** must be later than **Issue Date** — adjust **Validity Duration** or issue date |
| Tab caption differs | Look for **Medical Records** / **Medical records** on the Employee detail form |
| Missing scans | Add documents after save if validation did not require them |

## What to read next

- [Add an address of residence](add-address.md)
- [Add an education record](../employee/add-education.md)
- [Mark incomplete or complete](mark-incomplete.md)
