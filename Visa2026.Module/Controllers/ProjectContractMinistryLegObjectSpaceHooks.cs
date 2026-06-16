using System.ComponentModel;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>App-wide object-space hooks for <see cref="ProjectContractMinistryLeg"/> parent FK wiring.</summary>
internal static class ProjectContractMinistryLegObjectSpaceHooks
{
    internal static void Subscribe(IObjectSpace objectSpace)
    {
        objectSpace.ObjectSaving += OnObjectSaving;
        objectSpace.Committing += OnCommitting;
        objectSpace.Disposed += OnDisposed;
    }

    private static void Unsubscribe(IObjectSpace objectSpace)
    {
        objectSpace.ObjectSaving -= OnObjectSaving;
        objectSpace.Committing -= OnCommitting;
        objectSpace.Disposed -= OnDisposed;
    }

    private static void OnDisposed(object? sender, EventArgs e)
    {
        if (sender is IObjectSpace objectSpace)
            Unsubscribe(objectSpace);
    }

    private static void OnObjectSaving(object? sender, ObjectManipulatingEventArgs e)
    {
        switch (e.Object)
        {
            case ProjectContract contract:
                ProjectContractMinistryHelper.WireMinistryLegs(contract);
                break;
            case ProjectContractMinistryLeg leg when sender is IObjectSpace objectSpace:
            {
                var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
                ProjectContractMinistryHelper.TryAttachLegToParent(objectSpace, leg);
                ProjectContractMinistryHelper.PrepareLegForSave(objectSpace, rootObjectSpace, leg);
                break;
            }
        }
    }

    private static void OnCommitting(object? sender, CancelEventArgs e)
    {
        if (sender is IObjectSpace objectSpace)
            ProjectContractMinistryHelper.PrepareLegsForCommit(objectSpace);
    }
}

