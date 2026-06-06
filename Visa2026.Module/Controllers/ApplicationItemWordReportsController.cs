using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens item-scoped Resminamalar for one or more selected <see cref="ApplicationItem"/> rows from ListView.
/// </summary>
public class ApplicationItemWordReportsController : ViewController<ListView>
{
    private SimpleAction resminamalarAction;

    public ApplicationItemWordReportsController()
    {
        TargetObjectType = typeof(ApplicationItem);

        resminamalarAction = new SimpleAction(this, "ViewApplicationItemWordReports", "View");
        resminamalarAction.ImageName = "BO_FileAttachment";
        resminamalarAction.SelectionDependencyType = SelectionDependencyType.Independent;
        resminamalarAction.Execute += ResminamalarAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        resminamalarAction.Caption = VisaUiMessages.Get("ApplicationReportPackage.Title");
        View.SelectionChanged += View_SelectionChanged;
        UpdateActionState();
    }

    protected override void OnDeactivated()
    {
        View.SelectionChanged -= View_SelectionChanged;
        base.OnDeactivated();
    }

    private void View_SelectionChanged(object sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        var itemIds = GetSelectedItemIds();
        if (itemIds.Count == 0)
        {
            resminamalarAction.Enabled["Selection"] = false;
            resminamalarAction.Enabled["NoApplicableReports"] = false;
            return;
        }

        resminamalarAction.Enabled["Selection"] = true;

        if (!ApplicationItemReportPackageValidation.TryResolveApplication(
                ObjectSpace,
                itemIds,
                out var application,
                out _))
        {
            resminamalarAction.Enabled["NoApplicableReports"] = false;
            return;
        }

        var catalogService = Application.ServiceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var context = WordReportGenerationContext.ForApplicationItems(itemIds);
        var catalog = catalogService.Build(ObjectSpace, application!, context);
        resminamalarAction.Enabled["NoApplicableReports"] = catalog.TotalCount > 0;
    }

    private void ResminamalarAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var itemIds = GetSelectedItemIds();
        if (itemIds.Count < 1)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("ApplicationItemReportPackage.ErrorNoSelection"),
                InformationType.Warning);
            return;
        }

        if (!ApplicationItemReportPackageValidation.TryResolveApplication(
                ObjectSpace,
                itemIds,
                out var application,
                out var errorMessageKey))
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get(errorMessageKey ?? "ApplicationItemReportPackage.ErrorNoSelection"),
                InformationType.Warning);
            return;
        }

        var catalogService = Application.ServiceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var context = WordReportGenerationContext.ForApplicationItems(itemIds);
        if (catalogService.Build(ObjectSpace, application!, context).TotalCount == 0)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("WordReports.NoApplicableReports"),
                InformationType.Warning);
            return;
        }

        var objectSpace = Application.CreateObjectSpace(typeof(ApplicationItemReportPackageListHost));
        var host = objectSpace.CreateObject<ApplicationItemReportPackageListHost>();
        host.ItemIdsJson = JsonSerializer.Serialize(itemIds);

        var detailView = Application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.View;
        detailView.Caption = VisaUiMessages.Get("ApplicationReportPackage.Title");

        var showViewParameters = new ShowViewParameters(detailView)
        {
            TargetWindow = TargetWindow.NewModalWindow
        };

        var dialogController = Application.CreateController<DialogController>();
        dialogController.SaveOnAccept = false;
        dialogController.AcceptAction.Active.SetItemValue("ReportPackageReadOnly", false);
        dialogController.CancelAction.Active.SetItemValue("ReportPackageReadOnly", false);
        showViewParameters.Controllers.Add(dialogController);

        Application.ShowViewStrategy.ShowView(showViewParameters, new ShowViewSource(Frame, null));
    }

    private List<Guid> GetSelectedItemIds()
    {
        var selected = View.SelectedObjects?
            .OfType<ApplicationItem>()
            .Where(item => item != null)
            .ToList();

        if (selected is { Count: > 0 })
        {
            return selected
                .Select(item => (Guid)ObjectSpace.GetKeyValue(item))
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
        }

        if (View.CurrentObject is ApplicationItem current)
        {
            var id = (Guid)ObjectSpace.GetKeyValue(current);
            return id == Guid.Empty ? new List<Guid>() : new List<Guid> { id };
        }

        return new List<Guid>();
    }
}
