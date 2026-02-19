# Application Properties Visibility

This document describes the visibility of properties in the `Application` business object based on the selected `OrganizationType` and `ApplicationType`. The visibility is controlled using DevExpress's `Appearance` attributes in the `Application.cs` file.

## Visibility Control

The following table outlines the conditions under which specific properties are visible in the `Application` Detail View. The visibility is determined by the `ApplicationType` and its properties, such as `ShowProjectContract`, `ShowVisaPeriod`, `ShowVisaCategory`, and `ShowMinistry`.

## Visibility Matrix

| Property Name | Visibility Condition | Notes |
|---|---|---|
| `ProjectContract` | `ApplicationType is null or !ApplicationType.ShowProjectContract` | Hidden if `ApplicationType` is not selected or `ShowProjectContract` is false. |
| `VisaPeriod` | `ApplicationType is null or !ApplicationType.ShowVisaPeriod` | Hidden if `ApplicationType` is not selected or `ShowVisaPeriod` is false. |
| `VisaCategory` | `ApplicationType is null or !ApplicationType.ShowVisaCategory` | Hidden if `ApplicationType` is not selected or `ShowVisaCategory` is false. |
| `Ministry` | `ApplicationType is null or !ApplicationType.ShowMinistry` | Hidden if `ApplicationType` is not selected or `ShowMinistry` is false. |

## Implementation Details

The visibility is implemented using `Appearance` attributes in the `Application.cs` file. Here's an example:

```csharp
[Appearance("ProjectContractVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowProjectContract", Context = "DetailView")]
public virtual ProjectContract ProjectContract { get; set; }
```

In this example, the `ProjectContract` property is hidden in the Detail View if the `ApplicationType` is null or the `ShowProjectContract` property of the selected `ApplicationType` is false.

## Dynamic Visibility and ImmediatePostData

The `ApplicationType` property uses `ImmediatePostData` to refresh the view when the user changes the selected `ApplicationType`. This ensures that the visibility of the properties is updated dynamically based on the selected `ApplicationType`.

```csharp
[ImmediatePostData, RuleRequiredField]
[DataSourceCriteria("OrganizationType = '@This.OrganizationType' And (Category = 'Both' Or (Category = 'FamilyMember' And '@This.IsForFamily' = true) Or (Category = 'Employee' And '@This.IsForFamily' = false))")]
public virtual ApplicationType ApplicationType { get; set; }
```

The `DataSourceCriteria` attribute filters the available `ApplicationType` options based on the selected `OrganizationType` and the `IsForFamily` property.

## OrganizationType and ApplicationType Filtering

The `ApplicationType` lookup is filtered based on the selected `OrganizationType` and the `IsForFamily` property. The `DataSourceCriteria` attribute ensures that only the relevant `ApplicationType` options are displayed to the user. The `Category` property of the `ApplicationType` determines whether it is applicable to employees, family members, or both.

This filtering ensures that the user only sees the `ApplicationType` options that are relevant to the selected `OrganizationType` and the `IsForFamily` property, reducing the risk of errors and improving the user experience.

By centralizing the visibility logic in the `ApplicationType` business object and using `Appearance` attributes, the system provides a flexible and maintainable way to control the visibility of properties in the `Application` business object.