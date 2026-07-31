using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.Operations;

/// <summary>
/// Non-persistent shell for on-prem Import reimport history (Administrators only).
/// Reads JSON archives under ImportHistory:RootPath (sync-host history\ folder).
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Import reimport history")]
[ImageName("BO_Report")]
public class ImportReimportHistoryHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ImportReimportHistoryEditorAliases.History)]
    public string HistoryUi { get; set; } = string.Empty;
}
