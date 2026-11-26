# Application process steps — file map

Read [SKILL.md](./SKILL.md) first. This file is paths only.

## Docs

| File | Role |
|------|------|
| `docs/APPLICATION_PROFILE_PROGRESS_STEPS.md` | Per-profile visible/hidden steps. Snapshot of the seed. Update in the same task as `ProgressSteps`. |
| `docs/APPLICATION_PROFILE_PROGRESS_STEPS.xlsx` | Same matrix for officers (`Y` / `N`). |
| `docs/APPLICATION_PROFILE_LISTVIEW_PROGRESS_COLORS.md` | ListView progress palette and precedence, including the via-ministry green gate. |
| `docs/APPLICATION_PROFILE_PLAN.md` | Profile model. Step visibility is not redefined there. |

## Seed

| File | Role |
|------|------|
| `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/application-profile.calik-energi.json` | Each profile `ProgressSteps`: `Key` + `Visible`. Keys: `office`, `ministry`, `migration-submitted`, `migration`, `process-issued`, `process-rejected`, `process-cancelled`. |

## Runtime

| Type | Role |
|------|------|
| `ApplicationProfileProgressStepVisibility` | Reads visible keys for a profile. |
| `ApplicationWorkspaceProgressTimeline` | Case workspace timeline nodes. |
| `ApplicationWorkspaceViaMinistryIssuanceGreenGate` | Current list green: Via Ministries + `PROCESS_STARTED` + required chips N/N, plus `extend_visa_wp` day 0. |
| `ApplicationListViewDisplayState` | List row color precedence. |
| `ApplicationWorkspaceIssuedRecordsCatalog` | Result chip keys (invitation, work permit, issued visa, border zone, work-permit location). |

## Tests

| Project | Role |
|---------|------|
| `Visa2026.Module.Tests` | `ApplicationWorkspaceViaMinistryIssuanceGreenGateTests` and timeline tests that mention `ProgressSteps` or hidden Issued. |

Build with `scripts/local/Build-Isolated.ps1`. `Visa2026.Module.Tests` is not in `Visa2026.slnx`; build that `.csproj` directly when tests change.
