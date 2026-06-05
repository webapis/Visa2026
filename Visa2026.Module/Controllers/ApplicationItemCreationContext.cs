using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Resolves the parent <see cref="ApplicationItem"/> when a related object is created from its DetailView
/// (lookup editor New button or nested list). Blazor lookup editors use <see cref="NestedFrame"/>, not <see cref="Link"/>.
/// </summary>
internal static class ApplicationItemCreationContext
{
    internal static bool TryGetApplicationItem(
        Frame? frame,
        IObjectSpace objectSpace,
        out ApplicationItem? applicationItem)
    {
        applicationItem = null;
        if (frame == null)
            return false;

        // Lookup property editor "New" — documented XAF pattern (WinForms / some nested views).
        if (frame is NestedFrame nestedFrame)
        {
            if (TryResolveApplicationItemFromNestedFrame(nestedFrame, out var nestedItem))
            {
                applicationItem = BringIntoObjectSpace(objectSpace, nestedItem);
                return applicationItem != null;
            }
        }

        // Nested aggregated list / legacy Link owner chain.
        if (objectSpace.Owner is Link link
            && link.ListView?.CollectionSource is PropertyCollectionSource pcs
            && pcs.MasterObject is ApplicationItem linkItem)
        {
            applicationItem = BringIntoObjectSpace(objectSpace, linkItem);
            return applicationItem != null;
        }

        return false;
    }

    private static bool TryResolveApplicationItemFromNestedFrame(NestedFrame nestedFrame, out ApplicationItem? applicationItem)
    {
        applicationItem = nestedFrame.ViewItem?.CurrentObject as ApplicationItem
            ?? nestedFrame.ViewItem?.View?.CurrentObject as ApplicationItem;
        return applicationItem != null;
    }

    private static ApplicationItem? BringIntoObjectSpace(IObjectSpace objectSpace, ApplicationItem source)
    {
        if (source == null)
            return null;

        return objectSpace.IsNewObject(source)
            ? source
            : objectSpace.GetObjectByKey<ApplicationItem>(source.ID);
    }
}
