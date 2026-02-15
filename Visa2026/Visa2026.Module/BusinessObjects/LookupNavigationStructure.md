# Lookup Business Objects Navigation Structure

## Overview
This document outlines the navigational grouping strategy for "Lookup" or "Reference" Business Objects in the Visa2026 application. Grouping these objects improves the user experience by keeping the main navigation menu clean and organizing related configuration tables logically.

## Implementation
To assign a Business Object to a specific group, apply the `[NavigationItem]` attribute to the class definition. Use the forward slash `/` to create a hierarchy (e.g., "Lookup/Visa").

### Code Example
```csharp
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

[DefaultClassOptions]
[NavigationItem("Lookup/Visa")] // <--- Assigns the object to the "Visa" subgroup under "Lookup"
public class VisaCategory : BaseObject
{
    // ...
}
```

---

## Navigation Groups

### 1. Lookup/Visa
*Contains reference data specifically related to the visa application process, rules, and issuance.*

| Business Object | Description |
| :--- | :--- |
| **VisaCategory** | Categories like "Single-entry", "Multiple-entry". |
| **VisaType** | Types like "Work", "Business", "Diplomatic". |
| **VisaPeriod** | Durations like "3 Months", "1 Year". |
| **Urgency** | Processing priorities like "Normal", "Urgent". |
| **PurposeOfTravel** | Reasons for entry like "Work", "Tourism". |
| **VisaIssuedPlace** | Locations like "In Turkmenistan", "Abroad". |
| **BorderZone** | Restricted areas requiring special permits. |

### 2. Lookup/Organization
*Contains reference data related to the internal organizational structure, personnel classifications, and government bodies.*

| Business Object | Description |
| :--- | :--- |
| **Ministry** | Government ministries and agencies. |
| **Department** | Internal departments (e.g., HR, IT). |
| **Position** | Job titles and roles. |
| **Subcontractor** | External companies providing services. |

### 3. Lookup/Education
*Contains reference data related to Education.*

 | Business Object | Description |
 | :--- | :--- |
| **Specialty** | Educational or professional specialties. |
| **EducationLevel** | Levels like "Bachelor", "Master". |

### 4. Lookup/Geography
*Contains geographical data, administrative regions, and specific locations used in permits.*

| Business Object | Description |
| :--- | :--- |
| **Country** | Countries and ISO codes. |
| **Region** | Administrative regions (Welaýatlar). |
| **WorkPermitLocation** | Specific locations where work is permitted. |
| **CheckPoint** | Border entry/exit points. |

### 5. Lookup/Person
*Contains demographic classifiers and attributes related to individuals.*

| Business Object | Description |
| :--- | :--- |
| **Gender** | Gender identities. |
| **MaritalStatus** | Marital status options. |

### 6. Lookup/Passport
*Contains reference data related to passport.*

| Business Object | Description |
| :--- | :--- |
| **PassportType** | Types of passports (Regular, Diplomatic). |
---

## Additional Configuration Notes

### Icons
It is recommended to assign a specific icon to the top-level "Lookup" group in the Model Editor (e.g., `Action_Search` or `BO_List`) to visually distinguish it from operational modules.

### Security
These objects are typically "Configuration" data.
- **Administrators**: Full Read/Write access.
- **Standard Users**: Read-only access (to select values in dropdowns).