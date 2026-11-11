using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// One Work permit quota row: no New/Delete. ListView ensures the singleton exists.
/// </summary>
public sealed class WorkPermitQuotaSettingsController : ObjectViewController<ObjectView, WorkPermitQuotaSettings>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        View.AllowNew.SetItemValue(nameof(WorkPermitQuotaSettingsController), false);
        View.AllowDelete.SetItemValue(nameof(WorkPermitQuotaSettingsController), false);
        if (View is ListView)
        {
            var settings = WorkPermitQuotaSettings.GetOrCreateInstance(ObjectSpace);
            if (ObjectSpace.IsNewObject(settings) || ObjectSpace.IsModified)
                ObjectSpace.CommitChanges();
            View.ObjectSpace.Refresh();
        }
    }

    protected override void OnDeactivated()
    {
        View.AllowNew.RemoveItem(nameof(WorkPermitQuotaSettingsController));
        View.AllowDelete.RemoveItem(nameof(WorkPermitQuotaSettingsController));
        base.OnDeactivated();
    }
}
