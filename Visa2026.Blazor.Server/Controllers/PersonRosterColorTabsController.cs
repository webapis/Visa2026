using DevExpress.Blazor;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Blazor.Server.Components;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// All, Search, and Incomplete above Employees and Family Members.
/// A search selects Search and lists every match. Incomplete shows what is missing.
/// </summary>
public sealed class PersonRosterColorTabsController : ObjectViewController<ListView, Person>
{
    private readonly PersonRosterColorTabsModel strip = new();
    private FilterController? filterController;
    private string selectedKey = PersonRosterColorTab.All;
    private string tabBeforeSearch = PersonRosterColorTab.All;
    private bool searchActive;
    private bool applyingTabChoice;
    private bool populationCounted;
    private int allCount;
    private int incompleteCount;
    private int searchMatchCount;

    public PersonRosterColorTabsController()
    {
        TargetViewId = PersonRosterListViewIds.Employees + ";" + PersonRosterListViewIds.FamilyMembers;
        TargetViewNesting = Nesting.Root;
    }

    public PersonRosterColorTabsModel Strip => strip;

    public bool IsIncompleteTabSelected =>
        string.Equals(selectedKey, PersonRosterColorTab.Incomplete, StringComparison.Ordinal);

    protected override void OnActivated()
    {
        base.OnActivated();
        filterController = Frame.GetController<FilterController>();
        if (filterController?.FullTextFilterAction != null)
            filterController.FullTextFilterAction.Executing += OnSearchExecuting;

        if (View?.CollectionSource != null)
            View.CollectionSource.CollectionReloaded += OnCollectionReloaded;

        strip.SelectedKey = selectedKey;
        strip.OnSelect = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, OnTabSelected);
        ApplyTabCriteria();
        PublishTabs();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (PersonRosterSavedPersonFocus.Pending is { } pending
            && string.Equals(pending.ListViewId, View?.Id, StringComparison.Ordinal))
        {
            PersonRosterSavedPersonFocus.Pending = null;
            RevealPerson(pending.PersonId, pending.Incomplete);
            return;
        }

