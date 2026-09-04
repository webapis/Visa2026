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
/// Serves an Application Profile nested template file as PDF for <c>#visa-preview-slot</c>.
/// Prefers the wizard ObjectSpace so unsaved uploads still preview.
/// </summary>
public sealed class ApplicationProfileTemplateFilePreviewSource : IFilePreviewSource
{
    public const string Key = "application-profile-template";

    private readonly IApplicationProfileWizardSession _wizardSession;
    private readonly INonSecuredObjectSpaceFactory _objectSpaces;
    private readonly ApplicationWordReportOfficePreviewPdfConverter _converter;

    public ApplicationProfileTemplateFilePreviewSource(
        IApplicationProfileWizardSession wizardSession,
        INonSecuredObjectSpaceFactory objectSpaces,
        ApplicationWordReportOfficePreviewPdfConverter converter)
    {
        _wizardSession = wizardSession;
        _objectSpaces = objectSpaces;
        _converter = converter;
    }

    public string SourceType => Key;

    public Task<FilePreviewResult?> TryLoadAsync(Guid nestedTemplateId)
    {
        if (nestedTemplateId == Guid.Empty)
            return Task.FromResult<FilePreviewResult?>(null);

        var fromWizard = TryFromObjectSpace(_wizardSession.ObjectSpace, nestedTemplateId);
        if (fromWizard != null)
            return Task.FromResult<FilePreviewResult?>(fromWizard);

        using var objectSpace = _objectSpaces.CreateNonSecuredObjectSpace<ApplicationProfileTemplate>();
        return Task.FromResult(TryFromObjectSpace(objectSpace, nestedTemplateId));
    }

    private FilePreviewResult? TryFromObjectSpace(IObjectSpace? objectSpace, Guid nestedTemplateId)
    {
        if (objectSpace == null || objectSpace.IsDisposed)
            return null;

        var nested = objectSpace.GetObjectByKey<ApplicationProfileTemplate>(nestedTemplateId)
            ?? objectSpace.GetObjectsQuery<ApplicationProfileTemplate>()
                .Include(t => t.TemplateFile)
                .AsEnumerable()
                .FirstOrDefault(t => t.ID == nestedTemplateId);
        if (nested == null)
            return null;

        var bytes = nested.TemplateFile?.Content;
        var fileName = nested.TemplateFile?.FileName;
        if (bytes == null || bytes.Length == 0)
        {
            var linked = objectSpace.GetObjectsQuery<UserReportTemplate>()
                .Include(t => t.TemplateFile)
                .AsEnumerable()
                .FirstOrDefault(t =>
                    t.IsActive
                    && string.Equals(t.TemplateName, nested.TemplateName, StringComparison.OrdinalIgnoreCase));
            bytes = linked?.TemplateFile?.Content;
            fileName ??= linked?.TemplateFile?.FileName;
        }

        return OfficeFilePreviewResultFactory.FromOfficeOrPdf(
            _converter,
            bytes,
            fileName ?? nested.TemplateName + ".docx");
    }
}