using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Visa2026.Module;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Incomplete Employees / Family Members use the same native XAF ListView as All.
/// The node is cloned from the working roster view, then only caption and criteria change.
/// Hand-built columns (null ModelMember) crash DxGrid AddColumnCore and were left blank.
/// </summary>
internal static class PersonIncompleteRosterListViewSync
{
    public static void EnsureAll(IModelViews views)
    {
        Ensure(
            views,
            PersonRosterListViewIds.Employees,
            PersonRosterListViewIds.EmployeesIncomplete,
            PersonRosterListViewIds.EmployeesIncompleteCriteria,
            "Incomplete");
        Ensure(
            views,
            PersonRosterListViewIds.FamilyMembers,
            PersonRosterListViewIds.FamilyMembersIncomplete,
            PersonRosterListViewIds.FamilyMembersIncompleteCriteria,
            "Incomplete");
    }

    public static void Ensure(
        IModelViews views,
        string sourceViewId,
        string incompleteViewId,
        string criteria,
        string caption)
    {
        if (views == null || views[sourceViewId] is not IModelListView source)
            return;

        if (views[incompleteViewId] is IModelNode existing)
            existing.Remove();

        if (((ModelNode)source).Clone(incompleteViewId) is not IModelListView target)
            return;

        target.Id = incompleteViewId;
        target.Criteria = criteria;
        target.Caption = caption;
        target.DataAccessMode = CollectionSourceDataAccessMode.Server;
        if (source.DetailView != null)
            target.DetailView = source.DetailView;
    }
}