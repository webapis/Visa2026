using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Resolves the parent <see cref="Application"/> when working from an <see cref="ApplicationProfileInstanceProgress"/> ListView.
/// </summary>
public static class ApplicationProfileInstanceProgressParentContext
{
    internal static bool TryGetApplication(
        Frame? frame,
        IObjectSpace objectSpace,
        View? view,
        out ApplicationProfileInstance? application)
    {
        application = null;
        if (objectSpace == null)
            return false;

        if (frame is NestedFrame nestedFrame)
        {
            var nestedApplication = nestedFrame.ViewItem?.CurrentObject as ApplicationProfileInstance
                ?? nestedFrame.ViewItem?.View?.CurrentObject as ApplicationProfileInstance;
            if (BringIntoObjectSpace(objectSpace, nestedApplication) is { } fromNested)
            {
                application = fromNested;
                return true;
            }
        }

        if (objectSpace.Owner is Link link
            && link.ListView?.CollectionSource is PropertyCollectionSource pcs
            && BringIntoObjectSpace(objectSpace, pcs.MasterObject as ApplicationProfileInstance) is { } fromLink)
        {
            application = fromLink;
            return true;
        }

        if (view is ListView listView)
        {
            var progress = listView.SelectedObjects?.OfType<ApplicationProfileInstanceProgress>().FirstOrDefault()
                ?? listView.CurrentObject as ApplicationProfileInstanceProgress;
            if (BringIntoObjectSpace(objectSpace, progress?.ApplicationProfileInstance) is { } fromProgress)
            {
                application = fromProgress;
                return true;
            }
        }

        return false;
    }

    public static Guid GetApplicationProfileInstanceId(Frame? frame, IObjectSpace objectSpace, View? view)
    {
        if (!TryGetApplication(frame, objectSpace, view, out var application) || application == null)
            return Guid.Empty;

        var key = objectSpace.GetKeyValue(application);
        return key is Guid guid ? guid : Guid.Empty;
    }

    private static ApplicationProfileInstance? BringIntoObjectSpace(IObjectSpace objectSpace, ApplicationProfileInstance? source)
    {
        if (source == null)
            return null;

        return objectSpace.IsNewObject(source)
            ? source
            : objectSpace.GetObjectByKey<ApplicationProfileInstance>(source.ID);
    }
}
