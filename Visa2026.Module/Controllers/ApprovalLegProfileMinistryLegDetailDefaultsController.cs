using System.ComponentModel;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Wires a <see cref="ApprovalLegProfileMinistryLeg"/> to its parent contract when opened or saved
/// from the nested ministry-legs list (Blazor NestedFrame or legacy Link).
/// </summary>
public sealed class ApprovalLegProfileMinistryLegDetailDefaultsController
    : ObjectViewController<DetailView, ApprovalLegProfileMinistryLeg>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectSaving += ObjectSpace_ObjectSaving;
        ObjectSpace.Committing += ObjectSpace_Committing;
        WireParentIfNeeded();
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectSaving -= ObjectSpace_ObjectSaving;
        ObjectSpace.Committing -= ObjectSpace_Committing;
        base.OnDeactivated();
    }

    private void ObjectSpace_ObjectSaving(object? sender, ObjectManipulatingEventArgs e)
    {
        if (ReferenceEquals(e.Object, ViewCurrentObject))
            WireParentIfNeeded();
    }

    private void ObjectSpace_Committing(object? sender, CancelEventArgs e)
    {
        var leg = ViewCurrentObject;
        if (leg == null)
            return;

        WireParentIfNeeded();

        if (ApprovalLegProfileMinistryHelper.IsLegCommitRedirectInProgress
            || !ApprovalLegProfileMinistryHelper.ShouldPrepareLegsOnCommit(ObjectSpace))
        {
            return;
        }

        if (ApprovalLegProfileMinistryHelper.TryCommitParentWithLeg(ObjectSpace, leg, e))
            return;

        ApprovalLegProfileMinistryHelper.TryFinalizeLegCommit(ObjectSpace, e);
    }

    private void WireParentIfNeeded()
    {
        var leg = ViewCurrentObject;
        if (leg == null)
            return;

        if (ApprovalLegProfileMinistryHelper.TryAttachLegToParent(ObjectSpace, leg))
            return;

        if (TryResolveParentContract(out var contract) && contract != null)
        {
            var parentSpace = ObjectSpaceHelper.ResolveObjectSpace(ObjectSpace, contract);
            var contractInTarget = parentSpace.GetObject(contract) as ApprovalLegProfile
                ?? (parentSpace.IsNewObject(contract) ? contract : null)
                ?? (contract.ID != Guid.Empty ? parentSpace.GetObjectByKey<ApprovalLegProfile>(contract.ID) : null)
                ?? contract;
            ApprovalLegProfileMinistryHelper.EnsureLegInObjectSpace(parentSpace, contractInTarget, leg);
        }
    }

    private bool TryResolveParentContract(out ApprovalLegProfile? contract)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(ObjectSpace) ?? ObjectSpace;
        if (ApprovalLegProfileMinistryLegCreationContext.TryGetApprovalLegProfile(Frame, rootObjectSpace, out contract)
            && contract != null)
        {
            return true;
        }

        if (ApprovalLegProfileMinistryLegCreationContext.TryGetApprovalLegProfile(Frame, ObjectSpace, out contract)
            && contract != null)
        {
            return true;
        }

        return ApprovalLegProfileMinistryLegCreationContext.TryGetApprovalLegProfileFromMainWindow(
            Application,
            rootObjectSpace,
            out contract);
    }
}
