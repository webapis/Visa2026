# Seeded Business Objects List

The application seeds two types of data: **Lookup Data** (essential for application function in all environments) and **Demo Data** (used for development and testing).

## Lookup Business Objects (Production & Development)

These "Lookup" or "Helper" Business Objects are seeded in all environments to ensure the application has the necessary reference data. They are not specific to any particular Business Object and can be used by any Business Object, for example, Passport, Employee, or FamilyMember.

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
- **EducationInstitution** (`educationinstitutions.json`)
 - **ProjectContract** (`projectcontracts.json`) 

- **PassportType** (`passporttypes.json`) 
## Demo Business Objects (Development Only)

These objects are seeded only in development environments (e.g., Debug builds) to facilitate testing and demos.

- **Employee** (`employees.json`)
    - Note: This file includes embedded data for **Passport**, **Visa**, **Education**, **EmployeePositionHistory**

**Important Note**: Business Objects like Passport, Visa, EmployeePositionHistory, and Education can only belong to specific Employee or FamilyMember Business Objects. Therefore, their seeding is more complex than Lookup Business Objects. They must be seeded within the realm of their parent Business Objects (e.g., as part of the Employee seeding process).



## Location of Seed Data

All seed data is stored as JSON files in the `Visa2026.Module\DatabaseUpdate` folder. These files must be set as **Embedded Resource** in their build action to be accessible by the updater.

## How Data is Seeded

The seeding process is executed in `Visa2026.Module\DatabaseUpdate\Updater.cs` within the `UpdateDatabaseAfterUpdateSchema` method.

1.  **Embedded Resources**: The application reads the JSON files embedded in the assembly.
2.  **Deserialization**: The JSON data is deserialized into temporary data objects.
3.  **Idempotency Check**: The code checks if the business object already exists in the database (usually matching by Name or Code).
4.  **Creation**: If the object does not exist, it is created and populated with data from the JSON file.
5.  **Commit**: Changes are saved to the database.

**Note on Demo Data**: Demo data seeding is wrapped in preprocessor directives (e.g., `#if !RELEASE`) to ensure it does not execute in production builds.