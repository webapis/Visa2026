using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Ministry leg popup Save must not commit the leg alone when the parent
/// <see cref="ProjectContract"/> is still new — commit the parent session instead.
/// </summary>
public sealed class ProjectContractMinistryLegSaveGuardController
    : ObjectViewController<DetailView, ProjectContractMinistryLeg>
{
    private SimpleAction? _saveAction;
    private SimpleAction? _saveAndCloseAction;

    protected override void OnActivated()
    {
        base.OnActivated();
        var modificationsController = Frame.GetController<ModificationsController>();
        _saveAction = modificationsController?.SaveAction;
        if (_saveAction != null)
            _saveAction.Executing += SaveAction_Executing;

        _saveAndCloseAction = modificationsController?.SaveAndCloseAction;
        if (_saveAndCloseAction != null)
            _saveAndCloseAction.Executing += SaveAction_Executing;
    }

    protected override void OnDeactivated()
    {
        if (_saveAction != null)
            _saveAction.Executing -= SaveAction_Executing;
        _saveAction = null;

        if (_saveAndCloseAction != null)
            _saveAndCloseAction.Executing -= SaveAction_Executing;
        _saveAndCloseAction = null;

        base.OnDeactivated();
    }

    private void SaveAction_Executing(object? sender, CancelEventArgs e)
    {
        var leg = ViewCurrentObject;
        if (leg == null)
            return;

        WireParentFromFrame(leg);
    }

    private void WireParentFromFrame(ProjectContractMinistryLeg leg)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(ObjectSpace) ?? ObjectSpace;

        if (TryResolveAndAttach(ObjectSpace, leg, Frame))
            return;

        if (TryResolveAndAttach(ObjectSpace, leg, Application.MainWindow))
            return;

        if (ProjectContractMinistryLegCreationContext.TryGetProjectContractFromMainWindow(
                Application,
                rootObjectSpace,
                out var mainContract)
            && mainContract != null)
        {
            AttachLegInSpace(ObjectSpace, leg, mainContract);
        }
    }

    private bool TryResolveAndAttach(IObjectSpace legObjectSpace, ProjectContractMinistryLeg leg, Frame? frame)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(legObjectSpace) ?? legObjectSpace;
        if (!ProjectContractMinistryLegCreationContext.TryGetProjectContract(frame, rootObjectSpace, out var contract)
            || contract == null)
        {
            if (!ProjectContractMinistryLegCreationContext.TryGetProjectContract(frame, legObjectSpace, out contract)
                || contract == null)
            {
                return false;
            }
        }

        AttachLegInSpace(legObjectSpace, leg, contract);
        return true;
    }

    private static void AttachLegInSpace(
        IObjectSpace legObjectSpace,
        ProjectContractMinistryLeg leg,
        ProjectContract contract)
    {
        var parentSpace = ObjectSpaceHelper.ResolveObjectSpace(legObjectSpace, contract);
        var contractInTarget = parentSpace.GetObject(contract) as ProjectContract
            ?? (parentSpace.IsNewObject(contract) ? contract : null)
            ?? (contract.ID != Guid.Empty ? parentSpace.GetObjectByKey<ProjectContract>(contract.ID) : null)
            ?? contract;
        ProjectContractMinistryHelper.EnsureLegInObjectSpace(parentSpace, contractInTarget, leg);
    }
}
