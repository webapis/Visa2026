---
title: Person dossier
slug: person/dossier
locale: en
tier: 6
guideStatus: draft
bo: Person
navPath: Person
roles: [Visa Officer, Visa Chief]
prerequisiteSlugs:
  - person/open-and-search
  - getting-started/navigation
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/PERSON_DOSSIER.md
  - docs/REPORT_DASHBOARD.md
---

# Person dossier

This guide shows how to open the **person dossier** — a read-only **360° summary** of one employee, family member, or temporary visitor. Use it when a supervisor or director asks "everything about this person" without editing master data.

The dossier is **not** the editable person detail form. Officers **edit** on **Employees** / **Family Members** / **Temporary visitor** tabs; the dossier **displays** identity, current status, and history in one page.

!!! tip "Prerequisites"
    Know how to find a person ([Find and open a person](open-and-search.md)) and the shell ([Main navigation](../../getting-started/navigation.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| Concept | Officer meaning |
|---------|-----------------|
| **Person dossier** | Read-only summary page (dosye) — one person, all key records |
| **Detail form** | Editable tabs (Passports, Educations, …) — use for data entry |
| **Screen** / **Paper** | Two layouts on the dossier — dashboard vs print-shaped preview |
| **Document copies** (on dossier) | Person-scoped scan catalog in the **right preview panel** — not ministry PDF ZIP |

| Entry point | Where |
|-------------|-------|
| **Open dossier** | Person **detail form** toolbar (saved person only) |
| **Dossier** column | **Employees**, **Family Members**, or **Temporary visitor** list — first column link |
| **Person search** | **Report Dashboard** → **Person search** → click a result row |

Directors are typically **not** Visa2026 users — hand them the **Export for director** ZIP (step 8).

## Step 1 — Open the dossier

Choose one entry path:

### From a person detail form

1. Open **Employees**, **Family Members**, or **Temporary visitor** and open a **saved** person.
2. On the toolbar, select **Open dossier**.
3. Wait for the loading panel (progress message and percent).

### From a person list

1. Open the person list.
2. In the **Dossier** column (first column), select the link for the row.
3. Wait for the dossier to load.

### From Report Dashboard Person search

1. Open **Report Dashboard**.
2. Select **Person search**.
3. Search by name, **Personal Number**, or passport number.
4. Click a **result row** to open the dossier.

!!! note "Dossier vs detail form"
    Clicking the **row** (not **Dossier**) opens the editable detail form. See [Find and open a person](open-and-search.md).

![Open dossier from person detail](../../../assets/screenshots/v2026.08/en/person-dossier-step-01-entry.png)

## Step 2 — Read identity and status tiles

When loading completes, the dossier shows:

| Area | What you see |
|------|----------------|
| **Identity** | Photo, name, role, contract, key identity fields |
| **Status tiles** | Current **passport**, **visa**, **work permit**, **registration** — valid / expiring / expired / missing |

Tiles answer "is their visa valid today?" without opening each tab on the detail form.

Sections below list history (passports, visas, work permits, education, travel, applications, and more). Sections **depend on person role** — family members omit employee-only blocks; temporary visitors show a smaller set.

![Dossier Screen view](../../../assets/screenshots/v2026.08/en/person-dossier-step-02-screen.png)

## Step 3 — Switch Screen or Paper view

1. Use **Screen** for the officer dashboard layout (tables and status colours).
2. Use **Paper** for a print-shaped layout — preview of what the director export looks like.

**Paper** stays in the main area so **Document copies** can remain open on the right.

## Step 4 — Open Document copies beside the dossier

1. On the dossier toolbar, select **Document copies**.
2. The **preview panel** opens on the right with the person-scoped scan catalog.
3. Browse sections, **Preview** attachments, and download individual files as needed.

Full steps: [Person document copies](document-copies.md).

The dossier stays on the left; scans on the right — you can review data and files together.

!!! note "Not ministry PDF"
    Person **Document copies** is for browsing person scans. Ministry **PDF packages** use [Document copies on application items](../applications/document-copies.md).

![Document copies beside dossier](../../../assets/screenshots/v2026.08/en/person-dossier-step-03-copies-slot.png)

## Step 5 — Review sections (read-only)

Scroll through dossier sections — each shows a table of records with counts in the section header.

| Section (typical) | Employee | Family member | Temporary visitor |
|-------------------|----------|---------------|-------------------|
| Passports, visas, work permits | Yes | Yes | Varies |
| Education, salary, work duty | Yes | No | No |
| Applications, invitations | Yes | Yes | Yes |

You **cannot edit** from the dossier. To fix data, open the person **detail form** from the list.

## Step 6 — Return to editing (optional)

1. Close the dossier tab or use **Close all tabs** as needed.
2. Open the person from the list (click the row, not **Dossier**).
3. Edit nested tabs (passport, visa, address, …) using the relevant guides.

## Step 7 — Export for director (optional)

When management needs a hand-over package:

1. On the dossier toolbar, select **Export for director**.
2. Wait for the background job to queue.
3. Watch the **export** toast at the bottom of the screen until it completes.
4. Download the ZIP from the toast.

The ZIP typically contains:

- **Dossier.pdf** — summary document (same content as Paper view)
- Folders of merged scan PDFs (passports, visas, …)
- **EXPORT_NOTES.txt** — records that had no readable attachment

![Export toast](../../../assets/screenshots/v2026.08/en/person-dossier-step-04-export-toast.png)

!!! success "Hand-over ready"
    Send the ZIP to the director by your office procedure (email, share drive, print). The export uses your current UI language.

## Common problems

| Problem | What to do |
|---------|------------|
| **Open dossier** disabled | Save the person first — new records have no dossier |
| Loading stuck at 0% | Wait; refresh browser; person may have very large history |
| Empty section | No records of that type on the person — normal for role or new person |
| Document copies closed when navigating | Re-open **Document copies** from the dossier toolbar |
| Opened dossier but need to edit | Return to list → open **row** (not **Dossier**) for detail form |
| Export toast slow | Large scan history — wait; job runs in background |
| Wrong person | Check name and **Personal Number** in the identity header |

## What to read next

- [Person document copies](document-copies.md) — sectioned scan catalog (detail form, list **•** column, dossier)
- [Find and open a person](open-and-search.md) — lists, search, **Dossier** column
- [Mark incomplete or complete](mark-incomplete.md) — office flag for incomplete data
- [Ministry document copies](../applications/document-copies.md) — application-item PDF ZIP (different from person copies on dossier)
- [What Visa2026 does](../../about/capabilities.md) — Person dossier in the feature overview