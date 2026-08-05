# User manual — pre-implementation decisions

**Recorded:** 2026-08-04 (product/tech sign-off in chat). Use this as the implementation contract unless explicitly revised.

**Status hub:** [docs/USER_MANUAL_STATUS.md](../../../docs/USER_MANUAL_STATUS.md) · **Skill:** [SKILL.md](./SKILL.md)

---

## Recorded decisions (2026-08-04)

### Architecture (already fixed before this sign-off)

| Topic | Decision |
|-------|----------|
| Officer content | UI language only — no code ([content-policy.md](./content-policy.md)) |
| Locales | **en, tr, tk, ru** — default `en` ([localization.md](./localization.md)) |
| E2E | Inside `Build-UserManual.ps1` |
| Content generation | **Cursor Cloud Agent** primary; CI verifies manual PRs ([cursor-integration.md](./cursor-integration.md)) |
| Testing on manual | **Green tick only**; `manual-test-reports/` separate ([testing-evidence.md](./testing-evidence.md)) |
| Curriculum | Tiers 0–7 — CRUD first, templates last |
| Video in git | **Never** |

### D1–D17

| # | Decision | **Recorded choice** |
|---|----------|---------------------|
| **D1** | Manual hosting | **On-prem — Docker container** (static manual site + test-report path on LAN; align with company compose stack) |
| **D2** | Who sets `status: published` | **Tech** (technical publish authority) |
| **D3** | Screenshot environment | **Local E2E** default (`:5050`); **Staging E2E** optional against live staging URL |
| **D4** | Commit `bo-catalog.json`? | **Yes on `main`** |
| **D5** | Reviewers per language | **Name owners before Phase 2**; same person may cover multiple locales |
| **D6** | Generated `reference/*.md` in git? | **CI-only** at build time |
| **D7** | `LookupNavigationStructure.md` | **Deprecate** when catalog nav-tree generator ships |
| **D8** | tr/tk/ru production | **Agent draft + officer review** — no machine-only publish |
| **D9** | Locales before `published`? | **`en` first**; tr/tk/ru same release train or **explicit tracked debt** |
| **D10** | Test report hosting | **CI artifacts** in Phase 3; **internal HTML URL** on-prem when D1 container is live |
| **D11** | Phase 2 pilot scope | **Minimal:** login/navigation + `person/register`, then expand |
| **D12** | tr/tk/ru screenshots (interim) | **English screenshot + short note** until per-locale EasyTest |
| **D13** | In-app Help | **Defer** to Phase 5 (optional) |
| **D14** | Video storage (Phase 3) | **On-prem:** static HTTPS or **object store on LAN**; not Postgres primary; embed only if public + IT allows |
| **D15** | Video coverage | **Top 3–5 pilots** first; expand after D14 |
| **D16** | Cursor push-notify webhook | **Optional later** — not v1 |
| **D17** | Block app deploy if manual CI red? | **Yes** for **on-prem officer releases**; dev branches looser |
| **D18** | E2E driver for manual media | **Playwright** for Local + Staging (same journeys); EasyTest legacy until migrated |
| **D19** | Media on app deploy? | **No** — separate `Publish-ManualRelease.ps1` / `Record-PlaywrightE2e.ps1` step |
| **D20** | Staging E2E trigger | **Manual only** (`VISA2026_E2E_TARGET=Staging`); validates live staging before promote |
| **D21** | Officer manual media | **Screenshots-only** — doc-anchored PNGs from UserManual Playwright E2E; **no video** in guides or publish pipeline (video infra optional/deferred) |
| **D22** | Screenshot storage | **Commit** `user-manual/assets/screenshots/**/*.png` in git — powers **GitHub Pages** and clone-and-preview without E2E; refresh via E2E + commit |

### Governance

| Item | **Recorded choice** |
|------|---------------------|
| English reviewer | **Named person** — assign before Phase 2 (name: _TBD_) |
| tr / tk / ru reviewers | **Named** — may overlap with English reviewer (names: _TBD_) |
| Green tick | Tests passed on that build — **does not replace human review** |
| Test logs on manual | **Never** — green tick only on officer site |

### D2 + D8 interaction (implementation note)

- **Tech** may set `status: published` and merge/deploy the manual container.
- **Officer review** is still required for guide **accuracy** per D8 before tech publishes (workflow: `draft` → `review` → officer OK → tech `published`).

---

## Phase 0 unblocked

With D1–D4 and D11 recorded, **Phase 0 may start** (MkDocs + i18n scaffold + Docker publish path design).

**Still needed before Phase 2:** reviewer names (D5), officer walkthrough sign-off (D8).

---

## After future changes

Update this file + [tracking.md](./tracking.md) § Decisions + [USER_MANUAL_STATUS.md](../../../docs/USER_MANUAL_STATUS.md) §7.

Append [learnings.md](./learnings.md) when a decision is validated or revised in practice.

---

## Changelog

| Date | Change |
|------|--------|
| 2026-08-04 | Initial checklist |
| 2026-08-04 | **Recorded** D1–D17 + governance from product/tech sign-off |
