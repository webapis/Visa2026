using System.Text.Json;
using System.Text.Json.Serialization;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Sidecar metadata written next to a staged template file (<c>*.docx.meta.json</c>).</summary>
public sealed class UserReportTemplateStagingMeta
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public Guid TemplateId { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public TemplateOutputFormat OutputFormat { get; set; }

    public string DocumentFileName { get; set; } = string.Empty;

    public DateTime ExportedAtUtc { get; set; }

    public string ExportedByUserName { get; set; } = string.Empty;

    /// <summary>SHA-256 (hex) of DB <see cref="FileData"/> content at export time.</summary>
    public string SourceContentHashSha256 { get; set; } = string.Empty;

    public DateTime? LastImportedAtUtc { get; set; }

    /// <summary>SHA-256 (hex) of staged file content after the last successful import.</summary>
    public string? LastImportedContentHashSha256 { get; set; }

    public static UserReportTemplateStagingMeta ReadFromFile(string metaFilePath)
    {
        var json = File.ReadAllText(metaFilePath);
        var meta = JsonSerializer.Deserialize<UserReportTemplateStagingMeta>(json, JsonOptions)
            ?? throw new InvalidOperationException("Staging meta file is empty or invalid.");
        return meta;
    }

    public void WriteToFile(string metaFilePath)
    {
        var directory = Path.GetDirectoryName(metaFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(metaFilePath, json);
    }
}
