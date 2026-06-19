using System.Collections.Concurrent;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Services;

/// <summary>Loads and saves <see cref="UserReportTemplate"/> Excel bytes for the ASP.NET Core Spreadsheet host.</summary>
public sealed class UserReportTemplateSpreadsheetFileService
{
    private readonly INonSecuredObjectSpaceFactory _objectSpaceFactory;
    private readonly UserReportTemplateSpreadsheetHttpAccess _httpAccess;

    public UserReportTemplateSpreadsheetFileService(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        UserReportTemplateSpreadsheetHttpAccess httpAccess)
    {
        _objectSpaceFactory = objectSpaceFactory;
        _httpAccess = httpAccess;
    }

    public bool CanReadTemplates() => _httpAccess.CanReadTemplates();

    public bool CanEditTemplates() => _httpAccess.CanEditTemplates();

    public UserReportTemplateSpreadsheetLoadResult? TryLoad(Guid templateId)
    {
        if (templateId == Guid.Empty || !CanReadTemplates())
            return null;

        using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(UserReportTemplate));
        var template = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.TemplateFile)
            .FirstOrDefault(t => t.ID == templateId);

        if (template == null)
            return null;

        if (template.GetEffectiveOutputFormat() != TemplateOutputFormat.Excel)
            return null;

        var file = template.TemplateFile;
        var content = ReadFileContent(objectSpace, file);
        if (content == null || content.Length == 0)
            return new UserReportTemplateSpreadsheetLoadResult(templateId, Array.Empty<byte>(), file?.FileName ?? "template.xlsx");

        return new UserReportTemplateSpreadsheetLoadResult(
            templateId,
            content,
            string.IsNullOrWhiteSpace(file?.FileName) ? "template.xlsx" : file!.FileName);
    }

    private static byte[]? ReadFileContent(IObjectSpace objectSpace, FileData? file)
    {
        if (file == null)
            return null;

        var content = file.Content;
        if (content != null && content.Length > 0)
            return content.ToArray();

        if (file.ID == Guid.Empty)
            return content;

        return objectSpace.GetObjectsQuery<FileData>()
            .Where(f => f.ID == file.ID)
            .Select(f => f.Content)
            .FirstOrDefault();
    }

    public UserReportTemplateSpreadsheetSaveResult TrySave(Guid templateId, byte[] content)
    {
        if (templateId == Guid.Empty || content == null || content.Length == 0)
            return UserReportTemplateSpreadsheetSaveResult.Failed("Empty content.");

        if (!CanEditTemplates())
            return UserReportTemplateSpreadsheetSaveResult.Failed("Access denied.");

        using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(UserReportTemplate));
        var template = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.TemplateFile)
            .FirstOrDefault(t => t.ID == templateId);

        if (template == null)
            return UserReportTemplateSpreadsheetSaveResult.Failed("Template not found.");

        if (template.GetEffectiveOutputFormat() != TemplateOutputFormat.Excel)
            return UserReportTemplateSpreadsheetSaveResult.Failed("Not an Excel template.");

        if (template.TemplateFile == null)
            template.TemplateFile = objectSpace.CreateObject<FileData>();

        template.TemplateFile.Content = content;
        if (string.IsNullOrWhiteSpace(template.TemplateFile.FileName)
            || !template.TemplateFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = string.IsNullOrWhiteSpace(template.TemplateName)
                ? "template"
                : template.TemplateName.Trim();
            template.TemplateFile.FileName = $"{baseName}.xlsx";
        }

        objectSpace.CommitChanges();
        return UserReportTemplateSpreadsheetSaveResult.Succeeded();
    }
}

public sealed record UserReportTemplateSpreadsheetLoadResult(
    Guid TemplateId,
    byte[] Content,
    string FileName);

public sealed class UserReportTemplateSpreadsheetSaveResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static UserReportTemplateSpreadsheetSaveResult Succeeded() =>
        new() { Success = true };

    public static UserReportTemplateSpreadsheetSaveResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>Builds per-user Spreadsheet document ids and tracks reload generations.</summary>
public sealed class UserReportTemplateSpreadsheetSessionService
{
    private readonly ConcurrentDictionary<string, int> _generations = new(StringComparer.Ordinal);

    public string BuildDocumentId(Guid templateId, string? userKey, int generation) =>
        $"urt-{SanitizeKey(userKey)}-{templateId:N}-g{generation}";

    public int GetGeneration(Guid templateId, string? userKey)
    {
        var key = BuildSessionKey(templateId, userKey);
        return _generations.TryGetValue(key, out var generation) ? generation : 0;
    }

    public int BumpGeneration(Guid templateId, string? userKey)
    {
        var key = BuildSessionKey(templateId, userKey);
        return _generations.AddOrUpdate(key, 1, static (_, current) => current + 1);
    }

    private static string BuildSessionKey(Guid templateId, string? userKey) =>
        $"{SanitizeKey(userKey)}:{templateId:N}";

    private static string SanitizeKey(string? userKey) =>
        string.IsNullOrWhiteSpace(userKey) ? "anonymous" : userKey.Trim();
}
