---
title: Register a temporary visitor
slug: temporary-visitor/register
locale: en
tier: 2
guideStatus: draft
bo: Person
personRole: TemporaryVisitor
navPath: TemporaryVisitor
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
  - person/open-and-search
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
---

# Register a temporary visitor

This guide shows how to create a new **temporary visitor** person record in Visa2026. When you finish, the visitor appears in the **Temporary visitor** list and you can open their typed detail form.

!!! tip "Prerequisites"
    Sign in ([login guide](../../getting-started/login.md)) and know the shell ([navigation guide](../../getting-started/navigation.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with create access on **Temporary visitor** | Ask your supervisor if **New** is missing |
| A unique **Personal Number** | Office rules define the format |
| **Project Contract** and **Company (Subcontractor)** | Pick from dropdown lists |

Temporary visitors do **not** use **Sponsoring Employee**, **Relationship**, **Marital Status**, or **Foreign Address** — those fields are hidden or must stay empty.

## Step 1 — Open the Temporary visitor list

1. Sign in to Visa2026.
2. In the left menu, select **Temporary visitor**.
3. Wait for the list to load.

The toolbar should show **New** and **Refresh**.

![Temporary visitor list ready for a new record](../../../assets/screenshots/v2026.08/en/person-register-temporary-visitor-step-01-list.png)

## Step 2 — Start a new temporary visitor

1. On the list toolbar, select **New**.
2. Wait for the **temporary visitor** detail form to open.

The form shows a shorter **Person record data** tab strip than employees (no Educations, Salaries, Work duties, or CV files).

## Step 3 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **First Name** | Given name |
| **Last Name** | Family name |
| **Personal Number** | Unique ID (office rules) |
| **Date Of Birth** | Date picker |
| **Birth Place** | Text |
| **Country Of Birth** | Choose from the list |
| **Gender** | Choose from the list |
| **Nationality** | Choose from the list |
| **Project Contract** | Choose the active contract |
| **Company (Subcontractor)** | Choose the subcontractor company |

## Step 4 — Save the temporary visitor

1. Review the values you entered.
2. Select **Save** on the toolbar.
3. Wait until the save completes.

If **Save** fails, read the validation message and fill any missing required field.

![Temporary visitor detail after save](../../../assets/screenshots/v2026.08/en/person-register-temporary-visitor-step-02-saved-detail.png)

## Step 5 — Confirm in the list

1. Open **Temporary visitor** in the left menu again.
2. Find the row with the **Personal Number** you entered.
3. Open the row to view the detail form.

!!! success "Temporary visitor registered"
    When the person appears in **Temporary visitor** with the correct **Personal Number**, registration succeeded.

## Common problems

| Problem | What to do |
|---------|------------|
| **New** is disabled | Your role may not allow create — ask your supervisor |
| Duplicate **Personal Number** | Use another number; search the list first |
| **Sponsoring Employee** visible | You may have opened a family member — use **Temporary visitor** list **New** |

## What to read next

- [Add a passport](add-passport.md)
- [Add a travel history](add-travel.md)
- [Find and open a person](../person/open-and-search.md)