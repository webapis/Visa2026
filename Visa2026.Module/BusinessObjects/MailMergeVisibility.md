# Mail Merge Visibility Dynamic Configuration Implementation

This document outlines the implementation of a dynamic configuration for Mail Merge template visibility within the Visa2026 application. This system allows administrators to configure template visibility based on criteria evaluated at runtime, providing a flexible way to control which templates are available to users for specific records.

## 1. Overview

The `MailMergeVisibility` business object serves as a configuration tool that, based on certain criteria, determines mail merge template visibility at runtime.

As an administrator or developer, you use the Mail Merge Visibility to:
*   **Dynamically Show/Hide Templates**: Control template availability based on business object state.
*   **Contextualize Output**: Prevent users from generating irrelevant documents (e.g., generating a "Visa Grant" letter for a "Rejected" application).

## 2. Components Implemented

The system consists of the following components:

### 2.1. `MailMergeVisibility` Business Object

*   **Purpose**: Stores the configuration for mail merge visibility rules.
*   **File**: `Visa2026.Module\BusinessObjects\MailMergeVisibility.cs`
*   **Properties**:
    *   `TemplateName (String)`: The name of the mail merge template.
        *   **Details**: This property must match the `Name` of the `RichTextMailMergeData` object (or the caption of the action item) exactly.
    *   `TargetTypeFullName (String)`: Stores the full name of the target business object type (e.g., "Visa2026.Module.BusinessObjects.Application").
    *   `TargetType (Type)`: A non-persistent helper property that allows selecting the target business object type in the UI.
    *   `VisibilityCriteria (String)`: A criteria expression defining the conditions under which the template is visible.
        *   **Example**: `[Status] = 'Approved'`
    *   `AvailableTargetTypes (IList<Type>)`: Helper collection to populate the Target Type dropdown.

### 2.2. `IMailMergeVisibilityCacheService` and `MailMergeVisibilityCacheService`

*   **Purpose**: Provides caching for `MailMergeVisibility` records to prevent frequent database queries during UI refreshing.
*   **Files**:
    *   `Visa2026.Module\Module Interface\IMailMergeVisibilityCacheService.cs`
    *   `Visa2026.Module\Services\MailMergeVisibilityCacheService.cs`
    *   **Functionality**:
        *   Caches rules by `TemplateName` and `TargetType`.
        *   Ensures thread-safe access using `ConcurrentDictionary`.

### 2.3. `ShowMailMergeController`

*   **Purpose**: Intercepts the standard Mail Merge action and filters items.
*   **File**: `Visa2026.Module\Controllers\ShowMailMergeController.cs`
*   **Functionality**:
    *   Hooks into the `RichTextMailMergeController`.
    *   Iterates through available templates in the "Show In Mail Merge" action.
    *   Evaluates the `VisibilityCriteria` against the current object.
    *   Hides templates that do not match the criteria.

## 3. Configuration

### 3.1. Creating `MailMergeVisibility` Records

1.  Navigate to the **System** navigation group.
2.  Open the **Mail Merge Visibility** List View.
3.  Click **New**.
4.  **Template Name**: Enter the exact name of the Mail Merge template (e.g., "Contract Template").
5.  **Target Type**: Select the business object the template applies to (e.g., `Application`).
6.  **Visibility Criteria**: Use the popup editor to define the logic.

### 3.2. Example Configuration

To make the "Visa Grant Letter" template visible *only* when the Application status is 'Approved':

*   **Template Name**: Visa Grant Letter
*   **Target Type**: Application
*   **Visibility Criteria**: `[CurrentState.State] = 'Approved'`

To make a "Rejection Notice" visible *only* when the Application status is 'Rejected':

*   **Template Name**: Rejection Notice
*   **Target Type**: Application
*   **Visibility Criteria**: `[CurrentState.State] = 'Rejected'`

## 4. Important Considerations

*   **Caching**: The service caches rules for performance. If you manually update the database (SQL), you may need to restart the application or implement a cache clearing mechanism to see changes immediately.
*   **Template Naming**: If a user renames a Mail Merge template in the application, the corresponding `MailMergeVisibility` rule must be updated to match the new name, or the rule will no longer apply.
*   **Performance**: Avoid extremely complex `VisibilityCriteria` as these are evaluated client-side whenever the detail view is refreshed or the object changes.

## 5. Benefits

*   **Cleaner UI**: Users see only the document templates relevant to the specific stage of the workflow.
*   **Error Prevention**: Reduces the risk of users generating incorrect documents for the wrong status.
*   **No Code Changes**: Logic can be adjusted by admins without redeploying the application.

## 6. Code Reference

*   **Business Object**: `Visa2026.Module.BusinessObjects.MailMergeVisibility`
*   **Controller**: `Visa2026.Module.Controllers.ShowMailMergeController`
*   **Service**: `Visa2026.Module.Services.MailMergeVisibilityCacheService`