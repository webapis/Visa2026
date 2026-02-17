# Business Object Relationships

This document serves as the single source of truth for relationships between Business Objects in the Visa2026 project.

## One-to-Many Relationships (Aggregated & Collections)

| Parent Object | Collection Property | Child Object | Inverse Reference | Description |
| :--- | :--- | :--- | :--- | :--- |
| **Person** | `Passports` | `Passport` | `Person` | Aggregated. |
| **Person** | `AddressesOfResidence` | `AddressOfResidence` | `Person` | Aggregated. |
| **Person** | `Documents` | `PersonDocument` | `Person` | Aggregated. |
| **Person** | `Educations` | `Education` | `Person` | Aggregated. |
| **Person** | `MedicalRecords` | `MedicalRecord` | `Person` | Aggregated. |
| **Employee** | `FamilyMembers` | `FamilyMember` | `Employee` | Aggregated. |
| **Employee** | `PositionHistory` | `EmployeePositionHistory` | `Employee` | Aggregated. |
| **Employee** | `WorkPermits` | `WorkPermit` | `Employee` | - |
| **AddressOfResidence** | `Documents` | `AddressOfResidenceDocument` | `AddressOfResidence` | Aggregated. |
| **Education** | `DiplomaDocuments` | `EducationDocument` | `Education` | Aggregated. |
| **MedicalRecord** | `Documents` | `MedicalRecordDocument` | `MedicalRecord` | Aggregated. |
| **Visa** | `Documents` | `VisaDocument` | `Visa` | Aggregated. |
| **Ministry** | `ProjectContracts` | `ProjectContract` | `Ministry` | Aggregated. |
| **BorderZone** | `ProjectContracts` | `ProjectContract` | - | Associated with this border zone. |
| **Application** | `WorkPermits` | `WorkPermit` | `Application` | Aggregated. |
| **Application** | `Invitations` | `Invitation` | `Application` | Aggregated. |
| **Invitation** | `InvitationItems` | `InvitationItem` | `Invitation` | Aggregated. |
| **OrganizationType** | `ApplicationTypes` | `ApplicationType` | `OrganizationType` | Aggregated. |
| **Application** | `ProgressHistory` | `ApplicationProgress` | `Application` | Aggregated. |

## Many-to-One Relationships (Lookups)

| Source Object | Property | Target Object | Description |
| :--- | :--- | :--- | :--- |
| **Person** | `BirthCountry` | `Country` | Country of birth. |
| **Person** | `Gender` | `Gender` | - |
| **Person** | `MaritalStatus` | `MaritalStatus` | - |
| **Person** | `ForeignAddressCountry` | `Country` | Home country. |
| **Employee** | `Department` | `Department` | - |
| **Employee** | `Position` | `Position` | Current job title. |
| **Employee** | `Subcontractor` | `Subcontractor` | - |
| **Employee** | `ProjectContract` | `ProjectContract` | - |
| **Education** | `EducationCountry` | `Country` | - |
| **AddressOfResidence** | `Lodging` | `Lodging` | - |
| **Passport** | `PassportType` | `PassportType` | - |
| **Visa** | `Passport` | `Passport` | Required, Aggregated. |
 | **Visa** | `ApplicationItem` | `ApplicationItem` | - |
| **Visa** | `BorderZone` | `BorderZone` | - |
| **ApplicationType** | `OrganizationType` | `OrganizationType` | - |
| **Application** | `Ministry` | `Ministry` | - |
| **Application** | `CurrentState` | `ApplicationProgress` | - |
| **ApplicationProgress** | `State` | `ApplicationState` | - |
| **ApplicationProgress** | `Location` | `ApplicationLocation` | Required. |

## Simple Lookup Objects (No Inverse Relationships)

These Business Objects serve as reference data. They are referenced by other objects but do not maintain a collection of referring objects.

*   **CheckPoint**
*   **Country**
*   **ApplicationState**
*   **ApplicationLocation**
*   **Department**
*   **EducationInstitution**
*   **EducationLevel**
*   **Gender**
*   **MaritalStatus**
*   **PassportType**
*   **Position**
*   **PurposeOfTravel**
*   **Specialty**
*   **Subcontractor**
*   **Urgency**
*   **VisaCategory**
*   **VisaIssuedPlace**
*   **VisaPeriod**
*   **VisaType**
*   **WorkPermitLocation**