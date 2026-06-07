using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Experiment: stable selectors on <see cref="Person.FirstName"/> and <see cref="Person.LastName"/>
/// for browser DevTools / Playwright (<c>data-testid</c> + CSS class on the text input).
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
        View.CustomizeViewItemControl<StringPropertyEditor>(this, ApplyPersonNameSelectors);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyMemberSelector(nameof(Person.FirstName), "person-first-name");
        ApplyMemberSelector(nameof(Person.LastName), "person-last-name");
    }

    private void ApplyMemberSelector(string memberName, string testId)
    {
        if (View.FindItem(memberName) is BlazorPropertyEditorBase editor)
        {
            E2eTextEditorSelectorApplicator.Apply(editor, testId);
        }
    }

    private static void ApplyPersonNameSelectors(StringPropertyEditor editor)
    {
        string? testId = editor.PropertyName switch
        {
            nameof(Person.FirstName) => "person-first-name",
            nameof(Person.LastName) => "person-last-name",
            _ => null
        };

        if (testId == null)
        {
            return;
        }

        E2eTextEditorSelectorApplicator.Apply(editor, testId);
    }
}
