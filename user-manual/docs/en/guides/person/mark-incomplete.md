---
title: Mark incomplete or complete
slug: person/mark-incomplete
locale: en
tier: 3
guideStatus: draft
bo: Person
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
  - person/open-and-search
  - employee/register
screenshotsVersion: "2026.08"
verified: false
sourceDocs:
  - docs/PERSON_INCOMPLETE_DATA.md
  - docs/REPORT_DASHBOARD.md
---

# Mark incomplete or complete

This guide shows how to flag a person whose master data is **not yet complete** (for example during migration cleanup), record what is missing, and clear the flag when the record is ready.

The **Incomplete** flag is a **soft** reminder for officers — it does **not** block creating or editing applications.

!!! tip "Prerequisites"
    Open the correct person on the detail form ([Find and open a person](open-and-search.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with access to **Employees**, **Family Members**, or **Temporary visitor** | Same lists as other person guides |
| A saved person on the **detail form** | Not available from list-only views |
| At least one missing area and notes | Required when you mark incomplete |

Use this workflow when data is intentionally unfinished — not as a substitute for fixing fields ([Update employee details](edit-employee.md)).

## What the incomplete flag does

| Aspect | Behaviour |
|--------|-----------|
| **Purpose** | Track migration or cleanup work on person master data |
| **Applications** | Does **not** stop you from creating or editing applications |
| **Where you set it** | Person **detail form** toolbar only |
| **Where you see it** | **Incomplete data** tab on the detail form (after marking) |
| **Office overview** | **Report Dashboard** → **Persons with incomplete data** |

## Step 1 — Open the person detail form

1. Sign in to Visa2026.
2. Open **Employees** (or **Family Members** / **Temporary visitor**).
3. Find and open the person ([Find and open a person](open-and-search.md)).

![Employee detail form with toolbar](../../../assets/screenshots/v2026.08/en/person-mark-incomplete-step-01-detail-form.png)

## Step 2 — Mark incomplete

1. On the detail form toolbar, select **Mark incomplete**.
2. In the popup **Mark person incomplete**, tick **at least one** missing area:

| Checkbox | Use when |
|----------|----------|
| **Personal data** | Core identity fields are missing or wrong |
| **Passport** | Passport scan or passport record missing |
| **CV** | CV document missing |
| **Photo** | Person photo missing |
| **Education** | Education records or documents missing |
| **Medical** | Medical record missing |
| **Address** | Address of residence missing |
| **Family docs** | Family-related documents missing |
| **Other** | Another gap — explain in **Notes** |

3. In **Notes**, describe what is missing (required). Use free text; for **Other**, the notes are especially important.
4. Select **Apply**.

If you leave all checkboxes empty, Visa2026 shows *Select at least one missing-data area.* If **Notes** is empty, you see *Notes are required.*

![Mark person incomplete popup](../../../assets/screenshots/v2026.08/en/person-mark-incomplete-step-02-popup.png)

!!! success "Marked incomplete"
    The person is flagged. The toolbar now shows **Update incomplete** and **Mark complete** instead of only **Mark incomplete**.

## Step 3 — Review on the Incomplete data tab

1. On the person detail form, open the **Incomplete data** tab (it appears only while the person is incomplete).
2. Review read-only fields:

| Field | Meaning |
|-------|---------|
| **Incomplete** | Checked while the flag is active |
| **Missing areas** | Summary of ticked checkboxes |
| **Incomplete notes** | Text you entered in the popup |
| **Missing: …** | Individual area flags (read-only) |
| **Incomplete marked on** | Date and time of the last mark or update |
| **Incomplete marked by** | Officer user name |

You cannot edit these fields directly on the tab — use **Update incomplete** or **Mark complete** on the toolbar.

![Incomplete data tab](../../../assets/screenshots/v2026.08/en/person-mark-incomplete-step-03-incomplete-tab.png)

## Step 4 — Update incomplete (optional)

When more areas are missing or notes change:

1. Select **Update incomplete** on the toolbar (same popup as **Mark incomplete**).
2. Adjust checkboxes and **Notes**.
3. Select **Apply**.

**Incomplete marked on** and **Incomplete marked by** refresh to the latest update.

## Step 5 — Find incomplete persons on Report Dashboard

Supervisors and officers can monitor flagged persons from the home page. See [Report Dashboard](../tracking/report-dashboard.md) for the full dashboard walkthrough.

1. Open **Report Dashboard**.
2. Select the **Persons with incomplete data** category.
3. Review the chart (counts by missing area) and the preview table.
4. Use **Open ListView** or click a chart segment to see matching persons.

A person with several missing areas may appear in more than one chart bucket.

![Report Dashboard incomplete persons category](../../../assets/screenshots/v2026.08/en/person-mark-incomplete-step-04-dashboard.png)

!!! note "Not the same as notifications"
    Automatic **State notifications** (header bell) is **postponed** and not in officer rollout. The manual **Incomplete** flag is separate — see [Mark incomplete or complete](mark-incomplete.md).

## Step 6 — Mark complete

When master data is ready:

1. Open the person **detail form**.
2. Select **Mark complete** on the toolbar.
3. Confirm the message: *Clear incomplete status and notes for this person?*

Visa2026 clears **Incomplete**, all missing-area checkboxes, **Incomplete notes**, and the marked-on/by fields. The **Incomplete data** tab hides until you mark incomplete again.

!!! success "Marked complete"
    The person no longer appears in **Persons with incomplete data** on the dashboard.

## Common problems

| Problem | What to do |
|---------|------------|
| **Mark incomplete** missing | Open a **detail form**, not only a list |
| Cannot apply popup | Tick ≥1 area and fill **Notes** |
| **Incomplete data** tab hidden | Person is not incomplete — use **Mark incomplete** first |
| Still on dashboard after fix | Select **Mark complete** — editing fields alone does not clear the flag |
| Applications blocked? | They should not be — report to your supervisor if you see unexpected errors |

## What to read next

- [Update employee details](edit-employee.md) — fix field values
- [Find and open a person](open-and-search.md)
- [Main navigation](../../getting-started/navigation.md) — **Report Dashboard**
