---
title: Main navigation
slug: getting-started/navigation
locale: en
tier: 0
guideStatus: published
lastReviewed: "2026-08-05"
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
screenshotsVersion: "2026.08"
screenshotsCapturedAt: "2026-08-05T08:48:03.3957272Z"
mediaE2eRunId: "20260805-134241"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: true
sourceDocs:
  - docs/REPORT_DASHBOARD.md
  - docs/STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md
verifiedAt: "2026-08-05T08:49:33.7498506Z"
verifiedCommit: "2d70b13c"
---

# Main navigation

This guide explains how to move around Visa2026 after you sign in: the home page, the left menu, lists, detail forms, and the header tools.

!!! tip "Prerequisite"
    Complete [Sign in to Visa2026](login.md) first so you land on the **Report Dashboard**.

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## The application shell

After sign-in, Visa2026 shows four main areas:

| Area | Purpose |
|------|---------|
| **Header** (top) | Application title, notification bell, your user menu |
| **Navigation menu** (left) | Open lists and workspaces |
| **Main content** (center) | Dashboard, lists, or detail forms |
| **Toolbar** (above content) | Actions such as **New**, **Save**, and **Refresh** for the current view |

<!-- media-capture: navigation-step-01-shell -->
![Application shell with left navigation menu](../../assets/screenshots/v2026.08/en/navigation-step-01-shell.png)

## Home page — Report Dashboard

Visa2026 opens **Report Dashboard** when you sign in. Use it to review summary charts and open related lists.

On the dashboard you can:

- Switch **categories** (for example visa, invitation, or person search) using the tabs or chips on the page
- Select **Refresh** (top right of the dashboard card) to reload numbers from the database
- Open a chart segment or use **Open ListView** to see matching records in a list

You can return to the dashboard anytime by choosing **Report Dashboard** in the left menu.

For full steps (categories, charts, **Open ListView**, Excel, **Person search**, incomplete persons), see [Report Dashboard](../guides/tracking/report-dashboard.md).

## Left navigation menu

The **navigation menu** on the left lists the areas your role allows. Groups may expand to show sub-items.

Typical officer menus include:

| Menu item | What it is for |
|-----------|----------------|
| **Report Dashboard** | Home — charts and overview |
| **Employees** | Employee person records |
| **Family Members** | Family member person records |
| **Temporary visitor** | Temporary visitor person records |
| **Applications** | Application headers (via ministry or direct migration paths) |
| **Applications (via ministry)** / **Applications (direct migration)** | Application lists by workflow type |
| **Application items (ministry)** / **Application items (migration)** | Lines on an application |
| **Invitation** / **Invitation items** | Invitation workflows |
| **Rejection** / **Rejection items** | Rejection workflows |
| **Work Permit** / **Work permit items** | Work permit workflows |
| **Operations** | Officer tools (see below) |
| **Reports** | User report templates (when your role allows) |
| **Configuration** | Office settings — company, contracts, SLA, upload limits (**VisaOffice** / administrators) |

!!! note "Your menu may differ"
    Supervisors assign roles that control which items appear. If you expect a menu and do not see it, ask your administrator — do not sign in as someone else.

<!-- media-capture: navigation-step-02-left-menu -->
![Left navigation menu expanded](../../assets/screenshots/v2026.08/en/navigation-step-02-left-menu.png)

## Open a list

Lists show many records in a table (for example all employees).

1. In the left menu, select **Employees**.
2. Wait for the list to load.

The list toolbar usually includes **New** (create a record), **Refresh** (reload the list), and search or filter controls.

<!-- media-capture: navigation-step-03-employees-list -->
![Employees list](../../assets/screenshots/v2026.08/en/navigation-step-03-employees-list.png)

The same pattern applies to other list menus: **Family Members**, **Applications (via ministry)**, and so on.

## Open a detail form

A **detail form** shows one record (for example one employee).

1. From a list, click a row (or double-click, depending on your browser).
2. The detail form opens in the main area.

Detail forms often use **tabs** along the top or side (for example **Passports**, **Educations**) for related information on the same person.

Common toolbar actions on a detail form:

| Action | When to use |
|--------|-------------|
| **Save** | After you change fields |
| **Save and Close** | Save and return to the list |
| **New** | Create another record (on lists) |
| **Refresh** | Reload the current view |
| **Delete** / **Remove** | Only when your role allows removing records |

<!-- media-capture: navigation-step-04-detail-form -->
![Employee detail form with tabs](../../assets/screenshots/v2026.08/en/navigation-step-04-detail-form.png)

!!! warning "Unsaved changes"
    If you navigate away without **Save**, your changes may be lost. Save before opening another menu item.

## Header — notifications and user menu

### Notification bell (postponed)

**State notifications** (header bell + inbox) is **postponed** — not part of the officer manual rollout. A UI prototype exists for administrators only; your office should not expect the bell or **Operations → State notifications** in daily work.

Use [Report Dashboard](../guides/tracking/report-dashboard.md) for expiry-style overview and [Mark incomplete or complete](../guides/person/mark-incomplete.md) when an officer flags a person manually.

### User menu

Open your **user menu** (top right) for account actions, for example:

- **My Details** — view your user profile
- **Change Password** — update your password (when enabled)
- **Log Off** — end your session

## Operations menu

Under **Operations**, officers may see:

| Item | Purpose |
|------|---------|
| **State notifications** | *Postponed* — not in officer rollout |
| **Import reimport history** | History of data import runs (when shown) |

Administrators see additional operations; your list may be shorter.

## Configuration menu (VisaOffice / administrators)

Under **Configuration**, maintain tenant settings that officers consume indirectly:

| Item | Guide |
|------|-------|
| Overview — all eleven menu items | [Configuration overview](../guides/administration/configuration/overview.md) |
| Company, numbering, signatory, representative | [Organization settings](../guides/administration/configuration/organization.md) |
| Contracts, ministries, approval legs | [Contracts and approvals](../guides/administration/configuration/contracts-and-approvals.md) |
| Migration and ministry SLA | [SLA settings](../guides/administration/configuration/sla.md) |
| Expiry alerts and upload limits | [Alerts and upload limits](../guides/administration/configuration/alerts-and-upload-limits.md) |

Standard **Visa Officer** roles do not see **Configuration**.

## Tips for daily work

1. **Start at Report Dashboard** for overview, then drill into lists.
2. **Use Refresh** if numbers or lists look stale after a colleague saved changes.
3. **One record at a time** — finish **Save** on a detail form before starting unrelated work.
4. **Search** on long lists — use the search box when the list view provides one.

## Common problems

| Problem | What to do |
|---------|------------|
| Menu item missing | Your role may not include it — ask your supervisor |
| List is empty | Check filters; select **Refresh**; confirm you have access |
| Detail form read-only | Record or field may be locked by workflow — check with a senior officer |
| Cannot find **Save** | Scroll the toolbar; widen the window; some tabs save separately |

## What to read next

- [Sign in to Visa2026](login.md) — if you need to sign in again
- [Find and open a person](../guides/person/open-and-search.md) — search and open employee records
- [Register a new employee](../guides/employee/register.md) — create a person from the **Employees** list
