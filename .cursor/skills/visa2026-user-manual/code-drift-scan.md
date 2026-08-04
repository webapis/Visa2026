# User manual — code change detection (scan)

**Role:** **Mandatory code change detection procedure** for [visa2026-user-manual](./SKILL.md). Whenever that skill must know whether app code affects officer guides, run this doc — do not skip with ad-hoc greps or direct guide edits.

**Governance:** [advisory.md](./advisory.md) §7 · **Inventory:** [tracking.md](./tracking.md)

---

## Purpose

Detect **officer-visible code changes** that may require manual review or guide updates — **without editing guides** until a human approves.

| Mode | Agent may… | Agent must not… |
|------|------------|-----------------|
| **Scan** (default) | Read diff, regen catalog, produce impact report | Edit `user-manual/docs/guides/`, bump `status: published`, commit assets |
| **Update** (after approval) | Apply agreed C1–C4 changes; set `status: review` max | Set `status: published` without officer sign-off |

**Approval** means the user (or named reviewer in tracking) explicitly picks items from the scan report — e.g. "update register + add-passport screenshots only".

---

## When this skill requires a scan

Run this procedure when **visa2026-user-manual** needs code change detection — including:

| Situation | Scope |
|-----------|--------|
| **Update** after UI / BO / nav / workflow change | `git diff` on paths below |
| **Create** guide for a feature that already shipped | Diff since feature merge |
| User asks whether manual is stale | Same |
| Release / weekly drift review | `master` since last scan in tracking |
| **Plan** — coverage gap vs `[UserDocumentation]` | Catalog + inventory compare |
| Optional push webhook | [cursor-integration.md](./cursor-integration.md) §4 — triage only |

**Exception:** user names exact slug + change with no discovery (typo fix) — skip scan.

---

## Paths to scan (officer impact)

```text
Visa2026.Module/BusinessObjects/          # [UserDocumentation], fields, validation, Appearance
Visa2026.Module/Controllers/              # actions, captions, workflow
Visa2026.Module/Documentation/            # UserDocumentationAttribute
Visa2026.Blazor.Server/Editors/           # custom editors officers see
Visa2026.Blazor.Server/Pages/             # host-only officer UI (thin — prefer Module)
Visa2026.Module/Model.DesignedDiffs.xafml  # nav, DetailView/ListView layout
Visa2026.Blazor.Server/Model.xafml         # host model diffs
docs/PERSON_*.md docs/APPLICATION_*.md    # feature docs — adapt, do not auto-sync to guides
```

**Ignore** (developer-only — no officer guide impact unless user asks):

- `scripts/`, `docker-compose*`, `.github/workflows/` (except manual tooling)
- `Visa2026.DataImporter/`, VISA2014 migration
- `docs/ENVIRONMENTS.md`, deploy runbooks, agent skills

---

## Scan procedure (agent checklist)

1. **Read** [tracking.md](./tracking.md) guide inventory + doc debt; note last scan date.
2. **Diff** relevant paths (PR compare, `git log -p`, or user-supplied range).
3. **Regen catalog** (read-only compare): run generator or diff committed `bo-catalog.json` if BO/XAF attributes changed.
4. **Map each change** → affected guide slug(s) via:
   - `[UserDocumentation("slug")]` on changed types
   - Guide frontmatter `bo:` matching changed BO name
   - `e2eScenarioId` when E2E map or journey tests changed
   - Curriculum tier in [curriculum.md](./curriculum.md) for new workflows
5. **Classify impact** (per item):

   | Signal | Likely manual action |
   |--------|----------------------|
   | `DisplayName` / nav caption changed | Text review (C1); screenshots if visible (C2) |
   | Field added/removed/required | Regen catalog + guide steps (C1/C2) |
   | `[Appearance]` visibility / new tab | C2 screenshots + step text |
   | New `[UserDocumentation]` slug, no guide | Backlog row or A2 draft — **after approval** |
   | Controller action rename / new toolbar button | C2 + E2E map check |
   | Only internal refactor, no UI | **No guide change** — note in report |
   | Feature `docs/*.md` updated | Medium — adapt officer guide if published exists |

6. **Produce report** (template below) — add rows to [tracking.md](./tracking.md) § Doc debt with severity.
7. **Stop** — offer advisory options (C1–C4, A2, etc.); wait for approval before **Update** mode.
8. **After approval** — follow [SKILL.md](./SKILL.md) Update checklist; `status: review` until officer walkthrough.

---

## Report template (paste to user / issue)

```text
## Manual drift scan — {date} — {ref or PR}

**Range:** {base}..{head}
**Guides published:** {n} | **Draft/review:** {n}

| # | Code change (summary) | Affected slug(s) | Impact | Suggested action | Approved? |
|---|-------------------------|------------------|--------|------------------|-----------|
| 1 | Person: new optional tab X | person/register | medium | C2 text + screenshots | ☐ |
| 2 | ApplicationProgress state labels | applications/progress | high | C1 — guide backlog only | ☐ |

**Catalog:** regen needed? {yes/no} — {diff summary}
**E2E:** scenario `{id}` stale? {yes/no}
**No action:** {list paths reviewed with no officer impact}

**Recommended default:** {one paragraph}
**Next step:** Reply with approved row numbers, or "scan only".
```

---

## Approval gates (non-negotiable)

| Rule | Detail |
|------|--------|
| **No silent publish** | Never set `status: published` from a scan or drift fix PR without officer sign-off |
| **No guide edit on scan** | Scan mode is read-only for `user-manual/docs/guides/` |
| **PR split** | Prefer: (1) app feature PR, (2) scan report / tracking debt, (3) manual update PR after approval |
| **CI** | `user-manual.yml` proves manual PR; app-only push does not auto-update prose |
| **Stale flag** | May set `stale` in tracking/report suggestion — apply to frontmatter only after approval |

---

## Future automation (not required for scan mode)

| Item | Phase | Notes |
|------|-------|-------|
| `scripts/ci/Scan-UserManualDrift.ps1` | 2–3 | Diff paths + map to slugs; emits JSON/markdown report |
| CI comment on app PR | 3 | Warn when `[UserDocumentation]` changes without manual PR |
| Catalog warn: slug without guide | 2 | Validator upgrade per implementation plan §8 |

Until the script exists, the **agent** performs the procedure above manually.

---

## Related

- [cursor-integration.md](./cursor-integration.md) — agent-first; push = verify only
- [advisory.md](./advisory.md) §2 — when to create/update
- [prompts.md](./prompts.md) — copy-paste scan triggers

---

## Changelog

| Date | Change |
|------|--------|
| 2026-08-04 | Initial — scan/report/approve/update separation |
