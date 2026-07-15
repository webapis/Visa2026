#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Editors;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Blazor.Server.Editors;

[PropertyEditor(typeof(string), ReportDashboardEditorAliases.Dashboard, false)]
public class ReportDashboardPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? _application;
    private IReportDashboardQueryService? _queryService;

    public ReportDashboardPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    public override ReportDashboardModel ComponentModel => (ReportDashboardModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        _application = application;
        _queryService = application.ServiceProvider?.GetService<IReportDashboardQueryService>();
    }

    protected override IComponentModel CreateComponentModel()
    {
        var initialCategory = ReportDashboardCategory.VisaExtension;
        var model = new ReportDashboardModel
        {
            PersonType        = ReportDashboardPersonType.Employees,
            Category          = initialCategory,
            SubReport         = ReportDashboardCatalog.DefaultSubReport(initialCategory),
            ProjectKey        = "All",
            ChartView         = "pie",
            DateRangeMonths   = 6,
            ShowAllView       = true,
            IncludeArchivedPersons = false,
            PersonTypeChanged   = EventCallback.Factory.Create<ReportDashboardPersonType>(this, OnPersonTypeChanged),
            CategoryChanged     = EventCallback.Factory.Create<ReportDashboardCategory>(this, OnCategoryChanged),
            SubReportChanged    = EventCallback.Factory.Create<string>(this, OnSubReportChanged),
            ProjectKeyChanged   = EventCallback.Factory.Create<string>(this, OnProjectKeyChanged),
            ChartViewChanged    = EventCallback.Factory.Create<string>(this, OnChartViewChanged),
            ShowAllViewChanged  = EventCallback.Factory.Create<bool>(this, OnShowAllViewChanged),
            IncludeArchivedPersonsChanged = EventCallback.Factory.Create<bool>(this, OnIncludeArchivedPersonsChanged),
            OpenExcelRequested      = EventCallback.Factory.Create(this, OnOpenExcelAsync),
            OpenListViewRequested   = EventCallback.Factory.Create<string?>(this, OnOpenListView),
            DateRangeChanged        = EventCallback.Factory.Create<int>(this, OnDateRangeChanged)
        };
        Refresh(model);
        return model;
    }

    private void OnPersonTypeChanged(ReportDashboardPersonType personType)
    {
        ComponentModel.PersonType = personType;
        Refresh(ComponentModel);
    }

    private void OnCategoryChanged(ReportDashboardCategory category)
    {
        ComponentModel.Category    = category;
        ComponentModel.SubReport   = ReportDashboardCatalog.DefaultSubReport(category);
        ComponentModel.ChartView   = DefaultChartViewFor(category, ComponentModel.SubReport);
        ComponentModel.ShowAllView = false;
        ComponentModel.AllPanels   = null;
        Refresh(ComponentModel);
    }

    private void OnSubReportChanged(string subReport)
    {
        ComponentModel.SubReport = subReport;
        ComponentModel.ChartView = DefaultChartViewFor(ComponentModel.Category, subReport);
        Refresh(ComponentModel);
    }

    private void OnProjectKeyChanged(string projectKey)
    {
        ComponentModel.ProjectKey = projectKey;
        Refresh(ComponentModel);
    }


    private static string DefaultChartViewFor(ReportDashboardCategory category, string subReport) =>
        (category, subReport) switch
        {
            (ReportDashboardCategory.Passport, "by-citizenship") => "bar",
            _ => "pie"
        };
    private void OnChartViewChanged(string chartView)
    {
        ComponentModel.ChartView = chartView;
    }

    private void OnDateRangeChanged(int months)
    {
        ComponentModel.DateRangeMonths = months;
        Refresh(ComponentModel);
    }

    private void OnShowAllViewChanged(bool showAll)
    {
        ComponentModel.ShowAllView = showAll;
        Refresh(ComponentModel);
    }

    private void OnIncludeArchivedPersonsChanged(bool includeArchived)
    {
        ComponentModel.IncludeArchivedPersons = includeArchived;
        Refresh(ComponentModel);
    }

    private IObjectSpace? CreatePersistentObjectSpace() =>
        _application?.CreateObjectSpace(typeof(Person));

    private void Refresh(ReportDashboardModel model)
    {
        if (_application == null || _queryService == null) return;
        using var objectSpace = CreatePersistentObjectSpace();
        if (objectSpace == null) return;

        model.Snapshot = _queryService.LoadSnapshot(objectSpace, model.DateRangeMonths);

        if (model.ShowAllView)
        {
            var allPanels = new Dictionary<ReportDashboardCategory, ReportDashboardPanelData>();
            foreach (var cat in ReportDashboardCatalog.Categories)
            {
                var defaultSub = ReportDashboardCatalog.DefaultSubReport(cat);
                allPanels[cat] = _queryService.LoadPanel(
                    objectSpace, model.PersonType, cat, model.ProjectKey,
                    model.DateRangeMonths, defaultSub, includeArchivedPersons: false);
            }
            model.AllPanels = allPanels;
            model.Panel     = null;
        }
        else
        {
            model.AllPanels = null;
            model.Panel     = _queryService.LoadPanel(
                objectSpace,
                model.PersonType,
                model.Category,
                model.ProjectKey,
                model.DateRangeMonths,
                model.SubReport,
                model.IncludeArchivedPersons);
        }
    }

    private async Task OnOpenExcelAsync()
    {
        if (_application == null) return;

        var hint = ReportDashboardCatalog.ExcelTemplateNameHint(ComponentModel.Category);
        if (string.IsNullOrEmpty(hint))
        {
            _application.ShowViewStrategy.ShowMessage(
                "No Excel template is configured for this report yet.",
                InformationType.Info, 4000);
            return;
        }

        using var objectSpace = CreatePersistentObjectSpace();
        if (objectSpace == null) return;

        var template = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .FirstOrDefault(t => t.TemplateName != null
                && t.TemplateName.Contains(hint)
                && t.TemplateOutputFormat == TemplateOutputFormat.Excel);

        if (template?.TemplateFile == null)
        {
            _application.ShowViewStrategy.ShowMessage(
                $"Excel template '{hint}' was not found. Seed or upload it under Reports.",
                InformationType.Warning, 5000);
            return;
        }

        var downloader = _application.ServiceProvider?.GetService<IFileDownloader>();
        if (downloader == null) return;

        await using var ms = new MemoryStream();
        template.TemplateFile.SaveToStream(ms);
        ms.Position = 0;
        var fileName = string.IsNullOrWhiteSpace(template.TemplateFile.FileName)
            ? $"{hint}.xlsx"
            : template.TemplateFile.FileName;
        await downloader.DownloadAsync(
            fileName, ms,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private Task OnOpenListView(string? statusLabel)
    {
        if (_application == null) return Task.CompletedTask;

        var category   = ComponentModel.Category;
        var type       = ReportDashboardCatalog.ListViewType(category);
        var listViewId = ReportDashboardCatalog.ListViewId(category);
        var criteria   = ReportDashboardCatalog.BuildListCriteria(
            ComponentModel.PersonType, category, ComponentModel.ProjectKey, statusLabel);

        var objectSpace      = _application.CreateObjectSpace(type);
        var collectionSource = _application.CreateCollectionSource(objectSpace, type, listViewId);
        var listView         = _application.CreateListView(listViewId, collectionSource, true);
        if (!string.IsNullOrWhiteSpace(criteria))
            listView.CollectionSource.Criteria["ReportDashboard"] = CriteriaOperator.Parse(criteria);

        var window = _application.MainWindow;
        if (window == null) return Task.CompletedTask;

        _application.ShowViewStrategy.ShowView(
            new ShowViewParameters(listView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(window, null));
        return Task.CompletedTask;
    }
}