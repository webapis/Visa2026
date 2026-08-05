---
title: Add application items
slug: applications/add-items
locale: en
tier: 4
guideStatus: draft
bo: ApplicationItem
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - applications/overview
  - applications/create
  - employee/register
  - employee/add-passport
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: application-add-items.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/OPTIONAL_DETAIL_FIELDS.md
  - docs/APPLICATION_ITEM_DOCUMENT_COPIES.md
---

# Add application items

This guide shows how to add **application items** — one row per person — on a saved **application**. Each line links an employee or family member to the parent application and carries the passports, visas, and other documents needed for that procedure.

Complete person master data first (passport, visa, medical record, address, and so on). When you pick **Person**, Visa2026 fills **Current\*** document fields from the person's records.

**Two routes:** ministry-route applications use **Applications (via ministry)** and **Application items (ministry)**; direct-migration applications use **Applications (direct migration)** and **Application items (migration)**. See [Applications — ministry and direct migration](overview.md).

!!! tip "Prerequisites"
    You need a saved application ([Create an application](create.md)) and complete person records ([Register an employee](../employee/register.md), [Add a passport](../employee/add-passport.md), and related guides as needed).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/application-add-items.mp4"
  title="Add application items in Visa2026"></video>

<p class="visa-manual-video-caption">Recording placeholder — steps below match the application item flow.</p>

## Before you start

| You need | Notes |
|----------|--------|
| A saved **application** with **Application Type** set | [Create an application](create.md) |
| **Person** records with nested documents | Passport required; visa, medical, address, etc. depend on application type |
| The correct person role | Employee-only types list **Employees**; family types list **Family Members** |

| Application type category | Person picker shows |
|---------------------------|---------------------|
| Employee procedures | **Employees** only |
| Family member procedures | **Family members** only |
| Both | Employees and family members |

Each person can appear **only once** on the same application.

| Route | Open the header from | Search lines across applications |
|-------|----------------------|----------------------------------|
| **Via ministry** | **Applications (via ministry)** | **Application items (ministry)** |
| **Direct migration** | **Applications (direct migration)** | **Application items (migration)** |

## Step 1 — Open the application

Use the **same route** as when you created the header.

### From Applications (via ministry)

1. Left menu → **Applications** → **Applications (via ministry)**.
2. Find the application by **Full Application Number**.
3. Open the application **detail form**.

### From Applications (direct migration)

1. Left menu → **Applications** → **Applications (direct migration)**.
2. Find the application by **Full Application Number**.
3. Open the application **detail form**.

You can also open the application from **Report Dashboard** drill-down when you started from a chart.

![Application detail with tabs](../../../assets/screenshots/v2026.08/en/application-create-step-03-saved-header.png)

## Step 2 — Open the Application items tab

1. On the application detail form, select the **Application items** tab.
2. Wait for the nested list to load.

The nested toolbar should offer **New** (or **New Application Item**).

If the tab is hidden, the application type may not use person lines — confirm the **Application Type** with your supervisor.

## Step 3 — Start a new application item

1. On the **Application items** nested list toolbar, select **New**.
2. Wait for the application item **detail form** to open.

The parent **Application** is already linked.

![New application item form](../../../assets/screenshots/v2026.08/en/application-add-items-step-02-item-form-new.png)

## Step 4 — Select the person

1. In **Person**, open the dropdown and choose the employee or family member for this line.
2. Wait until the form refreshes.

Visa2026 copies **current** documents from the person:

| Field (when shown) | Typical source on the person |
|--------------------|------------------------------|
| **Current Passport** | Latest passport on the person |
| **Current Visa** | Visa on that passport (as of application date) |
| **Current Medical Record** | Latest medical record |
| **Current Address Of Residence** | Latest address |
| **Current Education** | Latest education (employee types) |
| **Current Position History** / **Salary** / **Work duty** | Employee-only fields when the type requires them |

