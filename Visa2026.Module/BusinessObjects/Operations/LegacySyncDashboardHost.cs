using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.Operations;

/// <summary>
/// Non-persistent shell for the on-prem VISA2015 legacy sync dashboard (admin only).
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[XafDisplayName("Legacy sync")]
[ImageName("BO_List")]
public class LegacySyncDashboardHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(LegacySyncDashboardEditorAliases.Dashboard)]
    public string DashboardUi { get; set; } = string.Empty;
}
