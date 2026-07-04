using System.ComponentModel;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>App-wide object-space hooks for <see cref="ApprovalLegProfileMinistryLeg"/> parent FK wiring.</summary>
internal static class ApprovalLegProfileMinistryLegObjectSpaceHooks
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
            case ApprovalLegProfile contract:
                ApprovalLegProfileMinistryHelper.WireMinistryLegs(contract);
                break;
            case ApprovalLegProfileMinistryLeg leg when sender is IObjectSpace objectSpace:
            {
                if (!ApprovalLegProfileMinistryHelper.ShouldPrepareLegsOnCommit(objectSpace))
                    break;

                var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
                ApprovalLegProfileMinistryHelper.TryAttachLegToParent(objectSpace, leg);
                ApprovalLegProfileMinistryHelper.PrepareLegForSave(objectSpace, rootObjectSpace, leg);
                break;
            }
        }
    }

    private static void OnCommitting(object? sender, CancelEventArgs e)
    {
        if (sender is IObjectSpace objectSpace
            && ApprovalLegProfileMinistryHelper.ShouldPrepareLegsOnCommit(objectSpace))
        {
            ApprovalLegProfileMinistryHelper.PrepareLegsForCommit(objectSpace);
        }
    }
}

