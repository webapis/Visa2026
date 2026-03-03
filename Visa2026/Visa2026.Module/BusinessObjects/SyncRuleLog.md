# Business Object: SyncRuleLog

## 1. Overview

The `SyncRuleLog` business object serves as an audit trail and diagnostic tool for the **Dynamic Synchronization Rule Engine**. It records the execution results of dynamic rules defined in the `SyncRule` object, providing visibility into automated system actions.

## 2. User Guide

### 2.1. Purpose
As an administrator or developer, you use the Sync Rule Logs to:
*   **Verify Success**: Confirm that a rule is firing and updating records as expected.
*   **Troubleshoot Issues**: Identify why a rule failed (e.g., "Target property not found") or why it didn't update anything (e.g., "Target path resulted in null").
*   **Audit Changes**: Track when specific automated updates occurred and which object triggered them.

### 2.2. Viewing Logs
Logs can be accessed in two ways:
1.  **Global List**: Navigate to **System > Sync Rule Logs** in the main navigation menu to see a chronological list of all executions.
2.  **Per Rule**: Open a specific **Sync Rule** detail view and look at the **Logs** tab. This shows the history specific to that rule.

### 2.3. Log Statuses
The `Status` column indicates the outcome of the rule execution:
*   **Success**: The rule executed and successfully updated at least one target object.
*   **Warning**: The rule ran, but encountered a non-critical issue (e.g., the target object wasn't found, or the property couldn't be written). No data was changed.
*   **Error**: A system exception occurred during execution. The log will contain technical details in the `Details` tab.
*   **Info**: General informational messages.

## 3. Technical Implementation

### 3.1. Data Structure
The object is defined in `SyncRuleLog.cs` and inherits from `BaseObject`. It uses `Date` as the default display property.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Date` | `DateTime` | The timestamp of execution (defaults to `DateTime.Now`). |
| `SyncRule` | `SyncRule` | Association to the rule definition. |
| `Status` | `SyncRuleLogStatus` | Enum (`Info`, `Success`, `Warning`, `Error`). |
| `SourceObjectId` | `string` | The ID of the object that triggered the rule (stored as string to be generic). **Max Length: 255.** |
| `Message` | `string` | A human-readable summary of the result. **Unlimited size.** |
| `Details` | `string` | Technical stack trace or detailed error information (unlimited size). |

### 3.2. Automation Logic
Logs are generated automatically by the `CrossObjectSyncHelper` class inside the `ExecuteDbRules` method. This applies to all trigger types (`Save`, `Create`, `Update`, `Delete`, `PropertyChanged`). The system does not log every single evaluation (to prevent spamming the database), but specifically logs outcomes:

1.  **Validation Failures (Warning)**:
    *   If the `TargetPath` configured in the rule resolves to `null` (e.g., the parent object is missing).
    *   If the `TargetProperty` does not exist on the target object or is read-only.

2.  **Successful Updates (Success)**:
    *   If the rule successfully updates one or more target objects, a log is created stating "Successfully updated X target(s)."

3.  **Exceptions (Error)**:
    *   If an unhandled exception occurs during rule processing (e.g., data type conversion error), it is caught, and the stack trace is saved to the `Details` property.

### 3.3. Code Reference
*   **Definition**: `Visa2026.Module.BusinessObjects.SyncRuleLog`
*   **Usage**: `Visa2026.Module.BusinessObjects.CrossObjectSyncHelper.CreateLog(...)`

```csharp
// Example of log creation in CrossObjectSyncHelper
CreateLog(link.ObjectSpace, rule, sourceObject, SyncRuleLogStatus.Warning, 
    $"Target path '{rule.TargetPath}' resulted in a null object.");
```