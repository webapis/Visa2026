using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Ministry leg popup Save must not commit the leg alone when the parent
/// <see cref="ApprovalLegProfile"/> is still new — commit the parent session instead.
/// </summary>
public sealed class ApprovalLegProfileMinistryLegSaveGuardController
    : ObjectViewController<DetailView, ApprovalLegProfileMinistryLeg>
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

    private void WireParentFromFrame(ApprovalLegProfileMinistryLeg leg)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(ObjectSpace) ?? ObjectSpace;

        if (TryResolveAndAttach(ObjectSpace, leg, Frame))
            return;

        if (TryResolveAndAttach(ObjectSpace, leg, Application.MainWindow))
            return;

        if (ApprovalLegProfileMinistryLegCreationContext.TryGetApprovalLegProfileFromMainWindow(
                Application,
                rootObjectSpace,
                out var mainContract)
            && mainContract != null)
        {
            AttachLegInSpace(ObjectSpace, leg, mainContract);
        }
    }

    private bool TryResolveAndAttach(IObjectSpace legObjectSpace, ApprovalLegProfileMinistryLeg leg, Frame? frame)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(legObjectSpace) ?? legObjectSpace;
        if (!ApprovalLegProfileMinistryLegCreationContext.TryGetApprovalLegProfile(frame, rootObjectSpace, out var contract)
            || contract == null)
        {
            if (!ApprovalLegProfileMinistryLegCreationContext.TryGetApprovalLegProfile(frame, legObjectSpace, out contract)
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
        ApprovalLegProfileMinistryLeg leg,
        ApprovalLegProfile contract)
    {
        var parentSpace = ObjectSpaceHelper.ResolveObjectSpace(legObjectSpace, contract);
        var contractInTarget = parentSpace.GetObject(contract) as ApprovalLegProfile
            ?? (parentSpace.IsNewObject(contract) ? contract : null)
            ?? (contract.ID != Guid.Empty ? parentSpace.GetObjectByKey<ApprovalLegProfile>(contract.ID) : null)
            ?? contract;
        ApprovalLegProfileMinistryHelper.EnsureLegInObjectSpace(parentSpace, contractInTarget, leg);
    }
}
