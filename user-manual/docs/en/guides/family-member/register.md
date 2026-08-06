---
title: Register a family member
slug: family-member/register
locale: en
tier: 2
guideStatus: review
bo: Person
personRole: FamilyMember
navPath: FamilyMember
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
  - person/open-and-search
  - employee/register
screenshotsVersion: "2026.08"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
---

# Register a family member

This guide shows how to create a new **family member** person record in Visa2026. When you finish, the family member appears in the **Family Members** list and you can open their typed detail form.

Family members are always linked to a **Sponsoring Employee**. Create the employee first if they are not in the system yet.

!!! tip "Prerequisites"
    Sign in ([login guide](../../getting-started/login.md)), know the shell ([navigation guide](../../getting-started/navigation.md)), and have the **sponsoring employee** registered ([Register a new employee](../employee/register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with create access on **Family Members** | Ask your supervisor if **New** is missing |
| An existing **Sponsoring Employee** | [Register a new employee](../employee/register.md) first |
| **Relationship** lookup value | For example Spouse or Child |
| A unique **Personal Number** | Office rules define the format |

The **Family members** tab on an employee detail form is **read-only** (browse linked dependents). Create new family members from the **Family Members** list — not from that nested tab.

For dependents who also need visa household text on PDF / Şahsy kagyz, maintain **Family members for visa (manual)** on the sponsoring employee separately — linked records do not feed those outputs. See [Family members for visa (manual)](../employee/family-members-for-visa-manual.md).

## Step 1 — Open the Family Members list

1. Sign in to Visa2026.
2. In the left menu, select **Family Members**.
3. Wait for the list to load.

The toolbar should show **New** and **Refresh**.

<!-- media-capture: person-register-family-member-step-01-family-members-list -->
![Family Members list ready for a new record](../../../assets/screenshots/v2026.08/en/person-register-family-member-step-01-family-members-list.png)

## Step 2 — Start a new family member

1. On the **Family Members** list toolbar, select **New**.
2. Wait for the **family member** detail form to open.

You are on a blank family member record. The form shows **Sponsoring Employee** and **Relationship** in the header — employee-only tabs (Educations, Salaries, and so on) are hidden.

## Step 3 — Fill required fields

Enter values using the on-screen labels. Typical **required** fields include:

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
| **Project Contract** | Often copies from **Sponsoring Employee** when you pick the sponsor |
| **Company (Subcontractor)** | Choose from the list |
| **Sponsoring Employee** | Pick the employee this person is related to |
| **Relationship** | Choose Spouse, Child, or other office value |

!!! note "Relationship exemption"
    If the sponsoring employee uses **manual visa family text** instead of linked family rows, **Relationship** may not be required on save. When in doubt, fill **Relationship** — it is required in most cases.

Family members do **not** use employee-only header fields such as **Marital Status** or **Foreign Address**.

## Step 4 — Save the family member

1. Review the values you entered.
2. Select **Save** on the toolbar.
3. Wait until the save completes.

If **Save** fails:

- Read any **Data Validation Error** message and fill the missing field.
- Confirm **Sponsoring Employee** and **Relationship** are set.
- If *already uses this personal number* appears, choose a different **Personal Number**.

<!-- media-capture: person-register-family-member-step-02-saved-detail -->
![Family member detail after save](../../../assets/screenshots/v2026.08/en/person-register-family-member-step-02-saved-detail.png)

## Step 5 — Confirm in the Family Members list

1. Open **Family Members** in the left menu again (or use **Save and Close**).
2. Find the row with the **Full Name** you entered (the list shows **Full Name** by default).
3. Open the row to view the detail form.

Confirm **Personal Number**, **Sponsoring Employee**, and **Relationship** on the detail form.

<!-- media-capture: person-register-family-member-step-03-open-from-list -->
![Family member opened from the list](../../../assets/screenshots/v2026.08/en/person-register-family-member-step-03-open-from-list.png)

!!! success "Family member registered"
    When the person appears in **Family Members** with the correct sponsor and relationship, registration succeeded.

## Common problems

| Problem | What to do |
|---------|------------|
| **New** is disabled | Your role may not allow create — ask your supervisor |
| **Sponsoring Employee** empty | Pick an active employee from the lookup |
| **Relationship** required | Choose a relationship value; check sponsor manual visa family rules |
| Duplicate **Personal Number** | Use another number; search the list for an existing person |
| Cannot create from employee tab | Use **Family Members** list **New** — the employee nested list is browse-only |

## What to read next

- [Add a passport](add-passport.md)
- [Add family relation documents](add-family-relation-documents.md)
- [Find and open a person](../person/open-and-search.md)