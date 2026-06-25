using System.Security.Cryptography;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <inheritdoc cref="IUserReportTemplateStagingService"/>
public sealed class UserReportTemplateStagingService : IUserReportTemplateStagingService
{
    private readonly TemplateEditStagingOptions _options;
    private readonly INonSecuredObjectSpaceFactory _objectSpaceFactory;
    private readonly IUserReportTemplateMaintenanceService _maintenanceService;

    public UserReportTemplateStagingService(
        IOptions<TemplateEditStagingOptions> options,
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IUserReportTemplateMaintenanceService maintenanceService)
    {
        _options = options.Value;
        _objectSpaceFactory = objectSpaceFactory;
        _maintenanceService = maintenanceService;
    }

    public async Task<UserReportTemplateStagingExportResult> ExportForEditAsync(
        Guid templateId,
        string exportedByUserName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureEnabled();
        EnsureEditAccess();

        if (templateId == Guid.Empty)
            throw new ArgumentException("Template id is required.", nameof(templateId));

        using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace<UserReportTemplate>();
        var template = LoadTemplate(objectSpace, templateId)
            ?? throw new InvalidOperationException("Template not found.");

        var content = ReadFileContent(objectSpace, template.TemplateFile);
        if (content == null || content.Length == 0)
            throw new InvalidOperationException("Template file has no content.");

        var outputFormat = template.GetEffectiveOutputFormat();
        var documentFileName = UserReportTemplateStagingPathHelper.BuildDocumentFileName(
            _options,
            templateId,
            template.TemplateName,
            outputFormat);

        return new UserReportTemplateStagingExportResult
        {
            TemplateId = templateId,
            DisplayName = template.TemplateName,
            DocumentFileName = documentFileName,
            SourceContentHashSha256 = ComputeSha256Hex(content),
            OutputFormat = outputFormat,
            FileContent = content,
        };
    }

    public Task<UserReportTemplateStagingImportResult> ImportFromUploadAsync(
        Guid templateId,
        byte[] content,
        CancellationToken cancellationToken = default) =>
        ImportUploadedContentAsync(templateId, content, cancellationToken);

    private async Task<UserReportTemplateStagingImportResult> ImportUploadedContentAsync(
        Guid templateId,
        byte[] stagedContent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureEnabled();
        EnsureEditAccess();

        using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace<UserReportTemplate>();
        var template = LoadTemplate(objectSpace, templateId);
        if (template == null)
        {
            return FailedImport(templateId, "Unknown template", "Template not found.");
        }

        var displayName = template.TemplateName;
        var outputFormat = template.GetEffectiveOutputFormat();

        if (stagedContent.Length == 0)
            return FailedImport(templateId, displayName, "Uploaded file is empty.");

        if (stagedContent.Length > _options.MaxFileSizeBytes)
        {
            return FailedImport(
                templateId,
                displayName,
                $"Uploaded file exceeds maximum size ({_options.MaxFileSizeBytes} bytes).");
        }

        var currentContent = ReadFileContent(objectSpace, template.TemplateFile) ?? Array.Empty<byte>();
        var stagedHash = ComputeSha256Hex(stagedContent);
        var currentHash = currentContent.Length > 0 ? ComputeSha256Hex(currentContent) : string.Empty;
        if (!string.IsNullOrEmpty(currentHash)
            && string.Equals(currentHash, stagedHash, StringComparison.OrdinalIgnoreCase))
        {
            return SkippedImport(
                templateId,
                displayName,
                UserReportTemplateStagingImportStatus.SkippedUnchanged,
                null);
        }

        return await ApplyImportedContentAsync(
            objectSpace,
            template,
            templateId,
            displayName,
            outputFormat,
            stagedContent,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<UserReportTemplateStagingImportResult> ApplyImportedContentAsync(
        IObjectSpace objectSpace,
        UserReportTemplate template,
        Guid templateId,
        string displayName,
        TemplateOutputFormat outputFormat,
        byte[] stagedContent,
        CancellationToken cancellationToken)
    {
        if (stagedContent.Length > _options.MaxFileSizeBytes)
        {
            return FailedImport(
                templateId,
                displayName,
                $"Staged file exceeds maximum size ({_options.MaxFileSizeBytes} bytes).");
        }

        if (template.TemplateFile == null)
            template.TemplateFile = objectSpace.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();

        template.TemplateFile.Content = stagedContent;
        var expectedExtension = UserReportTemplateStagingPathHelper.GetExtension(outputFormat);
        if (string.IsNullOrWhiteSpace(template.TemplateFile.FileName)
            || !template.TemplateFile.FileName.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            var safeName = UserReportTemplateStagingPathHelper.SanitizeTemplateName(template.TemplateName);
            template.TemplateFile.FileName = safeName + expectedExtension;
        }

        objectSpace.CommitChanges();

        var extractValidateRan = false;
        int? invalidCount = null;
        if (_options.AutoExtractValidateOnImport)
        {
            var maintenance = await _maintenanceService
                .ExtractAndValidatePlaceholdersAsync(templateId, cancellationToken)
                .ConfigureAwait(false);
            extractValidateRan = maintenance.Extract.Success;
            invalidCount = maintenance.Validate?.InvalidCount;
        }

        return new UserReportTemplateStagingImportResult
        {
            TemplateId = templateId,
            DisplayName = displayName,
            Status = UserReportTemplateStagingImportStatus.Imported,
            ExtractValidateRan = extractValidateRan,
            InvalidPlaceholderCount = invalidCount,
        };
    }

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("Template staging edit is disabled.");
    }

    private static void EnsureEditAccess()
    {
        if (!UserReportTemplateEditAccess.CanEditTemplates())
            throw new UnauthorizedAccessException("Template edit access denied.");
    }

    private static UserReportTemplate? LoadTemplate(IObjectSpace objectSpace, Guid templateId) =>
        objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.TemplateFile)
            .Include(t => t.Placeholders)
            .FirstOrDefault(t => t.ID == templateId);

    private static byte[]? ReadFileContent(IObjectSpace objectSpace, DevExpress.Persistent.BaseImpl.EF.FileData? file)
    {
        if (file == null)
            return null;

        var content = file.Content;
        if (content != null && content.Length > 0)
            return content.ToArray();

        if (file.ID == Guid.Empty)
            return content;

        return objectSpace.GetObjectsQuery<DevExpress.Persistent.BaseImpl.EF.FileData>()
            .Where(f => f.ID == file.ID)
            .Select(f => f.Content)
            .FirstOrDefault();
    }

    private static string ComputeSha256Hex(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash);
    }

    private static UserReportTemplateStagingImportResult SkippedImport(
        Guid templateId,
        string displayName,
        UserReportTemplateStagingImportStatus status,
        string? message) =>
        new()
        {
            TemplateId = templateId,
            DisplayName = displayName,
            Status = status,
            ErrorMessage = message,
        };

    private static UserReportTemplateStagingImportResult FailedImport(
        Guid templateId,
        string displayName,
        string message) =>
        new()
        {
            TemplateId = templateId,
            DisplayName = displayName,
            Status = UserReportTemplateStagingImportStatus.Failed,
            ErrorMessage = message,
        };
}
