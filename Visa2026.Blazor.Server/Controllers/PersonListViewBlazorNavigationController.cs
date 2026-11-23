using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonIntake;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Blazor list row open: Employees / Family members open the intake wizard (create and edit).
/// Temporary visitors keep the native typed Person DetailView.
/// </summary>
public sealed class PersonListViewBlazorNavigationController : ViewController<ListView>
{
    private ListViewProcessCurrentObjectController? _processCurrentObjectController;

    public PersonListViewBlazorNavigationController()
    {
        TargetViewId = PersonRosterListViewIds.Employees + ";" + PersonRosterListViewIds.EmployeesIncomplete + ";" + PersonRosterListViewIds.FamilyMembers + ";" + PersonRosterListViewIds.FamilyMembersIncomplete + ";" + PersonRosterListViewIds.TemporaryVisitors;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _processCurrentObjectController = Frame.GetController<ListViewProcessCurrentObjectController>();
        if (_processCurrentObjectController != null)
            _processCurrentObjectController.CustomHandleProcessSelectedItem += OnCustomHandleProcessSelectedItem;
    }

    protected override void OnDeactivated()
    {
        if (_processCurrentObjectController != null)
        {
            _processCurrentObjectController.CustomHandleProcessSelectedItem -= OnCustomHandleProcessSelectedItem;
            _processCurrentObjectController = null;
        }

        base.OnDeactivated();
    }

    private void OnCustomHandleProcessSelectedItem(object? sender, HandledEventArgs e)
    {
        if (View.CurrentObject is not Person person)
            return;

        if (View.ObjectSpace.IsNewObject(person))
            return;

        if (PersonRosterListViewIds.IsIntakeWizardList(View.Id))
        {
            var wizard = PersonIntakeWizardAccess.IsWizardEnabled(View.ObjectSpace)
                ? PersonIntakeWizardOpenHelper.OpenExisting(
                    Application,
                    person,
                    openedFromIncompleteList: PersonRosterListViewIds.IsIncompleteRoster(View.Id))
                : PersonIntakeWizardOpenHelper.OpenNativeExisting(Application, person);
            if (wizard == null)
                return;

            e.Handled = true;
            Application.ShowViewStrategy.ShowView(
                new ShowViewParameters(wizard)
                {
                    TargetWindow = TargetWindow.Current
                },
                new ShowViewSource(Frame, null));
            return;
        }

        // Temporary visitors: native typed DetailView (unchanged).
        PersonDetailViewNavigationContext.SourceListViewIdValue = View.Id;

        string detailViewId = PersonDetailViewModelHelper.ResolveDetailViewId(Application, View.Id, person);
        if (detailViewId == PersonDetailViewIds.Default)
            return;

        if (Application is not DevExpress.ExpressApp.Blazor.BlazorApplication blazorApplication)
            return;

        var navigationManager = blazorApplication.ServiceProvider?.GetService<Microsoft.AspNetCore.Components.NavigationManager>();
        if (navigationManager == null)
            return;

        string key = View.ObjectSpace.GetKeyValueAsString(person);
        if (string.IsNullOrEmpty(key))
            return;

        navigationManager.NavigateTo("/" + detailViewId + "/" + key);
        e.Handled = true;
    }
}