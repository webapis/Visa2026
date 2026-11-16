using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// One visa-issue-window row: no New/Delete. ListView ensures the singleton exists.
/// </summary>
public sealed class VisaIssueWindowSettingsController : ObjectViewController<ObjectView, VisaIssueWindowSettings>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        View.AllowNew.SetItemValue(nameof(VisaIssueWindowSettingsController), false);
        View.AllowDelete.SetItemValue(nameof(VisaIssueWindowSettingsController), false);
        if (View is ListView)
        {
            var settings = VisaIssueWindowSettings.GetOrCreateInstance(ObjectSpace);
            if (ObjectSpace.IsNewObject(settings) || ObjectSpace.IsModified)
                ObjectSpace.CommitChanges();
            View.ObjectSpace.Refresh();
        }
    }

    protected override void OnDeactivated()
    {
        View.AllowNew.RemoveItem(nameof(VisaIssueWindowSettingsController));
        View.AllowDelete.RemoveItem(nameof(VisaIssueWindowSettingsController));
        base.OnDeactivated();
    }
}
