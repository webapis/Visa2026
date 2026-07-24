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
    private int _refreshGeneration;

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
            PersonType        = ReportDashboardPersonType.All,
            Category          = initialCategory,
            SubReport         = ReportDashboardCatalog.DefaultSubReport(initialCategory),
            ProjectKey        = "All",
            ChartView         = "pie",
            DateRangeMonths   = ReportDashboardCatalog.DefaultCategoryDateRangeMonths,
            PassportDateRangeMonths = ReportDashboardCatalog.DefaultCategoryDateRangeMonths,
            PositionHistoryDateRangeMonths = ReportDashboardCatalog.DefaultCategoryDateRangeMonths,
            AddressOfResidenceDateRangeMonths = ReportDashboardCatalog.DefaultCategoryDateRangeMonths,
            MedicalRecordDateRangeMonths = ReportDashboardCatalog.DefaultCategoryDateRangeMonths,
            ShowAllView       = true,
            IncludeArchivedPersons = false,
            OneLastValidVisaPerPerson = true,
            OneLastValidWorkPermitPerPerson = true,
            ValidVisaPersonsOnly = true,
            IncludeCompletedApplicationProcesses = false,
            IncludeCancelledApplicationProcesses = false,
            PersonTypeChanged   = EventCallback.Factory.Create<ReportDashboardPersonType>(this, OnPersonTypeChanged),
            CategoryChanged     = EventCallback.Factory.Create<ReportDashboardCategory>(this, OnCategoryChanged),
            SubReportChanged    = EventCallback.Factory.Create<string>(this, OnSubReportChanged),
            ProjectKeyChanged   = EventCallback.Factory.Create<string>(this, OnProjectKeyChanged),
            ChartViewChanged    = EventCallback.Factory.Create<string>(this, OnChartViewChanged),
            ShowAllViewChanged  = EventCallback.Factory.Create<bool>(this, OnShowAllViewChanged),
            IncludeArchivedPersonsChanged = EventCallback.Factory.Create<bool>(this, OnIncludeArchivedPersonsChanged),
            OneLastValidVisaPerPersonChanged = EventCallback.Factory.Create<bool>(this, OnOneLastValidVisaPerPersonChanged),
            OneLastValidWorkPermitPerPersonChanged = EventCallback.Factory.Create<bool>(this, OnOneLastValidWorkPermitPerPersonChanged),
            ValidVisaPersonsOnlyChanged = EventCallback.Factory.Create<bool>(this, OnValidVisaPersonsOnlyChanged),
            IncludeCompletedApplicationProcessesChanged = EventCallback.Factory.Create<bool>(this, OnIncludeCompletedApplicationProcessesChanged),
            IncludeCancelledApplicationProcessesChanged = EventCallback.Factory.Create<bool>(this, OnIncludeCancelledApplicationProcessesChanged),
            IsLoading               = true,
            LoadingMessage          = "Loading overview…",
            LoadingProgressPercent  = 0,
            OpenExcelRequested      = EventCallback.Factory.Create(this, OnOpenExcelAsync),
            OpenListViewRequested   = EventCallback.Factory.Create<string?>(this, OnOpenListView),
            DateRangeChanged        = EventCallback.Factory.Create<int>(this, OnDateRangeChanged),
            PassportDateRangeChanged = EventCallback.Factory.Create<int>(this, OnPassportDateRangeChanged),
            PositionHistoryDateRangeChanged = EventCallback.Factory.Create<int>(this, OnPositionHistoryDateRangeChanged),
            AddressOfResidenceDateRangeChanged = EventCallback.Factory.Create<int>(this, OnAddressOfResidenceDateRangeChanged),
            MedicalRecordDateRangeChanged = EventCallback.Factory.Create<int>(this, OnMedicalRecordDateRangeChanged)
        };
        model.InitialLoadRequested = EventCallback.Factory.Create(this, () => RefreshAsync(model));
        return model;
    }

    private Task OnPersonTypeChanged(ReportDashboardPersonType personType)
    {
        ComponentModel.PersonType = personType;
        return RefreshAsync(ComponentModel);
    }

    private Task OnCategoryChanged(ReportDashboardCategory category)
    {
        ComponentModel.Category    = category;
        ComponentModel.SubReport   = ReportDashboardCatalog.DefaultSubReport(category);
        ComponentModel.ChartView   = DefaultChartViewFor(category, ComponentModel.SubReport);
        ComponentModel.ShowAllView = false;
        ComponentModel.AllPanels   = null;
        ComponentModel.Panel       = null;
        ComponentModel.SubReportCounts = null;
        return RefreshAsync(ComponentModel);
    }

    private Task OnSubReportChanged(string subReport)
    {
        ComponentModel.SubReport = subReport;
        ComponentModel.ChartView = DefaultChartViewFor(ComponentModel.Category, subReport);
        ComponentModel.Panel = null;
        return RefreshAsync(ComponentModel);
    }

    private Task OnProjectKeyChanged(string projectKey)
    {
        ComponentModel.ProjectKey = projectKey;
        return RefreshAsync(ComponentModel);
    }


    private static string DefaultChartViewFor(ReportDashboardCategory category, string subReport) =>
        (category, subReport) switch
        {
            (ReportDashboardCategory.Application, _) => "bar",
            (ReportDashboardCategory.Passport, "by-citizenship") => "bar",
            (ReportDashboardCategory.Education, "by-country") => "bar",
            (ReportDashboardCategory.Education, "by-specialty") => "bar",
            (ReportDashboardCategory.PositionHistory, "by-position") => "bar",
            (ReportDashboardCategory.PositionHistory, "by-actual-position") => "bar",
            (ReportDashboardCategory.Subcontractor, "by-company") => "bar",
            (ReportDashboardCategory.AddressOfResidence, "by-region") => "bar",
            (ReportDashboardCategory.AddressOfResidence, "by-city") => "bar",
            (ReportDashboardCategory.AddressOfResidence, "by-address-type") => "bar",
            (ReportDashboardCategory.AddressOfResidence, "by-address") => "bar",
            (ReportDashboardCategory.Registration, "check-in-by-city") => "pie",
            (ReportDashboardCategory.Registration, "expiring-state") => "bar",
            (ReportDashboardCategory.Registration, "to-be-checked-in") => "bar",
            (ReportDashboardCategory.Registration, "to-be-checked-out") => "bar",
            _ => "pie"
        };
    private void OnChartViewChanged(string chartView)
    {
        ComponentModel.ChartView = chartView;
    }

    private Task OnDateRangeChanged(int months)
    {
        ComponentModel.DateRangeMonths = months;
        return RefreshAsync(ComponentModel);
    }

    private Task OnPassportDateRangeChanged(int months)
    {
        ComponentModel.PassportDateRangeMonths = months;
        return RefreshAsync(ComponentModel);
    }

    private Task OnPositionHistoryDateRangeChanged(int months)
    {
        ComponentModel.PositionHistoryDateRangeMonths = months;
        return RefreshAsync(ComponentModel);
    }

    private Task OnAddressOfResidenceDateRangeChanged(int months)
    {
        ComponentModel.AddressOfResidenceDateRangeMonths = months;
        return RefreshAsync(ComponentModel);
    }

    private Task OnMedicalRecordDateRangeChanged(int months)
    {
        ComponentModel.MedicalRecordDateRangeMonths = months;
        return RefreshAsync(ComponentModel);
    }

    private static int ResolveDateRangeMonths(ReportDashboardModel model, ReportDashboardCategory category) =>
        category switch
        {
            ReportDashboardCategory.Education => model.DateRangeMonths,
            ReportDashboardCategory.Passport => model.PassportDateRangeMonths,
            ReportDashboardCategory.PositionHistory => model.PositionHistoryDateRangeMonths,
            ReportDashboardCategory.AddressOfResidence => model.AddressOfResidenceDateRangeMonths,
            ReportDashboardCategory.MedicalRecord => model.MedicalRecordDateRangeMonths,
            _ => ReportDashboardCatalog.DefaultCategoryDateRangeMonths
        };

    private Task OnShowAllViewChanged(bool showAll)
    {
        ComponentModel.ShowAllView = showAll;
        return RefreshAsync(ComponentModel);
    }

    private Task OnIncludeArchivedPersonsChanged(bool includeArchived)
    {
        ComponentModel.IncludeArchivedPersons = includeArchived;
        return RefreshAsync(ComponentModel);
    }

    private Task OnOneLastValidVisaPerPersonChanged(bool oneLast)
    {
        ComponentModel.OneLastValidVisaPerPerson = oneLast;
        return RefreshAsync(ComponentModel);
    }

    private Task OnOneLastValidWorkPermitPerPersonChanged(bool oneLast)
    {
        ComponentModel.OneLastValidWorkPermitPerPerson = oneLast;
        return RefreshAsync(ComponentModel);
    }

    private Task OnValidVisaPersonsOnlyChanged(bool validVisaOnly)
    {
        ComponentModel.ValidVisaPersonsOnly = validVisaOnly;
        return RefreshAsync(ComponentModel);
    }

    private Task OnIncludeCompletedApplicationProcessesChanged(bool include)
    {
        ComponentModel.IncludeCompletedApplicationProcesses = include;
        return RefreshAsync(ComponentModel);
    }

    private Task OnIncludeCancelledApplicationProcessesChanged(bool include)
    {
        ComponentModel.IncludeCancelledApplicationProcesses = include;
        return RefreshAsync(ComponentModel);
    }

    private IObjectSpace? CreatePersistentObjectSpace() =>
        _application?.CreateObjectSpace(typeof(Person));

    private async Task RefreshAsync(ReportDashboardModel model)
    {
        if (_application == null || _queryService == null) return;

        var generation = ++_refreshGeneration;
        model.IsLoading = true;
        model.LoadingProgressPercent = 0;
        model.LoadingMessage = model.ShowAllView ? "Loading overview…" : "Loading report…";
        // Let Blazor paint the overlay before synchronous DB work (Task.Yield is not enough).
        await Task.Delay(16);
        if (generation != _refreshGeneration) return;

        try
        {
            using var objectSpace = CreatePersistentObjectSpace();
            if (objectSpace == null) return;

            model.Snapshot = _queryService.LoadSnapshot(objectSpace, model.DateRangeMonths, model.PersonType);
            await Task.Yield();
            if (generation != _refreshGeneration) return;

            if (model.ShowAllView)
            {
                var categories = ReportDashboardCatalog.Categories.ToList();
                var allPanels = new Dictionary<ReportDashboardCategory, ReportDashboardPanelData>();
                for (var i = 0; i < categories.Count; i++)
                {
                    if (generation != _refreshGeneration) return;

                    var cat = categories[i];
                    model.LoadingMessage = $"Loading {ReportDashboardCatalog.CategoryLabel(cat)}…";
                    model.LoadingProgressPercent = (int)Math.Round(100.0 * i / Math.Max(1, categories.Count));
                    await Task.Delay(1);
                    if (generation != _refreshGeneration) return;

                    var defaultSub = ReportDashboardCatalog.DefaultSubReport(cat);
                    allPanels[cat] = _queryService.LoadPanel(
                        objectSpace, model.PersonType, cat, model.ProjectKey,
                        ResolveDateRangeMonths(model, cat), defaultSub, includeArchivedPersons: false,
                        oneLastValidVisaPerPerson: false,
                        oneLastValidWorkPermitPerPerson: false,
                        includeCompletedApplicationProcesses: false,
                        includeCancelledApplicationProcesses: false,
                        validVisaPersonsOnly: ReportDashboardCatalog.SupportsValidVisaPersonsOnly(cat)
                            ? model.ValidVisaPersonsOnly
                            : false);

                    // Progressive fill so Overview cards appear as each category finishes.
                    model.AllPanels = new Dictionary<ReportDashboardCategory, ReportDashboardPanelData>(allPanels);
                    model.LoadingProgressPercent = (int)Math.Round(100.0 * (i + 1) / Math.Max(1, categories.Count));
                    await Task.Yield();
                }

                if (generation != _refreshGeneration) return;
                model.Panel = null;
                model.SubReportCounts = null;
            }
            else
            {
                model.AllPanels = null;
                var subReports = ReportDashboardCatalog.SubReports(model.Category).ToList();
                var counts = new Dictionary<string, int>(StringComparer.Ordinal);
                ReportDashboardPanelData? activePanel = null;
                for (var i = 0; i < subReports.Count; i++)
                {
                    if (generation != _refreshGeneration) return;

                    var sub = subReports[i];
                    model.LoadingMessage = $"Loading {sub.Label}…";
                    model.LoadingProgressPercent = (int)Math.Round(100.0 * i / Math.Max(1, subReports.Count));
                    await Task.Delay(1);
                    if (generation != _refreshGeneration) return;

                    var panel = _queryService.LoadPanel(
                        objectSpace,
                        model.PersonType,
                        model.Category,
                        model.ProjectKey,
                        ResolveDateRangeMonths(model, model.Category),
                        sub.Key,
                        model.IncludeArchivedPersons,
                        model.OneLastValidVisaPerPerson,
                        model.OneLastValidWorkPermitPerPerson,
                        model.IncludeCompletedApplicationProcesses,
                        model.IncludeCancelledApplicationProcesses,
                        model.ValidVisaPersonsOnly);
                    counts[sub.Key] = panel.TotalCount;
                    if (string.Equals(sub.Key, model.SubReport, StringComparison.Ordinal)
                        || (activePanel == null && subReports.Count == 1))
                        activePanel = panel;

                    model.LoadingProgressPercent = (int)Math.Round(100.0 * (i + 1) / Math.Max(1, subReports.Count));
                    await Task.Yield();
                }

                if (generation != _refreshGeneration) return;

                // Active key may be "default" while catalog uses a concrete key — fall back to DefaultSubReport.
                if (activePanel == null)
                {
                    var fallbackKey = ReportDashboardCatalog.DefaultSubReport(model.Category);
                    if (counts.ContainsKey(fallbackKey))
                    {
                        activePanel = _queryService.LoadPanel(
                            objectSpace, model.PersonType, model.Category, model.ProjectKey,
                            ResolveDateRangeMonths(model, model.Category), fallbackKey, model.IncludeArchivedPersons,
                            model.OneLastValidVisaPerPerson,
                            model.OneLastValidWorkPermitPerPerson,
                            model.IncludeCompletedApplicationProcesses,
                            model.IncludeCancelledApplicationProcesses,
                            model.ValidVisaPersonsOnly);
                        counts[fallbackKey] = activePanel.TotalCount;
                    }
                }

                model.Panel = activePanel;
                model.SubReportCounts = counts;
            }
        }
        finally
        {
            if (generation == _refreshGeneration)
            {
                model.IsLoading = false;
                model.LoadingProgressPercent = 100;
                model.LoadingMessage = string.Empty;
            }
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
        var includeArchived = ComponentModel.IncludeArchivedPersons;
        var criteria   = ReportDashboardCatalog.BuildListCriteria(
            ComponentModel.PersonType, category, ComponentModel.ProjectKey, statusLabel,
            includeArchived);

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