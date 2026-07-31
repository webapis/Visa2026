using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Controllers;

/// <summary>
/// "Resminamalar" on the Application detail view — opens the inline report package slot (v2).
/// </summary>
public class WordReportsController : ViewController<DetailView>
{
    private SimpleAction resminamalarAction;

    public WordReportsController()
    {
        TargetObjectType = typeof(Application);
        TargetViewType = ViewType.DetailView;

        resminamalarAction = new SimpleAction(this, "GenerateWordReports", "Reports");
        resminamalarAction.ImageName = "Templates";
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
        var catalog = catalogService.Build(ObjectSpace, application, WordReportGenerationContext.ForApplication());

        string? emptyMessage = null;
        if (catalog.TotalCount == 0)
        {
            emptyMessage = VisaUiMessages.Format(
                "WordReports.NoApplicationScopeTemplates",
                ResolveApplicationTypeLabel(application));
        }

        var slotService = Application.ServiceProvider.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                emptyMessage ?? VisaUiMessages.Get("ApplicationReportPackage.Empty"),
                InformationType.Warning);
            return;
        }

        slotService.OpenResminamalarAsync(new ResminamalarSlotRequest
        {
            ApplicationId = applicationId,
            Scope = WordReportPackageScope.Application,
            EmptyCatalogMessage = emptyMessage,
        }, VisaPreviewSlotViewHelper.ResolveOwnerViewId(View)).GetAwaiter().GetResult();
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
