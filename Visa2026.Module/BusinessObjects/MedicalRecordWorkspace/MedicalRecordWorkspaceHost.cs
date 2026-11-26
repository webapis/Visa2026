using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.MedicalRecordWorkspace;

/// <summary>
/// Non-persistent shell for the Medical records list and detail.
/// XAF EF Core requires <see cref="DomainComponentAttribute"/> to register NonPersistentBaseObject hosts
/// (XAF0016); without it CreateObjectSpace uses EF and throws type-not-registered.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Medical records")]
[ImageName("BO_Document")]
public class MedicalRecordWorkspaceHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(MedicalRecordWorkspaceEditorAliases.Workspace)]
    public string WorkspaceUi { get; set; } = string.Empty;
}
