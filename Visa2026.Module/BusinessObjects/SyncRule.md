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
| **Target Match Criteria** | If `Target Path` points to a collection, this criteria is **required** to find the specific item(s) to update. Supports `@Source.` parameters. | Criteria Syntax. Use `@Source.PropName` to reference the triggering object. | `[Person.ID] = '@Source.Person.ID'` |
| **Target Type** | The type of the Business Object that will be updated. | Dropdown selection. | `ApplicationItem` |
| **Target Property** | The specific property on the Target object to update. Supports nested properties using dot-notation. | Dropdown (populated based on Target Type). | `VisaIssued`<br>`CurrentVisa.IsExtended` |
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
    *   **Object References**: Use `@Source` to assign the triggering object itself to the target property.
    *   **Nulls**: Use `@Null` to clear a property value.

## 4. Criteria Language Reference

This section details the syntax used for **Source Criteria** and **Target Match Criteria**.

### 4.1. Purpose
The Criteria Language allows administrators to define complex filtering logic directly in the database without modifying the source code. It acts as the "WHERE" clause in the synchronization engine.

### 4.2. Why use it?
*   **Flexibility**: Business rules often change (e.g., "Now we need to sync 'Business' visas too"). Criteria allow these changes at runtime.
*   **Precision**: It ensures rules only fire when absolutely necessary, preventing performance issues and unwanted data updates.

### 4.3. When to use it?
1.  **Source Criteria**: Use this when the rule applies to a **subset** of source objects (e.g., *only* Visas where `VisaType` is 'Work').
2.  **Target Match Criteria**: Use this **always** when the `TargetPath` points to a collection. You must define *how* to find the correct specific item in that list (e.g., matching by Person ID).

### 4.4. How to write it?
The syntax is similar to SQL or C# expressions.

#### Common Operators
| Operator | Description | Example |
| :--- | :--- | :--- |
| `=` or `==` | Equality | `[Status] = 'Active'` |
| `!=` | Inequality | `[Status] != 'Closed'` |
| `>`, `<`, `>=` | Comparison | `[Age] > 18` |
| `In` | List inclusion | `[Type] In ('A', 'B', 'C')` |
| `Like` | String matching | `[Name] Like 'John%'` |
| `Is Null` | Null check | `[Project] Is Null` |

#### Context Parameters (@Source)
In **Target Match Criteria**, you can reference values from the object that triggered the rule using the `@Source` prefix.
*   **Syntax**: `'@Source.PropertyName'`
*   **Example**: `[Person.Oid] = '@Source.Passport.Person.Oid'`
*   **Explanation**: This compares the `Person.Oid` of the target candidate against the `Passport.Person.Oid` of the source object.

## 5. Configuration Examples
This section provides examples for common synchronization patterns. The key is to correctly define the `TargetPath` to navigate from the Source object to the Target object.

### 5.1. Pattern: Updating "Cousin" Objects

This is the most common pattern. It is used when an item in one collection (e.g., an `InvitationItem` within `Application.Invitations`) needs to update a related item in a different collection under the same top-level parent (e.g., an `ApplicationItem` within `Application.ApplicationItems`).

**Key Configuration:**
*   **Target Path**: Navigates *up* to the common parent and then *down* to the target collection. Example: `Invitation.Application.ApplicationItems`.
*   **Target Match Criteria**: **Required**. Used to find the specific item in the target collection. Example: `[Person.ID] = '@Source.Person.ID'`.

#### Example: Mark Application Item when Invitation Item is Created
*Scenario: When a new person is added to an invitation (`InvitationItem`), find the corresponding person in the main application (`ApplicationItem`) and check the "InvitationItemIsIssued" box.*

