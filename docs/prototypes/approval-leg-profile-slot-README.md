# Approval leg profiles — preview-slot CRUD (no XAF UI)

Custom occupant in `#visa-preview-slot`. Replaces the native Configuration ListView popup from **Edit in Configuration**.

## Locked (officer 2026-08-27)

| Decision | Choice |
|----------|--------|
| Host | Preview slot only. No XAF ListView, DetailView, or OK/Cancel lookup. |
| First screen | Catalog of shared chains (search, **+ New**, **Open**). |
| Default for this template | Stays on wizard Identity radios. Slot does not set Default. |
| In-use chain | Ministry order locked. Snapshots on existing cases stay. Use **+ New** for a different chain. |

## Screens

| File | State |
|------|--------|
| [approval-leg-profile-slot-01-catalog.png](./approval-leg-profile-slot-01-catalog.png) | Catalog after **Edit in Configuration** |
| [approval-leg-profile-slot-02-edit-locked.png](./approval-leg-profile-slot-02-edit-locked.png) | **Open** TE-EN while used by applications |
| [approval-leg-profile-slot-03-edit.png](./approval-leg-profile-slot-03-edit.png) | **Open** unused chain — edit ministries, Save / Delete. **+ New ministry** creates a shared `ApprovingMinistry` then adds it to the chain. |
| [approval-leg-profile-slot-04-empty.png](./approval-leg-profile-slot-04-empty.png) | Empty catalog + wizard “no shared versions yet” |
| [approval-leg-profile-slot-05-new.png](./approval-leg-profile-slot-05-new.png) | **+ New** blank form — empty Code, no ministries, Cancel / Create |

Wizard Identity stays on the left. Slot is catalog CRUD only. **Shipped** (slice 8p): `#visa-preview-slot` occupant `ApprovalLegCatalog`.

## Not in this set

Delete confirm. Ask if that PNG is needed next.

## Domain

Shared `ApprovalLegProfile` + ordered `ApprovalLegProfileMinistryLeg`. Application Profile stores **Default** only. Instances snapshot ministries at create.