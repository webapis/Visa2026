using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Blocks root-list <c>New</c> on <see cref="WorkPermit"/> — issued work permits must be created from
/// <see cref="ApplicationProfileInstance"/> Issued records (sets <see cref="WorkPermit.ApplicationProfileInstance"/>).
/// </summary>
public sealed class WorkPermitStandaloneCreateBlockController : ViewController<ListView>
{
    public WorkPermitStandaloneCreateBlockController()
    {
        TargetObjectType = typeof(WorkPermit);
        TargetViewNesting = Nesting.Root;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.AllowNew.SetItemValue(nameof(WorkPermitStandaloneCreateBlockController), false);

        Frame.GetController<NewObjectViewController>()?
            .NewObjectAction.Active.SetItemValue(nameof(WorkPermitStandaloneCreateBlockController), false);
    }

    protected override void OnDeactivated()
    {
        View.AllowNew.RemoveItem(nameof(WorkPermitStandaloneCreateBlockController));

        Frame.GetController<NewObjectViewController>()?
            .NewObjectAction.Active.RemoveItem(nameof(WorkPermitStandaloneCreateBlockController));

        base.OnDeactivated();
    }
}
