# Application Profile Instance — Ýagdaý syny (A4 dialog)

Prototype for moving list state detail off the Application Profile Instance ListView onto one A4 sheet. Opened from a **Ýagdaý syny** link.

**Status:** Implemented (2026-11-17). The image remains the layout reference. Live UI is the **Ýagdaý syny** link on the instance list.

## Screens

| File | What it shows |
|------|----------------|
| [application-profile-instance-state-preview-a4.png](./application-profile-instance-state-preview-a4.png) | Dialog + fullest sheet (çakylyk with serhet ýaka, iş rugsady, wiza) |
| [application-profile-instance-state-preview-a4.html](./application-profile-instance-state-preview-a4.html) | Same mock, open in a browser |

Image text can be misread. This file is the source of truth. Sample document numbers (`IR-2041`, invitation `1574-43597`) are layout only. Case `9/-1862` and the person names are taken from the list screenshot used for the mock.

## Locked

| Decision | Choice |
|----------|--------|
| Opens as | Centered dialog over the list. While a copy preview is open, the dialog shifts left of that panel |
| Sheet | State pills stay labels. An issued invitation, work permit, visa, border zone, or work-location number opens that document’s side preview. “Entäk berilmedi” stays plain text. Print shows the number without the link |
| Many people | One A4-width sheet. Scrolls on screen. Print continues on the next A4 page |
| List keeps | Identity columns, plain people names, color filter tabs, row background |
| List loses | Progress pipeline, document state marks, Seretmezlik cards, completion % |
| List gains | One **Ýagdaý syny** link per row. Row click still opens the case |
| Visa-only, registration, change, cancellation, Seretmezlik | Progress steps only. No empty document sections |
| Seretmezlik on issuance profiles | Pink band on this sheet, under the progress steps |

## Sheet, top to bottom

1. Case identity and completion ring.
2. Progress steps for that profile (ministry legs only when the route has them). Current step uses the same color as the list row.
3. Seretmezlik band, when this case has one.
4. Issued-document sections for that profile only. State pills: **Güýjünde**, **Üýtgedildi**, **Ulanyldy**, **Möhleti geçen**, **Entäk berilmedi**.

An invitation number and its period show once, with that invitation’s people underneath. A work-permit number shows once; each person on it keeps their own period and state. People who were not issued are a name list after those groups. Visas stay one row per person.

Granted border zones are a nested row under the person, inside the invitation group, except on the work-permit sheet, where they nest under the visa. Granted work-permit locations nest under the person, inside the work-permit group.

## Profile sections (confirm before build)

| Sheet | Sections | Proposed profiles |
|-------|----------|-------------------|
| 1 | Çakylyk (serhet ýaka inside) + iş rugsady + wiza | `get_invitation_wp`, `extend_visa_wp` |
| 2 | Çakylyk (serhet ýaka inside) + wiza | `get_invitation`, `get_invitation_fm`, `get_invitation_service_passport`, `get_invitation_according_to_wp` |
| 3 | Iş rugsady (barjak ýeri inside) + wiza (serhet ýaka inside) | `workpermit_extension`, `visa_ext_according_to_wp` |
| 4 | Berlen serhet ýaka only | `get_border_zone` |
| 5 | Berlen iş rugsady ýerleri only | `change_workpermit` |

Cancellation profiles that target an issued document (`cancel_invitation`, `cancel_invitation_wp`, `cancel_visa`, `cancel_visa_wp`, `cancel_workpermit`, `cancel_borderzone`) are in the progress-only group until this table is confirmed.