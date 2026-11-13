---
title: Find and open a person
slug: person/open-and-search
locale: en
tier: 2
guideStatus: draft
stepsLocked: false
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - employee/register
screenshotsVersion: "2026.08"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
verifiedAt:
verifiedCommit:
---

# Find and open a person

This guide shows how to find an employee who is **already in Visa2026** and open their record. The list should have **more than one** employee so search can pick the right row. Register employees first if the list is empty or has only one person.

!!! tip "Prerequisite"
    Complete [Register a new employee](../employee/register.md) until **Employees** has **at least two** people. Search is only useful when more than one row is in the list.

!!! tip "Screenshots"
    These steps are **not locked** yet. Pictures are from an earlier English run and will be recaptured after the steps are locked.

## Before you start

| You need | From |
|----------|------|
| A signed-in **Visa Officer** session | [Sign in to Visa2026](../../getting-started/login.md) |
| At least two employees already saved | [Register a new employee](../employee/register.md) — search needs more than one row |
| A search clue | **Full Name**, **Personal Number**, or a passport number |

This guide uses **Employees**. The same search-and-open pattern works on **Family Members** and **Temporary visitor**.

## Step 1 — Open Employees

1. After sign-in, you should see **Report Dashboard**.
2. In the left menu, select **Employees**.
3. Wait until the list titled **Employees** loads.

**Employees** and **Family Members** open with a search box at the top left. Type a name, personal number, or passport number, then click **Search**. The list appears under that box. **Temporary visitor** still shows its rows when you open it.

You should see **Employees** highlighted in the left menu, an **Employees** tab, and the search box at the top left. There is no table yet.

<!-- media-capture: navigation-step-03-employees-list -->
![Employees list](../../assets/screenshots/v2026.08/en/navigation-step-03-employees-list.png)

## Step 2 — Check the list

The toolbar includes **New**, **Delete**, and **Close all tabs**. Employees and Family Members do not use the usual list search box.

The table columns include:

- **Dossier** and **Copies** (shortcuts — do not use these to edit)
- **Full Name**
- **Personal Number**
- **Date Of Birth**
- **Nationality**

You may also see **Age**, **Gender**, **Marital Status**, **Project Contract**, **Company (Subcontractor)**, and **Is Archived**.

The search box stays at the top left of the list. Register at least two employees first if search cannot tell them apart.

## Step 3 — Search the list

1. Click the search box at the top left.
2. Type part of a **First Name**, **Last Name**, **Personal Number**, or a **Passport Number**.
3. Click **Search**, or press **Enter**.

Tips:

- Extra words narrow the result (for example a first name and a last name together).
- After the list appears, the text you searched stays in the same box at the top left. Click **×** to clear it. The table goes away and the search box stays there. **Employees** and **Family Members** do not show the full roster.
- Select **Refresh** on the toolbar if a colleague just saved a change and your list looks old.

## Step 4 — Open the detail form

1. Find the row with the correct **Full Name** and **Personal Number**.
2. Click the row (not **Dossier** and not **Copies**).
3. Wait for the **detail form** to open in the main area.

You should see:

- The person's name in the tab
- Fields such as **First Name**, **Last Name**, and **Personal Number**
- **Save** on the toolbar
- Nested tabs such as **Passports** and **Educations**

<!-- media-capture: navigation-step-04-detail-form -->
![Employee detail form](../../assets/screenshots/v2026.08/en/navigation-step-04-detail-form.png)

!!! success "Employee opened"
    If **Personal Number** and the name match the person you wanted, you opened the right record. You can now create, change, or add child records on this employee.

## Common problems

| Problem | What to do |
|---------|------------|
| You still see **Report Dashboard** | Select **Employees** in the left menu |
| List is empty before you search | Type a name, **Personal Number**, or passport number and press **Enter** |
| Too many rows | Type more of the name or the full **Personal Number** |
| Person not found | Try **Family Members** or **Temporary visitor**; or register them if they are new |
| Dossier opens instead of the detail form | You clicked **Dossier** — click the **Full Name** / **Personal Number** row instead |

## What to read next

- [Update employee details](../employee/edit-employee.md) — change fields, then **Save**
- [Add a passport](../employee/add-passport.md) — **Passports** tab on the employee