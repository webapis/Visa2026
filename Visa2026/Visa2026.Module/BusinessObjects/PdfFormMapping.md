# Business Object: PdfFormMapping

## 1. Overview

The `PdfFormMapping` business object is the configuration interface for the **Dynamic PDF Mapping Engine**. It allows administrators to define rules that map data from application business objects (like `ApplicationItem` and its related objects) to specific fields in the visa application PDF template.

This system replaces hard-coded mapping logic, enabling runtime configuration of the PDF generation process without requiring code changes or a new deployment.

## 2. User Guide

To create or modify PDF mappings, navigate to **System > PDF Form Mappings** in the application navigation menu.

### 2.1. Property Reference

| Property | Description | Format / Notes | Example Value |
| :--- | :--- | :--- | :--- |
| **PdfFieldKey** | The exact name of the target field in the XFA PDF template. | String. Must match the key from the PDF. | `topmostSubform[0].Page1[0]._01[0]` |
| **Description** | A human-readable name for the mapping rule. | Free text. | `Map Person's Last Name` |
| **Mapping Mode** | The strategy used to get the value for the PDF field. | Dropdown (`Property`, `Expression`, `Constant`). | `Property` |
| **Property Path** | (Visible if Mode = `Property`) The navigation path from the `ApplicationItem` to the source property. | Dot-notation path. | `Person.LastName` |
| **Expression** | (Visible if Mode = `Expression`) A criteria language expression to calculate the value. | Criteria Language Syntax. | `Concat(Person.FirstName, ' ', Person.LastName)` |
| **Constant Value** | (Visible if Mode = `Constant`) A fixed, static value to be written to the PDF field. | String representation of the value. | `true` (for a checkbox) |
| **Converter Type** | (Optional) A value converter to transform the data before it's sent to the PDF. | Dropdown selection. | `PassportTypeValueConverter` |

### 2.2. Detailed Configuration Logic

#### Mapping Modes
The **Mapping Mode** determines the strategy used to retrieve data.

1.  **Property**
    *   **Use Case**: Used when the data exists directly on your business object. This covers most scenarios.
    *   **Configuration**: Set `PropertyPath` to the navigation path (e.g., `Person.LastName` or `Application.Company.Name`).

2.  **Expression**
    *   **Use Case**: Used when the data needs to be calculated, combined, or formatted before being placed in the PDF.
    *   **Configuration**: Set `Expression` using Criteria Language (e.g., `Concat(Person.FirstName, ' ', Person.LastName)`).

3.  **Constant**
    *   **Use Case**: Used when a field in the PDF should *always* have the same value, regardless of the application data (e.g., checking a specific checkbox by default).
    *   **Configuration**: Set `ConstantValue` (e.g., `true`).

#### Value Converters
The **Converter Type** is used to translate application data (Display Values) into PDF Form Codes (Raw Values).

*   **When to use**: For Dropdowns and Choice Lists where the text shown to the user differs from the internal code required by the PDF.
*   **Example**: Converting Urgency "Gyssagly tertipde!" (Fast) to code "2".
*   **Setup**: Select a converter (e.g., `UrgencyValueConverter`) from the dropdown. Ensure corresponding `PdfFormConstant` records exist in the database.

### 2.3. Configuration Examples

#### Example 1: Simple Property Mapping
*Scenario: Map the person's last name to the corresponding PDF field.*

*   **Description**: `Person Last Name`
*   **PdfFieldKey**: `topmostSubform[0].Page1[0]._01[0]`
*   **Mapping Mode**: `Property`
*   **Property Path**: `Person.LastName`

#### Example 2: Constant Value Mapping
*Scenario: Always check the "Legal Entity" checkbox when an application has a company.*

*   **Description**: `Legal Entity Checkbox`
*   **PdfFieldKey**: `topmostSubform[0].Page1[0].IP[1].#field[0]`
*   **Mapping Mode**: `Constant`
*   **Constant Value**: `true`

#### Example 3: Expression-Based Mapping
*Scenario: Combine the person's foreign address country code and the address string into a single field.*

*   **Description**: `Foreign Address (Country + Address)`
*   **PdfFieldKey**: `topmostSubform[0].Page1[0]._15[0]`
*   **Mapping Mode**: `Expression`
*   **Expression**: `Concat(Person.ForeignAddressCountry.Code, ', ', Person.ForeignAddress)`

#### Example 4: Using a Value Converter
*Scenario: Map the passport type. The application stores "Ordinary Passport", but the PDF requires the code "P".*

*   **Description**: `Passport Type`
*   **PdfFieldKey**: `topmostSubform[0].Page1[0]._10[0]`
*   **Mapping Mode**: `Property`
*   **Property Path**: `CurrentPassport.PassportType.Name`
*   **Converter Type**: `PassportTypeValueConverter` (This converter will look up "Ordinary Passport" in the `PdfFormConstant` table and find the value "P").

## 3. Technical Implementation

*   **Class**: `Visa2026.Module.BusinessObjects.PdfFormMapping`
*   **Helper**: `Visa2026.Module.Services.PdfMappingHelper`
*   **Execution**: The `ApplicationItemPdfController` and `ApplicationPdfController` fetch all `PdfFormMapping` records from the database. These records are passed to `PdfMappingHelper.MapApplicationData`. The helper iterates through each rule and uses the `MappingMode` to determine how to retrieve the data:
    *   `Property`: Uses reflection (`GetValueByPath`) to navigate the object graph.
    *   `Expression`: Uses `DevExpress.Data.Filtering.Helpers.ExpressionEvaluator` to compute the value.
    *   `Constant`: Uses the `ConstantValue` directly.
    *   If a `ConverterTypeName` is specified, it instantiates the converter and passes the retrieved value through it.

## 4. Troubleshooting

If a field in the generated PDF is empty or has an incorrect value:
1.  **Verify the `PdfFieldKey`**: Ensure it exactly matches a field name from the PDF template specification.
2.  **Check the Logs**: Enable `Debug` logging for `Visa2026.Module.Services`. The application log will contain detailed entries for each mapping, including warnings for null values or errors during evaluation.
3.  **Validate the Path/Expression**:
    *   For `Property` mode, ensure the `PropertyPath` is correct and that the intermediate objects (e.g., `Person`) are not null.
    *   For `Expression` mode, check the syntax in the `Expression` editor. The system has a validation rule (`IsExpressionValid`) to prevent saving invalid syntax.
4.  **Check the Converter**: If using a converter, ensure the corresponding `PdfFormConstant` records exist for the value you are trying to convert.

## 5. How to Find PDF Field Keys

To configure mappings correctly, you need the exact XFA field names from the PDF template. Here are a few ways to find them:

### Method 1: Application Logs (Recommended)
The application logs all available field names when it attempts to fill a form.
1.  Enable **Debug** logging in `appsettings.json`.
2.  Trigger a PDF generation action in the application.
3.  Check the application logs (console or file). Look for a warning log entry starting with: `XFA field names in template: [...]`.
4.  Copy the field names exactly as they appear (e.g., `topmostSubform[0].Page1[0]._01[0]`).

### Method 2: Adobe Acrobat Pro
1.  Open the PDF in Adobe Acrobat Pro.
2.  Go to **Prepare Form**.
3.  The field names will be displayed on the form. For XFA forms, you might need to look at the hierarchy view to get the full path.