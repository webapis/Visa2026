# User manual — advisory (before implementing)

Read this **before** scaffolding, coding, or drafting guides. The agent should **advise and offer options** — not jump straight to implementation unless the user already chose a path.

**Skill entry:** [SKILL.md](./SKILL.md) · **Status:** [docs/USER_MANUAL_STATUS.md](../../../docs/USER_MANUAL_STATUS.md) · [tracking.md](./tracking.md) · **Timeline:** [docs/USER_MANUAL_ROADMAP.md](../../../docs/USER_MANUAL_ROADMAP.md)

---

## 1. Agent rule: advise first

```text
User asks about manual / docs
        │
        ▼
Read learnings + tracking + this file
        │
        ▼
Classify intent (§2) ──► Is officer manual the right surface? (§3)
        │
        ▼
Code change detection needed? ──► YES: [code-drift-scan.md](./code-drift-scan.md) (report only)
        │                              NO:  exact user-scoped edit only
        ▼
Check phase readiness (§4) ──► Offer options menu (§5) ──► User picks path
        │
        ▼
Only then implement (SKILL.md checklists) — Update after scan approval
```

**Do not** in the first response:

- Scaffold `user-manual/` if Phase 0 not agreed
- Publish `status: published` without officer review
- Invent UI labels without `bo-catalog.json` or E2E map §3
- Put **code, class names, or developer `docs/` links** in officer guide body ([content-policy.md](./content-policy.md))
- Commit video MP4 to git
- Duplicate content in `docs/` and `user-manual/` for the same audience

**Do** in the first response:

- State current **phase** from [tracking.md](./tracking.md)
- Name **audience** (officer vs developer vs admin power-user)
- List **2–4 viable paths** with trade-offs
- Ask which path (or recommend one default)
- Note **dependencies** (E2E scenario, catalog generator, officer time)

---

## 2. When to create or update documentation

### Create (new artifact)

| Trigger | What to create | Priority |
|---------|----------------|----------|
| **New officer-visible workflow** ships or is about to ship | How-to guide + `[UserDocumentation]` + tracking row | **High** — same release or immediate follow-up |
| **Pilot / roadmap milestone** (Phase 0–2) | Infra (MkDocs, generator) or pilot guide from backlog | Per [ROADMAP](../../../docs/USER_MANUAL_ROADMAP.md) |
| **Officer support pattern** (same question 3+ times) | New guide or FAQ under Getting started | Medium |
| **New E2E scenario promoted** to `scenarios/ready/` | Guide draft linked via `e2eScenarioId` | High — steps must match map |
| **Admin-only power feature** (templates) | `administration/` page adapted from dev doc | Medium |

### Update (existing artifact)

| Trigger | Action | Urgency |
|---------|--------|---------|
| **BO renamed / field removed** | Regen catalog; fix guide `bo:` + prose; CI | **Blocker** if validator wired |
| **UI layout / labels changed** | Bump `screenshotsVersion`; `status: stale` → `review` | High |
| **Feature skill doc updated** (`docs/PERSON_*.md`) | Adapt officer guide; do not auto-sync silently | Medium |
| **Officer reports wrong steps** | Fix from catalog + feature doc + E2E map | **High** |
| **New locale content** (tr/tk/ru) | Translate from `en` draft; per-locale `status` | Per [localization.md](./localization.md) |

### Defer (do not write officer manual yet)

| Situation | Instead |
|-----------|---------|
| Feature still in **design / prototype** | Update `docs/*_PLAN.md` or feature skill only |
| **Developer-only** (deploy, migration, agents) | Keep in `docs/` — not `user-manual/` |
| **Lookup table** with no officer workflow | Reference page only when catalog generator exists |
| **Phase 0–1 not done** and user wants 10 guides | Advise: infra + validator first, or manual PNG-only pilot |
| No **officer reviewer** available | `status: draft` max — never `published` |

---

## 3. Right surface? (audience check)

