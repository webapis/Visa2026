# Application progress — user prompts

Copy into chat with `@visa2026-application-progress` (or `@.cursor/skills/visa2026-application-progress/SKILL.md`).

---

## Workflow / validation

- Progress save blocked with “invalid transition” — diagnose from latest `ApplicationProgress` row and contract leg count.
- Officer cannot select expected next state on `ApplicationProgress` detail — check transition graph and `AvailableStatesForNextStep`.
- Application requires project contract before second progress step — verify `ApplicationProgressProfileResolver` and `MinistryLegs` on selected contract.

## Ministry legs & contract

- Add a new GT-15 variant with three ministry legs — seed `ProjectContract` row + `MinistryLegs`, document in approval doc.
- Ministry name column empty on progress history — fix snapshot / `ProjectContractMinistryHelper`.
- Block structural edit on contract already used by applications — confirm `ProjectContractMinistryController` message.

## Ministry letter file

- Show upload only on approved/rejected ministry steps — `IsMinistryDecisionStateCode` + Appearance.
- Fix `Invalid column name 'MinistryLetterFileID'` after deploy — schema updater + lifecycle-docker.

## Tests

- Add unit test for four-leg transition chain from office prep to migration service.
- Extend `ApplicationProgressLegCodesDecisionTests` for new decision state pattern.

## Row colors (route elsewhere)

- Application list row background wrong for `1_REVIEW_STARTED` — use **@visa2026-bo-state-colors**, not this skill.
