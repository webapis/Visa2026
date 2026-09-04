# Issued work permit origin — Mermaid diagram sources

Canonical flowcharts for [`docs/APPLICATION_PROFILE_ISSUED_WORK_PERMIT_ORIGIN.md`](../../APPLICATION_PROFILE_ISSUED_WORK_PERMIT_ORIGIN.md).

| File | Purpose |
|------|---------|
| `mental-model-combined.mmd` | Both patterns (overview) |
| `work-permit-direct.mmd` | Direct work permit from instance (`ProduceWorkPermit`; e.g. `extend_visa_wp`, WP-only issuance) |
| `invitation-and-work-permit.mmd` | Dual **May produce** invitation + work permit from one case (`App_Inv_And_WP` / `get_invitation_wp`) |

The canonical doc embeds the same diagrams as fenced ` ```mermaid ` blocks. **Edit the `.mmd` files first**, then sync the matching blocks in `APPLICATION_PROFILE_ISSUED_WORK_PERMIT_ORIGIN.md`.

**Not shown in these diagrams (orthogonal):**

- **Input** `WorkPermitItem` M2M (`WorkPermitItem.ApplicationProfileInstances`) — predecessor lines when profile `RequirePersonWorkPermitItem` is on; not the issued header path.
- **Issued visa** on the same case — see [`../issued-visa-origin/`](../issued-visa-origin/).

Preview: open a `.mmd` in Cursor/VS Code with a Mermaid preview extension, or paste into [mermaid.live](https://mermaid.live).
