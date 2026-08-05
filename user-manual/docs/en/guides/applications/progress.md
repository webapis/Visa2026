---
title: Track application progress
slug: applications/progress
locale: en
tier: 4
guideStatus: draft
bo: ApplicationProgress
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - applications/overview
  - applications/create
  - applications/add-items
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/APPLICATION_PROGRESS_STATE_VALIDATION.md
  - docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md
---

# Track application progress

This guide shows how to record **application progress** — the timeline of where an application is in the workflow (office preparation, ministry review, migration service, issued, rejected, or cancelled).

Visa2026 keeps an **append-only history**: you **add a new progress row** when the file moves forward. You do not edit the current step in place. The **latest row** (by date and order) is the application's **current status** on lists and the header.

!!! tip "Prerequisites"
    Saved application header ([Create an application](create.md)), person lines when your office requires them ([Add application items](add-items.md)), and the correct route ([Applications — ministry and direct migration](overview.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). State labels are localized; steps are the same.

## Before you start

| Concept | Officer meaning |
|---------|-----------------|
| **Progress history** | Chronological list of workflow steps on the application |
| **State** | What happened (preparing, sent to ministry, approved, process started, issued, rejected, …) |
| **Current status** | The **latest** progress row — shown on application lists as **Status** / **Status date** |
| **Implied office step** | Before the first row exists, the file is treated as **being prepared at the office** |

| Route (from overview) | Typical first progress row officers add |
|-----------------------|----------------------------------------|
| **Applications (via ministry)** | Sent to **first ministry review** (or early rejection / cancellation) |
| **Applications (direct migration)** | **Process started** at the migration service (or cancellation) |

**Project Contract** on the header may be required before you can advance past office preparation — confirm it is set when the application type shows that field.

## Step 1 — Open the application

1. Open the application from the same header list you used when creating it:
   - **Applications (via ministry)**, or
   - **Applications (direct migration)**.
2. Find the row by **Full Application Number** and open the detail form.

Check **Status** and **Status date** on the header or list — they reflect the latest progress row (or implied office preparation when history is empty).

![Application detail — Progress tab area](../../../assets/screenshots/v2026.08/en/application-progress-step-01-progress-tab.png)

## Step 2 — Open the Progress tab

1. On the application detail form, select the **Progress** tab (upper tab group).
2. In **Progress history**, review existing rows.

Columns typically include **#**, **Status** (state plus ministry short name when relevant), **Date**, **Process number**, **Description**, and **Ministry letter** file name.

If the list is **empty**, the application is still in implied **office preparation** until you add the first row.

## Step 3 — Add a new progress row

1. In **Progress history**, select **New** on the nested toolbar.
2. Wait for the progress **detail form** to open.

Visa2026 usually **suggests the next state** allowed for this application route. You may change **State** only to values in the dropdown — illegal jumps are blocked on save.

![New progress row form](../../../assets/screenshots/v2026.08/en/application-progress-step-02-new-row-form.png)

## Step 4 — Set State and Date

1. In **State**, confirm or choose the workflow step (for example sent to ministry review, ministry approved, process started, issued).
2. Set **Date** to when this step took effect (defaults to today).
3. Optionally enter **Description** (short comment for colleagues).

| Field | When officers use it |
|-------|----------------------|
| **State** | Required — must be the next legal step for this route |
| **Date** | Required — cannot be before the previous row's date |
| **Description** | Optional note (max 255 characters) |
| **Process number** | When the state is **process started** at the migration service |
| **Ministry letter** | When the state is a **ministry approval or rejection** — attach the decision letter scan (PDF or image) |

!!! tip "Ministry name in the list"
    On ministry-route applications, the **Status** column may append the approving ministry short name from the **Project Contract** — verify the contract is correct before ministry steps.

## Step 5 — Save the progress row

1. Select **Save** on the progress detail toolbar.
2. Wait until the save completes.

Visa2026 updates the parent application's **current status** to this row.

If validation fails, read the message — common causes:

- **State** not allowed for this route (wrong ministry vs direct-migration application)
- **Date** before the previous step
- **Project Contract** missing when required
- Advancing from a **terminal** state (issued, rejected, cancelled)

![Progress row saved](../../../assets/screenshots/v2026.08/en/application-progress-step-03-row-saved.png)

!!! success "Progress recorded"
    The new row appears in **Progress history**. Application lists show the updated **Status** and **Status date**.

## Step 6 — Continue the workflow (typical paths)

Add another **New** row each time the file moves. The dropdown only offers **legal next steps**.

### Via ministry (simplified)

| Stage | What officers usually record |
|-------|------------------------------|
| Office | *(Implied until first row)* — prepare documents and application items |
| Ministry | Rows for **review started**, then **approved** or **rejected** per ministry leg on the contract |
| Migration service | **Process started** → **Issued** or **Rejected** |
| Closed | **Cancelled** if the case is withdrawn |

Multi-ministry contracts may require **second** (or further) ministry approval rows before the migration service step.

### Direct migration (simplified)

| Stage | What officers usually record |
|-------|------------------------------|
| Office | *(Implied)* — prepare the file |
| Migration service | **Process started** → **Issued** or **Rejected** |
| Closed | **Cancelled** if withdrawn |

No ministry review rows appear on direct-migration applications.

## Step 7 — Check status on the Applications list

1. Return to **Applications (via ministry)** or **Applications (direct migration)**.
2. Find the application and confirm **Status** / **Status date** match the latest progress row.
3. Use **Refresh** if needed.

**Report Dashboard** application charts use the same progress data for "on process" and "completed" views.

## Correcting the last step (supervisors)

Only the **last** row in **Progress history** may be deleted (if your role allows **Delete**). Use this for same-day corrections — not to rewrite older history.

If the application reached a **terminal** state (issued, rejected, cancelled), the header may become read-only except for controlled corrections — ask your visa chief.

## Common problems

| Problem | What to do |
|---------|------------|
| **State** dropdown is empty or short | Application may be in a terminal step, or route/contract legs do not match — ask supervisor |
| Cannot save first progress row | Set **Project Contract** if required; confirm you are on the correct ministry vs migration application |
| Ministry letter field hidden | **State** is not a ministry decision step — change state or attach on the correct row |
| Wrong status on the list | Open **Progress history** — latest **Date** wins; refresh the list |
| Cannot edit application header | Progress may have left office preparation or reached a terminal state — expected lock |
| Illegal transition message | Pick a **State** from the dropdown only; do not skip ministry legs |

## What to read next

- [Applications — ministry and direct migration](overview.md)
- [Create an application](create.md) · [Add application items](add-items.md)
- [Ministry document copies (PDF package)](document-copies.md)
- [What Visa2026 does](../../about/capabilities.md) — application progress in the feature overview