using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.ApplicationProfileOverview;

/// <summary>
/// Non-persistent shell for the Application Profile overview (read-only mock / preview UI).
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Application Profile overview")]
[ImageName("BO_List")]
public class ApplicationProfileOverviewHost : NonPersistentBaseObject
{
    [Browsable(false)]
    public Guid ApplicationProfileId { get; set; }

    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ApplicationProfileOverviewEditorAliases.Overview)]
    public string OverviewUi { get; set; } = string.Empty;
}
