# Mail Merge Templates: Design and Usage

This document provides a comprehensive guide to the design, implementation, and usage of Mail Merge templates within the Visa2026 application. It covers how templates are stored, how their visibility is controlled, and how developers and administrators can manage them.

## 1. Overview

Mail Merge templates in Visa2026 allow users to generate customized documents (e.g., letters, notices, forms) based on data from business objects like `Application`. The system leverages DevExpress XAF's Office Module for rich text editing and Mail Merge functionality, enhanced with custom visibility rules.

Key aspects:
*   **Document Storage**: Templates are stored in the database as `RichTextMailMergeData` objects, containing the actual `.docx` or `.rtf` file content.
*   **Dynamic Visibility**: A custom `MailMergeVisibility` system controls which templates are shown to users based on the state of the current business object (e.g., an "Approved" application will only show "Visa Grant Letter").
*   **Developer Seeding**: Default templates are embedded as resources and automatically loaded into the database during application updates.
*   **Runtime Management**: Administrators can create, edit, and configure template visibility directly within the application's UI.

## 2. Core Components

### 2.1. `RichTextMailMergeData` Business Object

*   **Purpose**: This is the standard DevExpress business object that stores the actual Mail Merge document.
*   **Key Properties**:
    *   `Name (String)`: The unique name of the template, used for identification and linking with `MailMergeVisibility` rules.
    *   `DataType (Type)`: Specifies the business object type (e.g., `Application`) that the template is designed for. This determines the available fields in the Mail Merge designer.
    *   `Template (byte[])`: Stores the binary content of the `.docx` or `.rtf` file.
*   **Location**: `DevExpress.Persistent.BaseImpl.EF.RichTextMailMergeData`

### 2.2. `MailMergeVisibility` Business Object

*   **Purpose**: A custom business object (`Visa2026.Module.BusinessObjects.MailMergeVisibility`) that defines rules for when a specific Mail Merge template should be visible in the UI.
*   **Key Properties**:
    *   `TemplateName (String)`: Must exactly match the `Name` of a `RichTextMailMergeData` object.
    *   `TargetTypeFullName (String)`: The full type name of the business object (e.g., `Visa2026.Module.BusinessObjects.Application`) to which this rule applies.
    *   `VisibilityCriteria (String)`: A DevExpress Criteria Language expression (e.g., `[CurrentState.State] = 'Approved'`) that determines if the template should be visible for a given object. An empty or `null` criteria means it's always visible for the `TargetType`.
*   **Location**: `Visa2026.Module\BusinessObjects\MailMergeVisibility.cs`

### 2.3. `MailMergeUpdater.cs`

*   **Purpose**: This `ModuleUpdater` is responsible for seeding and updating default `RichTextMailMergeData` and `MailMergeVisibility` records in the database.
*   **Functionality**:
    *   `EnsureTemplateExists`: Creates or updates `RichTextMailMergeData` records from embedded `.docx` files. During `DEBUG` builds, it always overwrites the template content to ensure developers see the latest changes. In `RELEASE` builds, it only loads if the template is missing, preserving user edits.
    *   `CreateMailMergeVisibility`: Creates or updates `MailMergeVisibility` rules, linking templates to target business objects and defining their display criteria.
*   **Location**: `Visa2026.Module\DatabaseUpdate\MailMergeUpdater.cs`

### 2.4. `ShowMailMergeController`

*   **Purpose**: This `ViewController` intercepts the standard DevExpress "Show in Mail Merge" action and applies the custom `MailMergeVisibility` rules.
*   **Functionality**: When the "Show in Mail Merge" action is executed, this controller filters the list of available templates based on the `VisibilityCriteria` of the `MailMergeVisibility` records and the properties of the currently selected business object.
*   **Location**: `Visa2026.Module\Controllers\ShowMailMergeController.cs` (not provided in context, but implied by `MailMergeVisibility.md`)

## 3. Usage and Management

### 3.1. For Developers: Adding/Updating Templates

