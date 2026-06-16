using System.ComponentModel;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Wires a <see cref="ProjectContractMinistryLeg"/> to its parent contract when opened or saved
/// from the nested ministry-legs list (Blazor NestedFrame or legacy Link).
/// </summary>
public sealed class ProjectContractMinistryLegDetailDefaultsController
    : ObjectViewController<DetailView, ProjectContractMinistryLeg>
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

        if (ProjectContractMinistryHelper.TryCommitParentWithLeg(ObjectSpace, leg, e))
            return;

        ProjectContractMinistryHelper.TryFinalizeLegCommit(ObjectSpace, e);
    }

    private void WireParentIfNeeded()
    {
        var leg = ViewCurrentObject;
        if (leg == null)
            return;

        if (ProjectContractMinistryHelper.TryAttachLegToParent(ObjectSpace, leg))
            return;

        if (TryResolveParentContract(out var contract) && contract != null)
        {
            var parentSpace = ObjectSpaceHelper.ResolveObjectSpace(ObjectSpace, contract);
            var contractInTarget = parentSpace.GetObject(contract) as ProjectContract
                ?? (parentSpace.IsNewObject(contract) ? contract : null)
                ?? (contract.ID != Guid.Empty ? parentSpace.GetObjectByKey<ProjectContract>(contract.ID) : null)
                ?? contract;
            ProjectContractMinistryHelper.EnsureLegInObjectSpace(parentSpace, contractInTarget, leg);
        }
    }

    private bool TryResolveParentContract(out ProjectContract? contract)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(ObjectSpace) ?? ObjectSpace;
        if (ProjectContractMinistryLegCreationContext.TryGetProjectContract(Frame, rootObjectSpace, out contract)
            && contract != null)
        {
            return true;
        }

        if (ProjectContractMinistryLegCreationContext.TryGetProjectContract(Frame, ObjectSpace, out contract)
            && contract != null)
        {
            return true;
        }

        return ProjectContractMinistryLegCreationContext.TryGetProjectContractFromMainWindow(
            Application,
            rootObjectSpace,
            out contract);
    }
}
