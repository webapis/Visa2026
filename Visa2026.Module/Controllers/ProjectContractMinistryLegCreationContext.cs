using System.Collections.Generic;
using System.Reflection;
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

        var visitedFrames = new HashSet<Frame>();
        for (var current = frame; current != null; current = GetParentFrame(current))
        {
            if (!visitedFrames.Add(current))
                break;
            if (current is NestedFrame nestedFrame
                && TryResolveFromNestedFrame(nestedFrame, objectSpace, out projectContract))
            {
                return true;
            }

            if (current.View is DetailView { CurrentObject: ProjectContract viewContract }
                && TryBringIntoObjectSpace(objectSpace, viewContract, out projectContract))
            {
                return true;
            }

            if (current.View is ListView { CollectionSource: PropertyCollectionSource framePcs }
                && framePcs.MasterObject is ProjectContract frameMaster
                && TryBringIntoObjectSpace(objectSpace, frameMaster, out projectContract))
            {
                return true;
            }
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
    /// Falls back to searching MDI child windows if MainWindow.View is null.
    /// </summary>
    internal static bool TryGetProjectContractFromMainWindow(
        XafApplication application,
        IObjectSpace objectSpace,
        out ProjectContract? projectContract)
    {
        projectContract = null;
        if (application.MainWindow?.View is DetailView mainDetail
            && mainDetail.CurrentObject is ProjectContract mainContract)
        {
            return TryBringIntoObjectSpace(objectSpace, mainContract, out projectContract);
        }

        // Fallback: search MDI child windows for an open ProjectContract detail view
        return TryGetProjectContractFromMdiWindows(application, objectSpace, out projectContract);
    }

    /// <summary>
    /// Searches MDI child windows to find an open ProjectContract detail view.
    /// This handles the scenario where a leg is edited from a separate window/tab.
    /// </summary>
    private static bool TryGetProjectContractFromMdiWindows(
        XafApplication application,
        IObjectSpace objectSpace,
        out ProjectContract? projectContract)
    {
        projectContract = null;

        if (application.MainWindow == null)
            return false;

        // Try to access MdiChildWindows using reflection (Blazor-specific)
        var mdiChildWindowsProperty = application.MainWindow.GetType().GetProperty("MdiChildWindows");
        if (mdiChildWindowsProperty?.GetValue(application.MainWindow) is System.Collections.IEnumerable mdiWindows)
        {
            foreach (var childWindow in mdiWindows)
            {
                if (childWindow == null)
                    continue;

                var viewProperty = childWindow.GetType().GetProperty("View");
                if (viewProperty?.GetValue(childWindow) is DetailView { CurrentObject: ProjectContract contract }
                    && TryBringIntoObjectSpace(objectSpace, contract, out projectContract))
                {
                    return true;
                }
            }
        }

        return false;
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

    private static Frame? GetParentFrame(Frame frame)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var propertyName in new[] { "Parent", "TemplateFrame", "ParentFrame" })
        {
            var property = frame.GetType().GetProperty(propertyName, flags);
            if (property?.GetValue(frame) is Frame parent && !ReferenceEquals(parent, frame))
                return parent;
        }

        return null;
    }

    private static bool TryBringIntoObjectSpace(
        IObjectSpace objectSpace,
        ProjectContract source,
        out ProjectContract? projectContract)
    {
        projectContract = objectSpace.GetObject(source) as ProjectContract;
        if (projectContract != null)
            return true;

        if (source.ID != Guid.Empty)
        {
            projectContract = objectSpace.GetObjectByKey<ProjectContract>(source.ID);
            if (projectContract != null)
                return true;
        }

        var sourceSpace = ObjectSpaceHelper.Get(source);
        if (sourceSpace != null)
        {
            if (sourceSpace.IsNewObject(source))
            {
                // Unsaved parent in another session — return source; callers resolve the owning space.
                projectContract = source;
                return true;
            }

            var rootSpace = ObjectSpaceHelper.GetRootObjectSpace(sourceSpace) ?? sourceSpace;
            projectContract = objectSpace.GetObject(source) as ProjectContract
                ?? rootSpace.GetObject(source) as ProjectContract
                ?? sourceSpace.GetObject(source) as ProjectContract;
            if (projectContract != null)
                return true;
        }

        if (objectSpace.IsNewObject(source))
        {
            projectContract = source;
            return true;
        }

        projectContract = null;
        return false;
    }
}
