---
title: Add an education record
slug: employee/add-education
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

# Add an education record

This guide shows how to add an **education** record on an existing employee. Education rows live on the employee detail form under the **Educations** tab.

Application items use **Current Education** from the person's nested list when you build application lines.

!!! tip "Prerequisites"
    The employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| **Education Institution** and **Specialty** | Choose from lookup catalogs |
| Optional **Graduation Year** | Must be in a valid year range when filled |

**Education Level** and **Education Country** may default on a new record — confirm them before save.

## Step 1 — Open the employee

1. [Find and open the employee](../person/open-and-search.md).
2. Wait for the employee **detail form** to load.

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-education-step-01-employee-detail.png)

## Step 2 — Open the Educations tab

1. On the employee detail form, select the **Educations** tab.
2. Wait for the nested **Educations** list to load.

## Step 3 — Start a new education record

1. On the nested list toolbar, select **New Education**.
2. Wait for the education **detail form** to open.

![New education detail form](../../../assets/screenshots/v2026.08/en/person-add-education-step-02-education-form-new.png)

## Step 4 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **Education Level** | Confirm or choose from the list |
| **Education Institution** | Type to search; pick a catalog row |
| **Education Country** | Confirm or choose from the list |
| **Specialty** | Type to search; pick a catalog row |

Use the optional-fields gear for graduation year, scans, or documents when your office requires them.

![Education fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-education-step-03-education-fields-filled.png)

## Add education institution or specialty (tenant catalog)

**Education institution** and **Specialty** are **tenant** catalogs. Officers add missing schools and specialties **from this education form** — not from a separate Lookup menu.

1. Open **Education Institution** or **Specialty**.
2. Search the dropdown for the name.
3. If it exists, select it.
4. If it is new, choose **New** → enter **Name (Tm)** → **Save** → select on the education row.

**Education level** and **Education country** are **global** catalogs — pick only; officers cannot add new levels or countries here.

!!! warning "Avoid duplicate education entries"
    Before **New**, check for the same institution or specialty under a different spelling. Duplicate rows make lists longer for every officer and split reporting — **reuse the existing catalog row**.

## Step 5 — Save the education record

1. Select **Save** on the education detail toolbar.
2. Wait until the save completes.

After save, **Education Institution** should show the value you selected.

![Education saved on detail form](../../../assets/screenshots/v2026.08/en/person-add-education-step-04-education-saved.png)

!!! success "Education added"
    The row appears on the **Educations** tab for this employee.

## Step 6 — Confirm on the Educations tab

1. Return to the employee detail form if needed.
2. Select the **Educations** tab.
3. Confirm your record appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| Lookup does not stick | Type the name, press **Tab** or **Enter**, then click the matching dropdown row |
| Duplicate institution or specialty | Search before **New** — select the existing catalog row |
| **Specialty** empty after save | Pick a catalog value — free text alone may not bind |
| **Graduation Year** error | Use a year between 1950 and ten years from today |

## What to read next

- [Add a medical record](add-medical-record.md)
- [Add a visa on a passport](add-visa.md)
- [Update employee details](edit-employee.md)
