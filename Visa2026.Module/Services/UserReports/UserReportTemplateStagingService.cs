using System.Security.Cryptography;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
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
        // Authentication for the HTTP upload path is enforced by [Authorize] on the controller.
        // EnsureEditAccess() (SecuritySystem.IsGranted) requires XAF's ValueManagerContext which is
        // only present in a Blazor circuit, not in a plain HTTP API request — skip it here.

        try
        {
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FailedImport(templateId, string.Empty, ex.Message);
        }
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

        var expectedExtension = UserReportTemplateStagingPathHelper.GetExtension(outputFormat);
        var fileName = template.TemplateFile?.FileName;
        if (string.IsNullOrWhiteSpace(fileName)
            || !fileName.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            var safeName = UserReportTemplateStagingPathHelper.SanitizeTemplateName(template.TemplateName);
            fileName = safeName + expectedExtension;
        }

        try
        {
            if (objectSpace is not EFCoreObjectSpace efObjectSpace)
                return FailedImport(templateId, displayName, "Database object space is not available.");

            // [FileAttachment] routes Content/LoadFromStream through Blazor browser storage, which is
            // absent in an HTTP upload context. Write directly to SQL, bypassing XAF change tracking.
            if (template.TemplateFile != null && template.TemplateFile.ID != Guid.Empty)
            {
                var rows = efObjectSpace.DbContext.Database.ExecuteSqlRaw(
                    "UPDATE [FileData] SET [Content] = {0}, [Size] = {1}, [FileName] = {2} WHERE [ID] = {3}",
                    stagedContent,
                    stagedContent.Length,
                    fileName,
                    template.TemplateFile.ID);
                if (rows != 1)
                    return FailedImport(templateId, displayName, "Template file content was not saved (0 rows affected).");
            }
            else
            {
                // No existing FileData row — insert one and wire the FK, all via raw SQL.
                var newFileId = Guid.NewGuid();
                efObjectSpace.DbContext.Database.ExecuteSqlRaw(
                    "INSERT INTO [FileData] ([ID], [GCRecord], [FileName], [Size], [Content]) VALUES ({0}, NULL, {1}, {2}, {3})",
                    newFileId, fileName, stagedContent.Length, stagedContent);
                efObjectSpace.DbContext.Database.ExecuteSqlRaw(
                    "UPDATE [UserReportTemplates] SET [TemplateFileID] = {0} WHERE [ID] = {1}",
                    newFileId, templateId);
            }
        }
        catch (Exception ex)
        {
            return FailedImport(templateId, displayName, ex.Message);
        }

        var extractValidateRan = false;
        int? invalidCount = null;
        if (_options.AutoExtractValidateOnImport)
        {
            try
            {
                var maintenance = await _maintenanceService
                    .ExtractAndValidatePlaceholdersAsync(templateId, cancellationToken)
                    .ConfigureAwait(false);
                extractValidateRan = maintenance.Extract.Success;
                invalidCount = maintenance.Validate?.InvalidCount;
            }
            catch
            {
                // Extract/validate runs inside a new ObjectSpace that also requires XAF context.
                // A failure here does not invalidate the import — the file content was saved.
            }
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
