---
title: Find and open a person
slug: person/open-and-search
locale: en
tier: 1
guideStatus: review
lastReviewed: "2026-08-05"
bo: Person
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-open-search.mp4
videoSource: recordings/passport-create.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/REPORT_DASHBOARD.md
  - docs/PERSON_DOSSIER.md
---

# Find and open a person

This guide shows how to locate an existing person in Visa2026 and open their record. When you finish, you can open an employee (or other person type) from a list and read their **detail form**.

!!! tip "Prerequisites"
    Sign in ([login guide](../../getting-started/login.md)) and know the shell ([navigation guide](../../getting-started/navigation.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-open-search.mp4"
  title="Find and open a person in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with read access on person lists | Ask your supervisor if a menu item is missing |
| At least one search clue | Name, **Personal Number**, or passport number |

Visa2026 stores people in separate lists by type. Use the list that matches who you are looking for.

| Left menu item | Who it contains |
|----------------|-----------------|
| **Employees** | Company employees |
| **Family Members** | Family members linked to employees |
| **Temporary visitor** | Temporary visitors |

## Step 1 — Open the right list

1. Sign in to Visa2026.
2. In the left menu, select **Employees** (or **Family Members** / **Temporary visitor**).
3. Wait for the list to load.

The table shows columns such as **Full Name**, **Personal Number**, **Date Of Birth**, and **Nationality**. The first columns may include **Dossier** and **Copies** shortcuts — this guide focuses on opening the standard detail form.

![Employees list](../../assets/screenshots/v2026.08/en/navigation-step-03-employees-list.png)

## Step 2 — Search the list

When the list is long, use the **search** field in the list toolbar (or open **Search** if your layout shows it as a button).

1. Click in the search area.
2. Type part of a **First Name**, **Last Name**, **Personal Number**, or a **Passport Number** from any passport on that person.
3. Press **Enter** or wait for the list to refresh.

Tips:

- Multiple words narrow the result (for example `Ali` and `Yilmaz` together).
- Accented letters often match without the accent (typing `u` may find `ü`).
- Clear the search box to see the full list again.
4. Select **Refresh** on the toolbar if the list looks out of date after a colleague saved changes.

!!! note "Which fields are searched"
    List search checks **First Name**, **Middle Name**, **Last Name**, **Personal Number**, and passport numbers on the **Passports** tab. It does not search unrelated application numbers.

## Step 3 — Open the detail form

1. In the filtered list, find the row with the correct **Full Name** and **Personal Number**.
2. Click the row (or double-click, depending on your browser).
3. Wait for the **detail form** to open in the main area.

You can now read tabs such as **Passports**, **Educations**, and **Addresses**. To change data, use **Save** after edits — see [Register a new employee](register.md) and later update guides.

![Employee detail form](../../assets/screenshots/v2026.08/en/navigation-step-04-detail-form.png)

!!! success "Person opened"
    When the detail form shows the expected **Personal Number** and name, you found the right person.

## Alternative — Person search on Report Dashboard

From the home **Report Dashboard**, you can also find people across all person types. See [Report Dashboard](../tracking/report-dashboard.md) (Step 9).

1. Open **Report Dashboard** (home after sign-in).
2. Select the **Person search** category.
3. Type a name, **Personal Number**, or passport number in the search box next to the category chips.
4. Review the results table and chart.
5. Click a row to open the **person dossier** — a read-only summary page (not the same as the editable detail form).

Use the left-menu lists when you already know the person type. Use **Person search** when you are not sure which list they are in.

To edit fields after you identify someone, open **Employees** (or the matching list), search again, and open the **detail form** as in step 3.

## Open dossier from a list (shortcut)

On **Employees**, **Family Members**, and **Temporary visitor** lists, the **Dossier** column opens the same read-only dossier as **Person search**. Use it when you need a quick overview without editing. Full walkthrough: [Person dossier](dossier.md).

## Common problems

| Problem | What to do |
|---------|------------|
| List is empty | Clear the search box; select **Refresh**; confirm you opened the correct menu item |
| Too many rows | Add more of the name or the full **Personal Number** |
| Person not found | Try **Person search** on **Report Dashboard**; check **Family Members** or **Temporary visitor** |
| Row opens dossier instead of detail form | You clicked **Dossier** or used **Person search** — open the list row itself for the detail form |
| **Search** missing | Your role may use a simplified toolbar — scroll the toolbar or ask your supervisor |

## What to read next

- [Person dossier](dossier.md) — read-only 360° summary and director export
- [Main navigation](../../getting-started/navigation.md) — lists, toolbars, and tabs
- [Register a new employee](register.md) — create a person when they are not in the list yet
- [Update employee details](edit-employee.md) — change fields on an existing employee
- [Add a passport](add-passport.md) — passport on the **Passports** tab
- **Person reference** — field help from the application catalog ([Business objects](../../reference/business-objects.md))
