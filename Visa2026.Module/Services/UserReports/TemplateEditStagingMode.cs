namespace Visa2026.Module.Services.UserReports;

/// <summary>How Resminamalar delivers templates for desktop Word/Excel editing.</summary>
public enum TemplateEditStagingMode
{
    /// <summary>Server SMB share + UNC paths (default on-prem IIS).</summary>
    Share = 0,

    /// <summary>Browser File System Access API folder on the officer PC (requires HTTPS).</summary>
    LocalFolder = 1,
}
