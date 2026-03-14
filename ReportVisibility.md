# Report Visibility Dynamic Configuration Implementation

This document outlines the implementation of a dynamic configuration for report visibility within the Visa2026 application. This system allows administrators to configure report visibility based on criteria evaluated at runtime, providing a flexible and user-friendly way to control access to reports.

## 1. Overview

The `ReportVisibility` business object serves as a configuration tool that, based on certain criteria, determines report visibility at runtime.

As an administrator or developer, you use the Report Visibility to:
*   **Dynamically Show/Hide Reports**: Control report visibility based on conditions.
*   **Tailor User Experience**: Show only relevant reports to users based on their context.

The goal is to enable administrators to define rules that determine whether a report should be visible to a user based on certain conditions. These rules are stored in the database and evaluated dynamically, ensuring that report visibility can be adjusted without requiring code changes.

## 2. Components Implemented

The following components are implemented to achieve this dynamic configuration:

### 2.1. `ReportVisibility` Business Object

*   **Purpose**: Stores the configuration for report visibility rules.
*   **File**: `Visa2026.Module\BusinessObjects\ReportVisibility.cs`
*   **Properties**:
    *   `ReportName (String)`: The unique name of the report (e.g., "ApplicationVisaExtEmp"). This should match the name used when registering the report.
    *   `ReportDisplayName (String)`: The display name of the report, shown in the UI.
    *   `TargetTypeFullName (String)`: Stores the full name of the target business object type (e.g., "Visa2026.Module.BusinessObjects.Application").
    *   `TargetType (Type)`: A non-persistent property that allows selecting the target business object type in the UI. It uses `TargetTypeFullName` to store the selected type.
    *   `VisibilityCriteria (String)`: A criteria expression that defines the conditions under which the report should be visible. This expression is evaluated against the target business object.
    *   `AvailableTargetTypes (IList<Type>)`: A non-persistent property that provides a list of available business object types for selection in the UI.

### 2.2. `IReportVisibilityCacheService` and `ReportVisibilityCacheService`

*   **Purpose**: Provides caching for `ReportVisibility` records to improve performance.
*   **Files**:
    *   `ReportVisibilityCacheService.cs` (interface and implementation)
*   The interface `IReportVisibilityCacheService` is located in `Visa2026.Module\Module Interface`. The `ReportVisibilityCacheService` implementation is located in `Visa2026.Module\Services`.
    *   Caches `ReportVisibility` records based on `ReportName` and `TargetType`.
    *   Provides methods to retrieve, invalidate, and clear the cache.
    *   Uses a `ConcurrentDictionary` for thread-safe caching.

### 2.3. `ShowReportController`

*   **Purpose**: Controls the visibility of reports in the UI based on the configured rules.
*   **File**: `Visa2026.Module\Controllers\ShowReportController.cs`
*   **Functionality**:
    *   Retrieves `ReportVisibility` records from the cache.
    *   Evaluates the `VisibilityCriteria` against the current business object.
    *   Dynamically adds or removes report actions based on the evaluation result.


### 2.4. `Updater.cs` (Database Update)

*   **Purpose**: Seeds the database with initial `ReportVisibility` records.
*   **File**: `Visa2026.Module\DatabaseUpdate\Updater.cs`
*   **Functionality**:
    *   Creates default `ReportVisibility` records during database updates.
    *   Ensures that the necessary report visibility rules are available when the application starts.



## 3. Configuration

### 3.1. Creating `ReportVisibility` Records

1.  Navigate to the "System" navigation item.
2.  Open the `ReportVisibility` List View.
3.  Create a new `ReportVisibility` record.
4.  Set the `ReportName` to the unique name of the report (as defined in `ReportsUpdater.cs`).
5.  Set the `ReportDisplayName` to the user-friendly name of the report.
6.  Select the `TargetType` to which the rule applies.
7.  Define the `VisibilityCriteria` using the Criteria Language Syntax. This expression will be evaluated against objects of the selected `TargetType`.

### 3.2. Example Configuration
To show the "Application For Employee's Visa Extension Report" only when the `Application.ApplicationType.Name` is "Wiza we Iş Rugsatnamasyny Uzaltmak (IŞG)", configure the `ReportVisibility` record as follows:

*   `ReportName`: ApplicationVisaExtEmp
*   `ReportDisplayName`: Application For Employee's Visa Extension Report
*   `VisibilityCriteria`: `[ApplicationType.Name] = 'Wiza we Iş Rugsatnamasyny Uzaltmak (IŞG)'`*   `EnableReportVisibility`: True


## 4. Important Considerations
*   **Caching**: The `ReportVisibilityCacheService` caches the `ReportVisibility` records to minimize database access. Ensure that the cache is invalidated whenever `ReportVisibility` records are created, updated, or deleted.

    To invalidate the cache you can use:
```csharp
    _reportVisibilityCacheService.ClearCache();
```
*   **Security**: Implement appropriate security measures to restrict access to the `ReportVisibility` List View. Only authorized users should be able to create, modify, or delete report visibility rules.
*   **Performance**: Optimize the `VisibilityCriteria` to ensure efficient evaluation. Avoid complex expressions that could impact performance.
*   **Naming Conventions**: Use consistent naming conventions for reports and properties to avoid errors and improve maintainability. The `ReportName` in the `ReportVisibility` object must exactly match the name used when the report is registered in code.
*   **Testing**: Thoroughly test all report visibility rules to ensure that they function as expected.

    To test report visibility rules, simulate different user scenarios and verify that the reports are displayed or hidden according to the configured criteria.
```csharp
// Example of how ShowReportController might use the cache service
var reportVisibility = _reportVisibilityCacheService.GetReportVisibility(reportName, targetType);
```
*   **CriteriaEditor**: Use of the `CriteriaOptionsAttribute` and the `EditorAlias(EditorAliases.PopupCriteriaPropertyEditor)` on the `VisibilityCriteria` property provides a user-friendly interface for building the criteria.


## 5. Benefits

*   **Flexibility**: Allows administrators to easily adjust report visibility without code changes.
*   **User Experience**: Provides a more tailored user experience by showing only relevant reports.
*   **Maintainability**: Simplifies the management of report visibility rules.
*   **Security**: Enhances security by restricting access to sensitive reports.

## 6. Future Enhancements

*   Implement a more sophisticated caching mechanism with sliding expiration.
*   Add support for user-specific report visibility rules.
*   Provide a UI for testing the visibility criteria.
*   Implement logging of report access events.

### 7. Code Reference

*   **Definition**: `Visa2026.Module.BusinessObjects.ReportVisibility`
*   **Controller**: `Visa2026.Module.Controllers.ShowReportController`
