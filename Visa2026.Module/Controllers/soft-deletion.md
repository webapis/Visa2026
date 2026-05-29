# Soft Deletion Implementation Guide

## 1. Overview

**Soft Deletion** (or "Fake Deletion") is a pattern where records are not physically removed from the database when a user deletes them. Instead, they are flagged as deleted.

### Why use this?
1.  **Data Integrity**: Preserves historical data and relationships.
2.  **Sync Rule Stability**: Our `SyncRule` engine relies on navigating relationships (e.g., `ApplicationItem.Invitation`). If an object is physically deleted, these relationships break immediately, causing rules to fail. Soft deletion keeps the object "alive" long enough for rules to run reliably.
3.  **Safety**: Allows administrators to restore accidentally deleted records.

---

## 2. Architecture & Components

The implementation consists of the following key components:

### 2.1. The Contract: `ISoftDelete`
*   **File**: `BusinessObjects\ISoftDelete.cs`
*   **Role**: Defines a standard interface ensuring any soft-deletable object has:
    *   `IsDeleted` (boolean)
    *   `DateDeleted` (DateTime?)
    *   `DeletedBy` (ApplicationUser)

### 2.2. The Logic Engine: `SoftDeleteController`
*   **File**: `Controllers\SoftDeleteController.cs`
*   **Target**: Any object implementing `ISoftDelete`.
*   **Responsibilities**:
    1.  **Hides Standard Delete**: Disables the default XAF Delete action.
    2.  **"Remove" Action**: Sets `IsDeleted = true`, records `DateDeleted` and `DeletedBy`, and triggers Sync Rules.
    3.  **"Restore" Action**: Resets `IsDeleted`, `DateDeleted`, and `DeletedBy` (Admin only).
    4.  **"Show Deleted" Toggle**: Allows Admins to see hidden records in the list.
    5.  **Default Filtering**: Automatically filters ListViews to hide deleted items unless the "Recycle Bin" or "Show Deleted" mode is active.

### 2.3. The Recycle Bin
*   **Controller**: `Controllers\RecycleBinNavigationController.cs`
    *   Dynamically creates a "Recycle Bin" navigation group.
    *   Populates it with items for **every** business object that implements `ISoftDelete`.
*   **Model Updater**: `Model\RecycleBinViewNodesGeneratorUpdater.cs`
    *   Automatically generates a specific Recycle Bin ListView for each type (e.g., `ApplicationItem_ListView_RecycleBin`).
    *   Adds `DateDeleted` and `DeletedBy` columns to these views.

---

## 3. How to Apply to New Business Objects

To enable Soft Deletion for a new Business Object (e.g., `Employee`), follow these steps:

### Step 1: Implement the Interface
Modify the class definition to implement `ISoftDelete`.

```csharp
public class Employee : Person, ISoftDelete // <--- Add Interface
{
    // ... existing code ...

    // Step 2: Add the Properties
    [Browsable(false)]
    public virtual bool IsDeleted { get; set; }

    [Browsable(false)]
    public virtual DateTime? DateDeleted { get; set; }

    [Browsable(false)]
    public virtual ApplicationUser DeletedBy { get; set; }
}
```

### Step 2: Visual Feedback (Automatic)
Types that implement `ISoftDelete` receive styling when `IsDeleted` is true:

- **Module** — `SoftDeleteAppearanceRegistration` (`CustomizeTypesInfo`): conditional appearance on **ListView** and **DetailView** (light gray `Gainsboro` background, gray text). No per-class `[Appearance]` attribute is required.
- **Blazor nested grids** — XAF conditional appearance does not reliably color rows in nested ListViews (e.g. **Person → Family Members**). `SoftDeleteGridRowAppearanceController` in `Visa2026.Blazor.Server` applies the `visa-soft-deleted-row` CSS class via `DxGrid.CustomizeElement` for every `ISoftDelete` list.

### Step 3: Validation and controllers
- **`SoftDeleteController`** — Remove / Restore / Show Deleted / list filtering (activates for any `ISoftDelete` type).
- **`SoftDeleteValidationController`** — Skips **`RuleRequiredField`** when `IsDeleted` is true (including rules from shared bases such as `PersonLinkedItemBase`).
- **`SoftDeleteRuleRequiredFieldRegistration`** — Adds `TargetCriteria = "!IsDeleted"` to required-field rules on members **declared** on each `ISoftDelete` type (model/UI alignment).

No per-class `TargetCriteria = "!IsDeleted"` is required on new soft-deletable types unless you need extra conditions (e.g. `IsEmployee And !IsDeleted`).

---

## 4. Integration with Sync Rules

Soft Deletion changes how we define "On Delete" logic in the `SyncRule` engine.

### The Problem with Hard Delete
When `TriggerType = Delete` is used with physical deletion, the object is often disconnected from its parents by the time the rule runs.
*   *Example*: Deleting an `ApplicationItem` breaks the link to `Application`, so we can't check `Application.ApplicationType`.

### The Solution with Soft Delete
Instead of triggering on `Delete`, we trigger on **Property Change** of `IsDeleted`.

**Configuration Pattern:**
*   **Source Type**: `ApplicationItem`
*   **Trigger Type**: `PropertyChanged`
*   **Source Property**: `IsDeleted`
*   **Source Value**: `true`
*   **Source Criteria**: `[Application.ApplicationType.Code] = '...'` (This works now because the object is still alive!)

---

## 5. User Experience Guide

### For Standard Users
*   **Deleting**: Select a record and click **"Remove"** (Trash icon).
    *   *Result*: The record disappears from the list.
*   **Visibility**: Deleted records are hidden by default.

### For Administrators
*   **Viewing Deleted**: Click the **"Show Deleted"** toggle in the toolbar to see grayed-out deleted records mixed with active ones.
*   **Restoring**:
    1.  Enable "Show Deleted" or go to the Recycle Bin.
    2.  Select the grayed-out record.
    3.  Click **"Restore"**.
*   **Permanent Delete**:
    1.  Go to the **Recycle Bin** navigation item.
    2.  Select the record.
    3.  Click the standard **"Delete"** (X) button. This performs a hard database delete.

---

## 6. Technical Reference: Controller Logic

The `SoftDeleteController` uses specific keys to manage state:

```csharp
// 1. Hiding Standard Delete
deleteController.DeleteAction.Active["SoftDeleteImplemented"] = false;

// 2. Filtering the List
listView.CollectionSource.Criteria["ExcludeDeleted"] = CriteriaOperator.Parse("IsDeleted = ?", false);

// 3. Handling Recycle Bin View
if (View.Id.Contains("RecycleBin")) {
    // Show ONLY deleted
    listView.CollectionSource.Criteria["ShowOnlyDeleted"] = CriteriaOperator.Parse("IsDeleted = ?", true);
    // Enable Hard Delete
    deleteController.DeleteAction.Active.RemoveItem("SoftDeleteImplemented");
}
```
```

<!--
[PROMPT_SUGGESTION]Can you update the RecycleBinNavigationController to automatically find all types that implement ISoftDelete and add them to the Recycle Bin navigation group?[/PROMPT_SUGGESTION]
[PROMPT_SUGGESTION]How do I add a "DateDeleted" and "DeletedBy" property to track when and who performed the soft delete?[/PROMPT_SUGGESTION]
