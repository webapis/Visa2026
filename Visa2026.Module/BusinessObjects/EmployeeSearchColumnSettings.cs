using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant singleton: shared Find employees result columns (order and which ones are included).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DisplayName("Find employees result columns")]
[ModelDefault("Caption", "Find employees result columns")]
public class EmployeeSearchColumnSettings : BaseObject
{
    /// <summary>
    /// JSON array of {"k":"PropertyName","on":true} in column order.
    /// Unchecked columns stay in the array and are left out of the table and Excel.
    /// </summary>
    public virtual string ColumnsJson { get; set; } = "[]";

    public static EmployeeSearchColumnSettings? TryGetInstance(IObjectSpace objectSpace) =>
        OrganizationSingletonHelper.TryGet(
            objectSpace, (EmployeeSearchColumnSettings _) => "Find employees columns");

    public static EmployeeSearchColumnSettings GetOrCreateInstance(IObjectSpace objectSpace) =>
        TryGetInstance(objectSpace) ?? objectSpace.CreateObject<EmployeeSearchColumnSettings>();
}