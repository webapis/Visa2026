---
title: Register a new employee
slug: person/register
locale: en
tier: 2
status: draft
bo: Person
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-register.mp4
videoSource: recordings/passport-create.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
---

# Register a new employee

This guide shows how to create a new **employee** person record in Visa2026. When you finish, the employee appears in the **Employees** list and you can open their detail form.

!!! tip "Prerequisites"
    Sign in ([login guide](../../getting-started/login.md)) and know how to use the left menu ([navigation guide](../../getting-started/navigation.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-register.mp4"
  title="Register a new employee in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with create access on **Employees** | Ask your supervisor if **New** is missing |
| A unique **Personal Number** | Your office rules define the format |
| Lookup values (country, contract, subcontractor) | Pick from the dropdown lists in the form |

## Step 1 — Open the Employees list

1. Sign in to Visa2026.
2. In the left menu, select **Employees**.
3. Wait for the list to load.

The toolbar should show **New** and **Refresh**.

![Employees list ready for a new record](../../../assets/screenshots/v2026.08/en/person-register-step-01-employees-list.png)

## Step 2 — Start a new employee

1. On the **Employees** list toolbar, select **New**.
2. Wait for the employee **detail form** to open.

You are now on a blank employee record. Required fields are marked on the form (and listed below).

## Step 3 — Fill required fields

Enter values using the on-screen labels. Typical **required** fields for a new employee include:

| Field | What to enter |
|-------|----------------|
| **First Name** | Employee given name |
| **Last Name** | Employee family name |
| **Personal Number** | Unique ID for this employee (office rules) |
| **Date Of Birth** | Date picker |
| **Birth Place** | Text |
| **Country Of Birth** | Choose from the list |
| **Gender** | Choose from the list |
| **Marital Status** | Choose from the list |
| **Nationality** | Choose from the list |
| **Foreign Address** | Address text |
| **Foreign Address Country** | Choose from the list |
| **Project Contract** | Choose the active contract |
| **Company (Subcontractor)** | Choose the subcontractor company |

!!! note "Lookup fields"
    For **Project Contract** and **Company (Subcontractor)**, open the dropdown and select an existing value. If a required lookup stays empty, **Save** may show a validation message.

Optional fields (photo, extra tabs) can be completed later — see the employee detail tabs after save.

## Step 4 — Save the employee

1. Review the values you entered.
2. Select **Save** on the toolbar.
3. Wait until the save completes.

If **Save** fails:

- Read any **Data Validation Error** or *must not be empty* message and fill the missing field.
- If *already uses this personal number* appears, choose a different **Personal Number** — that number is already in the system.

After a successful save, you usually remain on the employee **detail form** with your values shown.

![Employee detail after save](../../../assets/screenshots/v2026.08/en/person-register-step-02-saved-detail.png)

## Step 5 — Confirm the employee in the list

1. Open **Employees** in the left menu again (or use **Save and Close** if you prefer to return to the list).
2. Find the row with the **Personal Number** you entered.
3. Open the row to view the detail form.

Check that **First Name**, **Last Name**, and **Personal Number** match what you saved.

![Employee detail opened from the list](../../../assets/screenshots/v2026.08/en/person-register-step-03-open-from-list.png)

!!! success "Employee registered"
    When the employee appears in **Employees** and opens with the correct **Personal Number**, registration succeeded.

## Common problems

| Problem | What to do |
|---------|------------|
| **New** is disabled or missing | Your role may not allow create — ask your supervisor |
| Validation error on **Save** | Fill every required field; re-select **Project Contract** and **Company (Subcontractor)** |
| Duplicate **Personal Number** | Use another number; search the list to see if the person already exists |
| Cannot find the row after save | Select **Refresh** on the list; check filters |

## What to read next

- [Main navigation](../../getting-started/navigation.md) — lists, detail forms, and tabs
- [Add a passport](add-passport.md) — passport on the **Passports** tab
- **Person reference** — field help generated from the application catalog ([Business objects](../../reference/business-objects.md))
