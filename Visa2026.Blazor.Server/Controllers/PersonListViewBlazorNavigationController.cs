using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Components;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Blazor list row navigation: route URL drives which Person detail view opens
/// (<c>Person_DetailView_Employee</c> / <c>Person_DetailView_FamilyMember</c>).
/// </summary>
public sealed class PersonListViewBlazorNavigationController : ViewController<ListView>
{
    private ListViewProcessCurrentObjectController? _processCurrentObjectController;

    public PersonListViewBlazorNavigationController()
    {
        TargetViewId = "Person_ListView_Employees;Person_ListView_FamilyMembers";
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

        PersonDetailViewNavigationContext.SourceListViewIdValue = View.Id;

        string detailViewId = PersonDetailViewModelHelper.ResolveDetailViewId(Application, View.Id, person);
        if (detailViewId == PersonDetailViewIds.Default)
            return;

        if (Application is not BlazorApplication blazorApplication)
            return;

        NavigationManager? navigationManager = blazorApplication.ServiceProvider?.GetService<NavigationManager>();
        if (navigationManager == null)
            return;

        string key = View.ObjectSpace.GetKeyValueAsString(person);
        if (string.IsNullOrEmpty(key))
            return;

        navigationManager.NavigateTo("/" + detailViewId + "/" + key);
        e.Handled = true;
    }
}