        ApplyIncompleteColumns();
        PublishTabs();
    }

    /// <summary>
    /// After Save and Close: show this person on All, or on Incomplete when they were saved incomplete.
    /// </summary>
    public void RevealPerson(Guid personId, bool incomplete)
    {
        if (View is not { IsDisposed: false })
            return;

        selectedKey = incomplete ? PersonRosterColorTab.Incomplete : PersonRosterColorTab.All;
        tabBeforeSearch = selectedKey;
        searchActive = false;
        searchMatchCount = 0;
        applyingTabChoice = true;
        try
        {
            Frame.GetController<PersonRosterSearchGateController>()?.ClearSearch();
        }
        finally
        {
            applyingTabChoice = false;
        }

        ApplyTabCriteria();
        ApplyIncompleteColumns();
        FocusPerson(personId);
        PublishTabs();
    }

    protected override void OnDeactivated()
    {
        if (filterController?.FullTextFilterAction != null)
            filterController.FullTextFilterAction.Executing -= OnSearchExecuting;

        if (View?.CollectionSource != null)
            View.CollectionSource.CollectionReloaded -= OnCollectionReloaded;

        filterController = null;
        if (View?.CollectionSource?.Criteria.ContainsKey(PersonRosterColorTab.CriteriaKey) == true)
            View.CollectionSource.Criteria.Remove(PersonRosterColorTab.CriteriaKey);

        base.OnDeactivated();
    }

    private void OnSearchExecuting(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (applyingTabChoice)
        {
            searchActive = false;
            ApplyTabCriteria();
            ApplyIncompleteColumns();
            PublishTabs();
            return;
        }

        var text = filterController?.FullTextFilterAction?.Value as string;
        searchActive = !string.IsNullOrWhiteSpace(text);
        if (searchActive)
        {
            if (!PersonRosterColorTab.IsSearch(selectedKey))
                tabBeforeSearch = selectedKey;
            selectedKey = PersonRosterColorTab.Search;
            searchMatchCount = 0;
        }
        else if (PersonRosterColorTab.IsSearch(selectedKey))
        {
            selectedKey = tabBeforeSearch == PersonRosterColorTab.Incomplete
                ? PersonRosterColorTab.Incomplete
                : PersonRosterColorTab.All;
            searchMatchCount = 0;
        }

        ApplyTabCriteria();
        ApplyIncompleteColumns();
        PublishTabs();
    }

    private void OnTabSelected(string key)
    {
        var next = PersonRosterColorTab.Normalize(key);
        if (PersonRosterColorTab.IsSearch(next))
        {
            selectedKey = PersonRosterColorTab.Search;
            PublishTabs();
            return;
        }

        var leavingSearch = searchActive || PersonRosterColorTab.IsSearch(selectedKey);
        selectedKey = next;
        tabBeforeSearch = next;
        if (leavingSearch)
        {
            searchActive = false;
            searchMatchCount = 0;
            applyingTabChoice = true;
            try
            {
                Frame.GetController<PersonRosterSearchGateController>()?.ClearSearch();
            }
            finally
            {
                applyingTabChoice = false;
            }
        }
        else
        {
            ApplyTabCriteria();
        }

        ApplyIncompleteColumns();
        PublishTabs();
    }

    private void OnCollectionReloaded(object? sender, EventArgs e)
    {
        if (!searchActive || View is not { IsDisposed: false })
            return;

        try
        {
            searchMatchCount = View.CollectionSource?.GetCount() ?? 0;
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        PublishTabs();
    }

    private void FocusPerson(Guid personId)
    {
        if (View is not { IsDisposed: false })
            return;

        try
        {
            View.ObjectSpace?.Refresh();
            View.CollectionSource?.Reload();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            return;
        }

        var person = View.ObjectSpace?.GetObjectByKey<Person>(personId);
        if (person != null)
            View.CurrentObject = person;
    }

    private void ApplyTabCriteria()
    {
        if (View?.CollectionSource == null)
            return;

        if (searchActive || !IsIncompleteTabSelected)
        {
            if (View.CollectionSource.Criteria.ContainsKey(PersonRosterColorTab.CriteriaKey))
                View.CollectionSource.Criteria.Remove(PersonRosterColorTab.CriteriaKey);
            return;
        }

        View.CollectionSource.Criteria[PersonRosterColorTab.CriteriaKey] =
            PersonRosterColorTab.CriteriaFor(PersonRosterColorTab.Incomplete);
    }

    private void ApplyIncompleteColumns()
    {
        if (View?.Editor is not DxGridListEditor editor)
            return;

        var show = IsIncompleteTabSelected;
        editor.BeginUpdate();
        try
        {
            foreach (DxGridDataColumnModel column in editor.GridDataColumnModels)
            {
                if (column.FieldName is not (
                    nameof(Person.IncompleteMissingAreasDisplay)
                    or nameof(Person.IncompleteNotes)))
                    continue;

                column.Visible = show;
                column.AllowSort = false;
                column.VisibleIndex = column.FieldName == nameof(Person.IncompleteNotes) ? 4 : 3;
            }
        }
        finally
        {
            editor.EndUpdate();
        }
    }

    private void PublishTabs()
    {
        if (View is not { IsDisposed: false })
            return;

        EnsurePopulationCounts();
        var tabs = new List<PersonRosterColorTab.TabInfo>
        {
            PersonRosterColorTab.Describe(PersonRosterColorTab.All, allCount),
        };
        if (searchActive || PersonRosterColorTab.IsSearch(selectedKey))
            tabs.Add(PersonRosterColorTab.Describe(PersonRosterColorTab.Search, searchMatchCount));
        tabs.Add(PersonRosterColorTab.Describe(PersonRosterColorTab.Incomplete, incompleteCount));

        strip.Tabs = tabs;
        strip.SelectedKey = selectedKey;
        strip.NotifyChanged();
    }

    private void EnsurePopulationCounts()
    {
        if (populationCounted || ObjectSpace == null)
            return;

        var role = CriteriaOperator.Parse(RoleCriteria());
        var incomplete = new GroupOperator(
            GroupOperatorType.And,
            role,
            PersonRosterColorTab.CriteriaFor(PersonRosterColorTab.Incomplete));
        allCount = Count(role);
        incompleteCount = Count(incomplete);
        populationCounted = true;
    }

    private int Count(CriteriaOperator criteria)
    {
        try
        {
            return ObjectSpace?.GetObjectsCount(typeof(Person), criteria) ?? 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or ArgumentException)
        {
            return 0;
        }
    }

    private string RoleCriteria() =>
        string.Equals(View?.Id, PersonRosterListViewIds.FamilyMembers, StringComparison.Ordinal)
            ? PersonRoleHelper.FamilyMemberCriteria
            : PersonRoleHelper.EmployeeCriteria;
}
