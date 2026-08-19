using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Serves a User Report Template master file as PDF for <c>#visa-preview-slot</c> (File occupant).
/// Prefers the wizard ObjectSpace so unsaved Replace file still previews.
/// </summary>
public sealed class UserReportTemplateFilePreviewSource : IFilePreviewSource
{
    public const string Key = "user-report-template";

    private readonly IApplicationProfileWizardSession _wizardSession;
    private readonly INonSecuredObjectSpaceFactory _objectSpaces;
    private readonly ApplicationWordReportOfficePreviewPdfConverter _converter;

    public UserReportTemplateFilePreviewSource(
        IApplicationProfileWizardSession wizardSession,
        INonSecuredObjectSpaceFactory objectSpaces,
        ApplicationWordReportOfficePreviewPdfConverter converter)
    {
        _wizardSession = wizardSession;
        _objectSpaces = objectSpaces;
        _converter = converter;
    }

    public string SourceType => Key;

    public Task<FilePreviewResult?> TryLoadAsync(Guid templateId)
    {
        if (templateId == Guid.Empty)
            return Task.FromResult<FilePreviewResult?>(null);

        var fromWizard = TryFromObjectSpace(_wizardSession.ObjectSpace, templateId);
        if (fromWizard != null)
            return Task.FromResult<FilePreviewResult?>(fromWizard);

        using var objectSpace = _objectSpaces.CreateNonSecuredObjectSpace<UserReportTemplate>();
        return Task.FromResult(TryFromObjectSpace(objectSpace, templateId));
    }

    private FilePreviewResult? TryFromObjectSpace(IObjectSpace? objectSpace, Guid templateId)
    {
        if (objectSpace == null || objectSpace.IsDisposed)
            return null;

        var template = objectSpace.GetObjectByKey<UserReportTemplate>(templateId)
            ?? objectSpace.GetObjectsQuery<UserReportTemplate>()
                .Include(t => t.TemplateFile)
                .AsEnumerable()
                .FirstOrDefault(t => t.ID == templateId);
        if (template == null || !template.IsActive)
            return null;

        var file = template.TemplateFile;
        return OfficeFilePreviewResultFactory.FromOfficeOrPdf(
            _converter,
            file?.Content,
            file?.FileName ?? template.TemplateName + ".docx");
    }
}