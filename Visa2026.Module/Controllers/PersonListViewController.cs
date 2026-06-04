using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Routes Employees / Family members list views to typed Person detail views (non-Blazor hosts).
/// Blazor uses <see cref="PersonListViewBlazorNavigationController"/> for URL-based navigation.
/// </summary>
public sealed class PersonListViewController : ViewController<ListView>
{
    private ListViewProcessCurrentObjectController? _processCurrentObjectController;

    public PersonListViewController()
    {
        TargetViewId = "Person_ListView_Employees;Person_ListView_FamilyMembers;Person_ListView_TemporaryVisitors";
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        PersonDetailViewNavigationContext.SourceListViewIdValue = View.Id;
        View.CreateCustomCurrentObjectDetailView += OnCreateCustomCurrentObjectDetailView;

        _processCurrentObjectController = Frame.GetController<ListViewProcessCurrentObjectController>();
        if (_processCurrentObjectController != null)
        {
            if (_processCurrentObjectController.ProcessCurrentObjectAction != null)
                _processCurrentObjectController.ProcessCurrentObjectAction.Executing += OnProcessCurrentObjectExecuting;
            _processCurrentObjectController.CustomizeShowViewParameters += OnCustomizeShowViewParameters;

            if (Application.GetType().FullName?.Contains("Blazor", StringComparison.Ordinal) != true)
                _processCurrentObjectController.CustomHandleProcessSelectedItem += OnCustomHandleProcessSelectedItem;
        }

        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController != null)
            newObjectController.ObjectCreated += OnObjectCreated;
    }

    private void OnCreateCustomCurrentObjectDetailView(object sender, CreateCustomCurrentObjectDetailViewEventArgs e)
    {
        if (e.ListViewCurrentObject is not Person person)
            return;

        string detailViewId = PersonDetailViewModelHelper.ResolveDetailViewId(
            Application,
            e.ListViewModel?.Id ?? View.Id,
            person);
        if (PersonDetailViewModelHelper.CanUseDetailView(Application, detailViewId))
            e.DetailViewId = detailViewId;
    }

    private void OnCustomHandleProcessSelectedItem(object sender, HandledEventArgs e)
    {
        if (View.CurrentObject is not Person person)
            return;

        PersonDetailViewNavigationContext.SourceListViewIdValue = View.Id;

        string detailViewId = PersonDetailViewModelHelper.ResolveDetailViewId(Application, View.Id, person);
        if (detailViewId == PersonDetailViewIds.Default)
            return;

        if (!PersonDetailViewModelHelper.TryCreateDetailView(
                Application,
                View.ObjectSpace,
                person,
                detailViewId,
                out DetailView? detailView)
            || detailView == null)
        {
            return;
        }

        Application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView),
            new ShowViewSource(Frame, null));
        e.Handled = true;
    }

    private void OnProcessCurrentObjectExecuting(object sender, CancelEventArgs e) =>
        PersonDetailViewNavigationContext.SourceListViewIdValue = View.Id;

    private void OnCustomizeShowViewParameters(object sender, CustomizeShowViewParametersEventArgs e) =>
        TryAssignTypedDetailView(e.ShowViewParameters);

    private bool TryAssignTypedDetailView(ShowViewParameters showViewParameters)
    {
        if (View.CurrentObject is not Person person)
            return false;

        PersonDetailViewNavigationContext.SourceListViewIdValue = View.Id;

        string detailViewId = PersonDetailViewModelHelper.ResolveDetailViewId(Application, View.Id, person);
        if (detailViewId == PersonDetailViewIds.Default)
            return false;

        if (PersonDetailViewModelHelper.TryCreateDetailView(
                Application,
                View.ObjectSpace,
                person,
                detailViewId,
                out DetailView? detailView)
            && detailView != null)
        {
            showViewParameters.CreatedView = detailView;
            return true;
        }

        return false;
    }

    private void OnObjectCreated(object sender, ObjectCreatedEventArgs e)
    {
        if (e.CreatedObject is not Person person)
            return;

        PersonDetailViewNavigationContext.SourceListViewIdValue = View.Id;

        if (View.Id == "Person_ListView_Employees")
        {
            PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);
            VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);
        }
        else if (View.Id == "Person_ListView_FamilyMembers")
        {
            PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);
        }
        else if (View.Id == "Person_ListView_TemporaryVisitors")
        {
            PersonRoleHelper.ApplyRole(person, PersonRecordRole.TemporaryVisitor);
        }
    }

    protected override void OnDeactivated()
    {
        View.CreateCustomCurrentObjectDetailView -= OnCreateCustomCurrentObjectDetailView;

        if (_processCurrentObjectController != null)
        {
            if (_processCurrentObjectController.ProcessCurrentObjectAction != null)
                _processCurrentObjectController.ProcessCurrentObjectAction.Executing -= OnProcessCurrentObjectExecuting;
            _processCurrentObjectController.CustomizeShowViewParameters -= OnCustomizeShowViewParameters;
            _processCurrentObjectController.CustomHandleProcessSelectedItem -= OnCustomHandleProcessSelectedItem;
            _processCurrentObjectController = null;
        }

        var newObjectController = Frame.GetController<NewObjectViewController>();
        if (newObjectController != null)
            newObjectController.ObjectCreated -= OnObjectCreated;

        base.OnDeactivated();
    }
}
