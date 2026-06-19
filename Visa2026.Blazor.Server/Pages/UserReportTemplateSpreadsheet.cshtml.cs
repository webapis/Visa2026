using System.Text.Json;
using DevExpress.AspNetCore.Spreadsheet;
using DevExpress.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server.Pages;

[Authorize]
public class UserReportTemplateSpreadsheetModel : PageModel
{
    private readonly UserReportTemplateSpreadsheetFileService _fileService;
    private readonly UserReportTemplateSpreadsheetSessionService _sessionService;
    private readonly UserReportTemplateSpreadsheetHttpAccess _httpAccess;

    public UserReportTemplateSpreadsheetModel(
        UserReportTemplateSpreadsheetFileService fileService,
        UserReportTemplateSpreadsheetSessionService sessionService,
        UserReportTemplateSpreadsheetHttpAccess httpAccess)
    {
        _fileService = fileService;
        _sessionService = sessionService;
        _httpAccess = httpAccess;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TemplateId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Reload { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Embed { get; set; }

    public UserReportTemplateSpreadsheetPageModel Spreadsheet { get; private set; } = new();

    public IActionResult OnGet()
    {
        if (TemplateId == Guid.Empty || !_fileService.CanReadTemplates())
            return NotFound();

        var userKey = _httpAccess.ResolveCurrentUserKey();
        var generation = Reload
            ? _sessionService.BumpGeneration(TemplateId, userKey)
            : _sessionService.GetGeneration(TemplateId, userKey);

        var loaded = _fileService.TryLoad(TemplateId);
        if (loaded == null)
            return NotFound();

        byte[] contentSnapshot = loaded.Content;
        Spreadsheet = new UserReportTemplateSpreadsheetPageModel
        {
            TemplateId = TemplateId,
            DocumentId = _sessionService.BuildDocumentId(TemplateId, userKey, generation),
            ContentAccessor = () => contentSnapshot,
            CanEdit = _fileService.CanEditTemplates(),
            HasContent = contentSnapshot.Length > 0,
            FileName = loaded.FileName,
            SaveUrl = Url.Page(pageName: null, pageHandler: "Save") ?? string.Empty,
            ReloadUrl = Url.Page(pageName: null, values: new { templateId = TemplateId, reload = true, embed = Embed }) ?? string.Empty,
            StatusSavedText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.StatusSaved"),
            StatusUnsavedText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.StatusUnsaved"),
            SaveButtonText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveToTemplate"),
            ReloadButtonText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.ReloadFromDatabase"),
            NoFileText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.NoFile"),
            ReadOnlyText = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.ReadOnly"),
            SaveSuccessMessage = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveSuccess"),
            SaveFailedMessage = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveFailed"),
            ReloadConfirmMessage = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.ReloadConfirm"),
            HideToolbar = Embed,
        };

        return Page();
    }

    public IActionResult OnGetDxSpreadsheetRequest() =>
        SpreadsheetRequestProcessor.GetResponse(HttpContext);

    [IgnoreAntiforgeryToken]
    public IActionResult OnPostDxSpreadsheetRequest() =>
        SpreadsheetRequestProcessor.GetResponse(HttpContext);

    [ValidateAntiForgeryToken]
    public IActionResult OnPostSave([FromForm] Guid templateId)
    {
        if (templateId == Guid.Empty || !_fileService.CanEditTemplates())
            return Forbid();

        try
        {
            if (!Request.Form.TryGetValue("spreadsheetState", out var stateJson) || string.IsNullOrWhiteSpace(stateJson))
                return new JsonResult(new { success = false, message = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveFailed") });

            var spreadsheetState = JsonSerializer.Deserialize<SpreadsheetClientState>(
                stateJson.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (spreadsheetState == null)
                return new JsonResult(new { success = false, message = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveFailed") });

            var spreadsheet = SpreadsheetRequestProcessor.GetSpreadsheetFromState(spreadsheetState);
            byte[] content = spreadsheet.SaveCopy(DevExpress.Spreadsheet.DocumentFormat.Xlsx);
            var result = _fileService.TrySave(templateId, content);
            if (!result.Success)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = result.ErrorMessage ?? VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveFailed"),
                });
            }

            var userKey = _httpAccess.ResolveCurrentUserKey();
            _sessionService.BumpGeneration(templateId, userKey);

            return new JsonResult(new
            {
                success = true,
                message = VisaUiMessages.Get("UserReport.ExcelSpreadsheet.SaveSuccess"),
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new
            {
                success = false,
                message = VisaUiMessages.Format("UserReport.ExcelSpreadsheet.SaveError", ex.Message),
            });
        }
    }
}
