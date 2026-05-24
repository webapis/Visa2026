using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.StateNotifications;

/// <summary>
/// Non-persistent shell that hosts the state-notification inbox UI (prototype).
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[XafDisplayName("State notifications")]
[ImageName("BO_Validation")]
public class BoStateNotificationInboxHost : NonPersistentBaseObject
{
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(BoStateNotificationInboxEditorAliases.Inbox)]
    public string InboxUi { get; set; } = string.Empty;
}
