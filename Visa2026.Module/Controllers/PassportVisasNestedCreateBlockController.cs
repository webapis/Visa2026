using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Blocks nested <c>New</c> on <see cref="Passport.Visas"/> — issued visas must be created from
/// <see cref="ApplicationProfileInstance"/> Issued records (sets <see cref="Visa.IssuingApplicationProfileInstance"/>).
/// </summary>
public sealed class PassportVisasNestedCreateBlockController : ViewController<ListView>
{
    public PassportVisasNestedCreateBlockController()
    {
        TargetViewNesting = Nesting.Nested;
        TargetObjectType = typeof(Visa);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (!IsPassportVisasNestedList())
            return;

        View.AllowNew.SetItemValue(nameof(PassportVisasNestedCreateBlockController), false);

        Frame.GetController<NewObjectViewController>()?
            .NewObjectAction.Active.SetItemValue(nameof(PassportVisasNestedCreateBlockController), false);
    }

    protected override void OnDeactivated()
    {
        View.AllowNew.RemoveItem(nameof(PassportVisasNestedCreateBlockController));

        Frame.GetController<NewObjectViewController>()?
            .NewObjectAction.Active.RemoveItem(nameof(PassportVisasNestedCreateBlockController));

        base.OnDeactivated();
    }

    private bool IsPassportVisasNestedList()
    {
        if (View?.CollectionSource is not PropertyCollectionSource pcs)
            return false;

        if (pcs.MasterObject is not Passport)
            return false;

        return string.Equals(pcs.MemberInfo?.Name, nameof(Passport.Visas), StringComparison.Ordinal);
    }
}
