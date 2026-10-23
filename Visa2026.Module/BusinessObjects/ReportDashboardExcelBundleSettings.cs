using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant singleton: shared sheet order for Report Dashboard "Reports in this file".
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DisplayName("Report Dashboard Excel bundle order")]
[ModelDefault("Caption", "Report Dashboard Excel bundle order")]
public class ReportDashboardExcelBundleSettings : BaseObject
{
    /// <summary>
    /// JSON array of {"c":categoryInt,"k":"subReportKey"} in sheet order.
    /// </summary>
    public virtual string OrderJson { get; set; } = "[]";

    public static ReportDashboardExcelBundleSettings? TryGetInstance(IObjectSpace objectSpace) =>
        OrganizationSingletonHelper.TryGet(
            objectSpace, (ReportDashboardExcelBundleSettings _) => "Excel bundle order");

    public static ReportDashboardExcelBundleSettings GetOrCreateInstance(IObjectSpace objectSpace) =>
        TryGetInstance(objectSpace) ?? objectSpace.CreateObject<ReportDashboardExcelBundleSettings>();
}