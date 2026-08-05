---
title: Create an application
slug: applications/create
locale: en
tier: 4
guideStatus: draft
bo: Application
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
  - applications/overview
  - employee/register
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/APPLICATION_PROGRESS_STATE_VALIDATION.md
  - docs/BUSINESS_LOGIC_BASELINE.md
---

# Create an application

This guide shows how to create a new **application** header in Visa2026 — the collective case file for a ministry or migration request (invitation, visa extension, registration, work permit, and other procedure types).

An application holds shared header data (type, date, contract, visa settings). You add people on the **Application items** tab in a separate guide.

**Two header lists:** create on **Applications (via ministry)** or **Applications (direct migration)** — see [Applications — ministry and direct migration](overview.md) to choose the correct list.

!!! tip "Prerequisites"
    Sign in ([login](../../getting-started/login.md)), know the shell ([navigation](../../getting-started/navigation.md)), and have person master data ready ([Register an employee](../employee/register.md) or other person guides). Incomplete person records make application lines harder to fill later.

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with create access on **Applications** | Ask your supervisor if **New** is missing |
| The correct **workflow list** | Ministry route vs direct migration — see step 1 |
| **Application type code** | Three-digit ministry code (for example invitation or extension) — your office procedure list |
| Lookup values | **Project Contract**, **Urgency**, visa period/category/type when the type shows them |

| List you open | Use when the procedure… |
|---------------|-------------------------|
| **Applications (via ministry)** | Goes through ministry approval before the migration service |
| **Applications (direct migration)** | Goes directly to the migration service (no ministry leg) |

If you are unsure which list to use, ask your visa chief — the application type must match the route.

## Step 1 — Open the correct Applications list

Choose **one** header list under **Applications**. The steps below are the same on both lists; only the menu label and available application types differ.

### Applications (via ministry)

1. Sign in to Visa2026.
2. In the left menu, expand **Applications**.
3. Select **Applications (via ministry)**.
4. Wait for the list to load.

Use this list when the procedure goes through **ministry approval** before the migration service (for example invitation or visa extension).

### Applications (direct migration)

1. Sign in to Visa2026.
2. In the left menu, expand **Applications**.
3. Select **Applications (direct migration)**.
4. Wait for the list to load.

Use this list when the procedure goes **directly** to the migration service (for example registration check-in/out).

The toolbar on either list should show **New** and **Refresh**.

![Applications group in the left menu](../../../assets/screenshots/v2026.08/en/navigation-step-02-left-menu.png)

!!! note "Why two lists?"
    Visa2026 keeps ministry and direct-migration workflows separate so officers see the right application types and progress rules for each route. See [Applications — ministry and direct migration](overview.md).

!!! warning "Stay on the list where you started"
    A ministry-route application appears only under **Applications (via ministry)**. A direct-migration application appears only under **Applications (direct migration)**.

## Step 2 — Start a new application

1. On the list toolbar, select **New**.
2. Wait for the **application detail form** to open.

You are on a blank application. **Application Date** usually defaults to today.

![Applications list ready for New](../../../assets/screenshots/v2026.08/en/application-create-step-01-applications-list.png)

## Step 3 — Choose the application type

On the **Application** tab (top of the form):

1. In **Application Type Code**, enter the **three-digit code** for your procedure (for example ministry codes for invitation, change invitation, or work permit extension).
2. Or select the **…** (browse) control beside **Application Type Code** to pick from the filtered list.
3. Wait until **Application Type** (read-only name) fills in.

The form then shows only the header fields that apply to that type (contract, urgency, visa period, border zone, business trip dates, and so on).

![Application type selected on the header form](../../../assets/screenshots/v2026.08/en/application-create-step-02-type-selected.png)

!!! warning "Type cannot be changed casually"
    After you save, **Application Type** is fixed on the detail form. If you picked the wrong type, ask your supervisor before deleting or correcting the record.

## Step 4 — Fill header fields

Enter values using the on-screen labels. Fields **appear or hide** based on the application type.

| Field | When officers set it |
|-------|----------------------|
| **Application Date** | Usually today — confirm or adjust per office rules |
| **Project Contract** | Active construction contract for this request |
| **Urgency** | Processing priority (default may apply) |
| **Visa Period** / **Visa Category** / **Visa Type** | When the type is visa-related |
| **Migration Service** | Target migration office when shown |
| **Border Zone Location** | When the type requires border-zone permission |
| **Business Trip** dates / purpose | When the type is a business trip |
| **From City** / **To City** | When the type requires travel cities |

Optional **manual numbering** (legacy imports) is behind **Show optional fields** — normal new applications use automatic numbering on save.

## Step 5 — Save the application

1. Review the header values.
2. Select **Save** on the toolbar.
3. Wait until the save completes.

On first save, Visa2026 typically:

- Assigns **Application Number** and **Full Application Number** (office numbering profile)
- Leaves **Progress history** empty — the file is treated as **being prepared at the office** until you add the first progress row ([Track application progress](progress.md))

If **Save** fails, read the validation message — **Application Type** and any visible required fields must be filled.

![Application saved with number assigned](../../../assets/screenshots/v2026.08/en/application-create-step-03-saved-header.png)

!!! success "Application created"
    When **Full Application Number** appears on the form, the header is saved.

## Step 6 — Confirm on the Applications list

1. Return to the same **Applications** list (or use **Save and Close**).
2. Find the row by **Full Application Number** or **Application Date**.
3. Open the row to view the detail form.

Check **Application Type**, **Current status** (from latest progress), and the **Application items** tab — it starts empty until you add people.

## Step 7 — Check progress (optional)

1. On the application detail form, open the **Progress** tab.
2. **Progress history** may be empty — that means implied **office preparation**.
3. When the file moves, add progress rows — see [Track application progress](progress.md).

## Common problems

| Problem | What to do |
|---------|------------|
| **Applications** menu missing | Your role may not allow applications — ask your supervisor |
| Wrong type in the picker | You may be on the wrong list (via ministry vs direct migration) — start again from step 1 |
| **Application Type Code** not recognized | Check the three-digit code; use **…** to browse valid codes for this list |
| No **New** on the list | Open the list under **Applications**, not **Application items** |
| Header fields missing | They may not apply to this type — confirm the type code |
| Cannot add people yet | Save the header first, then use **Application items** (*next guide*) |

## What to read next

- [Applications — ministry and direct migration](overview.md) — choose the correct list
- [Add application items](add-items.md) — add people to this application
- [Track application progress](progress.md) — record workflow steps
- [What Visa2026 does](../../about/capabilities.md) — applications in the feature overview
- [Main navigation](../../getting-started/navigation.md) — Applications menu structure