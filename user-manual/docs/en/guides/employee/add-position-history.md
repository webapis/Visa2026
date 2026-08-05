---
title: Add position history
slug: employee/add-position-history
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
e2eScenarioId: person-officer-journey
verified: false
---

# Add position history

This guide shows how to add **position history** on an existing employee. Position history rows are on the employee detail form under the **Position History** tab.

Application items use **Current Position History** from this nested list. This guide applies to **employee** persons (not visitors).

!!! tip "Prerequisites"
    The employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| **Position (visa reports)** | Catalog position used on visa paperwork |
| **Position (actual / company)** | Actual job title in the company |

**Start Date** and **End Date** track when the assignment was active.

## Step 1 — Open the employee

1. [Find and open the employee](../person/open-and-search.md).
2. Wait for the employee **detail form** to load.

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-position-history-step-01-employee-detail.png)

## Step 2 — Open the Position History tab

1. Select **Position History** on the employee detail form.
2. Wait for the nested list to load.

## Step 3 — Start a new position history row

1. On the nested list toolbar, select **New Employee Position History** (or **New Position History**).
2. Wait for the position history **detail form** to open.

![New position history detail form](../../../assets/screenshots/v2026.08/en/person-add-position-history-step-02-position-form-new.png)

## Step 4 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **Position (visa reports)** | Search and pick from the catalog |
| **Position (actual / company)** | Search and pick the company title |
| **Start Date** | When this assignment began |
| **End Date** | Optional — leave empty for the current assignment |

Use the optional-fields gear for **Department** or other hidden fields when needed.

![Position history fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-position-history-step-03-position-fields-filled.png)

## Add position or actual position (tenant catalog)

**Position (visa reports)** and **Position (actual / company)** are **tenant** catalogs. Add missing job titles **from this position history form**.

1. Open **Position (visa reports)** or **Position (actual / company)**.
2. Search the dropdown.
3. If the title exists, select it.
4. If it is new, choose **New** → enter **Name (Tm)** → **Save** → select on the position history row.

**Department** (when shown) is a **global** catalog — officers select only; you cannot add departments from this form.

!!! warning "Avoid duplicate position entries"
    Search before **New**. Duplicate titles for the same job clutter dropdowns on every employee — **reuse the existing position row**.

## Step 5 — Save the position history row

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

Saving a new row with a later **Start Date** may close overlapping open assignments automatically.

![Position history saved](../../../assets/screenshots/v2026.08/en/person-add-position-history-step-04-position-saved.png)

!!! success "Position history added"
    The row appears on the **Position History** tab for this employee.

## Step 6 — Confirm on the Position History tab

1. Return to the employee detail form if needed.
2. Open the **Position History** tab.
3. Confirm your row appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| Lookup does not stick | Type the name, press **Tab** or **Enter**, then select the dropdown row |
| Duplicate position in list | Search before **New** — pick the existing title |
| Tab missing | Confirm the person is an **employee**, not a visitor-only record |
| Two open assignments | Set **End Date** on the older row or add a new row with a later **Start Date** |

## What to read next

- [Add a work duty](add-work-duty.md)
- [Add an address of residence](add-address.md)
- [Update employee details](edit-employee.md)
