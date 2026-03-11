# Helper Class: CrossObjectSyncHelper

## 1. Purpose

The `CrossObjectSyncHelper` is a static helper class designed to centralize the business logic where saving one object triggers a status update on another related object. It functions as the execution engine for the dynamic rules defined in the `SyncRule` business object.

## 2. The Problem Solved

Without a centralized engine, synchronization logic would be scattered across the `OnSaving()` methods of multiple business objects (`Visa`, `WorkPermitItem`, `InvitationItem`, etc.). This approach has several drawbacks:
- **Code Duplication**: The logic to find and update related objects would be repeated.
- **Poor Discoverability**: To understand the full impact of saving an object, a developer would need to find and read its specific `OnSaving()` method. There was no central place to see all such cross-object interactions.
- **Reduced Maintainability**: A change to the synchronization pattern would require finding and updating multiple files.
- **No Runtime Flexibility**: All rules would be hard-coded, requiring a new deployment for any change.

## 3. The Solution (Dynamic Rule Engine)

The `CrossObjectSyncHelper` acts as a **Mediator** that executes rules defined in the database.

### How it Works
1.  The `OnSaving()` or `OnDeleting()` method of a source object calls the helper (e.g., `CrossObjectSyncHelper.SyncOnSave(this)`).
2.  The helper queries the `SyncRule` table for active rules matching the object's type and the trigger event (`Save`, `Update`, or `Delete`).
3.  For each matching rule, it evaluates the conditions and updates the target object(s).
4.  Execution details (success, warnings, errors) are logged to the `SyncRuleLog` table for auditing and troubleshooting.

## 4. Benefits

- **Centralized Logic**: All cross-object update logic is driven by the `SyncRule` configurations.
- **Simplified Business Objects**: The `OnSaving()` methods of the source objects are now clean, one-line delegations.
- **Improved Readability**: It's clear that saving an object has side effects, which are managed by a dedicated, configurable service.
- **Extensibility**: New rules can be added via the database without recompiling the application.
- **Observability**: All automated updates are audited in the `SyncRuleLog`.

## 5. Implementation
This section describes how to invoke the sync engine from your business objects.

### 5.1. `SyncOnSave(BaseObject sourceObject)`
*   **When to use**: Call this in the `OnSaving()` override of a business object.
*   **Behavior**: Internally, this method determines if the object is new or being updated and calls `ExecuteDbRules` with the appropriate trigger (`Save`, `Create`, or `Update`).
*   **Example**:
```csharp
public override void OnSaving()
{
    base.OnSaving();
    CrossObjectSyncHelper.SyncOnSave(this);
}
```