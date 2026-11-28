using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Puts a template on the officer PC under %LOCALAPPDATA%\Visa2026\TemplateEdit.
/// Development (browser on this PC) writes the folder directly. Production (another server)
/// hands a short-lived token to a one-time opener installed on the officer PC.
/// </summary>
public sealed class TemplateEditDesktopBroker
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IMemoryCache _cache;
    private readonly IUserReportTemplateStagingService _stagingService;
    private readonly TemplateEditStagingOptions _options;
    private readonly TemplateEditRibbonTokenService _ribbonTokens;

    public TemplateEditDesktopBroker(
        IMemoryCache cache,
        IUserReportTemplateStagingService stagingService,
        IOptions<TemplateEditStagingOptions> options,
        TemplateEditRibbonTokenService ribbonTokens)
    {
        _cache = cache;
        _stagingService = stagingService;
        _options = options.Value;
        _ribbonTokens = ribbonTokens;
    }

    public async Task<TemplateEditMachineWriteResponse> WriteOnThisMachineAsync(
        HttpContext http,
        Guid templateId,
        string userName,
        CancellationToken cancellationToken)
    {
        var folder = TemplateEditStagingPathHints.TryThisMachineFolder(_options, http, create: true);
        if (folder == null)
            return new TemplateEditMachineWriteResponse { Available = false };

        var export = await _stagingService
            .ExportForAuthorizedHttpAsync(templateId, userName, cancellationToken)
            .ConfigureAwait(false);
        var serverBase = http.Request.Scheme + "://" + http.Request.Host;
        var outcome = await SaveExportAsync(folder, export, userName, serverBase, cancellationToken).ConfigureAwait(false);
        return outcome;
    }

    public async Task<TemplateEditMachineImportResponse> ImportOnThisMachineAsync(
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var folder = TemplateEditStagingPathHints.TryThisMachineFolder(_options, http, create: false);
        if (folder == null)
            return new TemplateEditMachineImportResponse { Handled = false };

        if (!Directory.Exists(folder))
            return EmptyImport();

        var uploads = new List<TemplateEditDesktopUploadItem>();
        foreach (var metaPath in Directory.EnumerateFiles(folder, "*.meta.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            UserReportTemplateStagingMeta meta;
            try
            {
                meta = UserReportTemplateStagingMeta.ReadFromFile(metaPath);
            }
            catch (Exception ex)
            {
                uploads.Add(Failed(Guid.Empty, Path.GetFileName(metaPath), ex.Message));
                continue;
            }

            if (meta.TemplateId == Guid.Empty || string.IsNullOrWhiteSpace(meta.DocumentFileName))
                continue;

            var docPath = Path.Combine(folder, meta.DocumentFileName);
            if (!File.Exists(docPath))
            {
                uploads.Add(new TemplateEditDesktopUploadItem
                {
                    TemplateId = meta.TemplateId,
                    Status = nameof(UserReportTemplateStagingImportStatus.SkippedNotFound),
                    FileName = meta.DocumentFileName,
                    DisplayName = meta.TemplateName,
                });
                continue;
            }

            byte[] bytes;
            try
            {
                bytes = await File.ReadAllBytesAsync(docPath, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                uploads.Add(Failed(meta.TemplateId, meta.DocumentFileName, "The template file is locked."));
                continue;
            }

            var hash = ComputeSha256Hex(bytes);
            var imported = await _stagingService
                .ImportFromUploadAsync(meta.TemplateId, bytes, cancellationToken)
                .ConfigureAwait(false);
            uploads.Add(new TemplateEditDesktopUploadItem
            {
                TemplateId = imported.TemplateId,
                Status = imported.Status.ToString(),
                ErrorMessage = imported.ErrorMessage,
                FileName = meta.DocumentFileName,
                DisplayName = string.IsNullOrWhiteSpace(imported.DisplayName) ? meta.TemplateName : imported.DisplayName,
            });
            if (imported.Status is UserReportTemplateStagingImportStatus.Imported
                or UserReportTemplateStagingImportStatus.SkippedUnchanged)
            {
                meta.SourceContentHashSha256 = hash;
                meta.LastImportedContentHashSha256 = hash;
                meta.LastImportedAtUtc = DateTime.UtcNow;
                meta.WriteToFile(metaPath);
            }
        }

        return Summarize(uploads);
    }

    public TemplateEditDesktopLaunchResponse CreateLaunch(
        HttpContext http,
        string purpose,
        Guid templateId,
        string userName)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var session = new TemplateEditDesktopSession
        {
            Token = token,
            Purpose = purpose,
            TemplateId = templateId,
            UserName = userName,
        };
        _cache.Set(CacheKey(token), session, TimeSpan.FromMinutes(3));

        var baseUri = $"{http.Request.Scheme}://{http.Request.Host}";
        var folder = FolderRelative();
        var launchUrl = "visa2026edit:"
            + purpose
            + "?token=" + token
            + "&base=" + Uri.EscapeDataString(baseUri)
            + "&folder=" + Uri.EscapeDataString(folder);
        return new TemplateEditDesktopLaunchResponse
        {
            Token = token,
            LaunchUrl = launchUrl,
        };
    }

    public async Task<TemplateEditDesktopFilePayload?> ReadOpenFileAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (!TryGet(token, out var session) || session.Purpose != "open" || session.TemplateId == Guid.Empty)
            return null;

        var export = await _stagingService
            .ExportForAuthorizedHttpAsync(session.TemplateId, session.UserName, cancellationToken)
            .ConfigureAwait(false);
        if (export.FileContent == null || export.FileContent.Length == 0)
            return null;

        return new TemplateEditDesktopFilePayload
        {
            TemplateId = export.TemplateId,
            DisplayName = export.DisplayName,
            DocumentFileName = export.DocumentFileName,
            SourceContentHashSha256 = export.SourceContentHashSha256 ?? string.Empty,
            OutputFormat = export.OutputFormat.ToString(),
            FileBase64 = Convert.ToBase64String(export.FileContent),
            FolderRelative = FolderRelative(),
            RibbonToken = _ribbonTokens.IssueHost(session.UserName),
        };
    }

    public async Task<UserReportTemplateStagingImportResult?> ImportDesktopUploadAsync(
        string token,
        Guid templateId,
        byte[] content,
        CancellationToken cancellationToken)
    {
        if (!TryGet(token, out var session) || session.Purpose != "sync")
            return null;

        if (templateId == Guid.Empty)
            return null;

        return await _stagingService
            .ImportFromUploadAsync(templateId, content, cancellationToken)
            .ConfigureAwait(false);
    }

    public bool Complete(string token, JsonElement result)
    {
        if (!TryGet(token, out var session))
            return false;

        session.Completed = true;
        session.ResultJson = result.GetRawText();
        _cache.Set(CacheKey(token), session, TimeSpan.FromMinutes(3));
        return true;
    }

    public bool TryReadResult(string token, string userName, out bool completed, out JsonElement? result)
    {
        completed = false;
        result = null;
        if (!TryGet(token, out var session))
            return false;

        if (!string.Equals(session.UserName, userName, StringComparison.OrdinalIgnoreCase))
            return false;

        completed = session.Completed;
        if (!string.IsNullOrWhiteSpace(session.ResultJson))
            result = JsonSerializer.Deserialize<JsonElement>(session.ResultJson, JsonOptions);

        return true;
    }

    private async Task<TemplateEditMachineWriteResponse> SaveExportAsync(
        string folder,
        UserReportTemplateStagingExportResult export,
        string userName,
        string serverBase,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(folder, export.DocumentFileName);
        var sourceHash = export.SourceContentHashSha256 ?? string.Empty;
        var response = new TemplateEditMachineWriteResponse
        {
            Available = true,
            TemplateId = export.TemplateId,
            DisplayName = export.DisplayName,
            FileName = export.DocumentFileName,
            FolderName = folder,
            CopyPath = path,
            SuggestedLocalFilePath = path,
            OutputFormat = export.OutputFormat.ToString(),
        };

        if (File.Exists(path))
        {
            try
            {
                var existing = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
                var existingHash = ComputeSha256Hex(existing);
                if (!string.IsNullOrEmpty(sourceHash)
                    && !string.Equals(existingHash, sourceHash, StringComparison.OrdinalIgnoreCase))
                {
                    response.Code = "needsSync";
                    StampRibbon(folder, path, export, userName, serverBase, sourceHash);
                    response.Opened = TryStartOffice(path);
                    return response;
                }
            }
            catch (IOException)
            {
                response.Code = "fileLocked";
                StampRibbon(folder, path, export, userName, serverBase, sourceHash);
                return response;
            }
        }
        else if (export.FileContent != null)
        {
            try
            {
                await File.WriteAllBytesAsync(path, export.FileContent, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                response.Code = "fileLocked";
                return response;
            }
        }

        StampRibbon(folder, path, export, userName, serverBase, sourceHash);
        response.Code = "exported";
        response.Opened = TryStartOffice(path);
        return response;
    }

    private void StampRibbon(
        string folder,
        string documentPath,
        UserReportTemplateStagingExportResult export,
        string userName,
        string serverBase,
        string sourceHash)
    {
        if (string.IsNullOrWhiteSpace(serverBase) || export.TemplateId == Guid.Empty)
            return;

        try
        {
            var token = _ribbonTokens.IssueHost(userName);
            var origin = serverBase.TrimEnd('/');
            var hostJson = JsonSerializer.Serialize(
                new RibbonHostFile { ServerBase = origin, RibbonToken = token },
                JsonOptions);
            File.WriteAllText(Path.Combine(folder, "ribbon-host.json"), hostJson);

            var metaPath = documentPath + ".meta.json";
            UserReportTemplateStagingMeta meta;
            if (File.Exists(metaPath))
            {
                try
                {
                    meta = UserReportTemplateStagingMeta.ReadFromFile(metaPath);
                }
                catch (Exception)
                {
                    meta = new UserReportTemplateStagingMeta();
                }
            }
            else
            {
                meta = new UserReportTemplateStagingMeta();
            }

            meta.TemplateId = export.TemplateId;
            meta.TemplateName = string.IsNullOrWhiteSpace(export.DisplayName) ? meta.TemplateName : export.DisplayName;
            meta.OutputFormat = export.OutputFormat;
            meta.DocumentFileName = export.DocumentFileName;
            if (meta.ExportedAtUtc == default)
                meta.ExportedAtUtc = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(meta.ExportedByUserName))
                meta.ExportedByUserName = userName;
            if (string.IsNullOrWhiteSpace(meta.SourceContentHashSha256))
                meta.SourceContentHashSha256 = sourceHash;
            meta.ServerBase = origin;
            meta.RibbonToken = token;
            meta.WriteToFile(metaPath);
        }
        catch (Exception)
        {
            // Opening Word still succeeds when the ribbon sidecar cannot be written.
        }
    }

    private sealed class RibbonHostFile
    {
        public string ServerBase { get; set; } = string.Empty;

        public string RibbonToken { get; set; } = string.Empty;
    }

    private static bool TryStartOffice(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string FolderRelative() =>
        string.IsNullOrWhiteSpace(_options.LocalFolderSubfolderName)
            ? @"Visa2026\TemplateEdit"
            : _options.LocalFolderSubfolderName.Trim('\\', '/');

    private bool TryGet(string token, out TemplateEditDesktopSession session)
    {
        session = null!;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128)
            return false;

        return _cache.TryGetValue(CacheKey(token), out session!) && session != null;
    }

    private static string CacheKey(string token) => "template-edit-desktop:" + token;

    private static string ComputeSha256Hex(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content));

    private static TemplateEditDesktopUploadItem Failed(Guid templateId, string fileName, string message) =>
        new()
        {
            TemplateId = templateId,
            Status = nameof(UserReportTemplateStagingImportStatus.Failed),
            ErrorMessage = message,
            FileName = fileName,
        };

    private static TemplateEditMachineImportResponse EmptyImport() =>
        new() { Handled = true };

    private static TemplateEditMachineImportResponse Summarize(List<TemplateEditDesktopUploadItem> uploads) =>
        new()
        {
            Handled = true,
            ImportedCount = uploads.Count(x => x.Status == nameof(UserReportTemplateStagingImportStatus.Imported)),
            SkippedUnchangedCount = uploads.Count(x => x.Status == nameof(UserReportTemplateStagingImportStatus.SkippedUnchanged)),
            SkippedNotFoundCount = uploads.Count(x => x.Status == nameof(UserReportTemplateStagingImportStatus.SkippedNotFound)),
            FailedCount = uploads.Count(x => x.Status == nameof(UserReportTemplateStagingImportStatus.Failed)),
            Uploads = uploads,
        };
}

