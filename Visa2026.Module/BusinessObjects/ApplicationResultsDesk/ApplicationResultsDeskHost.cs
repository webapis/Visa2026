using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.ApplicationResultsDesk;

/// <summary>
/// Non-persistent shell for the Application results desk.
/// XAF EF Core requires <see cref="DomainComponentAttribute"/> to register NonPersistentBaseObject hosts
/// (XAF0016); without it CreateObjectSpace uses EF and throws type-not-registered.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName(ApplicationResultsDeskViewIds.Caption)]
[ImageName("BO_List")]
public class ApplicationResultsDeskHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ApplicationResultsDeskEditorAliases.Desk)]
    public string DeskUi { get; set; } = string.Empty;
}