*   **Name**: `Set Invitation Issued Flag on Application Item`
*   **Source Type**: `InvitationItem`
*   **Trigger Type**: `Save`
*   **Source Criteria**: `[Invitation.Application.ApplicationType.Code] In ('get_invitation', 'get_invitation_wp', 'change_invitation')`
*   **Target Path**: `Invitation.Application.ApplicationItems`
*   **Target Match Criteria**: `[Person.ID] = '@Source.Person.ID'`
*   **Target Type**: `ApplicationItem`
*   **Target Property**: `InvitationItemIsIssued`
*   **Target Value**: `true`

#### Example: Mark Application Item as Rejected
*Scenario: When a new Rejection Item is created, find the corresponding person in the main application (`ApplicationItem`) and check the "RejectionIssued" box.*

*   **Name**: `Set Rejection Issued Flag`
*   **Source Type**: `RejectionItem`
*   **Trigger Type**: `Save`
*   **Target Path**: `Rejection.Application.ApplicationItems`
*   **Target Match Criteria**: `[Person.ID] = '@Source.Person.ID'`
*   **Target Type**: `ApplicationItem`
*   **Target Property**: `RejectionIssued`
*   **Target Value**: `true`

#### Example: Update a Nested Property on a "Cousin" Object
*Scenario: When a new Visa is created for an extension, find the corresponding `ApplicationItem` and mark its `CurrentVisa` as extended.*

*   **Name**: `Set Previous Visa as Extended`
*   **Source Type**: `Visa`
*   **Trigger Type**: `Save`
*   **Source Criteria**: `[Application.ApplicationType.Code] In ('visa_extension', 'extend_visa_wp')`
*   **Target Path**: `Application.ApplicationItems`
*   **Target Match Criteria**: `[Person.ID] = '@Source.Passport.Person.ID'`
*   **Target Type**: `ApplicationItem`
*   **Target Property**: `CurrentVisa.IsExtended`
*   **Target Value**: `true`

#### Example: Mark Application Item as Invitation Cancelled
*Scenario: When an `InvitationItem` is cancelled, find the corresponding `ApplicationItem` (via Person) and check the "InvitationItemIsCancelled" box.*

*   **Name**: `Set Invitation Cancelled Flag on Application Item`
*   **Source Type**: `InvitationItem`
*   **Source Property**: `IsCancelled`
*   **Source Value**: `true`
*   **Trigger Type**: `Save`
*   **Target Path**: `Person.ApplicationItems`
*   **Target Match Criteria**: `[CurrentInvitationItem.ID] = '@Source.ID' And [Application.ApplicationType.Code] In ('cancel_invitation', 'cancel_invitation_wp')`
*   **Target Type**: `ApplicationItem`
*   **Target Property**: `InvitationItemIsCancelled`
*   **Target Value**: `true`

### 5.2. Pattern: Updating Sibling Objects

This pattern is used when an action on one item in a collection needs to affect other items **in the same collection**.

**Key Configuration:**
*   **Target Path**: Navigates *up* to the parent and then *down* to the same collection. Example: `Passport.Visas`.
*   **Target Match Criteria**: **Required**. Must exclude the source object itself to prevent infinite loops. Example: `[ID] != '@Source.ID'`.

#### Example: Un-check "IsPrimary" on Sibling Visas
*Scenario: When a specific Visa is marked as "Primary", ensure all other Visas in the same Passport are un-marked.*

*   **Source Type**: `Visa`
*   **Source Property**: `IsPrimary` (Hypothetical property)
*   **Source Value**: `true`
*   **Trigger Type**: `Save`
*   **Target Path**: `Passport.Visas`
*   **Target Match Criteria**: `[ID] != '@Source.ID'`
*   **Target Property**: `IsPrimary`
*   **Target Value**: `false`

#### Example: Deactivate Sibling Visas
*Scenario: When a Visa is activated, ensure all other Visas on the same Passport are deactivated.*

*   **Name**: `Deactivate Sibling Visas`
*   **Source Type**: `Visa`
*   **Source Property**: `IsActive`
*   **Source Value**: `true`
*   **Trigger Type**: `Save`
*   **Target Path**: `Passport.Visas`
*   **Target Match Criteria**: `[ID] != '@Source.ID'`
*   **Target Type**: `Visa`
*   **Target Property**: `IsActive`
*   **Target Value**: `false`

