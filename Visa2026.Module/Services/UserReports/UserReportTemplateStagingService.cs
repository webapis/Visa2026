using System.Security.Cryptography;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <inheritdoc cref="IUserReportTemplateStagingService"/>
public sealed class UserReportTemplateStagingService : IUserReportTemplateStagingService
{
    private readonly TemplateEditStagingOptions _options;
    private readonly INonSecuredObjectSpaceFactory _objectSpaceFactory;
    private readonly IUserReportTemplateMaintenanceService _maintenanceService;
    private readonly ILogger<UserReportTemplateStagingService> _logger;

    public UserReportTemplateStagingService(
        IOptions<TemplateEditStagingOptions> options,
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IUserReportTemplateMaintenanceService maintenanceService,
        ILogger<UserReportTemplateStagingService> logger)
    {
        _options = options.Value;
        _objectSpaceFactory = objectSpaceFactory;
        _maintenanceService = maintenanceService;
        _logger = logger;
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
        var documentPath = UserReportTemplateStagingPathHelper.BuildDocumentPath(
            _options,
            templateId,
            template.TemplateName,
            outputFormat);
        var metaPath = UserReportTemplateStagingPathHelper.BuildMetaFilePath(documentPath);

        var stagingRoot = UserReportTemplateStagingPathHelper.ResolveStagingRoot(_options);
        Directory.CreateDirectory(stagingRoot);

        await WriteAllBytesAsync(documentPath, content, cancellationToken).ConfigureAwait(false);

        var sourceHash = ComputeSha256Hex(content);
        var meta = new UserReportTemplateStagingMeta
        {
            TemplateId = templateId,
            TemplateName = template.TemplateName,
            OutputFormat = outputFormat,
            DocumentFileName = documentFileName,
            ExportedAtUtc = DateTime.UtcNow,
            ExportedByUserName = exportedByUserName?.Trim() ?? string.Empty,
            SourceContentHashSha256 = sourceHash,
            LastImportedAtUtc = null,
            LastImportedContentHashSha256 = null,
        };
        meta.WriteToFile(metaPath);

        var uncPath = UserReportTemplateStagingPathHelper.BuildUncPath(_options, documentFileName);
        return new UserReportTemplateStagingExportResult
        {
            TemplateId = templateId,
            DisplayName = template.TemplateName,
            DocumentFileName = documentFileName,
            UncPath = uncPath,
            OfficeOpenUrl = UserReportTemplateStagingPathHelper.TryBuildOfficeOpenUrl(uncPath, outputFormat),
        };
    }

    public Task<UserReportTemplateStagingImportResult> TryImportAsync(
        Guid templateId,
        CancellationToken cancellationToken = default) =>
        ImportCoreAsync(templateId, cancellationToken);

    public async Task<UserReportTemplateStagingImportAllResult> ImportAllChangedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureEnabled();
        EnsureEditAccess();

        var stagingRoot = UserReportTemplateStagingPathHelper.ResolveStagingRoot(_options);
        if (!Directory.Exists(stagingRoot))
        {
            return new UserReportTemplateStagingImportAllResult
            {
                Results = Array.Empty<UserReportTemplateStagingImportResult>(),
            };
        }

        var templateIds = new HashSet<Guid>();
        foreach (var metaPath in Directory.EnumerateFiles(stagingRoot, "*.meta.json", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var meta = UserReportTemplateStagingMeta.ReadFromFile(metaPath);
                if (meta.TemplateId != Guid.Empty)
                    templateIds.Add(meta.TemplateId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping invalid staging meta file {MetaPath}", metaPath);
            }
        }

        var results = new List<UserReportTemplateStagingImportResult>();
        foreach (var templateId in templateIds.OrderBy(id => id))
        {
            results.Add(await ImportCoreAsync(templateId, cancellationToken).ConfigureAwait(false));
        }

        return new UserReportTemplateStagingImportAllResult { Results = results };
    }

    private async Task<UserReportTemplateStagingImportResult> ImportCoreAsync(
        Guid templateId,
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
        var documentPath = UserReportTemplateStagingPathHelper.BuildDocumentPath(
            _options,
            templateId,
            template.TemplateName,
            outputFormat);
        var metaPath = UserReportTemplateStagingPathHelper.BuildMetaFilePath(documentPath);

        if (!File.Exists(documentPath))
        {
            return SkippedImport(
                templateId,
                displayName,
                UserReportTemplateStagingImportStatus.SkippedNotFound,
                "Staged file not found.");
        }

        UserReportTemplateStagingMeta meta;
        try
        {
            meta = File.Exists(metaPath)
                ? UserReportTemplateStagingMeta.ReadFromFile(metaPath)
                : new UserReportTemplateStagingMeta { TemplateId = templateId };
        }
        catch (Exception ex)
        {
            return FailedImport(templateId, displayName, $"Invalid meta file: {ex.Message}");
        }

        if (meta.TemplateId != Guid.Empty && meta.TemplateId != templateId)
        {
            return FailedImport(templateId, displayName, "Staging meta template id mismatch.");
        }

        byte[] stagedContent;
        try
        {
            stagedContent = await ReadAllBytesWithShareReadAsync(documentPath, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Staged file locked for template {TemplateId}", templateId);
            return FailedImport(
                templateId,
                displayName,
                "The staged file is locked. Close Word or Excel and try again.");
        }

        if (stagedContent.Length == 0)
            return FailedImport(templateId, displayName, "Staged file is empty.");

        if (stagedContent.Length > _options.MaxFileSizeBytes)
        {
            return FailedImport(
                templateId,
                displayName,
                $"Staged file exceeds maximum size ({_options.MaxFileSizeBytes} bytes).");
        }

        var stagedHash = ComputeSha256Hex(stagedContent);
        if (!string.IsNullOrEmpty(meta.LastImportedContentHashSha256)
            && string.Equals(meta.LastImportedContentHashSha256, stagedHash, StringComparison.OrdinalIgnoreCase))
        {
            return SkippedImport(
                templateId,
                displayName,
                UserReportTemplateStagingImportStatus.SkippedUnchanged,
                null);
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

        meta.TemplateId = templateId;
        meta.TemplateName = template.TemplateName;
        meta.OutputFormat = outputFormat;
        meta.DocumentFileName = Path.GetFileName(documentPath);
        meta.LastImportedAtUtc = DateTime.UtcNow;
        meta.LastImportedContentHashSha256 = stagedHash;
        meta.WriteToFile(metaPath);

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

    private static async Task WriteAllBytesAsync(string path, byte[] content, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous);
        await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadAllBytesWithShareReadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            FileOptions.Asynchronous);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return memory.ToArray();
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
