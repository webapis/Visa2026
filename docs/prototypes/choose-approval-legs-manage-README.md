# Choose Approval legs — manage shared chains (2026-09-03)

Officer create-instance step. Reuse the existing `#visa-preview-slot` occupant (`ApprovalLegCatalog`). Do not copy chains onto the Application Profile.

**Status:** shipped 2026-09-03 — Choose Approval legs **Catalog** / + New / Open; Identity Default only.

## Locked for this set

| Decision | Choice |
|----------|--------|
| Where officers create / edit chains | **Choose Approval legs** (**Catalog**, + New, Open, **Make default**) |
| Host for CRUD | Same preview slot as Configuration (`ApprovalLegProfileSlotPanel`) |
| After Create / Save | New or updated card is selected on the left so **Use profile** can continue |
| Empty catalog | Create the first chain here. Do not send officers to Configure profile |
| In-use chain | Ministry order locked. Snapshots stay. Use + New for a different order |
| Profile wizard Identity | **Default** only (later). No Edit in Configuration / Refresh on this step |

## Screens

| File | State |
|------|--------|
| [choose-approval-legs-manage-01-picker.png](./choose-approval-legs-manage-01-picker.png) | Cards + **Catalog** / **+ New** + per-card **Open**. Default pill. Back / Use profile |
| [choose-approval-legs-manage-02-empty.png](./choose-approval-legs-manage-02-empty.png) | This profile has a route but **0** shared chains. Empty dashed box + **+ New**. Use profile disabled |
| [choose-approval-legs-manage-03-slot-catalog.png](./choose-approval-legs-manage-03-slot-catalog.png) | Left stays Choose Approval legs. Right slot catalog (search, + New, Open, Duplicate, Used by) |
| [choose-approval-legs-manage-04-slot-new.png](./choose-approval-legs-manage-04-slot-new.png) | Slot **+ New** form (Code, Active, ministries, Create) |
| [choose-approval-legs-manage-05-slot-edit.png](./choose-approval-legs-manage-05-slot-edit.png) | Slot **Open** unused chain (edit order, Save / Delete) |

## Not in this set

- Slim Configure Identity (Default radios only) — follow-up PNG if needed
- In-use locked Open (already [approval-leg-profile-slot-02-edit-locked.png](./approval-leg-profile-slot-02-edit-locked.png); same slot)

## Domain

Shared `ApprovalLegProfile` + ordered ministries. Profile stores **Default** only. Instance snapshots at create.