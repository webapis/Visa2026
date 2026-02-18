# Business Object: ApplicationProgress

## 1. Purpose
The `ApplicationProgress` business object represents a single step or event in the lifecycle of an `Application`. It serves as a historical audit trail, tracking changes in state, location, and other key details over time.

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---|---|---|---|
| `Application` | `Application` | The parent application this progress record belongs to. | Required. |
| `State` | `ApplicationState` | The status of the application at this specific point in time (e.g., Office, ToMinistry). | Required. |
| `Location` | `ApplicationLocation` | The physical or logical location of the application (e.g., Archive, Cabinet 5). | Required. |
| `Date` | `DateTime` | The specific date and time this progress step occurred. | Required; Default: Now. |
| `Description` | `string` | Optional comments or details about this step. | Max 255 chars. |

## 3. Relationships
*   **Parent**: `Application` (One-to-Many).
*   **Lookups**: `ApplicationState`, `ApplicationLocation`.

## 4. Current State Logic (The "Latest Item" Pattern)

Unlike the `SingleActiveBaseObject` pattern which uses a manual `IsActive` flag, the `ApplicationProgress` object dictates the state of the parent `Application` based strictly on the **timeline**.

### The Rule
The `Application.CurrentState` property must always reference the `ApplicationProgress` record associated with that application that has the **most recent `Date`**.

### Lifecycle Behavior

To ensure data integrity, the synchronization logic must occur during the following events:

1.  **On Insert (New Record)**:
    *   When a new `ApplicationProgress` is created, the system compares its `Date` with the current `Application.CurrentState`.
    *   If the new date is newer (or if there was no previous state), `Application.CurrentState` is updated to this new record.

2.  **On Update (Existing Record)**:
    *   If the `Date` of an existing record is modified, the system must re-evaluate the entire history collection of the parent `Application`.
    *   It finds the record with the new maximum `Date` and updates `Application.CurrentState` accordingly.

3.  **On Delete**:
    *   If the record being deleted is currently the `Application.CurrentState`, the system must find the *next* latest record in the history and promote it to the new `CurrentState`.
    *   If the history becomes empty, `Application.CurrentState` is set to `null`.

### Technical Implementation Strategy
*   **Trigger**: The `ApplicationProgress` object (Child) drives the update via `OnSaving` and `OnDeleting` overrides.
*   **Logic**:
    ```csharp
    var latest = Parent.ProgressHistory
        .OrderByDescending(x => x.Date)
        .FirstOrDefault();
    Parent.CurrentState = latest;
    ```