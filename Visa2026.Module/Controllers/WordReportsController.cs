using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.Extensions.DependencyInjection;
using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Controllers;

/// <summary>
/// "Resminamalar" on the Application detail view — opens the report package dialog (v2).
/// </summary>
public class WordReportsController : ViewController<DetailView>
{
    private SimpleAction resminamalarAction;

    public WordReportsController()
    {
        TargetObjectType = typeof(Application);
        TargetViewType = ViewType.DetailView;

        resminamalarAction = new SimpleAction(this, "GenerateWordReports", "Reports");
        resminamalarAction.ImageName = "BO_FileAttachment";
        resminamalarAction.Execute += ResminamalarAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        resminamalarAction.Caption = VisaUiMessages.Get("ApplicationReportPackage.Title");
        UpdateActionState();
        View.CurrentObjectChanged += View_CurrentObjectChanged;
    }

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        var application = View?.CurrentObject as Application;
        if (application == null)
        {
            resminamalarAction.Enabled["NoApplication"] = false;
            return;
        }

        var applicationId = (Guid)ObjectSpace.GetKeyValue(application);
        resminamalarAction.Enabled["NoApplication"] = applicationId != Guid.Empty;
    }

    private void ResminamalarAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var application = (Application)e.CurrentObject;
        var applicationId = (Guid)ObjectSpace.GetKeyValue(application);
        if (applicationId == Guid.Empty)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("WordReports.SaveApplicationBeforeReports"),
                InformationType.Warning);
            return;
        }

        var catalogService = Application.ServiceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        if (catalogService.Build(ObjectSpace, application, WordReportGenerationContext.ForApplication()).TotalCount == 0)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Format("WordReports.NoApplicationScopeTemplates", ResolveApplicationTypeLabel(application)),
                InformationType.Warning);
            return;
        }

        var objectSpace = Application.CreateObjectSpace(typeof(ApplicationReportPackageListHost));
        var host = objectSpace.CreateObject<ApplicationReportPackageListHost>();
        host.ApplicationId = applicationId;

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

    private static string ResolveApplicationTypeLabel(Application application)
    {
        var type = application.ApplicationType;
        if (type == null)
            return "—";

        if (!string.IsNullOrWhiteSpace(type.NameTm))
            return type.NameTm;

        return !string.IsNullOrWhiteSpace(type.Name) ? type.Name : type.ToString();
    }
}
