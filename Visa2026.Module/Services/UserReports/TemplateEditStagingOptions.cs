namespace Visa2026.Module.Services.UserReports;

/// <summary>Network-share staging for desktop Word/Excel template editing from Resminamalar.</summary>
public sealed class TemplateEditStagingOptions
{
    public const string SectionName = "TemplateEditStaging";

    /// <summary>When false, export/import API calls fail fast.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// UNC share root only, e.g. <c>\\fileserver\Visa2026TemplateEdit</c> or <c>\\127.0.0.1\Visa2026TemplateEdit</c>.
    /// Local drive paths (e.g. <c>D:\...</c>) are not supported — use the share UNC.
    /// </summary>
    public string StagingRootUnc { get; set; } = string.Empty;

    /// <summary>Document file name under the staging root. Tokens: <c>{templateId}</c>, <c>{safeName}</c>, <c>{extension}</c>.</summary>
    public string FileNamePattern { get; set; } = "{templateId}_{safeName}{extension}";

    /// <summary>After import when file hash changed, run Extract then Validate placeholders.</summary>
    public bool AutoExtractValidateOnImport { get; set; } = true;

    /// <summary>Reject imports larger than this size (default 50 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800;
}