1.  **Create/Edit `.docx` File**: Design your Mail Merge document using Microsoft Word or a similar editor. Save it as a `.docx` file.
2.  **Add to Resources**: Place the `.docx` file in the `Visa2026.Module\Resources` folder.
3.  **Set Build Action**: In Visual Studio, select the `.docx` file, go to its Properties, and set **Build Action** to **`Embedded Resource`**.
4.  **Update `MailMergeUpdater.cs`**:
    *   Call `EnsureTemplateExists` to register the `RichTextMailMergeData` object, providing its `name`, `dataType`, and the `resourceName` (e.g., `Visa2026.Module.Resources.MyTemplate.docx`).
    *   Call `CreateMailMergeVisibility` to define when and where this template should appear in the UI, specifying its `templateName`, `targetType`, and `criteria`.
5.  **Rebuild and Run**: The updater will automatically load the template and its visibility rule into the database.

### 3.2. For Administrators: Managing Templates in UI

Administrators can manage templates directly within the running application:

#### 3.2.1. Creating a New Template

1.  Navigate to **System > Rich Text Mail Merge Data**.
2.  Click **New**.
3.  Enter a unique **Name** (e.g., "New Employee Welcome Letter").
4.  Select the **Data Type** (e.g., `Person` or `EmployeeContract`) that this template will merge data from.
5.  Upload your `.docx` or `.rtf` file into the **Template** property.
6.  Save.

#### 3.2.2. Configuring Template Visibility

After creating a `RichTextMailMergeData` record, you need to define its visibility:

1.  Navigate to **System > Mail Merge Visibility**.
2.  Click **New**.
3.  **Template Name**: Enter the *exact* name of the template you created in the previous step (e.g., "New Employee Welcome Letter").
4.  **Target Type**: Select the business object type (e.g., `Person`) for which this template should be available.
5.  **Visibility Criteria**: Enter a criteria expression (e.g., `[IsEmployee] = True`) to control when the template appears. Leave blank for always visible.
6.  Save.

#### 3.2.3. Editing an Existing Template

1.  Navigate to **System > Rich Text Mail Merge Data**.
2.  Open the desired template.
3.  Click the "Edit Template" action (pencil icon) to open the DevExpress Rich Text Editor.
4.  Make your changes, including inserting Mail Merge fields (see section 3.3).
5.  Save the changes within the editor.

### 3.3. Adding Fields to the Word Document Template

To populate your Word document with data from your business objects:

1.  **Open the Template in the Application's Editor**: The easiest way is to navigate to **System > Rich Text Mail Merge Data**, open the template, and click the "Edit Template" action.
2.  **Use the Mail Merge Field List**: The DevExpress Rich Text Editor will provide a "Mail Merge" tab or panel. This panel automatically lists all properties of the `DataType` you specified for the `RichTextMailMergeData` object, including properties of related objects (e.g., `Person.FirstName`, `CurrentPassport.PassportNumber`).
3.  **Insert Fields**: Drag and drop fields from the list directly into your document. The fields will appear as placeholders (e.g., `<<Person.FirstName>>`).
4.  **Save Changes**: Save the document within the editor.

### 3.4. Navigating Related Objects (Navigation Properties)

When a template is bound to a `DataType` like `ApplicationItem`, you can access data from related objects using dot notation.

