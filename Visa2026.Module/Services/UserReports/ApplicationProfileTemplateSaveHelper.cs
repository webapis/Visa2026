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
        if (request.SetApplicability)
        {
            ApplyCatalogApplicability(
                template,
                objectSpace,
                request.CatalogScope,
                request.ApplicableProjectContractId,
                request.ApplicableMigrationServiceId);
        }
        template.RecycledAtUtc = null;
        template.RecycledByUserName = null;
        template.TemplateFile ??= objectSpace.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();
        template.TemplateFile.FileName = fileName;
        template.TemplateFile.Content = request.Content;

        TemplateCatalogAuditStamp.Touch(template, SecuritySystem.CurrentUserName);

        var userTemplate = ApplicationProfileTemplateUserReportBridge.EnsureLinkedUserReportTemplate(
            objectSpace,
            template,
            ApplicationProfileWizardTemplateCatalog.RootBoFromDataScope(request.DataScope));

        ApplicationProfileTemplateUserReportBridge.WriteMasterFile(objectSpace, userTemplate, request.Content, fileName);

        return template;
    }

    /// <summary>
    /// Same rule as the Application Profile Templates wizard: profile-specific rows may bind one
    /// Project contract (via ministry) or one Migration service (direct). Empty = every instance of this profile.
    /// Shared catalog rows never keep a contract/service filter.
    /// </summary>
    public static void ApplyCatalogApplicability(
        ApplicationProfileTemplate template,
        IObjectSpace? objectSpace,
        ApplicationProfileTemplateCatalogScope catalogScope,
        Guid? projectContractId,
        Guid? migrationServiceId)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (catalogScope != ApplicationProfileTemplateCatalogScope.ProfileSpecific)
        {
            ClearApplicability(template);
            return;
        }

        if (projectContractId is Guid contractId && contractId != Guid.Empty)
        {
            template.ApplicableMigrationService = null;
            template.ApplicableMigrationServiceId = null;
            template.ApplicableProjectContractId = contractId;
            template.ApplicableProjectContract = objectSpace?.GetObjectByKey<ProjectContract>(contractId);
            return;
        }

        if (migrationServiceId is Guid serviceId && serviceId != Guid.Empty)
        {
            template.ApplicableProjectContract = null;
            template.ApplicableProjectContractId = null;
            template.ApplicableMigrationServiceId = serviceId;
            template.ApplicableMigrationService = objectSpace?.GetObjectByKey<MigrationService>(serviceId);
            return;
        }

        ClearApplicability(template);
    }

    private static void ClearApplicability(ApplicationProfileTemplate template)
    {
        template.ApplicableProjectContract = null;
        template.ApplicableProjectContractId = null;
        template.ApplicableMigrationService = null;
        template.ApplicableMigrationServiceId = null;
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

    /// <summary>
    /// When true, persist <see cref="ApplicableProjectContractId"/> / <see cref="ApplicableMigrationServiceId"/>
    /// (Create from yellow marks). Convert leaves this false so an overwrite does not wipe a wizard binding.
    /// </summary>
    public bool SetApplicability { get; init; }

    public Guid? ApplicableProjectContractId { get; init; }

    public Guid? ApplicableMigrationServiceId { get; init; }
}
