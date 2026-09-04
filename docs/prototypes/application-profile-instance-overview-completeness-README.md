# Overview missing / complete Case summary (2026-09-03)

Same cue language as People & links: red when required Case summary fields are empty, green check when they are filled. Tiles already used fill-empty red; the nav was silent.

**Status:** implemented 2026-09-03.

## Locked for this set

| Decision | Choice |
|----------|--------|
| What counts | `FillState.Empty` on Case summary Use fields. Process number is ignored (Advance rule, same as the office-prep gate) |
| Empty tiles | Pale red, dashed red border, red label/value (match People short tiles) |
| Filled tiles | Unchanged blue default / green officer |
| Nav missing | Red badge = **count of empty required fields** |
| Nav complete | Green **check**, no number (including profiles with no required properties) |
| Independence | Overview badge is Case summary only. People & links keeps its own roster badge |
| Blocking | Unchanged: office-prep still blocks Progress / documents / Resminamalar / SLA |

## Screens

| File | State |
|------|--------|
| [application-profile-instance-overview-missing-prototype.png](./application-profile-instance-overview-missing-prototype.png) | Empty required tiles red; Overview nav red count |
| [application-profile-instance-overview-complete-prototype.png](./application-profile-instance-overview-complete-prototype.png) | All required fields filled; Overview nav green check |

The complete PNG may show extra green marks on other tabs; **implement Overview only** (People keeps its own red/green).