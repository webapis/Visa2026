---
title: Family members for visa (manual)
slug: employee/family-members-for-visa-manual
locale: en
tier: 3
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - employee/register
  - getting-started/navigation
  - person/open-and-search
screenshotsVersion: "2026.08"
screenshotsCapturedAt: "2026-08-06T04:29:37.0400000Z"
mediaE2eRunId: "20260805-172303"
e2eScenarioId: person-officer-journey
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
verified: false
---

# Family members for visa (manual)

This guide shows how to enter **manual family lines** on an employee when dependents are **not** registered as full **Family Members** persons in Visa2026 (for example family abroad). The lines appear on the **visa PDF** when the employee has no linked family member records.

Use the **Family Members** list instead when dependents accompany the employee in Turkmenistan and need passports, visas, and applications — see [Register a family member](../family-member/register.md).

!!! tip "Prerequisites"
    The employee must already exist ([Register a new employee](register.md)). You can open them from [Find and open a person](../person/open-and-search.md).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| A saved **employee** | [Register a new employee](register.md) first |
| **Marital status** other than **Single** (when applicable) | **Single** employees may show **Ýok** — meaning no manual lines |
| Dependent details | Full name, birth date, relationship, country of residence |

### Manual lines vs linked family members

| Situation | What to use |
|-----------|-------------|
| Dependents registered as **Family Members** persons | [Register a family member](../family-member/register.md) — visa PDF uses linked records |
| Dependents **not** in Visa2026 (abroad, no full person record) | **Family members for visa (manual)** on the employee (this guide) |
| Employee is **Single** | Field may show **Ýok** — no manual lines required |

!!! note "PDF precedence"
    When the employee has **linked** family member persons, those records take precedence on the visa PDF. Manual lines apply when the linked list is empty.

## Step 1 — Open the employee

1. Sign in to Visa2026.
2. Open **Employees** and open the employee you need to update.
3. On the employee detail form, stay on the **Person** tab (header fields).

Locate **Family members for visa (manual)** below **Marital status**. The field shows a read-only summary and a **…** button.

If **Marital status** is **Single**, the summary may show **Ýok** — change marital status only when office rules require manual family lines.

<!-- media-capture: employee-visa-family-manual-step-01-field -->
![Employee Person tab with Family members for visa manual field](../../../assets/screenshots/v2026.08/en/employee-visa-family-manual-step-01-field.png)

## Step 2 — Open the family editor

1. Select the **…** button next to **Family members for visa (manual)**.
2. Wait for the popup titled **Family members for visa (manual)**.

The popup lists current manual lines (empty on first use) and offers **Add member**.

<!-- media-capture: employee-visa-family-manual-step-02-popup-open -->
![Family members for visa manual popup open](../../../assets/screenshots/v2026.08/en/employee-visa-family-manual-step-02-popup-open.png)

## Step 3 — Add a family member line

1. In the popup, select **Add member**.
2. In the **Family member** dialog, fill every field:

| Field | What to enter |
|-------|----------------|
| **Full name** | As it should appear on the visa PDF |
| **Birth date** | Date picker (`dd.MM.yyyy`) |
| **Relationship** | Choose from the list (Turkmen label, e.g. spouse or child) |
| **Country of residence** | Choose from the country list |

3. Select **Save** on the member dialog.

**Save** stays disabled until all four fields are filled.

<!-- media-capture: employee-visa-family-manual-step-03-add-member-form -->
![Add family member line dialog with fields filled](../../../assets/screenshots/v2026.08/en/employee-visa-family-manual-step-03-add-member-form.png)

## Step 4 — Confirm the list

1. Check the new row in the popup list (name, birth date, relationship, country).
2. Add more members with **Add member** if needed.
3. Select **OK** on the main popup.

**OK** applies lines to the employee form. The employee is **not** saved to the database until you save the detail form.

<!-- media-capture: employee-visa-family-manual-step-04-popup-with-member -->
![Popup list with one manual family member](../../../assets/screenshots/v2026.08/en/employee-visa-family-manual-step-04-popup-with-member.png)

## Step 5 — Save the employee

1. On the employee detail toolbar, select **Save**.
2. Wait until the save completes.

The inline summary should show the member count (for example **1 family member(s)**).

<!-- media-capture: employee-visa-family-manual-step-05-saved-summary -->
![Employee saved with manual family summary visible](../../../assets/screenshots/v2026.08/en/employee-visa-family-manual-step-05-saved-summary.png)

!!! success "Manual family lines saved"
    When the summary shows your members and **Save** completed without errors, manual lines are stored on the employee.

## Edit or remove a line

1. Open the employee and select **…** on **Family members for visa (manual)** again.
2. Use **Edit** or **Delete** on a row.
3. Select **OK**, then **Save** the employee.

## Common problems

| Problem | What to do |
|---------|------------|
| **…** button missing | Open an **employee** — the field is not on family member or visitor forms |
| **Save** disabled on member dialog | Fill **Full name**, **Birth date**, **Relationship**, and **Country of residence** |
| Summary still shows **Ýok** | **Marital status** may be **Single**; or open **…** and add lines, then **OK** and **Save** |
| PDF shows linked family instead | Register dependents on **Family Members** — linked records override manual text |
| Changes lost after popup **OK** | Select **Save** on the **employee** detail toolbar |

## What to read next

- [Register a family member](../family-member/register.md) — full person records in Turkmenistan
- [Add a visa on a passport](add-visa.md) — visa on employee passport
- [Update employee details](edit-employee.md) — other header fields
