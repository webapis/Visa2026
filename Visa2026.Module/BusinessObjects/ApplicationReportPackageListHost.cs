using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Non-persistent shell for the Resminamalar report package dialog on an <see cref="Application"/>.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[XafDisplayName("Resminamalar")]
[ImageName("BO_FileAttachment")]
public class ApplicationReportPackageListHost : NonPersistentBaseObject
{
    [Browsable(false)]
    public Guid ApplicationId { get; set; }

    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ApplicationReportPackageEditorAliases.ListPanel)]
    public string ListUi { get; set; } = string.Empty;
}