*   **To access Person data**: Use `<<Person.PropertyName>>` (e.g., `<<Person.FullName>>`).
*   **To access nested data**: You can go multiple levels deep, such as `<<Person.Nationality.Name>>` or `<<Person.CurrentPassport.PassportNumber>>`.
*   **Flattened Fields (Recommended)**: Use pre-defined shortcuts on the ApplicationItem for better reliability:
    *   `<<PersonFullName>>`, `<<PersonPhoto>>`, `<<PersonGender>>`, `<<PersonMaritalStatus>>`, `<<PersonBirthPlace>>`, `<<PersonCountryOfBirth>>`, `<<PersonCountryOfBirthFull>>`, `<<PersonNationality>>`, `<<PersonNationalityFull>>`, `<<PersonForeignAddress>>`, `<<PersonForeignAddressCountryFull>>`, `<<PersonPosition>>`, `<<PersonDepartment>>`, `<<PersonPassportNumber>>`, `<<PersonPassportPersonalNumber>>`, `<<PersonPassportType>>`, `<<PersonPassportAuthority>>`, `<<PersonPassportCountry>>`, `<<PersonPassportCountryFull>>`, `<<PersonPassportIssueDate>>`, `<<PersonPassportIssueDateText>>`, `<<PersonPassportExpirationDate>>`, `<<PersonPassportExpirationDateText>>`, `<<PreviousPassportNumber>>`, `<<PreviousPassportPersonalNumber>>`, `<<PreviousPassportType>>`, `<<PreviousPassportAuthority>>`, `<<PreviousPassportCountry>>`, `<<PreviousPassportCountryFull>>`, `<<PreviousPassportIssueDate>>`, `<<PreviousPassportIssueDateText>>`, `<<PreviousPassportExpirationDate>>`, `<<PreviousPassportExpirationDateText>>`, `<<PersonDateOfBirth>>`, `<<PersonDateOfBirthText>>`, `<<PersonCurrentAddress>>`, `<<PersonCurrentAddressType>>`, `<<PersonCurrentAddressRegion>>`, `<<PersonCurrentAddressCity>>`, `<<PersonCurrentAddressStartDate>>`, `<<PersonCurrentAddressStartDateText>>`, `<<PersonCurrentAddressExpirationDate>>`, `<<PersonCurrentAddressExpirationDateText>>`, `<<PersonSalary>>`, `<<PersonSalaryText>>`, `<<PersonVisaNumber>>`, `<<PersonVisaCategory>>`, `<<PersonVisaType>>`, `<<PersonVisa
### 3.4. Handling Images and Sizing

To include an image (e.g., a passport photo) and control its size in the generated document:

1.  **Use a Table Container**: Insert a 1x1 table into your Word document.
2.  **Fix Dimensions**: In Table Properties, set a fixed Row Height (**Exactly**) and a fixed Column Width. In Table Options, uncheck **"Automatically resize to fit contents"**.
3.  **Insert Field**: Place the merge field (e.g., `<<Person.Photo>>`) inside the cell.
4.  **Behavior**: The Mail Merge engine will automatically scale the image to fit the cell boundaries while preserving the aspect ratio.
5.  **Alignment**: Use standard Word paragraph alignment (Center, Left, Right) inside the cell to position the image.

## 4. Key Considerations

*   **`DataType` Importance**: The `DataType` property of `RichTextMailMergeData` is critical. It tells the Mail Merge engine which business object's properties are available for merging and on which Detail Views the "Show in Mail Merge" action will appear.
*   **Template Name Matching**: The `TemplateName` in `MailMergeVisibility` must *exactly* match the `Name` of the `RichTextMailMergeData` record. Any mismatch will prevent the visibility rule from applying.
*   **`#if DEBUG` for Development**: The `MailMergeUpdater.cs` uses `#if DEBUG` to force template overwrites during development. This ensures that changes to embedded `.docx` files are always reflected in the database. In production (Release builds), user-made edits to templates are preserved.
*   **Caching**: The `MailMergeVisibilityCacheService` caches visibility rules for performance. If you make direct database changes to `MailMergeVisibility` records, you might need to restart the application to see the changes take effect immediately.
*   **Performance of Criteria**: While powerful, avoid overly complex `VisibilityCriteria` expressions, as they are evaluated client-side when the "Show in Mail Merge" action is prepared.
*   **Embedded Resource Naming**: When adding `.docx` files as embedded resources, the `resourceName` typically follows the pattern `[AssemblyDefaultNamespace].[FolderName].[FileName]`. For example, `Visa2026.Module.Resources.Visa_Grant_Letter.docx`.

## 5. Benefits

*   **User Empowerment**: Allows non-developers to create and manage document templates.
*   **Contextual UI**: Users only see relevant document generation options, reducing clutter and potential errors.
*   **Maintainability**: Centralized management of templates and their visibility rules.
*   **Version Control**: Default templates are stored as embedded resources, ensuring they are tracked in source control.

## 6. Code Reference

*   **`RichTextMailMergeData`**: `DevExpress.Persistent.BaseImpl.EF.RichTextMailMergeData`
*   **`MailMergeVisibility`**: `Visa2026.Module.BusinessObjects.MailMergeVisibility`
*   **`MailMergeUpdater`**: `Visa2026.Module.DatabaseUpdate.MailMergeUpdater`
*   **`ShowMailMergeController`**: `Visa2026.Module.Controllers.ShowMailMergeController`
*   **`IMailMergeVisibilityCacheService`**: `Visa2026.Module\Module Interface\IMailMergeVisibilityCacheService.cs`
*   **`MailMergeVisibilityCacheService`**: `Visa2026.Module\Services\MailMergeVisibilityCacheService.cs`