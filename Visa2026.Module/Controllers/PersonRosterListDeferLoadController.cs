using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Employees and Family Members open with no rows. The roster is queried only after
/// the officer submits a search, and returns to empty when that search is cleared.
/// Criteria changes around FullTextSearch are batched so clearing the box does not
/// load the full table before the empty filter is applied.
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

        if (string.IsNullOrWhiteSpace(CurrentSearchText()))
            HoldEmpty();
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
        if (string.IsNullOrWhiteSpace(CurrentSearchText()))
            HoldEmpty();
        else
            ReleaseEmpty();
    }

    private void OnSearchFinished(object sender, ActionBaseEventArgs e) => EndCriteriaUpdate();

    private void OnSearchHandleException(object sender, HandleExceptionEventArgs e) => EndCriteriaUpdate();

    private string? CurrentSearchText() => filterController?.FullTextFilterAction?.Value as string;

    private void HoldEmpty()
    {
        if (View?.CollectionSource == null)
            return;

        View.CollectionSource.Criteria[CriteriaKey] = CollectionSourceBase.EmptyCollectionCriteria;
    }

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