#### Example: Mark Sibling Visas as Changed
*Scenario: When a Visa is activated, mark all other Visas on the same Passport as changed.*

*   **Name**: `Mark Sibling Visas as Changed`
*   **Source Type**: `Visa`
*   **Source Property**: `IsActive`
*   **Source Value**: `true`
*   **Trigger Type**: `Save`
*   **Target Path**: `Passport.Visas`
*   **Target Match Criteria**: `[ID] != '@Source.ID'`
*   **Target Type**: `Visa`
*   **Target Property**: `IsChanged`
*   **Target Value**: `true`

### 5.3. Pattern: Updating a Parent Object

This pattern is used when a child object needs to update a property on its direct parent.

**Key Configuration:**
*   **Target Path**: The name of the navigation property that points to the parent. Example: `Application`.
*   **Target Match Criteria**: Should be empty, as the target is a single object.

#### Example: Update Application Status from a Progress Item
*Scenario: When a new `ApplicationProgress` item is created with the state "In Progress", update a status flag on the parent `Application`.*

*   **Name**: `Set Application to 'In Progress'`
*   **Source Type**: `ApplicationProgress`
*   **Trigger Type**: `Save`
*   **Source Criteria**: `[State.Code] = 'IN_PROGRESS'`
*   **Target Path**: `Application`
*   **Target Match Criteria**: (empty)
*   **Target Type**: `Application`
*   **Target Property**: `IsInProgress` (Hypothetical property)
*   **Target Value**: `true`

### 5.4. Pattern: Updating a "Grandparent" or Top-Level Parent

This pattern is used when a deeply nested child needs to update a property on an object higher up in the hierarchy.

**Key Configuration:**
*   **Target Path**: A dot-separated path navigating up through multiple parent properties. Example: `Rejection.Application.Company`.
*   **Target Match Criteria**: Should be empty.

#### Example: Flag Company for Rejection
*Scenario: When a `RejectionItem` is created, navigate up to the `Application` and then to the `Company` to set a flag indicating recent rejection activity.*

*   **Name**: `Flag Company on Rejection`
*   **Source Type**: `RejectionItem`
*   **Trigger Type**: `Save`
*   **Source Criteria**: (empty)
*   **Target Path**: `Rejection.Application.Company`
*   **Target Match Criteria**: (empty)
*   **Target Type**: `Company`
*   **Target Property**: `HasRecentRejection` (Hypothetical property)
*   **Target Value**: `true`

### 5.5. Pattern: Setting Object References

This pattern is used to link the Source object to a property on the Target object.

#### Example: Update Passport's Current Visa
*Scenario: When a Visa is activated, update the `CurrentVisa` property on the parent Passport to point to this Visa.*

*   **Source Type**: `Visa`
*   **Source Property**: `IsActive`
*   **Source Value**: `true`
*   **Trigger Type**: `Save`
*   **Target Path**: `Passport`
*   **Target Property**: `CurrentVisa`
*   **Target Value**: `@Source`

## 6. Troubleshooting

If a rule is not working:
1.  Check the **Sync Rule Logs** tab on the rule detail view.
2.  **Warning: Target path resulted in null**: This means the link between objects is missing (e.g., The Visa doesn't have an Application selected).
3.  **Warning: Target property not found**: Check if the `Target Property` name is spelled correctly (though the dropdown helps prevent this).
4.  **Skipped**: The `Source Criteria` or `Source Value` conditions were not met.

## 7. Technical Implementation

*   **Class**: `Visa2026.Module.BusinessObjects.SyncRule`
*   **Helper**: `Visa2026.Module.BusinessObjects.CrossObjectSyncHelper`
*   **Execution**: Rules are cached and executed via the `CrossObjectSyncHelper.SyncOnSave` and `SyncOnDelete` methods called from the Business Objects.