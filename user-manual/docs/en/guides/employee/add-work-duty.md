---
title: Add a work duty
slug: employee/add-work-duty
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
videoFile: person-add-work-duty.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Add a work duty

This guide shows how to add a **work duty** (purpose of work / visit text) on an existing **employee**. Work duty rows are on the employee detail form under the **Work Duties** tab in **Person record data**.

Application items use **Current Work Duty** from this nested list when you add people to application lines.

This guide applies to **employees** only — family members and temporary visitors do not have this tab.

!!! tip "Prerequisites"
    The employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Tab and field labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-add-work-duty.mp4"
  title="Add a work duty in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| Purpose / duty text | Free-text description for visa and application paperwork |

The tab caption may appear as **Work Duties** or **Gelmeginiň Maksady** depending on your UI language. The **New** action may read **New Work Duty** or **New Gelmeginiň Maksady**.

## Step 1 — Open the employee

1. [Find and open the employee](../person/open-and-search.md).
2. Wait for the employee **detail form** to load.

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-work-duty-step-01-employee-detail.png)

## Step 2 — Open the Work Duties tab

1. In **Person record data**, select the **Work Duties** tab (or the localized equivalent).
2. Wait for the nested list to load.

## Step 3 — Start a new work duty

1. On the nested list toolbar, select **New Work Duty** (or **New Gelmeginiň Maksady**).
2. Wait for the work duty **detail form** to open.

![New work duty detail form](../../../assets/screenshots/v2026.08/en/person-add-work-duty-step-02-work-duty-form-new.png)

## Step 4 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **Gelmeginiň Maksady** (purpose text) | Enter the duty / purpose description shown on the form label |

Use the exact on-screen label in your language — it is the only required field on this form.

![Work duty fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-work-duty-step-03-work-duty-fields-filled.png)

## Step 5 — Save the work duty

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

After save, the description text on the form should match what you entered.

![Work duty saved](../../../assets/screenshots/v2026.08/en/person-add-work-duty-step-04-work-duty-saved.png)

!!! success "Work duty added"
    The row appears on the **Work Duties** tab for this employee.

## Step 6 — Confirm on the Work Duties tab

1. Return to the employee detail form if needed.
2. Open the **Work Duties** tab again.
3. Confirm your row appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **Work Duties** tab missing | Confirm the person is an **employee**, not a family member or temporary visitor |
| **New** not found | Select the **Work Duties** tab first; wait for the nested list to load |
| **Save** validation error | Fill the purpose / description field — it is required |

## What to read next

- [Add a salary record](add-salary.md)
- [Add position history](add-position-history.md)
- [Update employee details](edit-employee.md)
