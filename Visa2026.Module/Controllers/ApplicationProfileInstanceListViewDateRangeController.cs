using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Toolbar "Last N months" filter (default 6, plus All) for Via ministry / Direct migration
/// ApplicationProfileInstance ListViews. Uses stored <see cref="ApplicationProfileInstance.ApplicationDate"/>.
/// </summary>
public sealed class ApplicationProfileInstanceListViewDateRangeController : ViewController<ListView>
{
    public const string CriteriaKey = "DateWindow";
    public const int DefaultMonths = 6;

    private readonly SingleChoiceAction _dateRangeAction;

    public ApplicationProfileInstanceListViewDateRangeController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
        TargetViewId = string.Join(";",
            ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries,
            ApplicationProfileInstanceProgressRouteNavigation.ListViewDirectMigration);

        _dateRangeAction = new SingleChoiceAction(
            this,
            "ApplicationProfileInstanceListDateRange",
            PredefinedCategory.Filters)
        {
            Caption = VisaUiMessages.Get("ApplicationProfileInstance.List.DateRange.Caption"),
            ImageName = "Action_Filter",
            ItemType = SingleChoiceActionItemType.ItemIsMode,
            PaintStyle = DevExpress.ExpressApp.Templates.ActionItemPaintStyle.Caption,
        };

        for (var months = 1; months <= 6; months++)
        {
            _dateRangeAction.Items.Add(new ChoiceActionItem(
                id: $"Months{months}",
                caption: FormatMonthsCaption(months),
                data: months));
        }

        _dateRangeAction.Items.Add(new ChoiceActionItem(
            id: "All",
            caption: VisaUiMessages.Get("ApplicationProfileInstance.List.DateRange.All"),
            data: 0));

        _dateRangeAction.Execute += OnDateRangeExecute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        RefreshActionCaptions();
        SelectDefaultAndApply();
    }

    protected override void OnDeactivated()
    {
        View.CollectionSource.Criteria.Remove(CriteriaKey);
        base.OnDeactivated();
    }

    private void OnDateRangeExecute(object sender, SingleChoiceActionExecuteEventArgs e)
    {
        var months = e.SelectedChoiceActionItem?.Data is int value ? value : DefaultMonths;
        ApplyDateWindow(months);
    }

    private void SelectDefaultAndApply()
    {
        var defaultItem = _dateRangeAction.Items.FindItemByID($"Months{DefaultMonths}");
        if (defaultItem == null)
        {
            foreach (ChoiceActionItem item in _dateRangeAction.Items)
            {
                if (Equals(item.Data, DefaultMonths))
                {
                    defaultItem = item;
                    break;
                }
            }
        }

        if (defaultItem != null && !ReferenceEquals(_dateRangeAction.SelectedItem, defaultItem))
            _dateRangeAction.SelectedItem = defaultItem;

        ApplyDateWindow(DefaultMonths);
    }

    private void ApplyDateWindow(int months)
    {
        if (months <= 0)
        {
            View.CollectionSource.Criteria.Remove(CriteriaKey);
            return;
        }

        var cutoff = DateTime.Today.AddMonths(-months);
        View.CollectionSource.Criteria[CriteriaKey] = new BinaryOperator(
            nameof(ApplicationProfileInstance.ApplicationDate),
            cutoff,
            BinaryOperatorType.GreaterOrEqual);
    }

    private void RefreshActionCaptions()
    {
        _dateRangeAction.Caption = VisaUiMessages.Get("ApplicationProfileInstance.List.DateRange.Caption");
        foreach (ChoiceActionItem item in _dateRangeAction.Items)
        {
            if (item.Data is int months && months > 0)
                item.Caption = FormatMonthsCaption(months);
            else if (Equals(item.Data, 0))
                item.Caption = VisaUiMessages.Get("ApplicationProfileInstance.List.DateRange.All");
        }
    }

    private static string FormatMonthsCaption(int months) =>
        months == 1
            ? VisaUiMessages.Get("ApplicationProfileInstance.List.DateRange.LastMonth1")
            : VisaUiMessages.Format("ApplicationProfileInstance.List.DateRange.LastMonths", months);
}