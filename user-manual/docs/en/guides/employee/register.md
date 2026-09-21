---
title: Register a new employee
slug: employee/register
locale: en
tier: 2
guideStatus: draft
stepsLocked: true
stepsLockedAt: "2026-09-21"
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
screenshotsVersion: "2026.08"
screenshotsCapturedAt: "2026-09-21T12:42:07.5770912Z"
mediaE2eRunId: "20260921-173914"
e2eTestFilter: PersonOfficerJourney_RegisterEmployee_Local
e2eScenarioId: person-officer-journey
verified: false
verifiedAt:
verifiedCommit:
---

# Register a new employee

This guide shows how to create a new **employee** person record in Visa2026. When you finish, the employee appears in the **Employees** list and you can open their detail form.

!!! tip "Prerequisites"
    Complete [Sign in to Visa2026](../../getting-started/login.md) first so you land on **Report Dashboard**.

!!! tip "Screenshots"
    These steps are **locked**. Pictures are from the English register journey.

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with create access on **Employees** | Ask your supervisor if **New** is missing |
| A unique **Personal Number** | Your office rules define the format |
| Lookup values (country, contract, subcontractor) | Pick from the dropdown lists in the form |

## Step 1 — Open the Employees list

1. After sign-in, you should see **Report Dashboard**.
2. In the left menu, select **Employees**.
3. Wait for the list titled **Employees** to load.

The toolbar should show **New**.

<!-- media-capture: person-register-step-01-employees-list -->
![Employees list after selecting Employees](../../../assets/screenshots/v2026.08/en/person-register-step-01-employees-list.png)

## Step 2 — Start a new employee

1. On the **Employees** list toolbar, select **New**.
2. Wait for the employee **detail form** to open.

You are now on a blank employee record. Required fields are marked on the form (and listed below).

<!-- media-capture: person-register-step-02-new -->
![Click marker on New on the Employees toolbar](../../../assets/screenshots/v2026.08/en/person-register-step-02-new.png)

## Step 3 — Fill required fields

Enter values using the on-screen labels. **Required** fields for a new employee:

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
| **Family members for visa (manual)** | Leave **Ýok** if there are no family members to list. Use **…** to add lines. |
| **Nationality** | Choose from the list |
| **Foreign Address** | Address text |
| **Foreign Address Country** | Choose from the list |
| **Project Contract** | Choose the active contract |
| **Company (Subcontractor)** | Choose the subcontractor company |

**Age** fills from **Date Of Birth** — you do not type it.

If **Family members for visa (manual)** already shows **Ýok**, leave it unless you need to list dependents. Adding lines is covered in [Family members for visa (manual)](family-members-for-visa-manual.md).

### Add a subcontractor (tenant catalog)

**Company (Subcontractor)** is a **tenant** catalog maintained from the employee form.

1. Open **Company (Subcontractor)** on the employee header.
2. Search the dropdown for the company name.
3. If it exists, select it.
4. If it is new, choose **New** → enter **Name (Tm)** → **Save** → select the row on the employee.

!!! warning "Avoid duplicate subcontractor entries"
    Search before **New**. A second row for the same company splits employee history across duplicates — **always pick the existing subcontractor** when the name is already in the list.

**Project Contract** is maintained under **Configuration** by VisaOffice staff — officers **select** an existing contract only; you cannot add contracts from the employee form.

!!! note "Other lookup fields"
    **Country**, **Gender**, and similar **global** lists are read-only catalogs — pick an existing value. If a value is missing, ask your supervisor.

Optional fields (photo, extra tabs) can be completed later — see the employee detail tabs after save.

<!-- media-capture: person-register-step-03-fields-filled -->
![Employee detail form with required fields filled, before Save](../../../assets/screenshots/v2026.08/en/person-register-step-03-fields-filled.png)

## Step 4 — Save the employee

1. Review the values you entered.
2. Select **Save** on the toolbar.
3. Wait until the save completes.

If **Save** fails:

- Read any **Data Validation Error** or *must not be empty* message and fill the missing field.
- If *already uses this personal number* appears, choose a different **Personal Number** — that number is already in the system.

After a successful save, you usually remain on the employee **detail form** with your values shown.

<!-- media-capture: person-register-step-02-saved-detail -->
![Employee detail form after a successful Save](../../../assets/screenshots/v2026.08/en/person-register-step-02-saved-detail.png)

## Step 5 — Confirm the employee in the list

The **Employees** list may already contain other people. Use search so you open the row you just saved.

1. Open **Employees** in the left menu again (or use **Save and Close** if you prefer to return to the list).
2. Click the search box (*Text to search...*) and type the **Personal Number** or **Full Name** you entered.
3. Open the matching row.

Check that **First Name**, **Last Name**, and **Personal Number** match what you saved.

<!-- media-capture: person-register-step-03-open-from-list -->
![Employee opened again from the Employees list](../../../assets/screenshots/v2026.08/en/person-register-step-03-open-from-list.png)

!!! success "Employee registered"
    When the employee appears in **Employees** and opens with the correct **Personal Number**, registration succeeded.

## Common problems

| Problem | What to do |
|---------|------------|
| **New** is disabled or missing | Your role may not allow create — ask your supervisor |
| Validation error on **Save** | Fill every required field; re-select **Project Contract** and **Company (Subcontractor)**; confirm **Family members for visa (manual)** shows **Ýok** or your family lines |
| Duplicate **Personal Number** | Use another number; search the list to see if the person already exists |
| Cannot find the row after save | Select **Refresh** on the list; check filters |

## What to read next

- [Find and open a person](../person/open-and-search.md) — search **Employees** for the record you just saved
- [Add a passport](add-passport.md) — passport on the **Passports** tab
- [Family members for visa (manual)](family-members-for-visa-manual.md) — manual family lines when dependents are not full person records
