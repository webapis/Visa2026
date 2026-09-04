# People & links missing / complete (2026-09-03)

Officers should see missing linked-person data without opening every tile. Same empty red as Case summary (`#fef2f2` / `#dc2626`). Green only on the **People & links** nav when every configured tile is filled.

**Status:** implemented 2026-09-03 (workspace tiles + nav). Prototypes remain the visual contract.

## Locked for this set

| Decision | Choice |
|----------|--------|
| Empty tiles | Red dashed border, pale red fill, red count. Profile-configured kinds only (`Count` below `ExpectedCount`, including Last-N `1/2`) |
| Filled tiles | Unchanged (not green) |
| Nav missing | Red badge. Count = **people** with at least one short tile. Empty roster = red **dot** (no number) |
| Nav complete | Green **check**, no number |
| Table Passport / Visa | Red `—` only when that kind is on the person and short |
| Issued records | Not this indicator (empty issued tiles stay dashed, not red) |

## Screens

| File | State |
|------|--------|
| [application-profile-instance-people-links-missing-prototype.png](./application-profile-instance-people-links-missing-prototype.png) | Gaps: red tiles, red table dashes, red nav count |
| [application-profile-instance-people-links-complete-prototype.png](./application-profile-instance-people-links-complete-prototype.png) | All required links present: filled tiles, green nav check |

## Not in this set

- Blocking Progress / Advance on people gaps (Case summary gate is separate)
- Green fill on complete tiles or person card titles
- Amber / partial nav state