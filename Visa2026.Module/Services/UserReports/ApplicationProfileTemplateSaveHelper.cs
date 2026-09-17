#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.WordReports;

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
        var requestedName = name;

        ApplicationProfileTemplate? template = null;
        if (request.AllowOverwriteByName)
        {
            template = FindExistingByNameAndKind(
                objectSpace,
                profile,
                name,
                request.TemplateKind);
        }

        if (template == null)
        {
            if (!request.AllowOverwriteByName)
            {
                name = AllocateUniqueTemplateName(
                    name,
                    CollectExistingTemplateNames(objectSpace, profile));
            }

            template = objectSpace.CreateObject<ApplicationProfileTemplate>();
            template.ApplicationProfile = profile;
            template.TemplateName = name;
            template.SortOrder = (profile.NestedTemplates?.Count ?? 0) + 1;
            if (profile.NestedTemplates != null && !profile.NestedTemplates.Contains(template))
                profile.NestedTemplates.Add(template);
        }

        var fileName = string.IsNullOrWhiteSpace(request.FileName)
            || string.Equals(
                Path.GetFileNameWithoutExtension(request.FileName),
                requestedName,
                StringComparison.OrdinalIgnoreCase)
            ? name + extension
            : request.FileName;

        var writeCatalogMetadata = objectSpace.IsNewObject(template)
            || !ApplicationProfileLockHelper.IsProfileConfigLocked(profile, objectSpace);
        if (writeCatalogMetadata)
        {
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
        }
        var content = request.TemplateKind == ApplicationProfileTemplateKind.Excel
            ? ExcelPreviewPageLayout.StampFromContent(request.Content)
            : request.Content;
        template.TemplateFile ??= objectSpace.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();
        template.TemplateFile.FileName = fileName;
        template.TemplateFile.Content = content;

        if (request.SourceContent is { Length: > 0 }
            && ScanOfficeYellowExtractor.HasHighlights(
                request.SourceContent,
                request.TemplateKind == ApplicationProfileTemplateKind.Excel
                    ? ScanSourceKind.Excel
                    : ScanSourceKind.Word))
        {
            template.SourceFile ??= objectSpace.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();
            template.SourceFile.FileName = string.IsNullOrWhiteSpace(request.SourceFileName)
                ? fileName
                : request.SourceFileName.Trim();
            template.SourceFile.Content = request.SourceContent;
        }

        if (request.ReviewPlanJson != null)
            template.ReviewPlanJson = string.IsNullOrWhiteSpace(request.ReviewPlanJson)
                ? null
                : request.ReviewPlanJson;

        TemplateCatalogAuditStamp.Touch(template, SecuritySystem.CurrentUserName);

        var userTemplate = ApplicationProfileTemplateUserReportBridge.EnsureLinkedUserReportTemplate(
            objectSpace,
            template,
            ApplicationProfileWizardTemplateCatalog.RootBoFromDataScope(request.DataScope));

        if (ShouldWriteLinkedMasterFile(
                request.CatalogScope,
                ApplicationProfileWizardTemplateCatalog.IsSharedIncludeName(
                    objectSpace,
                    name,
                    template.ID)))
        {
            ApplicationProfileTemplateUserReportBridge.WriteMasterFile(objectSpace, userTemplate, content, fileName);
        }

        return template;
    }

    /// <summary>
    /// Create from yellow marks appends <c>_2</c>, <c>_3</c>, … when the requested name
    /// is already a nested Word/Excel row on this profile (Review placeholders still overwrite).
    /// </summary>
    public static string AllocateUniqueTemplateName(
        string requestedName,
        IEnumerable<string?> existingNames)
    {
        var baseName = requestedName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseName))
            throw new ArgumentException("A template name is required.", nameof(requestedName));

        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in existingNames ?? Array.Empty<string?>())
        {
            if (!string.IsNullOrWhiteSpace(existing))
                taken.Add(existing.Trim());
        }

        if (!taken.Contains(baseName))
            return baseName;

        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{baseName}_{i}";
            if (!taken.Contains(candidate))
                return candidate;
        }

        return $"{baseName}_{Guid.NewGuid():N}";
    }

    private static IReadOnlyList<string> CollectExistingTemplateNames(
        IObjectSpace objectSpace,
        ApplicationProfile profile)
    {
        var names = new List<string>();
        if (profile?.NestedTemplates != null)
        {
            foreach (var nested in profile.NestedTemplates)
            {
                if (nested == null
                    || nested.TemplateKind == ApplicationProfileTemplateKind.PdfForm
                    || string.IsNullOrWhiteSpace(nested.TemplateName))
                {
                    continue;
                }

                names.Add(nested.TemplateName.Trim());
            }
        }

        var profileId = profile?.ID ?? Guid.Empty;
        if (objectSpace == null || profileId == Guid.Empty)
            return names;

        var fromDb = objectSpace.GetObjectsQuery<ApplicationProfileTemplate>()
            .Where(t => t.ApplicationProfileId == profileId
                && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm
                && t.TemplateName != null)
            .Select(t => t.TemplateName)
            .ToList();
        foreach (var nestedName in fromDb)
        {
            if (!string.IsNullOrWhiteSpace(nestedName))
                names.Add(nestedName.Trim());
        }

        return names;
    }

    private static ApplicationProfileTemplate? FindExistingByNameAndKind(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        string name,
        ApplicationProfileTemplateKind kind)
    {
        var fromCollection = profile.NestedTemplates?
            .Where(t => t != null
                && t.TemplateKind == kind
                && string.Equals(t.TemplateName, name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t!.RecycledAtUtc != null)
            .FirstOrDefault();
        if (fromCollection != null)
            return fromCollection;

        var profileId = profile.ID;
        if (objectSpace == null || profileId == Guid.Empty)
            return null;

        var lowered = name.ToLower();
        return objectSpace.GetObjectsQuery<ApplicationProfileTemplate>()
            .Where(t => t.ApplicationProfileId == profileId
                && t.TemplateKind == kind
                && t.TemplateName != null
                && t.TemplateName.ToLower() == lowered)
            .AsEnumerable()
            .OrderBy(t => t.RecycledAtUtc != null)
            .FirstOrDefault();
    }

    /// <summary>
    /// Shared-catalog Approve always updates the library master. This-profile Approve updates the
    /// private merge backing file only when that name is not already a Shared include.
    /// </summary>
    internal static bool ShouldWriteLinkedMasterFile(
        ApplicationProfileTemplateCatalogScope catalogScope,
        bool nameIsUsedAsSharedInclude) =>
        catalogScope != ApplicationProfileTemplateCatalogScope.ProfileSpecific
        || !nameIsUsedAsSharedInclude;

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
    /// Yellow-marked upload bytes. When omitted or empty, an existing
    /// <see cref="ApplicationProfileTemplate.SourceFile"/> is left unchanged (Convert overwrite).
    /// </summary>
    public byte[]? SourceContent { get; init; }

    public string? SourceFileName { get; init; }

    /// <summary>
    /// Approved Review snapshot. When omitted (Convert), an existing
    /// <see cref="ApplicationProfileTemplate.ReviewPlanJson"/> is left unchanged.
    /// </summary>
    public string? ReviewPlanJson { get; init; }

    /// <summary>
    /// When true, persist <see cref="ApplicableProjectContractId"/> / <see cref="ApplicableMigrationServiceId"/>
    /// (Create from yellow marks). Convert leaves this false so an overwrite does not wipe a wizard binding.
    /// </summary>
    public bool SetApplicability { get; init; }

    public Guid? ApplicableProjectContractId { get; init; }

    public Guid? ApplicableMigrationServiceId { get; init; }

    /// <summary>
    /// True for Review placeholders / Convert overwrite of the same catalog name.
    /// False for Create from yellow marks so a taken Word roster name becomes <c>_2</c>.
    /// </summary>
    public bool AllowOverwriteByName { get; init; } = true;
}
