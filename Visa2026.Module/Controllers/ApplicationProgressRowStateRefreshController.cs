using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Refreshes <see cref="Application"/> ListView row colors when <see cref="ApplicationProgress"/> changes.
/// </summary>
public sealed class ApplicationProgressRowStateRefreshController : ViewController
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
        var applications = ObjectSpace.ModifiedObjects.OfType<ApplicationProgress>()
            .Select(progress => progress.Application)
            .Concat(ObjectSpace.ModifiedObjects.OfType<Application>())
            .Where(application => application != null)
            .Distinct()
            .ToList();

        foreach (var application in applications)
            ApplicationLatestProgressSyncHelper.Sync(application, ObjectSpace);
    }

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object is ApplicationProgress progress)
        {
            progress.Application?.InvalidateListViewDisplayCache();
            RefreshAppearance();
            return;
        }

        if (e.Object is Application)
            RefreshAppearance();
    }

    private void RefreshAppearance()
    {
        if (Frame.View is ListView { ObjectTypeInfo.Type: var type } listView)
        {
            if (type == typeof(Application))
            {
                foreach (var application in listView.CollectionSource.List.OfType<Application>())
                    application.InvalidateListViewDisplayCache();
            }
            else if (type == typeof(ApplicationItem))
            {
                foreach (var application in listView.CollectionSource.List.OfType<ApplicationItem>()
                             .Select(item => item.Application)
                             .Where(application => application != null)
                             .Distinct())
                {
                    application!.InvalidateListViewDisplayCache();
                }
            }
        }

        Frame.GetController<AppearanceController>()?.Refresh();

        if (Frame.View is DetailView detailView
            && detailView.CurrentObject is BusinessObjects.Application)
        {
            detailView.Refresh();
        }
        else if (Frame.View is ListView { ObjectTypeInfo.Type: var itemListType }
                 && itemListType == typeof(ApplicationItem))
        {
            Frame.View.Refresh();
        }
    }
}