| Content | Put it here | Not here |
|---------|-------------|----------|
| Step-by-step for visa officers | `user-manual/docs/guides/` | `docs/` implementation plans |
| Architecture, CI, BO evaluators | `docs/` | Officer manual |
| Agent / Cursor procedures | `.cursor/skills/` | Officer manual |
| Caption-level field truth | `bo-catalog.json` + E2E `*_map.md` §3 | Prose alone |
| Template authoring for power users | `user-manual/administration/` (adapted) | Raw `USER_TEMPLATE_AUTHOR_GUIDE` copy |

If audience is mixed, **split**: short officer guide + link to admin section.

---

## 4. Phase readiness

Check [tracking.md](./tracking.md) § Phase progress before proposing work.

| User wants… | Minimum phase | If not met, recommend |
|-------------|---------------|------------------------|
| Local manual preview | **0** | Phase 0 scaffold first |
| BO-linked guides with CI gate | **1** | Generator + validator before bulk guides |
| 5 pilot guides + screenshots | **2** | Phase 1 + officer review loop |
| Auto screenshots + publish URL | **3** | E2E capture + `user-manual.yml` |
| tr/tk/ru guides | **4** (after en pilot) | English guide `published` first per slug |
| In-app Help button | **5** | Manual URL + slugs stable |

**Best practice:** do not skip Phase **1** validator before adding many guides — broken `bo:` links should fail CI.

---

## 5. Options menu (offer the user)

Present **relevant** options only. Respect **[curriculum.md](./curriculum.md)** tier order unless user explicitly skips (document skip in tracking).

> "We're in Phase **{n}**. For **{topic}**, next curriculum step is **{slug}** (tier **{t}**). Options:"

### A. New officer workflow (feature just shipped)

| Option | Effort | Best when |
|--------|--------|-----------|
| **A1 — Tracking row only** | Low | Feature shipped; doc scheduled next sprint |
| **A2 — Draft guide** (no screenshots) | Medium | Need text review before UI freeze |
| **A3 — Draft + manual screenshots** | Medium–High | Phase 2; UI stable |
| **A4 — E2E scenario + guide + media** | High | Long-term truth; CI media later (Phase 3) |
| **A5 — Adapt existing `docs/FEATURE.md`** | Medium | Feature skill doc already accurate |

**Default recommendation:** **A4** if E2E journey exists; else **A3** if UI stable; else **A2**. **Do not** recommend tier 5–7 guides until tier 2–4 exist for the same BO family.

### B. Infrastructure (user said "start manual")

| Option | Delivers | Best when |
|--------|----------|-----------|
| **B1 — Phase 0 only** | MkDocs shell, placeholder pages | First commit; prove `mkdocs serve` |
| **B2 — Phase 0 + 1** | + catalog generator + CI validate | Ready for real guides |
| **B3 — Full Phase 0–2 plan** | Roadmap + tracking update, no code | Planning session with product |
| **B4 — Agent-first manual** | Cursor Cloud Agent generates; CI on manual PR proves E2E + site | [cursor-integration.md](./cursor-integration.md) |

**Default recommendation:** **B1** for "try it"; **B2** if guides are imminent.

### C. Update / fix existing guide

**First:** run [code-drift-scan.md](./code-drift-scan.md) unless the user scoped an exact slug/edit with no discovery.

| Option | Best when |
|--------|-----------|
| **C1 — Text-only fix** | Wrong business wording; UI unchanged |
| **C2 — Text + new screenshots** | UI changed |
| **C3 — Re-record E2E video** | Flow changed materially |
| **C4 — Mark `stale` + debt ticket** | Officer unavailable now |

### D. Video

