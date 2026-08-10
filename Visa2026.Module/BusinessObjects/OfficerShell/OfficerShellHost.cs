using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.OfficerShell;

/// <summary>
/// Non-persistent shell for the Application Profile officer UI (lift from HTML prototype).
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Application Profiles")]
[ImageName("BO_List")]
public class OfficerShellHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(OfficerShellEditorAliases.Shell)]
    public string ShellUi { get; set; } = string.Empty;
}
