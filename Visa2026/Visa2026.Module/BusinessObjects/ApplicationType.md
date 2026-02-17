# Business Object: ApplicationType

## 1. Purpose & Problem Solved

The `ApplicationType` business object serves as a central configuration entity that defines all possible procedures and application types within the Visa2026 system (e.g., "Invitation Request", "Visa Extension", "Registration on Arrival").

Its primary purpose is to **decouple UI logic from the core business objects** (`Application` and `ApplicationItem`).

### The Problem
Before `ApplicationType`, the `Application` and `ApplicationItem` classes were cluttered with numerous `[Appearance]` attributes. The visibility of each field was determined by complex, hardcoded criteria strings that checked the value of an enum.

```csharp
// Old, hard-to-maintain approach
[Appearance("VisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!((!Application.IsForFamily && Application.EmployeeApplicationType In ('ApplicationForVisaExtention', ...))", Context = "DetailView")]
public virtual Visa Visa { get; set; }
```

This approach was:
- **Brittle**: A small change in logic required modifying code and recompiling.
- **Hard to Read**: The criteria strings were long and difficult to understand.
- **Difficult to Manage**: Adding a new application type or changing a rule involved hunting down and updating multiple attributes across different files.

### The Solution
The `ApplicationType` object solves this by centralizing the visibility logic. Each `ApplicationType` record holds a set of boolean flags (e.g., `ShowVisa`, `ShowProjectContract`). The `[Appearance]` attributes are now simplified to check these flags on the selected `ApplicationType`.

```csharp
// New, clean, data-driven approach
[Appearance("ShowVisa", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisa")]
public virtual Visa Visa { get; set; }
```

This makes the system:
- **Flexible**: UI behavior can be changed by simply updating the data in the `applicationtypes.json` seed file, without any code changes.
- **Maintainable**: All rules for a specific procedure are defined in one place.
- **Readable**: The logic is clear and easy to follow.

---

## 2. Key Properties

| Property Name | Data Type | Description |
| :--- | :--- | :--- |
| `Name` | `string` | The user-friendly name of the procedure (e.g., "Wiza möhletini uzaltmak"). |
| `Category` | `ApplicationTypeCategory` (Enum) | Determines who this application type is for: `Employee`, `FamilyMember`, or `Both`. Used to filter the lookup list. |
| `ShowProjectContract` | `bool` | If `true`, the `ProjectContract` field is visible on the `Application` form. |
| `ShowVisaPeriod` | `bool` | If `true`, the `VisaPeriod` field is visible on the `Application` form. |
| `ShowVisaCategory` | `bool` | If `true`, the `VisaCategory` field is visible on the `Application` form. |
| `ShowMinistry` | `bool` | If `true`, the `Ministry` field is visible on the `Application` form. |
| `CanRequireWorkPermit` | `bool` | If `true`, the `IsWorkPermitRequired` checkbox is visible on the `Application` form. |
| `ShowPreviousPassport` | `bool` | If `true`, the `PreviousPassport` field is visible on the `ApplicationItem` form. |
| `ShowVisa` | `bool` | If `true`, the `Visa` field is visible on the `ApplicationItem` form. |
| `ShowWorkPermit` | `bool` | If `true`, the `WorkPermit` field is visible on the `ApplicationItem` form. |
| `ShowPosition` | `bool` | If `true`, the `Position` field is visible on the `ApplicationItem` form. |
| `ShowAddressOfResidence`| `bool` | If `true`, the `AddressOfResidence` field is visible on the `ApplicationItem` form. |
| `ShowCheckPoint` | `bool` | If `true`, the `CheckPoint` field is visible on the `ApplicationItem` form. |
| `ShowEntryDate` | `bool` | If `true`, the `EntryDate` field is visible on the `ApplicationItem` form. |
| `ShowVisaIssuedPlace` | `bool` | If `true`, the `VisaIssuedPlace` field is visible on the `ApplicationItem` form. |
| `ShowPurposeOfTravel` | `bool` | If `true`, the `PurposeOfTravel` field is visible on the `ApplicationItem` form. |

---

## 3. How and When It Is Used

The `ApplicationType` is a central part of creating a new `Application`.

1.  A user creates a new `Application` record.
2.  The user selects whether the application is for an employee or a family member (`IsForFamily` checkbox).
3.  The user selects an `ApplicationType` from a lookup list. This list is filtered based on the `Category` property and the `IsForFamily` checkbox.
4.  **Immediately** (`[ImmediatePostData]`), the `Application` and its nested `ApplicationItem` forms update their visibility. Fields become visible or hidden based on the boolean flags of the selected `ApplicationType`.
5.  This ensures that the user is only presented with the fields that are relevant to the specific procedure they are performing, reducing errors and improving user experience.

---

## 4. Configuration

The `ApplicationType` is a **Lookup Business Object**. Its data is not meant to be managed by end-users but is pre-configured by administrators.

- **Source File**: All `ApplicationType` records and their visibility rules are defined in the `Visa2026.Module\DatabaseUpdate\applicationtypes.json` file.
- **Seeding**: This JSON file is used by the `Updater.cs` to populate the `ApplicationTypes` table in the database when the application starts or updates. Any changes to the UI logic should be made in this JSON file.