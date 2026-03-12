# Reports V2 Module Implementation Plan

This document outlines the plan for integrating the **DevExpress Reports V2 Module** into the Visa2026 project. The primary goal is to enable users to create, design, and store reports at **runtime**. These reports will be saved directly to the application's database.

This guide intentionally omits Predefined Reports to focus on the user-driven reporting workflow.

## 1. Overview

The Reports V2 module provides a complete reporting solution for XAF applications. By implementing it, we will add a `Reports` section to the Blazor UI. From there, authorized users can:

1.  **Create new reports** using a web-based report designer.
2.  **Select any business object** (e.g., `Person`, `Visa`, `EmployeeContract`) as a data source.
3.  **Design the layout** by dragging and dropping fields.
4.  **Save the report definition** to the database.
5.  **View, print, or export** the generated reports at any time.

This approach is highly flexible and shifts the responsibility of report creation from developers to end-users.

## 2. Implementation Steps

Follow these steps to add the module to the Visa2026 application.

### Step 1: Verify NuGet Packages

Ensure the following NuGet packages are referenced in your project files with the correct versions. The compilation errors you encountered were due to the `ReportsV2.EFCore` package being missing from the module project.

1


### Step 4: Create and Apply a Database Migration

After updating the `DbContext`, you need to create a new database migration to add the necessary tables for storing reports.

1.  Open a terminal in the directory of the `Visa2026.Blazor.Server` project.
2.  Run the following command to create the migration:
    ```sh
    dotnet ef migrations add AddReportsV2Module -c Visa2026EFCoreDbContext
    ```
3.  Apply the migration to your development database:
    ```sh
    dotnet ef database update -c Visa2026EFCoreDbContext
    ```
    This will create the `ReportDataV2` table and related tables in your SQL database.

## 3. Suggested Usage for Visa2026

Once the module is implemented, a `Reports` navigation item will appear in the application. Here are some suggested reports that users can create at runtime:

*   **Expiring Documents Report**:
    *   **Data Source**: `Visa` or `Passport`.
    *   **Purpose**: To create a weekly or monthly list of all documents that are expiring soon.
    *   **Filter in Designer**: `[ExpirationState] = 'ExpiringSoon'` or `[DaysRemaining] < 90`.

*   **All Active Employees Report**:
    *   **Data Source**: `EmployeeContract`.
    *   **Purpose**: A simple directory of all current employees and their positions.
    *   **Filter in Designer**: `[IsActive] = True`.
    *   **Displayed Fields**: `Employee.FullName`, `Position.Name`, `ContractStartDate`.

*   **Visa Status Overview**:
    *   **Data Source**: `Visa`.
    *   **Purpose**: To get a high-level overview of all visa statuses. The report can be grouped by `VisaStatus`.
    *   **Displayed Fields**: `Employee.FullName`, `VisaType`, `VisaStatus`, `ExpirationDate`.

## 4. Seeding Reports from Embedded Resources (Development Workflow)

While creating reports at runtime is the primary goal for end-users, it is often necessary for developers to pre-load a set of default reports. This is especially useful in development environments where the database is frequently deleted and recreated.

This approach allows you to design a report at runtime, save its layout as a `.repx` file, and embed it into the application. The application will then automatically load this report into the database on startup if it doesn't already exist.

### Step 1: Design and Export the Report

1.  Run the `Visa2026.Blazor.Server` application.
2.  Navigate to the **Reports** section.
3.  Create and design your report using the web-based designer (e.g., a report for `EmployeeContract`).
4.  In the designer's menu, export the report layout. Save it as a file (e.g., `EmployeeContract.repx`).

### Step 2: Add the Report File to the Module Project

1.  In Visual Studio, create a new folder inside the `Visa2026.Module` project named `Reports`.
2.  Right-click the `Reports` folder and choose **Add > Existing Item...**.
3.  Select the `EmployeeContract.repx` file you just saved.

### Step 3: Configure the Embedded Resource

This is the most important step. You must set the file to be embedded within the module's assembly.

1.  In the Solution Explorer, select the `EmployeeContract.repx` file.
2.  In the **Properties** window, change the **Build Action** from `None` to **`Embedded Resource`**.

### Step 4: Update the `Updater.cs` to Load the Report

Add logic to your `DatabaseUpdate/Updater.cs` file to read the embedded resource and create the report in the database.

```csharp
// In Visa2026.Module/DatabaseUpdate/Updater.cs
using DevExpress.Persistent.BaseImpl.EFCore.ReportsV2;
using System.IO;
using System.Reflection;
// ... other usings

public class Updater : ModuleUpdater
{
    // ... constructor and other methods

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        // ... existing user/role creation logic
        CreateReports();
        ObjectSpace.CommitChanges();
        // ... rest of the method
    }

    private void CreateReports()
    {
        // This line loads the embedded report.
        // The resource name is: [Default Namespace].[Folder Name].[File Name]
        CreateReport("Employee Contract", "Visa2026.Module.Reports.EmployeeContract.repx", typeof(BusinessObjects.EmployeeContract));
    }

    private void CreateReport(string reportName, string resourceName, Type dataType)
    {
        ReportDataV2 reportData = ObjectSpace.FirstOrDefault<ReportDataV2>(r => r.DisplayName == reportName);
        if (reportData == null)
        {
            reportData = ObjectSpace.CreateObject<ReportDataV2>();
            reportData.DisplayName = reportName;
            reportData.IsInplaceReport = true; // Make it available in the "Show in Report" action
            reportData.DataType = dataType;

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string repxContent = reader.ReadToEnd();
                    reportData.Content = System.Text.Encoding.UTF8.GetBytes(repxContent);
                }
            }
        }
    }
    
    // ... other methods like CreateAdminRole, CreateDefaultRole
}
```

By following this pattern, your default reports will be automatically seeded every time the database is updated, streamlining the development and testing process.
    *   **Purpose**: To get a high-level overview of all visa statuses. The report can be grouped by `VisaStatus`.
    *   **Displayed Fields**: `Employee.FullName`, `VisaType`, `VisaStatus`, `ExpirationDate`.