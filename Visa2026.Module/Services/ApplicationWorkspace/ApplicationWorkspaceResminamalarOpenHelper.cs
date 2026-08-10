using System;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Services.ApplicationWorkspace;

public static class ApplicationWorkspaceResminamalarOpenHelper
{
    public static bool TryOpen(XafApplication application, Guid applicationId, string? ownerViewId = null)
    {
        if (application == null || applicationId == Guid.Empty)
            return false;

        using var objectSpace = application.CreateObjectSpace(typeof(Application));
        var applicationBo = objectSpace.GetObjectByKey<Application>(applicationId);
        if (applicationBo == null)
            return false;

        var catalogService = application.ServiceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var catalog = catalogService.Build(objectSpace, applicationBo, WordReportGenerationContext.ForApplication());

        string? emptyMessage = null;
        if (catalog.TotalCount == 0)
        {
            emptyMessage = VisaUiMessages.Format(
                "WordReports.NoApplicationScopeTemplates",
                applicationBo.ApplicationProfile?.Name ?? applicationBo.ApplicationType?.NameTm ?? "Application");
        }

        var slotService = application.ServiceProvider.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
        {
            application.ShowViewStrategy.ShowMessage(
                emptyMessage ?? VisaUiMessages.Get("ApplicationReportPackage.Empty"),
                InformationType.Warning);
            return false;
        }

        slotService.OpenResminamalarAsync(new ResminamalarSlotRequest
        {
            ApplicationId = applicationId,
            Scope = WordReportPackageScope.Application,
            EmptyCatalogMessage = emptyMessage,
        }, ownerViewId).GetAwaiter().GetResult();

        return true;
    }
}
