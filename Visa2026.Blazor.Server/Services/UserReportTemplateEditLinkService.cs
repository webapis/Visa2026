using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>Builds edit links from Resminamalar custom template rows to <see cref="UserReportTemplate"/> DetailView.</summary>
public sealed class UserReportTemplateEditLinkService
{
    public const string DetailViewId = "UserReportTemplate_DetailView";

    private readonly XafApplicationHolder applicationHolder;

    public UserReportTemplateEditLinkService(XafApplicationHolder applicationHolder)
    {
        this.applicationHolder = applicationHolder;
    }

    public bool CanEditAny() => UserReportTemplateEditAccess.CanEditTemplates();

    public bool CanEdit(Guid templateId)
    {
        if (templateId == Guid.Empty || applicationHolder.Application == null)
            return false;

        using var objectSpace = applicationHolder.Application.CreateObjectSpace(typeof(UserReportTemplate));
        return UserReportTemplateEditAccess.CanOpenTemplateDetail(objectSpace, templateId);
    }

    public string GetDetailViewUrl(Guid templateId) =>
        templateId == Guid.Empty ? string.Empty : $"/{DetailViewId}/{templateId:D}";
}
