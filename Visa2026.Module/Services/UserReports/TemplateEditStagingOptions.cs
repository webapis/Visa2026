namespace Visa2026.Module.Services.UserReports;

/// <summary>Local sandbox staging for desktop Word/Excel template editing from Resminamalar.</summary>
public sealed class TemplateEditStagingOptions
{
    public const string SectionName = "TemplateEditStaging";

    /// <summary>When false, export/import API calls fail fast.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Relative path under each officer's <c>%LOCALAPPDATA%</c> for the template sandbox
    /// (e.g. <c>Visa2026\TemplateEdit</c> → <c>%LOCALAPPDATA%\Visa2026\TemplateEdit</c>).
    /// </summary>
    public string LocalFolderSubfolderName { get; set; } = @"Visa2026\TemplateEdit";

    /// <summary>Document file name under the local sandbox. Tokens: <c>{templateId}</c>, <c>{safeName}</c>, <c>{extension}</c>.</summary>
    public string FileNamePattern { get; set; } = "{safeName}{extension}";

    /// <summary>After import when file hash changed, run Extract then Validate placeholders.</summary>
    public bool AutoExtractValidateOnImport { get; set; } = true;

    /// <summary>Reject imports larger than this size (default 50 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800;
}
