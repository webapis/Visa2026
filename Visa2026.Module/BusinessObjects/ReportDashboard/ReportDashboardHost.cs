using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.ReportDashboard;

/// <summary>
/// Non-persistent shell for the officer Report Dashboard (post-login home).
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Report Dashboard")]
[ImageName("BO_Report")]
public class ReportDashboardHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ReportDashboardEditorAliases.Dashboard)]
    public string DashboardUi { get; set; } = string.Empty;
}
