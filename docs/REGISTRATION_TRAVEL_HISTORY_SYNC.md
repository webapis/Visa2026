# Registration ApplicationItem → TravelHistory sync

Registration movement data is stored on **`ApplicationItem`** (flattened travel fields). **`Person.TravelHistories`** is the longitudinal travel log. For the four check-in/out registration application types, saving an application line **upserts** a linked **`TravelHistory`** row.

## Source of truth

| Data | Master |
|------|--------|
| Registration travel on an application | `ApplicationItem` (`TravelDate`, `TravelType`, `MovementType`, `CheckPoint`, …) |
| Person travel log | `TravelHistory` |
| Rows created by this feature | `TravelHistory.SourceApplicationItem` → `ApplicationItem` |

**No bidirectional sync:** editing `TravelHistory` on the person does not change `ApplicationItem`. Linked rows are read-only in detail view (`[Appearance]` on `TravelHistory`).

**Person → Travel histories list:** `SourceApplication_FullApplicationNumber` and `SourceApplication_ApplicationDate` show the parent application for synced rows; manual entries leave these empty.

## Application types that sync

| `ApplicationType.Name` | `TravelHistory` type |
|------------------------|----------------------|
| `App_Reg_Check_In` | `ExternalArrival` |
| `App_Reg_Check_Out` | `ExternalDeparture` |
| `App_Reg_Check_In_Internal` | `InternalArrival` |
| `App_Reg_Check_Out_Internal` | `InternalDeparture` |

**Not synced:** info-change registration types (`App_Reg_Info_Change_*`, `App_Reg_ext`, same-city address change, etc.).

## Trigger

- **`ApplicationItem.OnSaving`** → `RegistrationTravelHistorySyncService.SyncFromApplicationItem` when travel data validates.
- **`Application.OnSaving`** when `IsDeleted` → soft-delete linked histories for all items.

Manual `TravelHistory` rows (no `SourceApplicationItem`) are unchanged.

## Field mapping

| `ApplicationItem` / `Application` | `TravelHistory` |
|-----------------------------------|-----------------|
| `Person` | `Person` |
| `TravelDate` | `TravelDate` |
| `TravelType`, `MovementType` | same |
| `CheckPoint` | `CheckPoint` (external) |
| `TravelNotes` | `Notes` (`Travel Notes` in UI) |

Travel purpose on the application line is **`CurrentPositionHistory`**, not a `PurposeOfTravel` lookup.
| Default country lookup | `Country` (external, if unset) |
| `Application.ToCity` + region | `City`, `Region` (internal **entry**) |
| `Application.FromCity` + region | `City`, `Region` (internal **exit**) |

## Delete / restore

- Application item or parent application **soft-deleted** → linked `TravelHistory` soft-deleted (FK kept).
- Item restored (`IsDeleted = false`) and saved again → linked history restored and fields refreshed.

## Implementation

- **Service:** `Visa2026.Module/Services/RegistrationTravelHistorySyncService.cs`
- **FK:** `TravelHistory.SourceApplicationItemID` (unique when not null)
- **Backfill:** `DatabaseUpdate/RegistrationTravelHistoryBackfillUpdater.cs` (idempotent after deploy)

## Legacy backfill

On database update, existing registration lines of the four types are processed: link by `SourceApplicationItemID`, or heuristic match (person + date + travel/movement type + concrete type), or create a new row. Manual histories without a link are not overwritten.
