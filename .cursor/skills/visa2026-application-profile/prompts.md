# Application Profile — user prompts

Copy into chat with `@visa2026-application-profile` (or `@.cursor/skills/visa2026-application-profile/SKILL.md`).

---

## Implementation & tracking

- What is the next Application Profile slice? Update IMPLEMENTATION_PLAN and suggest tasks for slice 5 (seed from ApplicationType).
- Implement ApplicationProfileSeedUpdater — map each ApplicationType to a profile and backfill Application.ApplicationProfile.
- Switch Application field visibility from ApplicationType.Show* to ApplicationProfile — audit and migrate Appearance rules.
- Enforce config lock on ApplicationProfile edit when IsConfigLocked is true.

## Officer configuration (suggestions)

- How should I configure an Application Profile for employee visa issuance via ministries?
- Suggest profile settings for cancellation of existing visas.
- What person-config toggles should I enable for a registration / family-member profile?
- We need a new profile variant but the old one is config-locked — what should officers do?

## UX prototypes

- Implement the configuration wizard from `docs/prototypes/application-profile-template-wizard-mockup.png` (steps 1–5 PNGs).
- Build staged profile queue from `docs/prototypes/staged-profiles-listview-table-mockup.png` / grid variant; Start process merge.
- Build in-process workspace from `docs/prototypes/process-started-application-profile-workspace-mockup.png` and `process-started-nav-*.png` tabs.
- Replace native XAF nav with `docs/prototypes/visa2026-custom-left-navigation-shell-mockup.png`.

## Person / Dossier entry

- Do **not** add Start application from Person DetailView or Dossier. Create Application Profile Instances only from Application Profile Instances lists (via ministry: profile → Approval legs).

## Dual-read / deprecation

- Application still requires ApplicationType on save — fix dual-read or document cutover step.
- Map VISA2014 import ApplicationType to ApplicationProfile FK.

## Route elsewhere

- Progress transition blocked — use **@visa2026-application-progress**, not this skill.
- Resminamalar template list from profile nested templates — coordinate **@visa2026-resminamalar**.
- ApplicationItem document copies — **@visa2026-document-copies** until M2M slice ships.
