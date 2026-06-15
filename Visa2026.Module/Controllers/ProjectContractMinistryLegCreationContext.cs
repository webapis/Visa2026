using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Resolves the parent <see cref="ProjectContract"/> when a ministry leg is created or saved
/// from a nested list or popup. Blazor uses <see cref="NestedFrame"/>, not only <see cref="Link"/>.
/// </summary>
internal static class ProjectContractMinistryLegCreationContext
{
    internal static bool TryGetProjectContract(
        Frame? frame,
        IObjectSpace objectSpace,
        out ProjectContract? projectContract)
    {
        projectContract = null;

        if (frame is NestedFrame nestedFrame
            && TryResolveFromNestedFrame(nestedFrame, objectSpace, out projectContract))
        {
            return true;
        }

        if (frame?.View is DetailView { CurrentObject: ProjectContract viewContract }
            && TryBringIntoObjectSpace(objectSpace, viewContract, out projectContract))
        {
            return true;
        }

        if (frame?.View is ListView { CollectionSource: PropertyCollectionSource framePcs }
            && framePcs.MasterObject is ProjectContract frameMaster
            && TryBringIntoObjectSpace(objectSpace, frameMaster, out projectContract))
        {
            return true;
        }

        if (objectSpace.Owner is Link { ListView.CollectionSource: PropertyCollectionSource linkPcs }
            && linkPcs.MasterObject is ProjectContract linkMaster
            && TryBringIntoObjectSpace(objectSpace, linkMaster, out projectContract))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Blazor leg popup keeps the parent contract on <see cref="Window.View"/> while the nested frame hosts the leg.
    /// </summary>
    internal static bool TryGetProjectContractFromMainWindow(
        XafApplication application,
        IObjectSpace objectSpace,
        out ProjectContract? projectContract)
    {
        projectContract = null;
        if (application.MainWindow?.View is not DetailView mainDetail
            || mainDetail.CurrentObject is not ProjectContract mainContract)
        {
            return false;
        }

        return TryBringIntoObjectSpace(objectSpace, mainContract, out projectContract);
    }

    private static bool TryResolveFromNestedFrame(
        NestedFrame nestedFrame,
        IObjectSpace objectSpace,
        out ProjectContract? projectContract)
    {
        projectContract = null;

        if (nestedFrame.ViewItem?.CurrentObject is ProjectContract itemContract
            && TryBringIntoObjectSpace(objectSpace, itemContract, out projectContract))
        {
            return true;
        }

        if (nestedFrame.ViewItem?.View is DetailView { CurrentObject: ProjectContract detailContract }
            && TryBringIntoObjectSpace(objectSpace, detailContract, out projectContract))
        {
            return true;
        }

        if (nestedFrame.ViewItem?.View is ListView { CollectionSource: PropertyCollectionSource pcs }
            && pcs.MasterObject is ProjectContract listMaster
            && TryBringIntoObjectSpace(objectSpace, listMaster, out projectContract))
        {
            return true;
        }

        return false;
    }

    private static bool TryBringIntoObjectSpace(
        IObjectSpace objectSpace,
        ProjectContract source,
        out ProjectContract? projectContract)
    {
        projectContract =
            objectSpace.GetObject(source) as ProjectContract
            ?? (source.ID != Guid.Empty ? objectSpace.GetObjectByKey<ProjectContract>(source.ID) : null)
            ?? (objectSpace.IsNewObject(source) ? source : null);

        return projectContract != null;
    }
}
