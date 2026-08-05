---
title: Add a passport to an employee
slug: employee/add-passport
locale: en
tier: 2
guideStatus: published
lastReviewed: "2026-08-05"
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - employee/register
screenshotsVersion: "2026.08"
screenshotsCapturedAt: "2026-08-05T08:48:03.3957272Z"
mediaE2eRunId: "20260805-134241"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: true
verifiedAt: "2026-08-05T08:49:33.7498506Z"
verifiedCommit: "2d70b13c"
---

# Add a passport to an employee

This guide shows how to add a **passport** record on an existing employee. Passports are created on the employee detail form under the **Passports** tab — not from a separate top-level menu.

!!! tip "Prerequisites"
    The employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| **Passport Number** and dates | Use office rules for numbering |
| **Passport Type** and **Issued Country** | Choose from lookup lists |

## Step 1 — Open the employee

1. In the left menu, select **Employees**.
2. Open the employee who needs a passport (use **Personal Number** or name).
3. Wait for the employee **detail form** to load.

You should see tabs such as **Passports**, **Educations**, and other collections for this person.

<!-- media-capture: person-add-passport-step-01-employee-detail -->
![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-passport-step-01-employee-detail.png)

## Step 2 — Open the Passports tab

1. On the employee detail form, select the **Passports** tab.
2. Wait for the nested **Passports** list to load.

The nested toolbar should offer **New Passport** (or **New** on the passports list).

## Step 3 — Start a new passport

1. On the **Passports** nested list toolbar, select **New Passport**.
2. Wait for the passport **detail form** to open.

The new passport is linked to the employee you opened in step 1.

!!! tip "Default passport type"
    **Passport Type** is preset to **P — National passport** (the type officers use most often). Change it only when the document is a different type.

![New passport detail form](../../../assets/screenshots/v2026.08/en/person-add-passport-step-02-passport-form-new.png)

## Step 4 — Fill required fields

Enter values using the on-screen labels. Typical **required** fields include:

| Field | What to enter |
|-------|----------------|
| **Passport Number** | Unique passport number |
| **Passport Type** | Usually already **P — National passport**; change from the list if needed |
| **Issue Date** | Date picker |
| **Expiration Date** | Date picker |
| **Authority** | Issuing authority text |
| **Issued Country** | Choose from the list |

![Passport fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-passport-step-03-passport-fields-filled.png)

## Step 5 — Save the passport

1. Select **Save** on the passport detail toolbar.
2. Wait until the save completes.

If validation fails, read the message and complete any missing required field.

After save, the **Passport Number** on the form should match what you entered.

<!-- media-capture: person-add-passport-step-04-passport-saved -->
![Passport saved on detail form](../../../assets/screenshots/v2026.08/en/person-add-passport-step-04-passport-saved.png)

!!! success "Passport added"
    When **Passport Number** shows the value you saved, the passport is on this employee.

## Step 6 — Confirm on the Passports tab

1. Return to the employee detail form if you navigated away.
2. Select the **Passports** tab again.
3. Confirm your passport appears in the nested list.

Select **Refresh** on the nested list if the row does not appear immediately.

## Common problems

| Problem | What to do |
|---------|------------|
| **Passports** tab missing | Your role may not allow this collection — ask your supervisor |
| **New Passport** not found | Open the **Passports** tab first; wait for the nested list to load |
| Detail form does not open | Select **New Passport** again; widen the window |
| **Save** validation error | Fill every required field; check date format |

## What to read next

- [Add a visa on this passport](add-visa.md) — next step after the passport is saved
- [Register a new employee](register.md) — if you still need to create the person
- [Add an education record](add-education.md) — other employee master data
- [Main navigation](../../getting-started/navigation.md) — tabs and nested lists
