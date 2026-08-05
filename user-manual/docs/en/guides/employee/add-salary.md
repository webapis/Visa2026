---
title: Add a salary record
slug: employee/add-salary
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
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-salary.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Add a salary record

This guide shows how to add a **salary** record on an existing **employee**. Salary rows are on the employee detail form under the **Salaries** tab in **Person record data**.

Application items use **Current Salary** from this nested list when you add people to application lines.

This guide applies to **employees** only — family members and temporary visitors do not have this tab.

!!! tip "Prerequisites"
    The employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-add-salary.mp4"
  title="Add a salary record in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| **Amount** and **Currency** | Use office rules for pay figures |
| **Start Date** | Often defaults to today on a new record |

**End Date** is optional — leave empty for the current salary row.

## Step 1 — Open the employee

1. [Find and open the employee](../person/open-and-search.md).
2. Wait for the employee **detail form** to load.

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-salary-step-01-employee-detail.png)

## Step 2 — Open the Salaries tab

1. In **Person record data**, select the **Salaries** tab.
2. Wait for the nested **Salaries** list to load.

## Step 3 — Start a new salary record

1. On the nested list toolbar, select **New Employee Salary** (or **New Salary**).
2. Wait for the salary **detail form** to open.

![New salary detail form](../../../assets/screenshots/v2026.08/en/person-add-salary-step-02-salary-form-new.png)

## Step 4 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **Amount** | Salary amount (text/number as shown on the form) |
| **Currency** | Choose from the list (for example **TMT**) |
| **Start Date** | Confirm or set when this salary applies |

Use the optional-fields gear for **End Date** when closing a previous salary period.

![Salary fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-salary-step-03-salary-fields-filled.png)

## Step 5 — Save the salary record

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

After save, **Amount** should show the value you entered.

![Salary saved](../../../assets/screenshots/v2026.08/en/person-add-salary-step-04-salary-saved.png)

!!! success "Salary added"
    The row appears on the **Salaries** tab for this employee.

## Step 6 — Confirm on the Salaries tab

1. Return to the employee detail form if needed.
2. Open the **Salaries** tab again.
3. Confirm your row appears in the nested list.

Saving a new row with a later **Start Date** may set **End Date** on overlapping open salary rows automatically.

## Common problems

| Problem | What to do |
|---------|------------|
| **Salaries** tab missing | Confirm the person is an **employee** |
| **Currency** does not stick | Type or search, then pick a row from the dropdown |
| **Save** validation error | Fill **Amount** and **Currency** — both are required |

## What to read next

- [Add a travel history](add-travel.md)
- [Add a work duty](add-work-duty.md)
- [Update employee details](edit-employee.md)
