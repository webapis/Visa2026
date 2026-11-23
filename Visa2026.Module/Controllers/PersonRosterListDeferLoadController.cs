using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Previously kept Employees and Family Members empty until a custom search.
/// Those lists now load as a native XAF ListView, so this controller stays inactive.
/// </summary>
public sealed class PersonRosterListDeferLoadController : ObjectViewController<ListView, Person>
{
    public const string CriteriaKey = "DeferUntilSearch";

    private FilterController? filterController;
    private bool criteriaUpdateOpen;

    public PersonRosterListDeferLoadController()
    {
        Active["NativeRosterList"] = false;
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
