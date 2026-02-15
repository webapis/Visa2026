# Data Seeding Documentation

## Overview
This document describes the mechanism used to populate the database with initial "lookup" or "helper" data (e.g., Countries, Genders, Visa Categories) in the Visa2026 application. This ensures that essential reference data is available immediately after the application is deployed or updated.

## Location of Seed Data
All seed data is stored as JSON files in the following project folder:
`Visa2026.Module\DatabaseUpdate`

## List of Seeded Objects
The following Business Objects are currently populated via this mechanism:

- **Country** (`countries.json`)
- **Gender** (`genders.json`)
- **MaritalStatus** (`maritalstatuses.json`)
- **Urgency** (`urgencies.json`)
- **VisaCategory** (`visacategories.json`)
- **VisaPeriod** (`visaperiods.json`)
- **VisaType** (`visatypes.json`)
- **EducationLevel** (`educationlevels.json`)
- **PurposeOfTravel** (`purposeoftravels.json`)
- **CheckPoint** (`checkpoints.json`)
- **VisaIssuedPlace** (`visaissuedplaces.json`)
- **BorderZone** (`borderzones.json`)
- **Ministry** (`ministries.json`)
- **WorkPermitLocation** (`workpermitlocations.json`)
- **Region** (`regions.json`)
- **Department** (`departments.json`)
- **Position** (`positions.json`)
- **Specialty** (`specialties.json`)

## How It Works
The seeding logic is implemented in `Visa2026.Module\DatabaseUpdate\Updater.cs`.

1.  **Embedded Resources**: The JSON files are marked as **Embedded Resource** in their file properties. This allows them to be compiled into the assembly DLL rather than being deployed as loose files.
2.  **Updater Class**: The `UpdateDatabaseAfterUpdateSchema` method calls specific `Create...` methods for each entity type.
3.  **Generic Seeding**: A helper method `SeedData<TEntity, TData>` handles the deserialization and database insertion.
    - It reads the embedded JSON resource stream using the namespace path `Visa2026.Module.DatabaseUpdate.{filename}`.
    - It checks if an entity already exists (using a unique property like `Name` or `Code`) to prevent duplicates (Idempotency).
    - If the entity is not found, it creates a new object and maps the JSON data to the entity properties.
    - Changes are committed to the database.

## Adding New Seed Data
To add data for a new Business Object:

1.  **Create JSON File**: Add a new `.json` file in the `Visa2026.Module\DatabaseUpdate` folder containing the array of objects.
2.  **Set Build Action**: **CRITICAL**: Right-click the file in Visual Studio -> Properties -> Set **Build Action** to **Embedded Resource**. If this step is skipped, the `Updater` will not find the file.
3.  **Update Updater.cs**:
    - Define a private class representing the JSON structure (if existing `NameData` or `CountryData` doesn't fit).
    - Add a `Create[ObjectName]` method calling `SeedData`.
    - Call the new method inside `UpdateDatabaseAfterUpdateSchema`.

## Important Notes for Future Reference
- **Namespace Dependency**: The resource loader in `Updater.cs` constructs the resource name using `Visa2026.Module.DatabaseUpdate.{jsonFileName}`. If you move the JSON files to a different folder, you must update the namespace path in the `SeedData` method.
- **Case Sensitivity**: The JSON deserializer is configured with `PropertyNameCaseInsensitive = true`, so JSON property names (e.g., "name", "Name") do not need to match the C# class properties exactly case-for-case.
- **Idempotency**: The logic is designed to be safe to run on every startup. It only inserts *missing* records. It does **not** update existing records if the JSON file changes (unless you modify the logic to do so).
- **Performance**: The current implementation performs a database lookup (`FirstOrDefault`) for every item in the JSON file. This is efficient for standard lookup tables (typically < 1000 records). If seeding massive datasets, a bulk-load approach (loading all IDs into memory first) would be more performant.