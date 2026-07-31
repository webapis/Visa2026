# TravelHistory — manual officer CRUD only

`Person.TravelHistories` is the longitudinal travel log. Officers create and edit rows on the person detail (External/Internal Arrival/Departure). Registration **`ApplicationItem`** travel fields are independent application-line data and **do not** create, update, or delete `TravelHistory`.

## Former sync (removed)

Earlier builds upserted linked `TravelHistory` rows from check-in/out registration types via `SourceApplicationItemID`. That behavior is **removed**:

| Removed | Replacement |
|---------|-------------|
| `RegistrationTravelHistorySyncService` | Manual CRUD on `Person.TravelHistories` |
| `RegistrationTravelHistoryBackfillUpdater` | `TravelHistorySourceApplicationItemCleanupUpdater` (NULL + drop FK) |
| `TravelHistory.SourceApplicationItem` / `SourceApplicationItemID` | Dropped; existing rows kept as editable |

See **`docs/DEPRECATED.md`**.

## Officer workflow

1. Open person DetailView → Travel histories nested list.
2. **New** → External Arrival / External Departure / Internal Arrival / Internal Departure.
3. Fill required fields and save.

Registration applications (`App_Reg_Check_In`, etc.) still store movement on the line (`TravelDate`, `TravelType`, …) for the application workflow; they no longer mirror into the person travel log.
