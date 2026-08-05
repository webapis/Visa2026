---
title: What Visa2026 does
slug: about/capabilities
locale: en
tier: 0
guideStatus: published
---

# What Visa2026 does

Visa2026 is the visa department's operational system for foreign **employees**, **family members**, and **temporary visitors** in Turkmenistan migration procedures.

The list below is ordered by **importance** — from what officers use every day to supporting tools. Each item states the **problem** it solves and what you do in the application.

!!! tip "How-to guides"
    This page explains **why** each area exists. Step-by-step instructions are in **Guides** (or marked *guide coming* below). See [Manual roadmap](roadmap.md) for publication status.

---

## 1. Report Dashboard

**Problem:** Officers cannot see at a glance which visas, passports, registrations, or applications need action — work is discovered too late or by manual lists.

**What Visa2026 does:** After sign-in you land on **Report Dashboard** — charts by category (visa, passport, registration, work permit, travel, incomplete persons, person search, and more). Open a chart segment to drill down to a filtered list or export to Excel.

**Guide:** [Report Dashboard](../guides/tracking/report-dashboard.md) — also [Main navigation](../getting-started/navigation.md).

---

## 2. Person master data

**Problem:** One foreign worker has many documents (passport, visa, medical, address, education, travel, and more) scattered across paper files or spreadsheets — hard to keep complete and consistent.

**What Visa2026 does:** Store each person once under **Employees**, **Family Members**, or **Temporary visitor**, with nested tabs for passports, visas, medical records, addresses, and other records. Application lines pull the correct **current** documents automatically.

**Guides:** [Find and open a person](../guides/person/open-and-search.md) · [Register an employee](../guides/employee/register.md) · [Family member](../guides/family-member/register.md) · [Temporary visitor](../guides/temporary-visitor/register.md)

---

## 3. Applications and application items

**Problem:** Ministry and migration requests (invitation, visa and work permit, extension, registration check-in/out, border zone, business trip, cancellation, and more) are prepared on mixed forms with no single case file per request.

**What Visa2026 does:** Create an **Application** (header: type, contract, dates) and add **application items** — one row per person with the fields that application type requires. Separate navigation for **Applications (via ministry)** and **Applications (direct migration)**.

**Guides:** [Applications — ministry and direct migration](../guides/applications/overview.md) · [Create an application](../guides/applications/create.md) · [Add application items](../guides/applications/add-items.md) — person records should be complete first (see guides above).

---

## 4. Application progress

**Problem:** "Where is the file?" and "how long has it been at the ministry?" are tracked in email or notebooks — no shared timeline or SLA view.

**What Visa2026 does:** Append **application progress** rows — each **state** records a workflow step (preparing, ministry review, process started, issued, rejected, …). Ministry-route steps may show the approving ministry name. The latest row is the current status; officers can attach **ministry decision letters** on approval/rejection steps.

**Guide:** [Track application progress](../guides/applications/progress.md)

---

## 5. Document copies (ministry PDF package)

**Problem:** Ministry ZIP packages fail or surprise officers because missing passport scans or form gaps are found only after queueing the job.

**What Visa2026 does:** On an **application item** list, open **Document copies** — see per-document **readiness**, preview each slot, confirm gaps, then enqueue a **PDF package** (filled application forms plus supporting scans).

**Guide:** [Ministry document copies (PDF package)](../guides/applications/document-copies.md)

---

## 6. State notifications

**Problem:** Expiring visas, passports, or work permits and missing required scans are noticed only when someone opens each record.

**What Visa2026 does:** *(Planned, not in officer rollout.)* A header **bell** and **Operations → State notifications** inbox would list validity and data-completeness alerts. A developer UI prototype exists in the codebase but is **not** being productized for officers.

**Guide:** *Postponed* — use [Report Dashboard](../guides/tracking/report-dashboard.md) for operational overview and [Mark incomplete or complete](../guides/person/mark-incomplete.md) for officer-flagged persons.

---

## 7. Templates (report packages)

**Problem:** Cover letters, ministry letters, and operational Word/Excel reports are assembled manually from many templates with no preview or batch control.

**What Visa2026 does:** Open **Templates** on an **Application** or **application item** — pick from a catalog, check readiness, preview, and download a ZIP of generated reports.

**Guide:** [Templates report package (Resminamalar)](../guides/applications/resminamalar.md). Administrators: [User report templates](../guides/administration/user-report-templates.md) · [Edit and sync templates](../guides/administration/template-staging.md).

---

## 8. Person dossier

**Problem:** Supervisors ask for "everything about this employee" and officers rebuild the picture from many screens and folders.

**What Visa2026 does:** Open **Person dossier** from **Report Dashboard → Person search** or the **Dossier** column on person lists — a read-only **360°** view (screen or paper layout) with optional director export (HTML/PDF and document ZIP).

**Guide:** [Person dossier](dossier.md) — also introduced in [Find and open a person](open-and-search.md).

---

## 9. Mark incomplete / Incomplete persons

**Problem:** After legacy migration or partial data entry, there is no office-wide way to mark "still fixing this person" without blocking real work.

**What Visa2026 does:** **Mark incomplete** / **Mark complete** on a person record (with notes and missing-area checkboxes). **Report Dashboard → Incomplete persons** shows the office-wide list.

**Guide:** [Mark incomplete or complete](../guides/person/mark-incomplete.md)

---

## 10. Person document copies

**Problem:** Scans are spread across many person tabs — officers preview attachments one tab at a time before ministry or management review.

**What Visa2026 does:** **Document copies** on a person detail form (or **Copies** on a list row) opens a sectioned catalog in the preview panel — browse and preview scans from all person tabs in one place. Ministry ZIP packaging still uses **Document copies** on application items.

**Guide:** [Person document copies](../guides/person/document-copies.md). Ministry ZIP: [Document copies on application items](../guides/applications/document-copies.md).

---

## 11. Configuration (office settings)

**Problem:** Company letterhead, application numbering, ministry approval routes, SLA thresholds, and upload limits are scattered in spreadsheets or IT tickets — officers cannot self-serve when templates or workflows need updating.

**What Visa2026 does:** The **Configuration** menu ( **VisaOffice** / administrators) holds singletons (company, signatory, numbering, ministry SLA, upload limits) and catalogs (project contracts, approving ministries, approval leg profiles, migration SLA profiles, document expiration alerts).

**Guides:** [Configuration overview](../guides/administration/configuration/overview.md) · [Organization settings](../guides/administration/configuration/organization.md) · [Contracts and approvals](../guides/administration/configuration/contracts-and-approvals.md) · [SLA settings](../guides/administration/configuration/sla.md) · [Alerts and upload limits](../guides/administration/configuration/alerts-and-upload-limits.md)

---

## Who this manual is for

| Role | Typical use |
|------|-------------|
| **Visa Officer** | Person records, applications, document packages, dashboard |
| **Visa Chief / supervisor** | Dashboard, dossier, Excel exports, incomplete-persons view |
| **Administrator** | [Configuration](../guides/administration/configuration/overview.md), [User report templates](../guides/administration/user-report-templates.md), [Edit and sync templates](../guides/administration/template-staging.md), roles |

## Start learning

1. [Sign in](../getting-started/login.md)  
2. [Main navigation](../getting-started/navigation.md)  
3. [Find and open a person](../guides/person/open-and-search.md)  
4. [Register an employee](../guides/employee/register.md)