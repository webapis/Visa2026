# Application Profile Instance — Ýagdaý syny cancelled invitation

Prototype for **cancel invitation** (and **cancel invitation + work permit**) on Ýagdaý syny — same two-sided pattern as change, but **no** second issued-document section on the original case.

Related change prototype: [application-profile-instance-state-preview-renewed-invitation-README.md](./application-profile-instance-state-preview-renewed-invitation-README.md).

**Status:** Implemented (2026-11-24). Watermark remains out of scope.

## Screens

| File | What it shows |
|------|----------------|
| [application-profile-instance-state-preview-cancelled-invitation.jpg](./application-profile-instance-state-preview-cancelled-invitation.jpg) | Left: original obtain-invitation. Right: cancel-only look-back + cancel invitation+WP (invitation and WP) |

Image text can be misread. This file is the source of truth. Sample numbers (`9/-1602`, `8/-1475`, `7/-1278`) are layout-inspired by production cancel rows.

## Locked

| Decision | Choice |
|----------|--------|
| Layout | Same two-sided pattern as change |
| Original case | **Ýatyryldy** + under-name line + cancel-application link — **no** second document section |
| Cancel case look-back title | **Ýatyrylan çakylyk** |
| Strike-through | **Person name only** when life is **Ýatyryldy** |
| Numbers / clicks | Cancelled invitation № → PDF; cancel-application № → case (both) |
| Cancel invitation + WP | Show cancelled **work permit** the same way (`Ýatyrylan iş rugsady`) |
| Prototype | **Separate** from the change/renewed invitation prototype |
| Watermark | Not in scope for now |

## Sheet content

### Original obtain-invitation (`Çakylyk almak`)

1. **Berlen çakylyklar** — cancelled people: name strike-through, **Ýatyryldy**, under-name **Ýatyrylan çakylyk · {cancel app №}**.
2. No **Täze** / renewed section.

### Cancel invitation only (`Çakylyk ýatyrmak`)

1. **Ýatyrylan çakylyk** — look-back to original invitation (PDF + original application case).

### Cancel invitation + work permit (`Çakylyk we iş rugsadyny ýatyrmak`)

1. **Ýatyrylan çakylyk** — same look-back for invitation.
2. **Ýatyrylan iş rugsady** — same pattern for the cancelled work permit.

## Work permits on WP-producing originals (2026-11-24)

On **Ýagdaý syny** only (Berlen iş rugsatnamalary): cancelled WP shows **Ýatyryldy**, name strike-through, under-name **Ýatyrylan iş rugsady · {cancel app №}** (app → case; WP № → PDF). No renewed-WP section. Applies to all WP-producing profiles (extend_visa_wp, get_invitation_wp, workpermit_extension, …). Look-back also on cancel_workpermit / cancel_invitation_wp.
