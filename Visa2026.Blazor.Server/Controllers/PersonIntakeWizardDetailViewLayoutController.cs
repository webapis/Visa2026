using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Layout;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Keeps the intake wizard editor visible on Employee / Family member intake detail views.
/// </summary>
public sealed class PersonIntakeWizardDetailViewLayoutController : ObjectViewController<DetailView, Person>
{
    public PersonIntakeWizardDetailViewLayoutController()
    {
        TargetViewId = PersonDetailViewIds.EmployeeIntake + ";" + PersonDetailViewIds.FamilyMemberIntake;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated += OnLayoutItemCreated;

        ShowWizardEditor();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ShowWizardEditor();
    }

    protected override void OnDeactivated()
    {
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated -= OnLayoutItemCreated;

        base.OnDeactivated();
    }

    private void OnLayoutItemCreated(object? sender, BlazorLayoutManager.ItemCreatedEventArgs e)
    {
        if (string.Equals(e.ModelLayoutElement?.Id, nameof(Person.PersonIntakeWizardUi), StringComparison.Ordinal)
            || string.Equals(e.ModelLayoutElement?.Id, "PersonIntakeWizardHost", StringComparison.Ordinal)
            || string.Equals(e.ModelLayoutElement?.Id, "Main", StringComparison.Ordinal)
            || string.Equals(e.ModelLayoutElement?.Id, "SimpleEditors", StringComparison.Ordinal))
        {
            var visible = e.LayoutControlItem?.GetType().GetProperty("Visible");
            visible?.SetValue(e.LayoutControlItem, true);
        }
    }

    private void ShowWizardEditor()
    {
        if (View.FindItem(nameof(Person.PersonIntakeWizardUi)) is IAppearanceVisibility editor)
            editor.Visibility = ViewItemVisibility.Show;
    }
}