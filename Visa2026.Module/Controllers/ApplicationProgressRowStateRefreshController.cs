using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Refreshes <see cref="Application"/> ListView row colors when <see cref="ApplicationProfileInstanceProgress"/> changes.
/// </summary>
public sealed class ApplicationProfileInstanceProgressRowStateRefreshController : ViewController
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
        ObjectSpace.Committed += ObjectSpace_Committed;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        ObjectSpace.Committed -= ObjectSpace_Committed;
        base.OnDeactivated();
    }

    private void ObjectSpace_Committed(object sender, EventArgs e)
    {
        SyncLatestProgressFields();
        if (ObjectSpace.ModifiedObjects.Count > 0)
            ObjectSpace.CommitChanges();
        RefreshAppearance();
    }

    private void SyncLatestProgressFields()
    {
        var applications = ObjectSpace.ModifiedObjects.OfType<ApplicationProfileInstanceProgress>()
            .Select(progress => progress.ApplicationProfileInstance)
            .Concat(ObjectSpace.ModifiedObjects.OfType<ApplicationProfileInstance>())
            .Where(application => application != null)
            .Distinct()
            .ToList();

        foreach (var application in applications)
            ApplicationLatestProgressSyncHelper.Sync(application, ObjectSpace);
    }

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object is ApplicationProfileInstanceProgress progress)
        {
            progress.ApplicationProfileInstance?.InvalidateListViewDisplayCache();
            RefreshAppearance();
            return;
        }

        if (e.Object is ApplicationProfileInstance)
            RefreshAppearance();
    }

    private void RefreshAppearance()
    {
        if (Frame.View is ListView { ObjectTypeInfo.Type: var type } listView)
        {
            if (type == typeof(ApplicationProfileInstance))
            {
                foreach (var application in listView.CollectionSource.List.OfType<ApplicationProfileInstance>())
                    application.InvalidateListViewDisplayCache();
            }
            else if (type == typeof(ApplicationRosterMergeLine))
            {
                foreach (var application in listView.CollectionSource.List.OfType<ApplicationRosterMergeLine>()
                             .Select(item => item.ApplicationProfileInstance)
                             .Where(application => application != null)
                             .Distinct())
                {
                    application!.InvalidateListViewDisplayCache();
                }
            }
        }

        Frame.GetController<AppearanceController>()?.Refresh();

        if (Frame.View is DetailView detailView
            && detailView.CurrentObject is BusinessObjects.ApplicationProfileInstance)
        {
            detailView.Refresh();
        }
        else if (Frame.View is ListView { ObjectTypeInfo.Type: var itemListType }
                 && itemListType == typeof(ApplicationRosterMergeLine))
        {
            Frame.View.Refresh();
        }
    }
}
