using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selectors on scalar <see cref="Person"/> detail fields for DevTools / Playwright
/// on typed detail views (Employee, Family member, Temporary visitor).
/// </summary>
public sealed class PersonDetailViewE2eSelectorsController : ViewController<DetailView>
{
    public PersonDetailViewE2eSelectorsController()
    {
        TargetObjectType = typeof(Person);
        TargetViewId =
            $"{PersonDetailViewIds.Employee};{PersonDetailViewIds.FamilyMember};{PersonDetailViewIds.TemporaryVisitor}";
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.CustomizeViewItemControl<PropertyEditor>(this, ApplyPersonFieldSelectors);
        ApplyAllMemberSelectors();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyAllMemberSelectors();
    }

    private void ApplyAllMemberSelectors()
    {
        foreach (string memberName in PersonE2eMemberHooks.GetScalarMembersForDetailView(View?.Id))
        {
            ApplyMemberSelector(memberName);
        }
    }

    private void ApplyMemberSelector(string memberName)
    {
        if (View.FindItem(memberName) is BlazorPropertyEditorBase editor)
        {
            E2ePropertySelectorApplicator.Apply(editor, PersonE2eMemberHooks.TestId(memberName));
        }
    }

    private void ApplyPersonFieldSelectors(PropertyEditor editor)
    {
        if (editor is not BlazorPropertyEditorBase blazorEditor)
        {
            return;
        }

        if (!PersonE2eMemberHooks.IsScalarDetailMember(blazorEditor.PropertyName))
        {
            return;
        }

        IReadOnlyList<string> membersForView = PersonE2eMemberHooks.GetScalarMembersForDetailView(View?.Id);
        if (!membersForView.Contains(blazorEditor.PropertyName))
        {
            return;
        }

        E2ePropertySelectorApplicator.Apply(
            blazorEditor,
            PersonE2eMemberHooks.TestId(blazorEditor.PropertyName));
    }
}
