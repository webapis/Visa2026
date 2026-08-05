---
title: State notifications
slug: tracking/state-notifications
locale: en
tier: 6
guideStatus: postponed
bo: —
navPath: Operations / State notifications
roles: [Administrator]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: state-notifications.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md
  - docs/PERSON_INCOMPLETE_DATA.md
---

# State notifications

!!! danger "Postponed"
    **State notifications** is **not** in the officer manual rollout. This page is kept for reference only. Use [Report Dashboard](report-dashboard.md) and [Mark incomplete](../person/mark-incomplete.md) instead.

This guide describes **State notifications** — the planned officer **inbox** for expiry alerts and missing-data issues (passport validity, required scans, and similar).

In the **current release** the inbox is a **UI prototype** with **sample rows** — not live data from your database. It is available to **administrators** under **Operations → State notifications**. Standard visa officer roles do not see this menu item yet.

!!! warning "Current release (prototype)"
    - Inbox shows **UI prototype** sample notifications only.
    - **Sync states**, **Open person**, and **Open record** show demonstration messages — they do not navigate to real records yet.
    - The header **bell** is implemented but may be **disabled** on your server until IT enables it.
    - Officer access is planned in a later phase.

!!! tip "Not the same as"
    | Feature | Purpose |
    |---------|---------|
    | [Report Dashboard](../tracking/report-dashboard.md) | Charts and lists by category (visa, passport, applications, …) |
    | [Mark incomplete](../person/mark-incomplete.md) | Manual **Incomplete** flag on a person — officer-controlled |
    | **State notifications** (future) | Automatic **validity** and **data completeness** inbox |

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/state-notifications.mp4"
  title="State notifications in Visa2026"></video>

<p class="visa-manual-video-caption">Recording placeholder — prototype inbox for administrators.</p>

## Where it is implemented (for IT / developers)

| Layer | Location |
|-------|----------|
| Plan | `docs/STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md` |
| Module — inbox host + sample data | `Visa2026.Module/BusinessObjects/StateNotifications/` |
| Module — summary / filters | `Visa2026.Module/Services/StateNotifications/` |
| Blazor — inbox UI | `Visa2026.Blazor.Server/Editors/BoStateNotificationInboxComponent.razor` |
| Blazor — header bell | `Visa2026.Blazor.Server/Components/StateNotificationHeaderBadge.razor` |
| Navigation | `BoStateNotificationInboxModelUpdater.cs` → **Operations → State notifications** |
| Permissions | `Updater.cs` — `EnsureAdminOnlyOperationsDeny` (officers denied until Phase 2) |

## Step 1 — Open State notifications (administrators)

1. Sign in with an **administrator** account.
2. In the left menu, expand **Operations**.
3. Select **State notifications**.

If **State notifications** is missing, your role does not include it — this is expected for standard visa officers today.

![State notifications inbox](../../../assets/screenshots/v2026.08/en/state-notifications-step-01-inbox.png)

## Step 2 — Understand the inbox layout

| Area | Purpose |
|------|---------|
| **UI prototype** badge | Reminder that rows are sample data |
| **Sync states** | Future: recompute notifications from live evaluators (prototype shows a message only) |
| Summary tiles | Counts by **Critical**, **Warning**, **Info**, **Open**, **Missing data** — click to filter |
| Search box | Filter cards by person name or text |
| Status chips | **All**, **Open**, **Snoozed**, **Resolved** |
| Category chips | **All**, **Validity state**, **Missing data** |

## Step 3 — Read a notification card

Each card shows:

| Element | Meaning |
|---------|---------|
| Severity | **Critical**, **Warning**, or **Info** |
| Category | **Validity state** (dates/process) or **Missing data** (attachments / required records) |
| Title | Person name and document identifier (sample text in prototype) |
| Message | Why the item needs attention |
| Metadata | Event date, days remaining, person, detected date |

**Open** items show **Open person** or **Open record** (wording depends on category) and **Snooze**. **Snoozed** items show **Reopen**.

## Step 4 — Filter and search

1. Click a **summary tile** (for example **Critical**) to narrow the list.
2. Use **Missing data** to see data-completeness-style samples.
3. Type in the **search** box to find a person name.
4. Combine **Open** / **Snoozed** / **Resolved** chips with category filters.
5. Select **Reset** when filters are active to clear them.

## Step 5 — Try prototype actions (demonstration only)

| Action | Prototype behaviour |
|--------|---------------------|
| **Sync states** | Spinner + toast — no database recompute |
| **Open person** / **Open record** | Toast only — no navigation to Person detail |
| **Snooze** / **Reopen** | Changes the sample row in memory for the session |

When live evaluators ship, **Sync states** will refresh from real person and document data, and open actions will jump to the correct form.

## Step 6 — Header bell (when enabled)

When your server enables the header widget:

1. The **bell** appears in the top bar.
2. A badge shows the count of **open critical** notifications.
3. Clicking the bell opens **State notifications** (critical filter when count &gt; 0).

If you do not see a bell, use **Operations → State notifications** (administrators) or wait for your office rollout.

## Planned officer workflow (future)

When Phase 2+ is released for visa officers:

1. Bell or **Operations → State notifications** shows **live** issues.
2. Officers fix data on the person or document form.
3. **Sync states** clears items automatically when the condition no longer applies — no manual “mark done”.
4. [Report Dashboard](../tracking/report-dashboard.md) remains for aggregate charts; the inbox is for **action now**.

## Common problems

| Problem | What to do |
|---------|------------|
| Menu item missing | Officer roles are denied today — use Report Dashboard or ask IT about rollout |
| All sample / fake names | Expected — **UI prototype** badge |
| Open person does nothing useful | Prototype only — open the person from **Employees** manually |
| Confused with **Incomplete** flag | [Mark incomplete](../person/mark-incomplete.md) is manual; notifications are automatic (when live) |
| Bell missing | May be disabled in host configuration — administrator path still works |

## What to read next

- [Main navigation](../../getting-started/navigation.md) — shell, Operations menu, bell (intro)
- [Report Dashboard](../tracking/report-dashboard.md) — charts and drill-down lists
- [Mark incomplete or complete](../person/mark-incomplete.md) — manual incomplete persons
- [What Visa2026 does](../../about/capabilities.md) — feature #6