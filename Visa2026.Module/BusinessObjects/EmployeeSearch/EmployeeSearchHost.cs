using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.EmployeeSearch;

/// <summary>
/// Non-persistent shell for Find employees.
/// XAF EF Core requires <see cref="DomainComponentAttribute"/> to register NonPersistentBaseObject hosts
/// (XAF0016); without it CreateObjectSpace uses EF and throws type-not-registered.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName(EmployeeSearchViewIds.Caption)]
[ImageName("Action_Search")]
public class EmployeeSearchHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(EmployeeSearchEditorAliases.Search)]
    public string SearchUi { get; set; } = string.Empty;
}
