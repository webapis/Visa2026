# User manual — documentation curriculum

**Pedagogy:** document from **simplest** (read/navigate, basic CRUD on one BusinessObject) to **hardest** (Word/Excel **template authoring** and staging). Do not publish advanced guides before earlier tiers exist for the same BO family.

**Skill:** [SKILL.md](./SKILL.md) · **Advisory:** [advisory.md](./advisory.md) · **Backlog:** [tracking.md](./tracking.md) § Guide inventory

---

## 1. Why this order

| Reason | Detail |
|--------|--------|
| Officer learning curve | Officers master Person before Application before packages |
| E2E complexity | `PersonOfficerJourney` before multi-BO application flows |
| BO catalog dependency | Reference pages for `Person`/`Passport` before `ApplicationItem` |
| Risk | Template guides wrong if CRUD guides omit required fields |
| Manual nav | MkDocs sidebar follows tiers — readers see a clear path |

**Rule:** new guide `tier` frontmatter must be ≥ any prerequisite tier. Validator may warn on skip (Phase 2+).

---

## 2. Tier ladder (simple → hard)

| Tier | Name | Officer skills | Typical BOs | Doc focus |
|------|------|----------------|-------------|-----------|
| **0** | Getting started | Login, shell, roles, daily overview | — | No CRUD |
| **1** | **Read & navigate** | Open lists, filters, detail views | `Person`, lookups (read-only use) | **R** in CRUD |
| **2** | **Create** | New employee, child records | `Person`, `Passport`, `Education` | **C** |
| **3** | **Update & maintain** | Edit fields, incomplete flag, collections | `Person`, `EmployeeSalary`, nested tabs | **U** |
| **4** | **Application workflow** | Header + items + progress | `Application`, `ApplicationItem`, `ApplicationProgress` | Multi-BO |
| **5** | **Packages & previews** | Document copies, Resminamalar ZIP | `ApplicationItem`, hosts | Generate/download |
| **6** | **360° & tracking** | Dossier, Report Dashboard | `PersonDossierHost`, dashboard | Read + export |
| **7** | **Template generation** | Upload template, placeholders, staging sync | `UserReportTemplate` | **Hardest** |

**Delete:** rarely officer-facing; document only where product allows (e.g. cancel application) — usually **tier 4** side note, not a dedicated tier.

---

## 3. Guide sequence (recommended publish order)

Publish in this order within each phase. **Phase 2 pilots** = first rows through tier 3–4.

| Order | Tier | Slug (planned) | Title | `bo` | Prereq guides |
|-------|------|----------------|-------|------|---------------|
| 1 | 0 | `getting-started/login` | Login and roles | — | — |
| 2 | 0 | `getting-started/navigation` | Main navigation | — | 1 |
| 3 | 1 | `person/open-and-search` | Find and open a person | Person | 2 |
| 4 | 2 | `person/register` | Register a new employee | Person | 3 |
| 5 | 2 | `person/add-passport` | Add a passport | Passport | 4 |
| 6 | 3 | `person/edit-employee` | Update employee details | Person | 4 |
| 7 | 3 | `person/mark-incomplete` | Mark incomplete / complete | Person | 4 |
| 8 | 3 | `person/nested-education` | Education records | Education | 4 |
| 9 | 4 | `applications/create` | Create an application | Application | 4, 6 |
| 10 | 4 | `applications/add-items` | Add application items | ApplicationItem | 9 |
| 11 | 4 | `applications/progress` | Track application progress | ApplicationProgress | 9, 10 |
| 12 | 5 | `applications/document-copies` | Ministry document copies | ApplicationItem | 10 |
| 13 | 5 | `applications/resminamalar` | Resminamalar report package | Application | 9 |
| 14 | 6 | `person/dossier` | Person dossier | Person | 4, 6 |
| 15 | 6 | `tracking/report-dashboard` | Report Dashboard | — | 2 |
| 16 | 7 | `administration/user-report-templates` | User report templates | UserReportTemplate | 13 |
| 17 | 7 | `administration/template-staging` | Edit and sync templates | UserReportTemplate | 16 |

