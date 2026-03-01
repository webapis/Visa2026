# Business Object: SyncRule

## 1. Overview

The `SyncRule` business object is the configuration interface for the **Dynamic Synchronization Engine**. It allows administrators to define automated "If This Then That" logic to update related records in the database without writing C# code.

When a business object (Source) is saved, deleted, or updated, the system checks these rules. If the conditions match, it automatically updates specific properties on related business objects (Target).

## 2. User Guide

To create a new rule, navigate to **System > Sync Rules** in the application navigation menu.

### 2.1. Property Reference

The following table explains each property, the expected format, and provides example values.

| Property | Description | Format / Notes | Example Value |
| :--- | :--- | :--- | :--- |
| **Name** | A descriptive name for the rule to identify it in lists. | Free text. | `Update Application when Visa Issued` |
| **Source Type** | The type of Business Object that triggers this rule. | Dropdown selection. | `Visa` |
| **Source Property** | (Optional) If selected, the rule only runs if this specific property on the Source object is involved. | Dropdown (populated based on Source Type). | `IsCancelled` |
| **Source Value** | (Optional) If `Source Property` is selected, the rule only runs if the property's value matches this string. | String representation of the value. | `true` |
| **Trigger Type** | The event that initiates the rule. | Dropdown (`Save`, `Delete`, `Update`). | `Save` |
| **Source Criteria** | (Optional) An expression to filter which Source objects trigger the rule. | Criteria Language Syntax. | `[VisaType.Name] = 'Work'` |
| **Target Path** | The navigation path from the Source object to the Target object(s). | Dot-notation path. | `Application.ApplicationItems` |
| **Target Match Criteria** | (Optional) If `Target Path` points to a collection, this criteria finds the specific item(s) to update. Supports `@Source.` parameters. | Criteria Syntax. Use `@Source.PropName` to reference the triggering object. | `[Person.Oid] = '@Source.Passport.Person.Oid'` |
| **Target Type** | The type of the Business Object that will be updated. | Dropdown selection. | `ApplicationItem` |
| **Target Property** | The specific property on the Target object to update. | Dropdown (populated based on Target Type). | `VisaIssued` |
| **Target Value** | The value to write to the Target Property. | String representation. | `true` |
| **Is Active** | Master switch to enable or disable the rule. | Checkbox. | `Checked` |

## 3. Detailed Logic

### 3.1. Triggering Logic
*   **Trigger Type**:
    *   `Save`: Runs when a new object is created or an existing one is saved.
    *   `Update`: Runs specifically when an existing object is modified.
    *   `Delete`: Runs when an object is being deleted (useful for reverting flags).
*   **Source Property & Value**: These work together. If you select `IsCancelled` as the property and `true` as the value, the rule **only** executes if `IsCancelled` is actually `true` on the object being saved.

### 3.2. Target Resolution
*   **Target Path**: This tells the system how to "walk" from the Source to the Target.
    *   *Example*: From a `Visa`, you can reach the `Application` via `Application`, and then its items via `ApplicationItems`. Path: `Application.ApplicationItems`.
*   **Target Match Criteria**: Since `ApplicationItems` is a list, you need to pick the right one.
    *   *Syntax*: `[TargetProp] = '@Source.SourceProp'`
    *   *Example*: `[Person.Oid] = '@Source.Passport.Person.Oid'` means "Find the ApplicationItem where the Person is the same as the Person on the Visa's Passport."

### 3.3. Value Conversion
*   **Target Value**: Always entered as a string. The system automatically converts it to the correct type for the `Target Property`.
    *   **Booleans**: Use `true` or `false`.
    *   **Numbers**: Use `123`, `10.5`.
    *   **Dates**: Use standard date formats (though static dates are rarely used in rules).
    *   **Strings**: Just text.

## 4. Configuration Examples

### Example A: Mark Application Item as "Visa Issued"
*Scenario: When a Visa is saved, find the corresponding person in the Application and check the "Visa Issued" box.*

*   **Name**: `Set Visa Issued Flag`
*   **Source Type**: `Visa`
*   **Trigger Type**: `Save`
*   **Target Path**: `Application.ApplicationItems`
*   **Target Match Criteria**: `[Person.Oid] = '@Source.Passport.Person.Oid'`
*   **Target Type**: `ApplicationItem`
*   **Target Property**: `VisaIssued`
*   **Target Value**: `true`

### Example B: Sync Cancellation Status
*Scenario: If a Visa is cancelled, mark the Application Item as "Visa Cancelled".*

*   **Name**: `Sync Visa Cancellation`
*   **Source Type**: `Visa`
*   **Source Property**: `IsCancelled`
*   **Source Value**: `true`
*   **Trigger Type**: `Save`
*   **Target Path**: `Application.ApplicationItems`
*   **Target Match Criteria**: `[Person.Oid] = '@Source.Passport.Person.Oid'`
*   **Target Type**: `ApplicationItem`
*   **Target Property**: `VisaIsCancelled`
*   **Target Value**: `true`

### Example C: Reset Status on Delete
*Scenario: When a Visa is deleted, uncheck the "Visa Issued" box.*

*   **Name**: `Revert Visa Issued on Delete`
*   **Source Type**: `Visa`
*   **Trigger Type**: `Delete`
*   **Target Path**: `Application.ApplicationItems`
*   **Target Match Criteria**: `[Person.Oid] = '@Source.Passport.Person.Oid'`
*   **Target Type**: `ApplicationItem`
*   **Target Property**: `VisaIssued`
*   **Target Value**: `false`

### Example D: Update Sibling Objects (Advanced)
*Scenario: When a specific Visa is marked as "Primary", ensure all other Visas in the same Passport are un-marked.*

*   **Source Type**: `Visa`
*   **Source Property**: `IsPrimary` (Hypothetical property)
*   **Source Value**: `true`
*   **Target Path**: `Passport.Visas`
*   **Target Match Criteria**: `[ID] != '@Source.ID'`
    *   *Note*: You **must** exclude the source object using its ID to prevent an infinite update loop.
*   **Target Property**: `IsPrimary`
*   **Target Value**: `false`

### Example E: Mark Application Item when Invitation Item is Created
*Scenario: When a new person is added to an invitation (`InvitationItem`), find the corresponding person in the main application (`ApplicationItem`) and check the "InvitationItemIsIssued" box.*

*   **Name**: `Set Invitation Issued Flag on Application Item`
*   **Source Type**: `InvitationItem`
*   **Trigger Type**: `Save`
*   **Target Path**: `Invitation.Application.ApplicationItems`
*   **Target Match Criteria**: `[Person.ID] = '@Source.Person.ID'`
*   **Target Type**: `ApplicationItem`
*   **Target Property**: `InvitationItemIsIssued`
*   **Target Value**: `true`

## 5. Troubleshooting

If a rule is not working:
1.  Check the **Sync Rule Logs** tab on the rule detail view.
2.  **Warning: Target path resulted in null**: This means the link between objects is missing (e.g., The Visa doesn't have an Application selected).
3.  **Warning: Target property not found**: Check if the `Target Property` name is spelled correctly (though the dropdown helps prevent this).
4.  **Skipped**: The `Source Criteria` or `Source Value` conditions were not met.

## 6. Technical Implementation

*   **Class**: `Visa2026.Module.BusinessObjects.SyncRule`
*   **Helper**: `Visa2026.Module.BusinessObjects.CrossObjectSyncHelper`
*   **Execution**: Rules are cached and executed via the `CrossObjectSyncHelper.SyncOnSave` and `SyncOnDelete` methods called from the Business Objects.