using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selectors on required scalar <see cref="Passport"/> detail fields.
/// </summary>
public sealed class PassportDetailViewE2eSelectorsController : ViewController<DetailView>
{
    public PassportDetailViewE2eSelectorsController()
    {
        TargetObjectType = typeof(Passport);
        TargetViewId = PassportE2eMemberHooks.DetailViewId;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.CustomizeViewItemControl<PropertyEditor>(this, ApplyPassportFieldSelectors);
        ApplyAllMemberSelectors();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyAllMemberSelectors();
    }

    private void ApplyAllMemberSelectors()
    {
        foreach (string memberName in PassportE2eMemberHooks.GetScalarMembers())
        {
            ApplyMemberSelector(memberName);
        }
    }

    private void ApplyMemberSelector(string memberName)
    {
        if (View.FindItem(memberName) is BlazorPropertyEditorBase editor)
        {
            E2ePropertySelectorApplicator.Apply(editor, PassportE2eMemberHooks.TestId(memberName));
        }
    }

    private void ApplyPassportFieldSelectors(PropertyEditor editor)
    {
        if (editor is not BlazorPropertyEditorBase blazorEditor)
        {
            return;
        }

        if (!PassportE2eMemberHooks.IsScalarDetailMember(blazorEditor.PropertyName))
        {
            return;
        }

        E2ePropertySelectorApplicator.Apply(
            blazorEditor,
            PassportE2eMemberHooks.TestId(blazorEditor.PropertyName));
    }
}
