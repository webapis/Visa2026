using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Blocks configuration edits on locked <see cref="ApplicationProfile"/> rows and nested templates.
/// Approval-leg versions and legs may still change (instances keep a snapshot).
/// </summary>
internal static class ApplicationProfileConfigLockObjectSpaceHooks
{
    internal static void Subscribe(IObjectSpace objectSpace)
    {
        objectSpace.ObjectSaving += OnObjectSaving;
        objectSpace.Disposed += OnDisposed;
    }

    private static void Unsubscribe(IObjectSpace objectSpace)
    {
        objectSpace.ObjectSaving -= OnObjectSaving;
        objectSpace.Disposed -= OnDisposed;
    }

    private static void OnDisposed(object? sender, EventArgs e)
    {
        if (sender is IObjectSpace objectSpace)
            Unsubscribe(objectSpace);
    }

    private static void OnObjectSaving(object? sender, ObjectManipulatingEventArgs e)
    {
        if (sender is not IObjectSpace objectSpace)
            return;

        switch (e.Object)
        {
            case ApplicationProfile profile:
                ApplicationProfileLockHelper.EnsureConfigurationEditable(profile, objectSpace);
                break;
            case ApplicationProfileApprovalLegVersion version:
            {
                var parent = ApplicationProfileLockHelper.TryResolveOwningProfile(version, objectSpace);
                if (parent != null && objectSpace.IsObjectToDelete(version))
                    ApplicationProfileLockHelper.EnsureCanRemoveApprovalLegVersion(parent, version, objectSpace);
                break;
            }
            case ApplicationProfileApprovalLeg:
                break;
            case ApplicationProfileTemplate
                or ApplicationProfileProgressStateSetting:
            {
                var parent = ApplicationProfileLockHelper.TryResolveOwningProfile(e.Object, objectSpace);
                if (parent != null)
                    ApplicationProfileLockHelper.EnsureNestedConfigurationEditable(parent, objectSpace, e.Object);
                break;
            }
        }
    }
}
