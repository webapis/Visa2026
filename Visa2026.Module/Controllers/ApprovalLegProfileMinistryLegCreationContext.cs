using System.Collections.Generic;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Resolves the parent <see cref="ApprovalLegProfile"/> when a ministry leg is created or saved
/// from a nested list or popup. Blazor uses <see cref="NestedFrame"/>, not only <see cref="Link"/>.
/// </summary>
internal static class ApprovalLegProfileMinistryLegCreationContext
{
    internal static bool TryGetApprovalLegProfile(
        Frame? frame,
        IObjectSpace objectSpace,
        out ApprovalLegProfile? approvalLegProfile)
    {
        approvalLegProfile = null;

        var visitedFrames = new HashSet<Frame>();
        for (var current = frame; current != null; current = GetParentFrame(current))
        {
            if (!visitedFrames.Add(current))
                break;
            if (current is NestedFrame nestedFrame
                && TryResolveFromNestedFrame(nestedFrame, objectSpace, out approvalLegProfile))
            {
                return true;
            }

            if (current.View is DetailView { CurrentObject: ApprovalLegProfile viewContract }
                && TryBringIntoObjectSpace(objectSpace, viewContract, out approvalLegProfile))
            {
                return true;
            }

            if (current.View is ListView { CollectionSource: PropertyCollectionSource framePcs }
                && framePcs.MasterObject is ApprovalLegProfile frameMaster
                && TryBringIntoObjectSpace(objectSpace, frameMaster, out approvalLegProfile))
            {
                return true;
            }
        }

        if (objectSpace.Owner is Link { ListView.CollectionSource: PropertyCollectionSource linkPcs }
            && linkPcs.MasterObject is ApprovalLegProfile linkMaster
            && TryBringIntoObjectSpace(objectSpace, linkMaster, out approvalLegProfile))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Blazor leg popup keeps the parent contract on <see cref="Window.View"/> while the nested frame hosts the leg.
    /// Falls back to searching MDI child windows if MainWindow.View is null.
    /// </summary>
    internal static bool TryGetApprovalLegProfileFromMainWindow(
        XafApplication application,
        IObjectSpace objectSpace,
        out ApprovalLegProfile? approvalLegProfile)
    {
        approvalLegProfile = null;
        if (application.MainWindow?.View is DetailView mainDetail
            && mainDetail.CurrentObject is ApprovalLegProfile mainContract)
        {
            return TryBringIntoObjectSpace(objectSpace, mainContract, out approvalLegProfile);
        }

        // Fallback: search MDI child windows for an open ApprovalLegProfile detail view
        return TryGetApprovalLegProfileFromMdiWindows(application, objectSpace, out approvalLegProfile);
    }

    /// <summary>
    /// Searches MDI child windows to find an open ApprovalLegProfile detail view.
    /// This handles the scenario where a leg is edited from a separate window/tab.
    /// </summary>
    private static bool TryGetApprovalLegProfileFromMdiWindows(
        XafApplication application,
        IObjectSpace objectSpace,
        out ApprovalLegProfile? approvalLegProfile)
    {
        approvalLegProfile = null;

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
                if (viewProperty?.GetValue(childWindow) is DetailView { CurrentObject: ApprovalLegProfile contract }
                    && TryBringIntoObjectSpace(objectSpace, contract, out approvalLegProfile))
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
        out ApprovalLegProfile? approvalLegProfile)
    {
        approvalLegProfile = null;

        if (nestedFrame.ViewItem?.CurrentObject is ApprovalLegProfile itemContract
            && TryBringIntoObjectSpace(objectSpace, itemContract, out approvalLegProfile))
        {
            return true;
        }

        if (nestedFrame.ViewItem?.View is DetailView { CurrentObject: ApprovalLegProfile detailContract }
            && TryBringIntoObjectSpace(objectSpace, detailContract, out approvalLegProfile))
        {
            return true;
        }

        if (nestedFrame.ViewItem?.View is ListView { CollectionSource: PropertyCollectionSource pcs }
            && pcs.MasterObject is ApprovalLegProfile listMaster
            && TryBringIntoObjectSpace(objectSpace, listMaster, out approvalLegProfile))
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
        ApprovalLegProfile source,
        out ApprovalLegProfile? approvalLegProfile)
    {
        approvalLegProfile = objectSpace.GetObject(source) as ApprovalLegProfile;
        if (approvalLegProfile != null)
            return true;

        if (source.ID != Guid.Empty)
        {
            approvalLegProfile = objectSpace.GetObjectByKey<ApprovalLegProfile>(source.ID);
            if (approvalLegProfile != null)
                return true;
        }

        var sourceSpace = ObjectSpaceHelper.Get(source);
        if (sourceSpace != null)
        {
            if (sourceSpace.IsNewObject(source))
            {
                // Unsaved parent in another session — return source; callers resolve the owning space.
                approvalLegProfile = source;
                return true;
            }

            var rootSpace = ObjectSpaceHelper.GetRootObjectSpace(sourceSpace) ?? sourceSpace;
            approvalLegProfile = objectSpace.GetObject(source) as ApprovalLegProfile
                ?? rootSpace.GetObject(source) as ApprovalLegProfile
                ?? sourceSpace.GetObject(source) as ApprovalLegProfile;
            if (approvalLegProfile != null)
                return true;
        }

        if (objectSpace.IsNewObject(source))
        {
            approvalLegProfile = source;
            return true;
        }

        approvalLegProfile = null;
        return false;
    }
}
