using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Employees and Family Members batch the search criteria with the color-tab filter.
/// The tab controller decides which rows are visible. This controller no longer hides the list.
/// </summary>
public sealed class PersonRosterListDeferLoadController : ObjectViewController<ListView, Person>
{
    public const string CriteriaKey = "DeferUntilSearch";

    private FilterController? filterController;
    private bool criteriaUpdateOpen;

    public PersonRosterListDeferLoadController()
    {
        TargetViewId = "Person_ListView_Employees;Person_ListView_FamilyMembers";
        TargetViewNesting = Nesting.Root;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        filterController = Frame.GetController<FilterController>();
        if (filterController?.FullTextFilterAction != null)
        {
            filterController.FullTextFilterAction.Executing += OnSearchExecuting;
            filterController.FullTextFilterAction.Executed += OnSearchFinished;
            filterController.FullTextFilterAction.ExecuteCanceled += OnSearchFinished;
            filterController.FullTextFilterAction.HandleException += OnSearchHandleException;
        }

        ReleaseEmpty();
    }

    protected override void OnDeactivated()
    {
        EndCriteriaUpdate();
        if (filterController?.FullTextFilterAction != null)
        {
            filterController.FullTextFilterAction.Executing -= OnSearchExecuting;
            filterController.FullTextFilterAction.Executed -= OnSearchFinished;
            filterController.FullTextFilterAction.ExecuteCanceled -= OnSearchFinished;
            filterController.FullTextFilterAction.HandleException -= OnSearchHandleException;
        }

        filterController = null;
        base.OnDeactivated();
    }

    private void OnSearchExecuting(object sender, System.ComponentModel.CancelEventArgs e)
    {
        BeginCriteriaUpdate();
        ReleaseEmpty();
    }

    private void OnSearchFinished(object sender, ActionBaseEventArgs e) => EndCriteriaUpdate();

    private void OnSearchHandleException(object sender, HandleExceptionEventArgs e) => EndCriteriaUpdate();

    private void ReleaseEmpty()
    {
        if (View?.CollectionSource == null)
            return;

        if (View.CollectionSource.Criteria.ContainsKey(CriteriaKey))
            View.CollectionSource.Criteria.Remove(CriteriaKey);
    }

    private void BeginCriteriaUpdate()
    {
        if (criteriaUpdateOpen || View?.CollectionSource == null)
            return;

        View.CollectionSource.BeginUpdateCriteria();
        criteriaUpdateOpen = true;
    }

    private void EndCriteriaUpdate()
    {
        if (!criteriaUpdateOpen || View?.CollectionSource == null)
        {
            criteriaUpdateOpen = false;
            return;
        }

        criteriaUpdateOpen = false;
        View.CollectionSource.EndUpdateCriteria();
    }
}
