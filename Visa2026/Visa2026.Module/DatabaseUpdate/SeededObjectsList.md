# Seeded Business Objects List

The application seeds two types of data: **Lookup Data** (essential for application function in all environments) and **Demo Data** (used for development and testing).

## Lookup Business Objects (Production & Development)

These "Lookup Data" Business Objects are seeded in all environments to ensure the application has the necessary reference data. They are not specific to any particular Business Object and can be used by any Business Object, for example, Passport, Employee, or FamilyMember.

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
- **City** (`cities.json`)
- **Position** (`positions.json`)
- **Specialty** (`specialties.json`)
- **EducationInstitution** (`educationinstitutions.json`)
- **ProjectContract** (`projectcontracts.json`)
- **PassportType** (`passporttypes.json`)
- **Subcontractor** (`subcontractor.json`)
- **Relationship** (`relationship.json`)
- **OrganizationType** (`organizationtype.json`)
- **ApplicationState** (`applicationstates.json`)
- **ApplicationLocation** (`applicationlocations.json`)
- **ApplicationType** (`applicationtypes.json`)
- **ValidityDuration** (`validityduration.json`)

## Demo Business Objects (Development Only)

These objects are seeded only in development environments (e.g., Debug builds) to facilitate testing and demos.

- **Employee** (`employees.json`)
    - Note: This file includes embedded data for **Passport**, **Visa**, **Education**, **EmployeePositionHistory**

**Important Note**: Business Objects like Passport, Visa, EmployeePositionHistory, and Education can only belong to specific Employee or FamilyMember Business Objects. Therefore, their seeding is more complex than Lookup Business Objects. They must be seeded within the realm of their parent Business Objects (e.g., as part of the Employee seeding process).


To seed the `Employee` demo object and its child Business Objects, you'll need to:
1. Create a JSON structure in `employees.json` that includes the `Employee` object and its related `Passport`, `Visa`, `Education`, and `EmployeePositionHistory` objects.
2. Implement the deserialization logic in `Updater.cs` to handle the `employees.json` file and create the corresponding `Employee` objects and their related objects.
    - Handle the nested creation of `Passport`, `Visa`, `Education`, and `EmployeePositionHistory` objects within the `Employee` seeding logic. **Visa should be embedded inside specific Passport BO seed data because it is a child of Passport BO.** This involves iterating through the collections in the JSON data and creating corresponding business objects, linking them to the `Employee` object.
    - Before creating any business object, it is important to check if it already exists.
3. Ensure that the demo data seeding code is wrapped in preprocessor directives (e.g., `#if DEBUG`) to prevent it from running in production environments.

## Location of Seed Data

All seed data is stored as JSON files in the `Visa2026.Module\DatabaseUpdate` folder. These files must be set as **Embedded Resource** in their build action to be accessible by the updater.
## JSON file structure
```json
[
 {
  "EmployeeId": "string",
  "FirstName": "string",
  "LastName": "string",
  "Gender": "string",
  "HireDate": "datetime",
  "Position": "string",
  "Email": "string",
  "ProjectContract": "string",
  "PositionHistories": [
   {
    "StartDate": "datetime",
    "EndDate": "datetime?",
    "Position": "string",
    "Department": "string"
   }
  ],
  "Passports": [
   {
    "PassportNumber": "string",
    "PassportType": "string",
    "IssueDate": "datetime",
    "ExpirationDate": "datetime",
    "Authority": "string",
    "Visas": [
      {
        "VisaNumber": "string",
        "StartDate": "datetime",
        "ExpirationDate": "datetime"
      }
    ],
   }
  ],

]
 
```
## How Data is Seeded

The seeding process is executed in `Visa2026.Module\DatabaseUpdate\Updater.cs` within the `UpdateDatabaseAfterUpdateSchema` method.

1.  **Embedded Resources**: The application reads the JSON files embedded in the assembly.
2.  **Deserialization**: The JSON data is deserialized into temporary data objects.
3.  **Idempotency Check**: The code checks if the business object already exists in the database (usually matching by Name or Code).
4.  **Creation**: If the object does not exist, it is created and populated with data from the JSON file.
5.  **Commit**: Changes are saved to the database.

**Note on Demo Data**: Demo data seeding is wrapped in preprocessor directives (e.g., `#if !RELEASE`) to ensure it does not execute in production builds.



Example of preprocessor directives: `#if DEBUG`