using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Blocks root-list <c>New</c> on <see cref="Invitation"/> — issued invitations must be created from
/// <see cref="ApplicationProfileInstance"/> Issued records (sets <see cref="Invitation.ApplicationProfileInstance"/>).
/// </summary>
public sealed class InvitationStandaloneCreateBlockController : ViewController<ListView>
{
    public InvitationStandaloneCreateBlockController()
    {
        TargetObjectType = typeof(Invitation);
        TargetViewNesting = Nesting.Root;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.AllowNew.SetItemValue(nameof(InvitationStandaloneCreateBlockController), false);

        Frame.GetController<NewObjectViewController>()?
            .NewObjectAction.Active.SetItemValue(nameof(InvitationStandaloneCreateBlockController), false);
    }

    protected override void OnDeactivated()
    {
        View.AllowNew.RemoveItem(nameof(InvitationStandaloneCreateBlockController));

        Frame.GetController<NewObjectViewController>()?
            .NewObjectAction.Active.RemoveItem(nameof(InvitationStandaloneCreateBlockController));

        base.OnDeactivated();
    }
}