| Option | Best when |
|--------|-----------|
| **D1 — Defer video** | Phase &lt; 3 or storage undecided (open decision #6) |
| **D2 — Record locally** (`Record-EasyTest.ps1`) | Review cut; storage TBD |
| **D3 — Wait for storage decision** | Need Postgres vs static vs embed |

**Default recommendation:** **D1** until Phase 3 unless user explicitly needs a demo recording.

### E. AI-assisted draft

| Option | Guardrails |
|--------|------------|
| **E1 — Outline only** | Bullet steps for officer approval |
| **E2 — Full draft `status: draft`** | Input: catalog + `sourceDocs` + E2E map |
| **E3 — No AI prose** | Officer writes; agent wires frontmatter + assets |

**Default recommendation:** **E2** with mandatory officer review before `published`.

---

## 6. Recommended paths by scenario

### Scenario: "Document how to register an employee"

```text
1. Confirm Person + officer journey in E2E (person-employee-create)
2. If no scenario → visa2026-easytest-e2e first (or parallel)
3. Draft guide slug person/register from map §3 + bo-catalog
4. status: draft → officer review → published
5. Screenshots: manual (Phase 2) or CaptureStep (Phase 3)
6. Video: optional; storage TBD
```

### Scenario: "We changed Application progress UI"

```text
1. Run code-drift-scan.md on the merge diff — report only
2. Find guide applications/progress in tracking
3. If none → backlog row + A2 draft (don't publish under pressure)
4. If exists → status: stale; diff feature doc APPLICATION_PROGRESS_*
5. Regen catalog if BO/properties changed
6. Offer C2 vs C1 based on scan report; wait for approval
7. Then Update checklist
```

### Scenario: "Start the user manual project"

```text
1. Read ROADMAP + tracking + curriculum (Phase 0 not started)
2. Offer B1 vs B2 vs B3
3. Phase 2 pilots = curriculum tiers 0–4 only (not templates first)
4. First guides: login → navigation → open person → register → passport
```

### Scenario: "Document template generation"

```text
1. Confirm tiers 0–5 exist or are planned (Resminamalar prerequisite)
2. Tier 7 guides only: user-report-templates → template-staging
3. Adapt USER_TEMPLATE_AUTHOR_GUIDE + TEMPLATE_STAGING_EDIT — admin audience
4. Defer until tier 5 Resminamalar guide is at least draft
```

### Scenario: "Should this go in AGENTS.md / docs/?"

```text
→ §3 audience check
→ Developer: docs/ or feature skill
→ Officer: user-manual/
→ Offer to extract officer subset only
```

---

## 7. Quality gates (status workflow)

| Status | Meaning | Who |
|--------|---------|-----|
| `backlog` | Identified; no file yet | Product / tracking |
| `draft` | Markdown exists; may use AI | Agent + dev |
| `review` | Ready for officer walkthrough | **Visa officer** |
| `published` | On site; signed off | Officer + tech |
| `stale` | UI/BO drift suspected | Anyone; triggers update |

**Never skip `review` → `published`** for compliance-facing steps.

---

## 8. Pre-flight questions (ask when unclear)

Copy/adapt:

1. **Audience** — Visa officers, admins, or developers?
2. **UI stability** — Is the screen frozen for this release?
3. **Phase** — OK to do infra only, or must officers see content now?
4. **E2E** — Does a `scenarios/ready/` journey exist (or should we create one)?
5. **Reviewer** — Who signs off before `published`?
6. **Locale** — English only for v1?
7. **Media** — Screenshots now? Video now or defer (storage open)?
8. **Publish path** — always `Build-UserManual.ps1` for release (includes E2E), not separate `dotnet test`?

If user says "just do it", state your **recommended default** and proceed unless they object.

---

## 9. Anti-patterns

| Anti-pattern | Why | Instead |
|--------------|-----|---------|
| Guide before catalog validator | Broken `bo:` undetected | Phase 1 first, or minimal pilot |
| Published guide without E2E/map | Steps drift | Link `e2eScenarioId` or mark draft |
| Copy `docs/` verbatim to officers | Jargon, file paths, phases, **code** | Adapt per [content-policy.md](./content-policy.md) |
| Code blocks in guide body | Officers are not developers | UI labels + screenshots only |
| Screenshots from prod DB | PII risk | EasyTest `:5050` + seed data |
| One giant "Visa2026 manual" page | Unmaintainable | One guide per job-to-be-done |
| Implement Phase 3 video pipeline when Phase 0 missing | Wasted work | Match phase to tracking |

---

## 10. Changelog

| Date | Change |
|------|--------|
| 2026-08-04 | Initial advisory v0.1 — advise-before-implement |
| 2026-08-04 | Curriculum tier order referenced in options menu |
| 2026-08-04 | [content-policy.md](./content-policy.md) — officer manual must not contain code |
