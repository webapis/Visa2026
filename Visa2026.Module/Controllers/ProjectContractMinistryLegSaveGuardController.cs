using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Blocks leg-only save when the parent <see cref="ProjectContract"/> is still unsaved,
/// before EF raises FK_ProjectContractMinistryLegs_ProjectContracts.
/// </summary>
public sealed class ProjectContractMinistryLegSaveGuardController
    : ObjectViewController<DetailView, ProjectContractMinistryLeg>
{
    private SimpleAction? _saveAction;

    protected override void OnActivated()
    {
        base.OnActivated();
        var modificationsController = Frame.GetController<ModificationsController>();
        _saveAction = modificationsController?.SaveAction;
        if (_saveAction != null)
            _saveAction.Executing += SaveAction_Executing;
    }

    protected override void OnDeactivated()
    {
        if (_saveAction != null)
            _saveAction.Executing -= SaveAction_Executing;
        _saveAction = null;
        base.OnDeactivated();
    }

    private void SaveAction_Executing(object? sender, CancelEventArgs e)
    {
        var leg = ViewCurrentObject;
        if (leg == null)
            return;

        WireParentFromFrame(leg);

        if (!ProjectContractMinistryHelper.CanCommitLeg(ObjectSpace, leg))
        {
            e.Cancel = true;
            throw new UserFriendlyException(VisaUiMessages.Get("ProjectContract.SaveBeforeMinistryLeg"));
        }
    }

    private void WireParentFromFrame(ProjectContractMinistryLeg leg)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(ObjectSpace) ?? ObjectSpace;
        if (TryResolveAndAttach(ObjectSpace, rootObjectSpace, leg, Frame))
            return;

        if (TryResolveAndAttach(ObjectSpace, rootObjectSpace, leg, Application.MainWindow))
            return;

        ProjectContractMinistryLegCreationContext.TryGetProjectContractFromMainWindow(
            Application,
            rootObjectSpace,
            out var mainContract);
        if (mainContract != null)
            AttachLegInSpace(rootObjectSpace, leg, mainContract);
    }

    private bool TryResolveAndAttach(
        IObjectSpace legObjectSpace,
        IObjectSpace rootObjectSpace,
        ProjectContractMinistryLeg leg,
        Frame? frame)
    {
        if (!ProjectContractMinistryLegCreationContext.TryGetProjectContract(frame, rootObjectSpace, out var contract)
            || contract == null)
        {
            if (!ProjectContractMinistryLegCreationContext.TryGetProjectContract(frame, legObjectSpace, out contract)
                || contract == null)
            {
                return false;
            }
        }

        AttachLegInSpace(ObjectSpaceHelper.ResolveObjectSpace(legObjectSpace, contract), leg, contract);
        return true;
    }

    private static void AttachLegInSpace(
        IObjectSpace objectSpace,
        ProjectContractMinistryLeg leg,
        ProjectContract contract)
    {
        var targetSpace = ObjectSpaceHelper.ResolveObjectSpace(objectSpace, contract);
        var contractInTarget = targetSpace.IsNewObject(contract)
            ? targetSpace.GetObject(contract) as ProjectContract ?? contract
            : targetSpace.GetObjectByKey<ProjectContract>(contract.ID) ?? contract;
        var legInTarget = targetSpace.GetObject(leg) as ProjectContractMinistryLeg ?? leg;
        ProjectContractMinistryHelper.AttachLegToContract(contractInTarget, legInTarget, targetSpace);
    }
}
