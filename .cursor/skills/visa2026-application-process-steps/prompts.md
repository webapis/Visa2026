# Application process steps — user prompts

Copy into chat with `@visa2026-application-process-steps`.

---

## Visibility

- Show or hide a progress step for one Application Profile. Update the tenant `ProgressSteps` seed and `docs/APPLICATION_PROFILE_PROGRESS_STEPS.md` together.
- Refresh the progress-step matrix after the Calik catalog changes.

## Completion and green

- A producing profile (invitation, work permit, visa, border zone, or work-permit location) is complete only after the last visible step and every required result for every person. Invitation profiles do not wait on visa.
- A profile that produces none of those is complete when its last visible step is reached.
- Keep `extend_visa_wp` green at visa-window day 0 when every work permit is in, even if visas are missing.
- Rejected and cancelled cases turn green on the list and in the case workspace. Leave Report Dashboard colors unchanged.

## Refactor

- Move timeline or completion code without changing which steps are visible or when a case is complete. Re-read the matrix and the locked rule in SKILL.md before editing.

## Route elsewhere

- Illegal next progress state, ministry legs, or the ministry letter file — `@visa2026-application-progress`.
- Profile wizard, May-produce flags, or case tabs — `@visa2026-application-profile`.
- Change a progress color hex — `@visa2026-bo-state-colors`.
