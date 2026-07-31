# Business Object: TravelHistory

## 1. Purpose
The `TravelHistory` business object represents the travel history of a `Person`. It stores information about their trips, including dates, types of travel, locations, and notes. Officers maintain these rows manually on the person detail (nested Travel histories list). Registration application lines do **not** auto-create or lock travel history.

---

## 2. Inheritance

Inherits `BaseObject`. Concrete types: `ExternalArrival`, `ExternalDeparture`, `InternalArrival`, `InternalDeparture` (TPH discriminator).

---

## 3. Properties

| Property Name    | Data Type    | Description                                                              | Constraints / Validation Rules                                                                                                                                                                                                   |
|-------------------|-------------|--------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Person`           | `Person`      | The person to whom this travel history record belongs.                      | Required.                                                                                                                                                                                                                    |
| `TravelDate`       | `DateTime`    | The date of the travel.                                                    | Required.                                                                                                                                                                                                                    |
| `TravelType`       | `TravelType?` | The type of travel (Internal / External).                                  | Required.                                                                                                                                                                                                                    |
| `MovementType`     | `MovementType?`| Entry or Exit.                                                             | Required.                                                                                                                                                                                                                    |
| `CheckPoint`       | `CheckPoint`  | The border checkpoint used for external travel.                            | Required if `TravelType` is `External`. Hidden when not external.                                                                                                                                                              |
| `Country`          | `Country`     | Country for external travel.                                               | Required if external.                                                                                                                                                                                                        |
| `Region` / `City`  | lookups       | Destinations for internal travel.                                          | Required if internal; city filtered by region.                                                                                                                                                                               |
| `Notes`            | `string`      | Travel notes (`Travel Notes` in UI).                                       | Optional.                                                                                                                                                                                                                    |
| `Title`            | `string`      | Summary title.                                                             | Not Mapped. Default display property.                                                                                                                                                                                        |

Former `SourceApplicationItem` / display aliases were removed — see **`docs/DEPRECATED.md`** and **`docs/REGISTRATION_TRAVEL_HISTORY_SYNC.md`**.

---

## 4. Business Rules & Logic

- **Travel Type and Checkpoint**: `CheckPoint` and `Country` are only required/visible when `TravelType` is `External`; `Region`/`City` when `Internal`.
- **Manual CRUD only**: no link from `ApplicationItem`; all rows are editable on the person detail.
- **Defaults on create**: `TravelDate` = today; default `CheckPoint` / `Country` when present; concrete subclasses set `TravelType` / `MovementType`.

---

## 5. UI & Behavior Notes

- **Navigation**: Nested under Person detail (Travel histories), not a top-level nav item.
- **New actions**: External Arrival, External Departure, Internal Arrival, Internal Departure.
- **Immediate Post Data**: `TravelType` / `Region` drive show/hide and cascade clears.
