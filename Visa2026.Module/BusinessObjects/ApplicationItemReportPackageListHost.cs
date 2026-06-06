using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Non-persistent shell for item-scoped Resminamalar on one or more selected application lines.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[XafDisplayName("Resminamalar")]
[ImageName("BO_FileAttachment")]
public class ApplicationItemReportPackageListHost : NonPersistentBaseObject
{
    [Browsable(false)]
    public string ItemIdsJson { get; set; } = "[]";

    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ApplicationItemReportPackageEditorAliases.ListPanel)]
    public string ListUi { get; set; } = string.Empty;
}