---

## 4. Tier → CRUD mapping (per BO family)

Use when splitting one BO into multiple guides.

### Person family (tiers 1–3)

| Operation | Guide pattern | Example |
|-----------|---------------|---------|
| **Read** | Open list → open detail | `person/open-and-search` |
| **Create** | New → required fields → Save | `person/register` |
| **Create child** | Detail tab → New → Save | `person/add-passport` |
| **Update** | Detail → edit → Save | `person/edit-employee` |
| **Update (flag)** | Incomplete tab / toolbar | `person/mark-incomplete` |

### Application family (tier 4)

| Operation | Guide pattern |
|-----------|---------------|
| **Create** | Application type → header fields → Save |
| **Create child** | Items tab → add person lines |
| **Update** | Progress history / manual advance |
| **Read** | List filters, status columns |

### Packages (tier 5) — not CRUD

Officers **select and generate**; underlying BOs are not edited in the dialog.

| Feature | Guide | Skill doc source |
|---------|-------|------------------|
| Ministry PDF ZIP | `applications/document-copies` | `APPLICATION_ITEM_DOCUMENT_COPIES.md` |
| Word/Excel ZIP | `applications/resminamalar` | `APPLICATION_REPORT_PACKAGE.md` |

### Template generation (tier 7) — hardest

| Step | Officer/admin action | Dev counterpart |
|------|----------------------|-----------------|
| 1 | Understand when to use custom template | `USER_TEMPLATE_AUTHOR_GUIDE.md` |
| 2 | Upload `.docx` / `.xlsx` | User Report Templates BO |
| 3 | Extract placeholders | Toolbar |
| 4 | Validate placeholders | Toolbar |
| 5 | Set visibility / active | Detail form |
| 6 | (Optional) Edit template → Sync to database | `TEMPLATE_STAGING_EDIT.md` |
| 7 | Verify in Resminamalar preview + ZIP | Tier 5 guide |

**Do not** put tier 7 content in tier 2 guides — link forward only.

---

## 5. MkDocs sidebar structure (follow tiers)

```text
Getting started          (tier 0)
Person records           (tiers 1–3) — ordered: search → register → passport → edit → …
Applications             (tier 4)
Document packages        (tier 5)
Tracking & dossier       (tier 6)
Administration           (tier 7 — templates)
Reference                (auto from catalog — all tiers)
```

Within **Person records**, sort guides by **tier** then **order** column in tracking.

---

## 6. Frontmatter (tier + prerequisites)

```yaml
---
title: Register a new employee
slug: person/register
tier: 2
tierName: Create
bo: Person
prerequisiteSlugs:
  - person/open-and-search
operations: [create]
status: draft
---
```

| Field | Purpose |
|-------|---------|
| `tier` | 0–7; controls sidebar sort |
| `tierName` | Human label for index pages |
| `prerequisiteSlugs` | Must be `published` before this guide goes `published` |
| `operations` | `read` \| `create` \| `update` \| `delete` \| `generate` |

---

## 7. Agent rules

1. **Plan backlog** using §3 order — do not jump to tier 5/7 first.
2. **Offer user** “next guide in curriculum” vs “infra only” ([advisory.md](./advisory.md) §5).
3. **One CRUD operation per guide** when possible (easier E2E + screenshots).
4. **Reference pages** (catalog) can exist early; **how-to** guides follow tier order.
5. **Template generation** last — depends on Resminamalar (tier 5) for verification story.

---

## 8. Phase alignment

| Phase | Curriculum coverage |
|-------|---------------------|
| **2** | Tiers 0–4 pilots (through `applications/progress` or first tier-5 if time) |
| **3** | E2E + media for tiers 2–4 guides |
| **4** | Tiers 5–6 + **tr/tk/ru** for earlier tiers |
| **4–5** | Tier 7 administration |

---

## 9. Changelog

| Date | Change |
|------|--------|
| 2026-08-04 | Initial curriculum v0.1 — CRUD-first through template generation |
