# User manual — user prompts

Copy-paste in Cursor with `@visa2026-user-manual`.

---

## Before starting (advisory — use first)

- What is the **next guide in the curriculum** (tier order: CRUD → templates)?
- We need documentation for **{feature}** — which **tier** (0–7) and prerequisites?
- Should **{topic}** be an officer manual guide, a developer `docs/` page, or both?
- We're in Phase **{n}** per tracking.md — what is the recommended next documentation step?
- Offer paths to document **register employee** — draft only vs E2E + screenshots vs full Phase 2 pilot.
- What pre-flight questions should we answer before publishing a guide?
- Feature **{X}** ships next week — when should we create the officer guide (now vs after UI freeze vs with E2E)?

## Create

- Scaffold `user-manual/` with MkDocs Material (Phase 0) — after confirming B1 vs B2 path.
- Implement `UserManualManifestGenerator` and `UserDocumentationAttribute` (Phase 1).
- Draft officer guide "Register an employee" from `bo-catalog.json` — `status: draft` only.
- Create `guides/_template.md` with required frontmatter.
- Add `[UserDocumentation]` to a new officer feature BO in the same PR as the feature.

## Update

- Application progress UI changed — offer update options (text vs screenshots) then apply.
- Adapt the latest `docs/PERSON_DOSSIER.md` into the person dossier officer guide.
- Regenerate `bo-catalog.json` and list guides that need review after a BO rename.
- Add **tr, tk, ru** translations for guide `{slug}` from the English draft ([localization.md](./localization.md)).

## Plan

- What is the next documentation phase per tracking.md and USER_MANUAL_ROADMAP?
- Propose guide backlog for Phase 4 (invitations, work permits, progress, Resminamalar).
- Gap analysis: which `[UserDocumentation]` anchors have no published guide?
- Update tracking.md phase progress after Phase 0 scaffold ships.

## Fix

- user-manual CI failed: unknown `bo:` in guide frontmatter — triage and fix.
- mkdocs build broken after nav change — fix mkdocs.yml and links.
- Officer reported wrong steps in document-copies guide — fix from catalog + APPLICATION_ITEM_DOCUMENT_COPIES.md.
- Duplicate slug in guides — resolve and update cross-links.
- Publish-UserManualPages.ps1 failed on main — triage gh-pages deploy.

## Track

- Status report: phase, published guides, open doc debt, CI health, recommended next path.
- Update guide inventory row for `person/register` to `published` with officer sign-off date.
- Add doc debt item: stale screenshots on application-create guide.
- Append learnings.md after today's catalog generator fix.

## E2E media (with @visa2026-easytest-e2e)

- Wire `person/register` guide to `e2eScenarioId: person-employee-create` per USER_MANUAL_E2E_MEDIA.md.
- Record EasyTest video for person-register guide; leave `videoStorage: tbd` until Phase 3 decision.
- Plan UserManualMediaCapture steps for the five pilot guides (Phase 3).

## Triage

- Should this live in `docs/` (developer) or `user-manual/` (officer)?
- New feature shipped — what slug, guide update, and tracking rows are required?
- Is this a feature behaviour bug or a documentation bug? Route to feature skill vs this skill.

## AI drafting (guardrails)

- Using only `bo-catalog.json` and `docs/PERSON_DOSSIER.md`, write officer steps for opening a person dossier. Do not invent field names. Output as draft frontmatter + body only — not published.
