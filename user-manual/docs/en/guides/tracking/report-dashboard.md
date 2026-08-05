---
title: Report Dashboard
slug: tracking/report-dashboard
locale: en
tier: 6
guideStatus: draft
bo: —
navPath: Report Dashboard
roles: [Visa Officer, Visa Chief]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/REPORT_DASHBOARD.md
  - docs/PERSON_INCOMPLETE_DATA.md
---

# Report Dashboard

This guide shows how to use the **Report Dashboard** — the officer **home page** after sign-in. Review charts by work area (visa, passport, applications, registration, and more), drill down to filtered lists, and export to Excel when configured.

The dashboard answers: *what needs attention today?* It does **not** replace person editing — open **Employees** or other lists to change records.

!!! tip "Prerequisites"
    [Sign in](../getting-started/login.md) and know the shell ([Main navigation](../getting-started/navigation.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Category names differ by language; steps are the same.

## Before you start

| Area | Purpose |
|------|---------|
| **Overview** | Summary cards for major categories — click a card to open that category |
| **Category detail** | Charts, filters, preview table, and actions for one work area |
| **Preview table** | Sample rows for the current chart selection (paginated) |
| **State notifications** (header bell) | *Postponed* — use Report Dashboard for overview |

| Typical categories | What officers monitor |
|--------------------|------------------------|
| **Visa**, **Invitation**, **Work permit** | Validity and process status |
| **Application (via ministry)** / **Application (direct migration)** | Cases in progress or completed |
| **Registration**, **Travel**, **Passport** | Movement and document status |
| **Persons with incomplete data** | Manual incomplete flags — see [Mark incomplete](../person/mark-incomplete.md) |
| **Person search** | Find any person → opens [Person dossier](../person/dossier.md) |

## Step 1 — Open Report Dashboard

1. Sign in to Visa2026 — you land on **Report Dashboard** by default.
2. Or select **Report Dashboard** in the left navigation menu anytime.

The page shows **Overview** cards with counts per category.

![Report Dashboard Overview](../../../assets/screenshots/v2026.08/en/report-dashboard-step-01-overview.png)

## Step 2 — Select a category

1. On **Overview**, click a category card (for example **Visa** or **Invitation**).
2. Or use the category list on the left side of the dashboard to switch directly.

The detail view opens with:

- **Sub-report** chips (when the category has more than one report)
- **Chart** — list, pie, or bar layout depending on category
- **Preview table** at the bottom

## Step 3 — Read the chart

Charts group records into **status buckets** (for example valid, expiring, expired, on process).

| Chart type | How to use it |
|------------|----------------|
| **List** | Click a row to filter the preview and list |
| **Pie** | Click a slice or legend item |
| **Bar** | Click a bar |

Colours follow office status rules (green / amber / red where applicable).

!!! tip "Totals"
    The number on the chart should match the **Total** shown when you open the list for the same selection.

## Step 4 — Review the preview table

Below the chart, the **preview table** shows rows for the current selection.

1. Scroll the table to scan key columns.
2. Use **page size** and **previous / next** when many rows exist.
3. For **Person search**, clicking a row opens the **person dossier** (read-only) — not the editable detail form.

## Step 5 — Open ListView (full list)

1. Select **Open ListView** above the preview table.
2. Wait for the filtered **read-only list** to open in the main area.

This list matches the dashboard selection — use it for a full scrollable view or to open a record's detail form from a row (except **Person search**, which opens the dossier from the dashboard).

To return to the dashboard, choose **Report Dashboard** in the left menu.

![Open ListView from dashboard](../../../assets/screenshots/v2026.08/en/report-dashboard-step-03-listview.png)

## Step 6 — Open in Excel (optional)

When your office configured an Excel template for the category:

1. Select **Open in Excel** (enabled when a template is linked).
2. Wait for the file download.

If **Open in Excel** is disabled, no template is configured for that report — use **Open ListView** or ask an administrator.

## Step 7 — Use category filters (when shown)

Some categories show extra toggles above the chart, for example:

| Toggle (examples) | Effect |
|-------------------|--------|
| **Include archived** | Show archived persons in counts |
| **Include completed** / **cancelled** | Show finished application processes |
| **One last valid visa** / **work permit** | Narrow permit/visa rules per office setup |
| **Last** (months) | Limit education, passport, or similar history windows |

Change a toggle, then select **Refresh** (top right of the dashboard card) to reload numbers from the database.

## Step 8 — Refresh data

1. Select **Refresh** on the dashboard toolbar (top right of the card).
2. Wait until loading completes.

Use **Refresh** after you save person or application changes and need updated counts.

## Step 9 — Person search category

Cross-person lookup without knowing the list (Employees vs family):

1. Open **Report Dashboard**.
2. Select **Person search**.
3. Type a name, **Personal Number**, or passport number in the **search box**.
4. Review chart buckets (valid / expiring / expired / no visa) and the preview table.
5. Click a row to open the [Person dossier](../person/dossier.md).

Clear the search box (×) to list all persons again (subject to current filters).

See also [Find and open a person](../person/open-and-search.md).

## Step 10 — Persons with incomplete data

Monitor the manual **Incomplete** flag across the office:

1. Select **Persons with incomplete data**.
2. Review the chart by missing area.
3. Use **Open ListView** or click a chart segment to open matching persons.

Details: [Mark incomplete or complete](../person/mark-incomplete.md).

![Incomplete persons category](../../../assets/screenshots/v2026.08/en/report-dashboard-step-02-category.png)

## Common problems

| Problem | What to do |
|---------|------------|
| Dashboard empty or zero | **Refresh**; confirm database connection; new environment may have little data |
| Counts look stale | **Refresh** after editing records |
| **Open in Excel** disabled | No Excel template for this report — administrator task |
| List does not match chart | **Refresh**; report a mismatch to IT (Preview and ListView should match) |
| Person search opens dossier | Expected — use **Employees** list to edit |
| Cannot find a category | Your role may hide some areas — ask supervisor |
| Confused with bell notifications | **State notifications** feature is **postponed** — use Report Dashboard or manual **Incomplete** flag |

## What to read next

- [Main navigation](../getting-started/navigation.md) — left menu and shell
- [Find and open a person](../person/open-and-search.md) · [Person dossier](../person/dossier.md)
- [Mark incomplete or complete](../person/mark-incomplete.md)
- [Applications — ministry and direct migration](../applications/overview.md)
- [What Visa2026 does](../../about/capabilities.md) — Report Dashboard as feature #1