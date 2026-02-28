# Helper Class: CrossObjectSyncHelper

## 1. Purpose

The `CrossObjectSyncHelper` is a static helper class designed to centralize the business logic where saving one object triggers a status update on another related object. This pattern is common throughout the application, for example, when issuing a `Visa` should automatically mark the corresponding `ApplicationItem` as `VisaIssued = true`.

## 2. The Problem Solved

Previously, this synchronization logic was scattered across the `OnSaving()` methods of multiple business objects (`Visa`, `WorkPermitItem`, `InvitationItem`, etc.). This approach had several drawbacks:
- **Code Duplication**: The logic to find the related `ApplicationItem` was repeated.
- **Poor Discoverability**: To understand the full impact of saving an object, a developer would need to find and read its specific `OnSaving()` method. There was no central place to see all such cross-object interactions.
- **Reduced Maintainability**: A change to the synchronization pattern would require finding and updating multiple files.

## 3. The Solution (Mediator, Registration & Dynamic Rules)

The `CrossObjectSyncHelper` acts as a **Mediator** that combines two approaches:
1.  **Code-Based Registration**: Allows developers to define complex, compile-time safe rules in C#.
2.  **Dynamic Database Rules**: Allows administrators to define synchronization logic at runtime using the `SyncRule` business object.

### How it Works
1.  The `OnSaving()` or `OnDeleting()` method of a source object calls the helper.
2.  **Step 1 (Code Rules)**: The helper executes any hard-coded rules registered for the object's type.
3.  **Step 2 (DB Rules)**: The helper queries the `SyncRule` table for active rules matching the object type and trigger.
4.  **Step 3 (Logging)**: Execution details (success, warnings, errors) are logged to the `SyncRuleLog` table.

## 4. Benefits

- **Centralized Logic**: All cross-object update rules are now in one place, making them easy to find, understand, and maintain.
- **Simplified Business Objects**: The `OnSaving()` methods of the source objects are now clean and simple.
- **Improved Readability**: It's clearer that saving an object has side effects, which are managed by a dedicated service.
- **Extensibility**: New rules can be added via the database without recompiling the application.
- **Observability**: All automated updates are audited in the `SyncRuleLog`.

## 5. Implementation

### A. Registering Code-Based Rules
You can register rules in the static constructor of the helper or in your Module initialization code.

```csharp
CrossObjectSyncHelper.RegisterRule<MyNewObject>(
    onSave: obj => {
        // Logic to execute when MyNewObject is saved
    },
    onDelete: obj => {
        // Logic to execute when MyNewObject is deleted
    }
);
```