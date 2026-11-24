# Application Profile Instance — Ýagdaý syny renewed invitation

Prototype for showing the **renewed invitation** next to the **changed** invitation on Ýagdaý syny.

**Status:** Implemented (2026-11-24). Watermark remains out of scope.

Related cancel prototype: [application-profile-instance-state-preview-cancelled-invitation-README.md](./application-profile-instance-state-preview-cancelled-invitation-README.md).

## Screens

| File | What it shows |
|------|----------------|
| [application-profile-instance-state-preview-renewed-invitation.jpg](./application-profile-instance-state-preview-renewed-invitation.jpg) | Left: initial `Çakylyk almak`. Right: change case look-back `Üýtgedilen çakylyk` |

Image text can be misread. This file is the source of truth.

## Locked — Ýagdaý syny

| Decision | Choice |
|----------|--------|
| Layout | New **Täze çakylyklar** section (renewed invitations grouped in that one section) |
| Numbers | **Both** renewed invitation № and change-application № |
| Where | Initial `Çakylyk almak` **and** change `Çakylygy üýtgetmek` look-back |
| Change look-back title | **Üýtgedilen çakylyk** |
| Clicks | Invitation № → PDF preview; application № → open that case |
| Scope | Invitations only (not WP / visa yet) |
| Strike-through | **Person name only** (not under-name line, not state pill) |
| Under-name line | **Keep** `Täze çakylyk · … · №` under the struck name |
| Life / link data | Treat current production life/link as correct; this work is UI |

## Deferred

| Topic | Note |
|-------|------|
| Invitation preview life-text watermark | Discussed to stop accidental send of non-valid invitations; **not required for now** |