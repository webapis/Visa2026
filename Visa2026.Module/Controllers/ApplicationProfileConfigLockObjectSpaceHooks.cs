using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>Blocks configuration edits on locked <see cref="ApplicationProfile"/> rows and nested legs/templates.</summary>
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
            case ApplicationProfileApprovalLegVersion
                or ApplicationProfileApprovalLeg
                or ApplicationProfileTemplate
                or ApplicationProfileProgressStateSetting:
            {
                var parent = ApplicationProfileLockHelper.TryResolveOwningProfile(e.Object, objectSpace);
                if (parent != null && objectSpace.IsObjectToDelete(e.Object))
                {
                    ApplicationProfileLockHelper.EnsureNestedConfigurationEditable(parent, objectSpace);
                }
                else if (parent != null && !objectSpace.IsNewObject(e.Object))
                {
                    ApplicationProfileLockHelper.EnsureNestedConfigurationEditable(parent, objectSpace);
                }
                else if (parent != null && objectSpace.IsNewObject(e.Object))
                {
                    ApplicationProfileLockHelper.EnsureNestedConfigurationEditable(parent, objectSpace);
                }

                break;
            }
        }
    }
}