public sealed class TemplateEditDesktopSession
{
    public string Token { get; init; } = string.Empty;

    public string Purpose { get; init; } = string.Empty;

    public Guid TemplateId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public bool Completed { get; set; }

    public string? ResultJson { get; set; }
}

public sealed class TemplateEditMachineWriteResponse
{
    public bool Available { get; set; }

    public string Code { get; set; } = string.Empty;

    public Guid TemplateId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FolderName { get; set; } = string.Empty;

    public string? CopyPath { get; set; }

    public string? SuggestedLocalFilePath { get; set; }

    public string OutputFormat { get; set; } = string.Empty;

    public bool Opened { get; set; }
}

public sealed class TemplateEditMachineImportResponse
{
    public bool Handled { get; set; }

    public int ImportedCount { get; set; }

    public int SkippedUnchangedCount { get; set; }

    public int SkippedNotFoundCount { get; set; }

    public int FailedCount { get; set; }

    public bool Cancelled { get; set; }

    public List<TemplateEditDesktopUploadItem> Uploads { get; set; } = new();
}

public sealed class TemplateEditDesktopUploadItem
{
    public Guid TemplateId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? FileName { get; set; }

    public string? DisplayName { get; set; }
}

public sealed class TemplateEditDesktopLaunchResponse
{
    public string Token { get; set; } = string.Empty;

    public string LaunchUrl { get; set; } = string.Empty;
}

public sealed class TemplateEditDesktopFilePayload
{
    public Guid TemplateId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string DocumentFileName { get; set; } = string.Empty;

    public string SourceContentHashSha256 { get; set; } = string.Empty;

    public string OutputFormat { get; set; } = string.Empty;

    public string FileBase64 { get; set; } = string.Empty;

    public string FolderRelative { get; set; } = string.Empty;

    /// <summary>Authorizes the Home-tab Sync button for every template this officer edits.</summary>
    public string RibbonToken { get; set; } = string.Empty;
}
