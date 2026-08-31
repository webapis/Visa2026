#nullable enable

using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Shared nested-template save path for Convert and Scan (SD-D8).
/// </summary>
public static class ApplicationProfileTemplateSaveHelper
{
    public static ApplicationProfileTemplate Save(ApplicationProfileTemplateSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.TemplateName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A template name is required.", nameof(request));

        var objectSpace = request.ObjectSpace;
        var profile = request.Profile;
        var extension = request.TemplateKind == ApplicationProfileTemplateKind.Excel ? ".xlsx" : ".docx";
        var fileName = string.IsNullOrWhiteSpace(request.FileName) ? name + extension : request.FileName;

        var template = profile.NestedTemplates?
            .FirstOrDefault(t => string.Equals(t.TemplateName, name, StringComparison.OrdinalIgnoreCase));

        if (template == null)
        {
            template = objectSpace.CreateObject<ApplicationProfileTemplate>();
            template.ApplicationProfile = profile;
            template.TemplateName = name;
            template.SortOrder = (profile.NestedTemplates?.Count ?? 0) + 1;
            if (profile.NestedTemplates != null && !profile.NestedTemplates.Contains(template))
                profile.NestedTemplates.Add(template);
        }

        template.TemplateKind = request.TemplateKind;
        template.CatalogScope = request.CatalogScope;
        template.DataScope = request.DataScope;
        template.RecycledAtUtc = null;
        template.RecycledByUserName = null;
        template.TemplateFile ??= objectSpace.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();
        template.TemplateFile.FileName = fileName;
        template.TemplateFile.Content = request.Content;

        var userTemplate = ApplicationProfileTemplateUserReportBridge.EnsureLinkedUserReportTemplate(
            objectSpace,
            template,
            ApplicationProfileWizardTemplateCatalog.RootBoFromDataScope(request.DataScope));

        ApplicationProfileTemplateUserReportBridge.WriteMasterFile(objectSpace, userTemplate, request.Content, fileName);

        return template;
    }
}

public sealed class ApplicationProfileTemplateSaveRequest
{
    public required IObjectSpace ObjectSpace { get; init; }

    public required ApplicationProfile Profile { get; init; }

    public required string TemplateName { get; init; }

    public ApplicationProfileTemplateKind TemplateKind { get; init; } = ApplicationProfileTemplateKind.Word;

    public required ApplicationProfileTemplateDataScope DataScope { get; init; }

    public required ApplicationProfileTemplateCatalogScope CatalogScope { get; init; }

    public required byte[] Content { get; init; }

    public string? FileName { get; init; }
}
