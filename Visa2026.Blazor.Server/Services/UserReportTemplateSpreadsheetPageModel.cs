namespace Visa2026.Blazor.Server.Services;

public sealed class UserReportTemplateSpreadsheetPageModel
{
    public Guid TemplateId { get; init; }
    public string DocumentId { get; init; } = string.Empty;
    public Func<byte[]> ContentAccessor { get; init; } = () => Array.Empty<byte>();
    public bool CanEdit { get; init; }
    public bool HasContent { get; init; }
    public string FileName { get; init; } = "template.xlsx";
    public string SaveUrl { get; init; } = string.Empty;
    public string ReloadUrl { get; init; } = string.Empty;
    public string StatusSavedText { get; init; } = "Saved";
    public string StatusUnsavedText { get; init; } = "Unsaved changes";
    public string SaveButtonText { get; init; } = "Save to template";
    public string ReloadButtonText { get; init; } = "Reload from database";
    public string NoFileText { get; init; } = "Upload a template file on the General tab first.";
    public string ReadOnlyText { get; init; } = "Read-only";
    public string SaveSuccessMessage { get; init; } = "Template saved. Run Extract Placeholders if tokens changed.";
    public string SaveFailedMessage { get; init; } = "Could not save template.";
    public string ReloadConfirmMessage { get; init; } = "Reload and discard unsaved changes?";
    public bool HideToolbar { get; init; }
}
