# Business Object: PdfFormConstant

## 1. Overview

The `PdfFormConstant` business object provides a user-configurable, database-driven key-value store. Its primary purpose is to replace hard-coded dictionaries used for converting application data into the specific raw codes required by the PDF form.

For example, a dropdown in the UI might display "Ordinary Passport", but the PDF form's choice list requires the value "P". This object allows an administrator to define that mapping in the database.

## 2. User Guide

To create or modify PDF constants, navigate to **System > PDF Form Constants** in the application navigation menu.

### 2.1. Purpose
Use this feature to manage the lookup values for dropdowns (`choiceList`) in the PDF. When the application's display value (e.g., from a `Gender` or `MaritalStatus` object) differs from the value the PDF field expects, a `PdfFormConstant` record bridges the gap.

### 2.2. Property Reference

| Property | Description | Format / Notes | Example Value |
| :--- | :--- | :--- | :--- |
| **Category** | A grouping key that links a set of constants together. This **must** match the category name used by the associated `IValueConverter`. | Free text. Case-insensitive. | `PassportType` |
| **Display Value** | The value as it exists in the application's database (e.g., the `Name` property of a lookup object). | String. Case-insensitive. | `Ordinary Passport` |
| **PDF Value** | The raw string value that the PDF form field expects. | String. | `P` |

### 2.3. Configuration Example

*Scenario: The `Urgency` dropdown in the PDF expects the raw values '1', '2', or '3', but the application stores the names 'ADATY', 'TIZ', and 'ORAN TIZ'.*

You would create the following records:

| Category | Display Value | PDF Value |
| :--- | :--- | :--- |
| `Urgency` | `ADATY` | `1` |
| `Urgency` | `TIZ` | `2` |
| `Urgency` | `ORAN TIZ` | `3` |

## 3. Technical Implementation

*   **Class**: `Visa2026.Module.BusinessObjects.PdfFormConstant`
*   **Service**: `Visa2026.Module.Services.PdfFormConstants` (static helper)
*   **Converters**: Implementations of `IValueConverter` (e.g., `UrgencyValueConverter`)

### 3.1. How it Works

1.  A `PdfFormMapping` rule is configured to use a value converter (e.g., `UrgencyValueConverter`).
2.  During PDF generation, `PdfMappingHelper` gets a value (e.g., "TIZ") and calls `UrgencyValueConverter.Convert("TIZ", objectSpace)`.
3.  The converter calls the static helper: `PdfFormConstants.GetValue("Urgency", "TIZ", objectSpace)`.
4.  The `PdfFormConstants` helper checks its internal static cache for the "Urgency" category.
5.  **If the cache is empty**, it queries the database for all `PdfFormConstant` records, groups them by `Category`, and populates the cache. This happens only once per application run unless manually refreshed.
6.  It then looks up "TIZ" within the "Urgency" category in the cache and returns the corresponding `PdfValue` ("2").
7.  If no match is found, it returns the original display value as a fallback.

This design provides the flexibility of database configuration with the performance of an in-memory dictionary lookup for subsequent calls.

## 4. Adding a New Set of Constants

1.  **Define a Category**: Decide on a new, unique `Category` name (e.g., "DocumentStatus").
2.  **Create a Converter**: If one doesn't already exist, create a new class that implements `IValueConverter`. The `Convert` method should call `PdfFormConstants.GetValue("DocumentStatus", ...)`.
3.  **Populate the Database**: In the UI, navigate to **System > PDF Form Constants** and add new records for the "DocumentStatus" category.
4.  **Use the Converter**: In the **PDF Form Mappings** UI, find the rule that needs this conversion and select your new converter from the `Converter Type` dropdown.