# Document copies missing / complete (2026-09-03)

Same cue language as People & links and Overview: red when required scans are missing, green check when every required copy is present.

**Status:** implemented 2026-09-04 (workspace catalog rows + nav). Prototypes remain the visual contract.

## Proposed (for implementation)

| Decision | Choice |
|----------|--------|
| What counts | Required Document copies slots with no file (`GapSlotCount`). Partial (some people missing a shared type) also counts as missing |
| Missing rows | Pale amber, dashed orange border, amber **Missing** chip (warning, not Case-summary red) |
| Ready rows | Unchanged green **Ready** chip; do not paint the whole row green |
| Nav missing | Amber warning badge = **count of missing required slots** (not people) |
| Nav complete | Green **check**, no number |
| Independence | Document copies badge is scans only. Overview / People keep their own badges |
| Empty roster | Red **dot** (no people to attach copies to) |

## Screens

| File | State |
|------|--------|
| [application-profile-instance-document-copies-missing-prototype.png](./application-profile-instance-document-copies-missing-prototype.png) | Missing rows amber warning; Document copies nav amber count |
| [application-profile-instance-document-copies-complete-prototype.png](./application-profile-instance-document-copies-complete-prototype.png) | All Ready; Document copies nav green check |

The complete PNG may show extra green marks on other tabs; **implement Document copies only**.