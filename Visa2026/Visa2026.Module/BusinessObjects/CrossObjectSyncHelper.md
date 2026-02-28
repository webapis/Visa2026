# Helper Class: CrossObjectSyncHelper

## 1. Purpose

The `CrossObjectSyncHelper` is a static helper class designed to centralize the business logic where saving one object triggers a status update on another related object. This pattern is common throughout the application, for example, when issuing a `Visa` should automatically mark the corresponding `ApplicationItem` as `VisaIssued = true`.

## 2. The Problem Solved

Previously, this synchronization logic was scattered across the `OnSaving()` methods of multiple business objects (`Visa`, `WorkPermitItem`, `InvitationItem`, etc.). This approach had several drawbacks:
- **Code Duplication**: The logic to find the related `ApplicationItem` was repeated.
- **Poor Discoverability**: To understand the full impact of saving an object, a developer would need to find and read its specific `OnSaving()` method. There was no central place to see all such cross-object interactions.
- **Reduced Maintainability**: A change to the synchronization pattern would require finding and updating multiple files.

## 3. The Solution (Mediator Pattern)

The `CrossObjectSyncHelper` acts as a **Mediator**. It decouples the objects from each other. Now, the individual business objects no longer need to know the specific details of which other objects to update. They simply notify the helper, which contains all the rules.

### How it Works
1.  The `OnSaving()` method of a source object (e.g., `Visa`) is now simplified to a single call: `CrossObjectSyncHelper.SyncOnSave(this);`.
2.  The `SyncOnSave` method in the helper class receives the object being saved.
3.  It uses a series of `if (sourceObject is ...)` checks to identify the type of the object.
4.  Once the type is identified, it executes the specific logic to find and update the related target object(s).

## 4. Benefits

- **Centralized Logic**: All cross-object update rules are now in one place, making them easy to find, understand, and maintain.
- **Simplified Business Objects**: The `OnSaving()` methods of the source objects are now clean and simple.
- **Improved Readability**: It's clearer that saving an object has side effects, which are managed by a dedicated service.

## 5. Implementation

```csharp
public static class CrossObjectSyncHelper
{
    public static void SyncOnSave(BaseObject sourceObject)
    {
        if (sourceObject is WorkPermitItem wpi)
        {
            // Logic to update ApplicationItem...
        }
        // ... and so on for other types.
    }
}
```