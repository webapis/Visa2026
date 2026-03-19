--- /dev/null
+++ c:\Users\IT\source\repos\Visa2026\Visa2026.Module\BusinessObjects\StateChangeTracking.md
@@ -0,0 +1,148 @@
+# Design Document: State Change Tracking System
+
+## 1. Overview
+
+The State Change Tracking System is a generic, rule-based engine designed to log the lifecycle and state transitions of any business object in the application. Unlike the `SyncRule` engine which *updates* data, this engine *records* history.
+
+It allows administrators to define dynamic rules in the UI (e.g., "When an Application is Approved, log a 'Process Completed' entry for the associated Visa").
+
+## 2. Architecture
+
+The system consists of three main components:
+
+1.  **`StateChangeRule` (Configuration)**: Defines *when* to log an event and *what* the state string should be.
+2.  **`StateChangeLog` (Storage)**: The immutable record stored in the database containing the timestamp, state, and links to the involved objects.
+3.  **`StateChangeTrackingHelper` (Engine)**: The static helper class that hooks into Entity Framework events to execute rules.
+
+## 3. Business Objects
+
+### 3.1. StateChangeRule (The Logic)
+
+This business object stores the configuration rules. It allows you to refine triggers dynamically from the UI.
+
+| Property | Type | Description |
+| :--- | :--- | :--- |
+| **Name** | `String` | Unique name of the rule (e.g., "Log Visa Extension Start"). |
+| **Source Type** | `Type` | The Business Object type that triggers the rule (e.g., `ApplicationProgress`). |
+| **Trigger Type** | `Enum` | Events: `Save`, `Create`, `Update`, `Delete`, `PropertyChanged`. |
+| **Source Property** | `String` | (Optional) specific property to watch (e.g., `State`). |
+| **Source Criteria** | `Criteria` | (Optional) Logic to filter triggers (e.g., `[State] = 'Approved'`). |
+| **Target Path** | `String` | Navigation path from Source to the Tracked Object (e.g., `Application.Visas`). |
| **Target Match Criteria** | `Criteria` | (Optional) If Target Path is a collection, filters which items to log against. |
| **Target Sub-Path** | `String` | (Optional) Navigates from the resolved target (or collection item) to a nested object (e.g., `CurrentVisa`). |
+| **State (Result)** | `String` | The string to write to the log (e.g., "Visa Extended"). |
+| **Description Template**| `String` | (Optional) A template for the log description allowing macros (e.g., "Approved by @Source.User.Name"). |
+| **Is Active** | `Bool` | Master switch to enable/disable the rule. |
+
+### 3.2. StateChangeLog (The Data)
+
+This generic business object stores the actual history. It is loosely coupled (using string IDs and Types) to allow tracking any object without hard-coded relationships.
+
+| Property | Type | Description |
+| :--- | :--- | :--- |
+| **Target BO Type** | `Type` | The type of the object being tracked (e.g., `Visa`). |
+| **Target Object ID** | `String` | The primary key of the tracked object. |
+| **State** | `String` | The specific state recorded (e.g., "Process Started"). |
+| **DateTime** | `DateTime`| When the change occurred. |
+| **Source BO Type** | `Type` | The type of object that caused the change (e.g., `Application`). |
+| **Source Object ID** | `String` | The ID of the triggering object. |
+| **Rule Name** | `String` | The name of the `StateChangeRule` that created this log. |
+| **Description** | `String` | Additional details generated from the rule template. |
+| **User** | `String` | The username of the person who triggered the change. |
+
+## 4. Workflow Logic
+
+The `StateChangeTrackingHelper` will be injected into the `DbContext` or `Save` events, similar to `CrossObjectSyncHelper`.
+
+1.  **Event Trigger**: A user saves an object (e.g., a new `ApplicationItem`).
+2.  **Rule Lookup**: The Helper finds all active `StateChangeRules` where `SourceType` matches the saved object.
+3.  **Criteria Check**: It evaluates `SourceCriteria` (e.g., is `VisaIssued == true`?).
+4.  **Target Resolution**:
+    *   It parses `TargetPath`.
+    *   If `TargetPath` is `Application.ApplicationItems`, it iterates through the collection.
+    *   It uses `TargetMatchCriteria` (if needed) to find specific items.
    *   If `TargetSubPath` is defined, it navigates further (e.g., from `ApplicationItem` to `CurrentVisa`).
+5.  **Log Creation**:
+    *   It creates a new `StateChangeLog` record.
+    *   It snapshots the current time, user, and the state defined in the rule.
+    *   It saves the log to the database.
+
+## 5. UI Integration
+
+To view these logs "in context" (e.g., seeing the history tab on a Visa Detail View):
+
+1.  **Controller**: A `StateChangeLogListViewController` will be created.
+2.  **Filter Logic**: When opening a Detail View (e.g., Visa), the controller will:
+    *   Find the `StateChangeLog` list view.
+    *   Filter it where `TargetObjectType == CurrentObject.Type` AND `TargetObjectId == CurrentObject.ID`.
+
+This provides a universal "History" tab for any object in the system without adding a `History` collection to every single class.
+
+## 6. Example Scenarios
+
+### Scenario A: Track Visa Extension Process
+*Goal: When an Application is sent to the ministry, the Visa history should show "Extension Process Started".*
+
+*   **Rule Configuration**:
+    *   **Name**: `Log Visa Extension Start`
+    *   **Source Type**: `Application`
+    *   **Trigger Type**: `PropertyChanged` (`CurrentState`)
+    *   **Source Criteria**: `[CurrentState.Code] = 'SENT_TO_MINISTRY' And [ApplicationType.Code] = 'visa_extension'`
+    *   **Target Path**: `Visas` (Collection on Application)
+    *   **State**: `Extension Process Started`
+    *   **Description**: `Application sent to ministry on @Source.ApplicationDate`
+
+*   **Result (Log Entry)**:
+    *   **Target**: Visa #1024
+    *   **State**: "Extension Process Started"
+    *   **Date**: 2026-03-20 10:00 AM
+
+### Scenario B: Track Work Permit Issue
+*Goal: When a WorkPermit is created, log "Permit Issued" on the Employee (Person).*
+
+*   **Rule Configuration**:
+    *   **Name**: `Log Employee Work Permit`
+    *   **Source Type**: `WorkPermit`
+    *   **Trigger Type**: `Create`
+    *   **Target Path**: `Employee`
+    *   **State**: `Work Permit Issued`
+    *   **Description**: `Permit #@Source.Number issued.`
+
+## 7. Implementation Roadmap
+
+1.  **Create Business Objects**: Implement `StateChangeRule` and `StateChangeLog` in the Module.
+2.  **Implement Helper**: Create `StateChangeTrackingHelper.cs` with logic similar to `CrossObjectSyncHelper`, but utilizing `IObjectSpace.CreateObject<StateChangeLog>()`.
+3.  **Register Events**: Hook the helper into `DbContext` or XAF Controller events (Save/Delete).
+4.  **Create UI Controller**: Implement the logic to display logs on Detail Views.
+5.  **Seed Rules**: Add default rules in `Updater.cs`.
