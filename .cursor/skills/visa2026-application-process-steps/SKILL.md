---
name: visa2026-application-process-steps
description: >-
  Keeps Visa2026 application process-step visibility, the completion rule, and
  their docs in sync with code. Use when changing which progress steps a profile
  shows, when an Application Profile instance is considered complete, list or
  workspace green for a finished case, ProgressSteps seed, or
  APPLICATION_PROFILE_PROGRESS_STEPS.md. Refactor of timeline or completion
  code belongs here. Not transition graphs (visa2026-application-progress),
  profile wizard/seed cutover (visa2026-application-profile), or the color
  hex registry (visa2026-bo-state-colors). Always read learnings.md first;
  append after verified changes.
disable-model-invocation: false
---

# Visa2026 — Application process steps

**User prompts:** [prompts.md](./prompts.md)

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) (newest first) and [`docs/APPLICATION_PROFILE_PROGRESS_STEPS.md`](../../../docs/APPLICATION_PROFILE_PROGRESS_STEPS.md).
2. **Classify** with the table below. Leave the other skill's procedure there; do not copy it into this folder.
3. **Change seed, runtime, and the matrix doc in the same task** when visibility or completion changes. The tenant JSON `ProgressSteps` array is the seed; the markdown matrix is the officer-facing snapshot and must match.
4. **Verify** — isolated build (`scripts/local/Build-Isolated.ps1`). Add or update tests when visibility or the green/complete gate changes.
5. **Record** — append [learnings.md](./learnings.md) after a verified change ([MATURITY.md](./MATURITY.md)).

## Which skill

| Change | Skill |
|--------|--------|
| Which steps are visible, when the case is complete, timeline nodes for those steps, green because the case is complete | **This skill** |
| Legal next state, ministry legs, ministry letter, contract snapshot | [visa2026-application-progress](../visa2026-application-progress/SKILL.md) |
| Profile wizard, May-produce flags, case workspace tabs, catalog cutover | [visa2026-application-profile](../visa2026-application-profile/SKILL.md) |
| Hex, Turkmen color labels, `BoStateAppearanceColors`, SLA tint | [visa2026-bo-state-colors](../visa2026-bo-state-colors/SKILL.md) |
| `ProgressSteps` keys inside the Calik tenant JSON row | This skill, editing the file application-profile owns |

## Locked completion rule (2026-11-26)

A case is **complete**, and the **list row** and the **case workspace** turn **green**, when one of these is true:

1. **Produces a result** — profile may produce invitation, work permit, visa, border zone, or a work-permit location. The last **visible** process step is reached, and every required result is filled for every person on the case. When the profile produces an invitation, the visa result does not block completion.
2. **Produces none of those** — the last visible process step is reached. Registration finishes at Issued. Cancellation and business trip finish at Issued. Change finishes at the Migration / AS number. Seretmezlik finishes at Submitted.
3. **Wiza we Iş Rugsatnamasyny Uzaltmak** (`extend_visa_wp`) — also complete when every work permit is in and the visa window is 0 days, even while visas are still missing.
4. **Rejected or cancelled** — complete once that outcome is the latest step. Paint green on the list and in the workspace.

Report Dashboard colors stay on their own rules.

**Code today:** Seretmezlik list rows turn Issued green when the latest step is `SUBMITTED_MIGRATION` (`ApplicationListViewDisplayState.IsCompleteAtHandIn`). `ApplicationWorkspaceViaMinistryIssuanceGreenGate` still greens only Via Ministries rows already at `PROCESS_STARTED` when required chips are N/N, plus the `extend_visa_wp` day-0 case. Direct producers, registration/cancellation at Issued, and rejected/cancelled green are **not** implemented yet.

## Same-task checklist

- [ ] `ProgressSteps` in `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/application-profile.calik-energi.json`
- [ ] Matrix and pattern counts in [`docs/APPLICATION_PROFILE_PROGRESS_STEPS.md`](../../../docs/APPLICATION_PROFILE_PROGRESS_STEPS.md) (refresh [`APPLICATION_PROFILE_PROGRESS_STEPS.xlsx`](../../../docs/APPLICATION_PROFILE_PROGRESS_STEPS.xlsx) when that workbook is part of the change)
- [ ] Timeline visibility (`ApplicationProfileProgressStepVisibility`, `ApplicationWorkspaceProgressTimeline`)
- [ ] Completion / green gate when the complete condition changes (`ApplicationWorkspaceViaMinistryIssuanceGreenGate`, `ApplicationListViewDisplayState`)
- [ ] Palette doc only for label or precedence wording: [`docs/APPLICATION_PROFILE_LISTVIEW_PROGRESS_COLORS.md`](../../../docs/APPLICATION_PROFILE_LISTVIEW_PROGRESS_COLORS.md). Hex stays in **visa2026-bo-state-colors**
- [ ] Unit test for the branch you changed
- [ ] Hidden Issued/Rejected/Cancelled must not erase the Migration / AS node ([learnings.md](./learnings.md))

## File map

[reference.md](./reference.md)
