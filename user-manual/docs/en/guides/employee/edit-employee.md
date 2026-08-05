---
title: Update employee details
slug: employee/edit-employee
locale: en
tier: 3
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
  - person/open-and-search
  - employee/register
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-edit-employee.mp4
videoSource: recordings/passport-create.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/OPTIONAL_DETAIL_FIELDS.md
---

# Update employee details

This guide shows how to change fields on an existing **employee** person record and save your updates. When you finish, the new values appear on the employee **detail form** and in the **Employees** list.

!!! tip "Prerequisites"
    You can [find and open a person](../person/open-and-search.md) and the employee already exists ([register guide](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-edit-employee.mp4"
  title="Update employee details in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with edit access on **Employees** | Ask your supervisor if fields are read-only |
| The correct employee open on the detail form | Use [Find and open a person](../person/open-and-search.md) |
| Approval for sensitive changes | Your office may restrict **Personal Number** or contract changes |

This guide updates **scalar fields** on the employee header (name, address, lookups). For passports, education, or travel tabs, use the dedicated guides — do not duplicate those steps here.

## Step 1 — Open the employee detail form

1. Sign in to Visa2026.
2. In the left menu, select **Employees**.
3. Search for the employee if needed (see [Find and open a person](../person/open-and-search.md)).
4. Click the row to open the **detail form**.

Confirm **Personal Number** and **Full Name** match the person you intend to edit.

![Employee detail form open for editing](../../../assets/screenshots/v2026.08/en/person-edit-employee-step-01-detail-form.png)

## Step 2 — Change the fields you need

Edit values directly on the form. Common updates include:

| Field | When officers change it |
|-------|-------------------------|
| **Foreign Address** | Address abroad changed |
| **Foreign Address Country** | Country of that address changed |
| **Email** | Contact email added or corrected (optional — see step 3) |
| **Project Contract** | Employee moved to another contract (office policy) |
| **Company (Subcontractor)** | Subcontractor assignment changed |
| **Marital Status** | Status updated |
| **Hire Date** | HR date correction (optional field) |

!!! warning "Personal Number"
    **Personal Number** is the primary key for many office processes. Change it only when your supervisor confirms the old number was wrong. If you enter a number already used by someone else, **Save** shows a duplicate error.

Required fields from registration (**First Name**, **Last Name**, **Date Of Birth**, **Gender**, **Nationality**, and so on) must stay filled. If **Save** reports *must not be empty*, return to the missing field.

## Step 3 — Show optional fields (gear)

Some fields are hidden until you expand optional details:

1. At the top of the employee form, select **Show optional fields** (gear control).
2. Optional members appear — for example **Middle Name**, **Email**, **Photo**, **Hire Date**, **Is Archived**.
3. Edit the optional values you need.
4. Select **Hide optional fields** when you want a shorter form again.

Optional fields stay available on saved records; the gear does not delete data — it only shows or hides the inputs.

## Step 4 — Save your changes

1. Review every field you changed.
2. Select **Save** on the toolbar.
3. Wait until the save completes.

If validation fails, read the message (for example *Data Validation Error* or duplicate **Personal Number**) and correct the field.

To return to the list after a successful save, use **Save and Close** instead of **Save**.

![Employee detail after save](../../../assets/screenshots/v2026.08/en/person-edit-employee-step-02-after-save.png)

## Step 5 — Confirm in the list

1. Open **Employees** again (or stay on the list if you used **Save and Close**).
2. Find the employee by **Personal Number**.
3. Open the row and check that your changes appear.

Select **Refresh** on the list if another officer saved at the same time.

!!! success "Employee updated"
    When the detail form and list show your new values, the update succeeded.

## What this guide does not cover

| Topic | Where to read |
|-------|----------------|
| New employee | [Register a new employee](register.md) |
| Passport tab | [Add a passport](add-passport.md) |
| Incomplete flag | [Mark incomplete / complete](mark-incomplete.md) |
| Read-only dossier | [Find and open a person](../person/open-and-search.md) — **Person search** / **Dossier** column |

## Common problems

| Problem | What to do |
|---------|------------|
| Fields are read-only | Your role may not allow edit — ask your supervisor |
| **Save** disabled | Another user may have the record open; refresh and try again |
| Duplicate **Personal Number** | Revert the number or confirm with a senior officer |
| Cannot find **Show optional fields** | Scroll to the top of the form; widen the window |
| Changes missing on list | Select **Refresh**; reopen the detail form |

## What to read next

- [Find and open a person](../person/open-and-search.md)
- [Mark incomplete or complete](mark-incomplete.md) — flag migration gaps
- [Add a passport](add-passport.md)
- [Main navigation](../../getting-started/navigation.md)
- **Person reference** — all fields ([Business objects](../../reference/business-objects.md))
