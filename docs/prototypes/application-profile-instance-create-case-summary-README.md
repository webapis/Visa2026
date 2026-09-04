# Create Case summary (2026-09-04)

Officers should set Case summary Use fields while creating the Application Profile Instance, so Overview does not start empty and they do not need an extra Edit/Update after create.

**Status:** implemented 2026-09-04.

## How (proposed)

Add a last picker step after **Choose Organization**. Organization becomes **Continue**. This step is **Create application**.

| Route | Steps |
|-------|--------|
| Via ministry | Profile → Approval legs → Organization → **Case summary** → create |
| Direct migration | Profile → Organization → **Case summary** → create |

Reuse `ApplicationWorkspaceCaseHeaderFieldsHelper` (same visible Use fields as Overview Edit). Apply profile defaults and auto application number/date in memory, then persist them on the first save with Company / Signatory / Representative.

## Locked for this prototype (confirm before implement)

| Decision | Choice |
|----------|--------|
| Where | Last create-picker step, not a landing-on-Overview-already-in-Edit shortcut |
| Fields | Same profile-gated Use fields as Case summary Edit. **Application number**, **Application date**, and **Process number** are not on create (auto / Advance rule). One field per line. |
| Defaults | Profile defaults pre-filled (Visa type / Category / Period / Urgency / …). Officer can change them |
| Required | Create stays disabled while a required Use field is empty (same list as the office-prep gate, minus Process number) |
| After create | Case workspace Overview should already be complete. Edit on Overview stays for later changes |
| Does not change | Application Profile template. Approval legs and Organization stay their own steps |

## Screen

| File | State |
|------|--------|
| [application-profile-instance-create-case-summary-prototype.png](./application-profile-instance-create-case-summary-prototype.png) | Last step, defaults + officer values filled, **Create application** enabled |

Intended subtitle (if the PNG text is off): *Same Use fields as the case Overview. Profile defaults are already filled. Set the rest now so you do not need to Edit after create.*