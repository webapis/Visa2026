# Application process steps — learnings (append-only)

Read **before** step-visibility or completion work; **append** after a verified change. Promotion rules: [MATURITY.md](./MATURITY.md).

Neighbor logs when the fix landed there:

- Timeline / hidden Issued: [visa2026-application-progress/learnings.md](../visa2026-application-progress/learnings.md)
- `ProgressSteps` seed and chips: [visa2026-application-profile/learnings.md](../visa2026-application-profile/learnings.md)
- List row hex: [visa2026-bo-state-colors/learnings.md](../visa2026-bo-state-colors/learnings.md)

---

## Entries

### 2026-11-26 — Seretmezlik list row green at hand-in

- **Symptom**: Seretmezlik instances on the Direct list stayed the submitted yellow (`#FFF59D`) after **Migrasiýa gullugyna tabşyryldy** was done.
- **Cause**: `CompletedRouteAppearance` already mapped a terminal hand-in to `PROCESS_ISSUED`, then the submitted-yellow branch overwrote it.
- **Fix**: `IsCompleteAtHandIn` wins before the submitted yellow. Stored step stays `SUBMITTED_MIGRATION`. Profiles that still show Issued stay yellow at hand-in.
- **Test**: `Resolve_seretmezlik_hand_in_is_green`, `Resolve_submitted_stays_yellow_when_a_later_step_is_still_visible`.
- **Prevent**: Do not paint every `SUBMITTED_MIGRATION` row green. Only when hand-in is the last visible step.
- **Cross-skill**: visa2026-bo-state-colors

### 2026-11-26 — Completion rule locked; code still narrower

- **Decision**: A producing profile (invitation, work permit, visa, border zone, or work-permit location) is complete when the last visible process step is reached and every required result is filled for every person. An invitation profile does not wait on visa. A profile that produces none of those is complete at its last visible step (registration and cancellation/business trip at Issued, change at Migration / AS, Seretmezlik at Submitted). `extend_visa_wp` stays complete at visa-window day 0 when every work permit is in, even if visas are missing. Rejected and cancelled turn green on the list and in the workspace once that outcome is latest. Report Dashboard colors stay unchanged.
- **Code**: Seretmezlik list green at hand-in shipped later the same day. `ApplicationWorkspaceViaMinistryIssuanceGreenGate` still greens only Via Ministries at `PROCESS_STARTED` with chips N/N, plus the `extend_visa_wp` day-0 case. Direct producers and rejected/cancelled green are not implemented.
- **Prevent**: Do not treat the green gate as already matching the full rule. Do not drop the Migration / AS node when Issued is hidden.
- **Cross-skill**: visa2026-application-profile, visa2026-bo-state-colors, visa2026-application-progress
