using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.ApplicationWorkspace;

/// <summary>
/// Non-persistent shell for the custom ApplicationProfileInstance workspace DetailView (M2M prototype).
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Case workspace")]
[ImageName("BO_List")]
public class ApplicationWorkspaceHost : NonPersistentBaseObject
{
    [Browsable(false)]
    public Guid ApplicationProfileInstanceId { get; set; }

    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ApplicationWorkspaceEditorAliases.Workspace)]
    public string WorkspaceUi { get; set; } = string.Empty;
}