!!! warning "Archived persons"
    Archived persons may still appear in the list. Visa2026 may show a warning — confirm with your office policy before proceeding.

!!! tip "Missing Current fields?"
    If a required **Current\*** field stays empty, return to the person record and add or update the nested document (for example [Add a passport](../employee/add-passport.md)), then try again.

## Step 5 — Review visible fields

Fields on the form **depend on the application type**. Only review what you see.

| Area | What to check |
|------|----------------|
| **Document links** | **Current Passport** (always required), **Current Visa**, work permit, invitation, education, etc. |
| **Border zone** / **Work permitted locations** | Multi-select catalogs when shown |
| **Travel** (registration types) | **Travel Date**, **Check Point** — often pre-filled; use **Show optional fields** for extra travel columns |
| **Business trip** | Address when the type uses business trips |

Change any **Current\*** lookup if the auto-selected document is not the one for this procedure (for example an older passport).

## Add border zone or work-permitted location labels (tenant catalog)

When the application type shows **Border zone** or **Work permitted locations** (multi-select), officers maintain the underlying **tenant** labels from the same form:

| Field | Tenant catalog | Typical use |
|-------|----------------|-------------|
| **Border zone** | **Border zone name** | Application items, visas — comma-separated zone labels |
| **Work permitted locations** | **Work-permitted location name** | Work permit items |

### Steps

1. On the application item row, open the multi-select picker for the field.
2. Search for the label.
3. If it exists, select it.
4. If it is new, choose **New** → enter **Name (Tm)** → **Save** → add to the selection.
5. **Save** the application item.

!!! warning "Avoid duplicate catalog labels"
    Search before **New**. Duplicate border-zone or location labels split selections across officers and break consistent reporting — **reuse the existing label** when it is already in the catalog.

## Step 6 — Save the application item

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

If validation fails:

- Read the message — **Person**, **Current Passport**, and visible required **Current\*** fields must be filled.
- *Person already on this application* means that person is on another line — edit the existing row or pick someone else.

![Application item saved](../../../assets/screenshots/v2026.08/en/application-add-items-step-03-item-saved.png)

!!! success "Person line added"
    The row appears on the **Application items** tab with the person's name and document links.

## Step 7 — Add more people (optional)

1. Return to the **Application items** tab on the same application.
2. Select **New** again for each additional person.
3. Repeat steps 4–6.

The application header **Person count** (on lists) updates as you add lines.

## Step 8 — Confirm on the Application items tab

1. Open the **Application items** tab.
2. Confirm each person appears once with the expected **Person** name.
3. Use **Refresh** if a row does not appear immediately.

### Search lines on the standalone lists

Use the matching **Application items** list when you work across many applications:

| Parent route | Standalone list |
|--------------|-----------------|
| Ministry route | **Application items (ministry)** |
| Direct migration | **Application items (migration)** |

From these lists you can open a line, jump to the parent application, or use **Document copies** ([Ministry document copies](document-copies.md)).

## Common problems

| Problem | What to do |
|---------|------------|
| Person not in the dropdown | Wrong application type category (employee vs family); person already on this application; check role |
| **Current Passport** empty | Add a passport on the person first |
| **Current Visa** empty | Add a visa on the passport; confirm application date for visa validity |
| Required field hidden | Select **Show optional fields** (gear) for registration travel extras |
| Duplicate border zone label | Search the picker before **New** — select the existing label |
| Cannot save registration line | Fill **Travel Date** and **Check Point** for external registration types |
| Tab read-only | Parent application may be in a terminal workflow state — ask a senior officer |

## What to read next

- [Applications — ministry and direct migration](overview.md) — ministry vs direct migration lists
- [Create an application](create.md) — if you still need the header
- [Track application progress](progress.md)
- [Ministry document copies (PDF package)](document-copies.md)
- [What Visa2026 does](../../about/capabilities.md) — feature overview