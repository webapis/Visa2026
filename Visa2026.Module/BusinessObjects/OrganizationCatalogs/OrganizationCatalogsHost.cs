using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.OrganizationCatalogs;

/// <summary>
/// Non-persistent shell for Configuration → Organization catalogs (Company / Signatory / Representative).
/// XAF EF Core requires <see cref="DomainComponentAttribute"/> to register NonPersistentBaseObject hosts
/// (XAF0016); without it CreateObjectSpace uses EF and throws type-not-registered.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Organization catalogs")]
[ImageName("BO_Organization")]
public class OrganizationCatalogsHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(OrganizationCatalogsEditorAliases.Catalogs)]
    public string CatalogsUi { get; set; } = string.Empty;
}
