namespace Visa2026.Module.Services.UserReports;

/// <summary>Network-share staging for desktop Word/Excel template editing from Resminamalar.</summary>
public sealed class TemplateEditStagingOptions
{
    public const string SectionName = "TemplateEditStaging";

    /// <summary>When false, export/import API calls fail fast.</summary>
    public bool Enabled { get; set; }

    /// <summary>Share (UNC) or local officer folder via browser File System Access API.</summary>
    public TemplateEditStagingMode Mode { get; set; } = TemplateEditStagingMode.Share;

    /// <summary>
    /// UNC share root for officers and Office open URLs, e.g. <c>\\fileserver\Visa2026TemplateEdit</c>.
    /// </summary>
    public string StagingRootUnc { get; set; } = string.Empty;

    /// <summary>
    /// Optional local folder for app export/import I/O (IIS app pool). When set, file operations use this path
    /// while <see cref="StagingRootUnc"/> is returned to officers. Required on IIS when the pool cannot write via UNC.
    /// </summary>
    public string StagingLocalPath { get; set; } = string.Empty;

    /// <summary>Document file name under the staging root. Tokens: <c>{templateId}</c>, <c>{safeName}</c>, <c>{extension}</c>.</summary>
    public string FileNamePattern { get; set; } = "{safeName}{extension}";

    /// <summary>After import when file hash changed, run Extract then Validate placeholders.</summary>
    public bool AutoExtractValidateOnImport { get; set; } = true;

    /// <summary>Reject imports larger than this size (default 50 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800;
}
