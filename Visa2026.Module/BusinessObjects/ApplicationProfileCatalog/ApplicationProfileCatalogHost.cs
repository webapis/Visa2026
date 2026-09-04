using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.ApplicationProfileCatalog;

/// <summary>
/// Non-persistent shell for the Application Profile catalog (custom home replacing native ListView).
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Application Profile")]
[ImageName("BO_List")]
public class ApplicationProfileCatalogHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ApplicationProfileCatalogEditorAliases.Catalog)]
    public string CatalogUi { get; set; } = string.Empty;
}