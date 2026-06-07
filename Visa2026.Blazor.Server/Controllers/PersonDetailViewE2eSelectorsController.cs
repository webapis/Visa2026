using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selectors on scalar <see cref="Person"/> detail fields for DevTools / Playwright.
/// </summary>
public sealed class PersonDetailViewE2eSelectorsController : ViewController<DetailView>
{
    public PersonDetailViewE2eSelectorsController()
    {
        TargetObjectType = typeof(Person);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        View.CustomizeViewItemControl<PropertyEditor>(this, ApplyPersonFieldSelectors);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        foreach (string memberName in PersonE2eMemberHooks.ScalarDetailMembers)
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

    private static void ApplyPersonFieldSelectors(PropertyEditor editor)
    {
        if (editor is not BlazorPropertyEditorBase blazorEditor)
        {
            return;
        }

        if (!PersonE2eMemberHooks.IsScalarDetailMember(blazorEditor.PropertyName))
        {
            return;
        }

        E2ePropertySelectorApplicator.Apply(
            blazorEditor,
            PersonE2eMemberHooks.TestId(blazorEditor.PropertyName));
    }
}
