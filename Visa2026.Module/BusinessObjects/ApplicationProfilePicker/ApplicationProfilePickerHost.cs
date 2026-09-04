using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.ApplicationProfilePicker;

/// <summary>
/// Non-persistent shell for choosing an <see cref="ApplicationProfile"/> when creating an <see cref="Application"/>.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Choose Application Profile")]
[ImageName("BO_List")]
public class ApplicationProfilePickerHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ApplicationProfilePickerEditorAliases.Picker)]
    public string PickerUi { get; set; } = string.Empty;
}
