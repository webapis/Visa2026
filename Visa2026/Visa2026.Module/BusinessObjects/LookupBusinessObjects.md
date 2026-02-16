# Lookup Business Objects

## 1. Purpose

Lookup Business Objects represent reference data used throughout the application. They are primarily used to populate dropdown lists (comboboxes) in other forms, ensuring data consistency and standardization across the system.

---

## 2. Standard Structure

To maintain uniformity, all Lookup Business Objects follow a standard schema:

| Property Name | Data Type | Description | Constraints / Validation Rules |
| :--- | :--- | :--- | :--- |
| `Name` | `string` | The descriptive name of the record (e.g., "Turkmenistan", "Urgent"). | **Required**; **Unique**; Max 100 chars. |
| `Code` | `string` | A short alphanumeric code used for integration, reporting, or internal logic (e.g., "TM", "URG"). | Optional (unless specified); **Unique**; Max 10-20 chars. |

---

## 3. Standard Behavior

- **Seeding**: These objects are pre-populated during the database update process using JSON seed files (e.g., `countries.json`, `visatypes.json`).
- **UI Representation**: The `Name` property is set as the default display member for lookup editors.
- **Navigation**: These objects are typically grouped under "Lookup" in the navigation menu.

---

## 4. List of Lookup Objects

The following Business Objects adhere to this pattern:

| Business Object | Description | Specific Notes / Extra Properties |
| :--- | :--- | :--- |
| **BorderZone** | Restricted border areas permitted for entry. | - |
| **CheckPoint** | Official border crossing points. | - |
| **Country** | Countries of the world. | - |
| **Department** | Internal departments within the organization. | - |
| **EducationInstitution** | Universities, colleges, and schools. | - |
| **EducationLevel** | Academic degrees (e.g., Bachelor, Master). | - |
| **Gender** | Biological sex (Male, Female). | - |
| **MaritalStatus** | Civil status (Single, Married, etc.). | - |
| **PassportType** | Types of passports (Regular, Diplomatic). | - |
| **Position** | Job titles and roles. | - |
| **PurposeOfTravel** | Reasons for entry (e.g., Work, Tourism). | - |
| **Region** | Administrative regions (Welaýatlar). | Used to group `WorkPermitLocation`. |
| **Specialty** | Academic or professional specialties. | - |
| **Subcontractor** | External companies providing services. | May include contact info. |
| **Urgency** | Application processing priority levels. | Includes `Priority` (int) for sorting. |
| **VisaCategory** | Entry frequency (Single, Multiple). | - |
| **VisaIssuedPlace** | Locations where visas are issued. | Includes `IsDefault` (bool). |
| **VisaPeriod** | Standardized durations for visas. | Includes `Months` (int) for calculations. |
| **VisaType** | Classification codes (e.g., WP, BS). | - |
| **WorkPermitLocation** | Specific sites authorized for work. | - |