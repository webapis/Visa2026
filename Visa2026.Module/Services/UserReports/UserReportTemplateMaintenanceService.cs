using System.IO;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;

namespace Visa2026.Module.Services.UserReports;

/// <inheritdoc cref="IUserReportTemplateMaintenanceService"/>
public sealed class UserReportTemplateMaintenanceService : IUserReportTemplateMaintenanceService
{
    private readonly INonSecuredObjectSpaceFactory _objectSpaceFactory;
    private readonly IUserReportPlaceholderExtractor _wordPlaceholderExtractor;
    private readonly IExcelTemplatePlaceholderExtractor _excelPlaceholderExtractor;
    private readonly IUserReportValidationService _wordValidationService;
    private readonly IExcelReportValidationService _excelValidationService;
    private readonly ILogger<UserReportTemplateMaintenanceService> _logger;

    public UserReportTemplateMaintenanceService(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IUserReportPlaceholderExtractor wordPlaceholderExtractor,
        IExcelTemplatePlaceholderExtractor excelPlaceholderExtractor,
        IUserReportValidationService wordValidationService,
        IExcelReportValidationService excelValidationService,
        ILogger<UserReportTemplateMaintenanceService> logger)
    {
        _objectSpaceFactory = objectSpaceFactory;
        _wordPlaceholderExtractor = wordPlaceholderExtractor;
        _excelPlaceholderExtractor = excelPlaceholderExtractor;
        _wordValidationService = wordValidationService;
        _excelValidationService = excelValidationService;
        _logger = logger;
    }

    public async Task<UserReportTemplateExtractResult> ExtractPlaceholdersAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace<UserReportTemplate>();
            var template = LoadTemplate(objectSpace, templateId);
            if (template == null)
                return FailedExtract("Template not found.");

            var content = ReadFileContent(objectSpace, template.TemplateFile);
            if (content == null || content.Length == 0)
                return FailedExtract("Template file has no content.");

            using var fileStream = new MemoryStream(content, writable: false);
            IList<string> placeholders;
            if (template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
                placeholders = await _excelPlaceholderExtractor.ExtractPlaceholdersAsync(fileStream).ConfigureAwait(false);
            else
                placeholders = await _wordPlaceholderExtractor.ExtractPlaceholdersAsync(fileStream).ConfigureAwait(false);

            foreach (var existing in template.Placeholders.ToList())
                objectSpace.Delete(existing);

            foreach (var placeholder in placeholders)
            {
                var placeholderObj = objectSpace.CreateObject<UserReportPlaceholder>();
                placeholderObj.Template = template;
                placeholderObj.PlaceholderKey = placeholder;
                placeholderObj.IsValid = false;
                template.Placeholders.Add(placeholderObj);
            }

            objectSpace.CommitChanges();

            return new UserReportTemplateExtractResult
            {
                Success = true,
                PlaceholderCount = placeholders.Count,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extract placeholders failed for template {TemplateId}", templateId);
            return FailedExtract(ex.Message);
        }
    }

    public async Task<UserReportTemplateValidateResult> ValidatePlaceholdersAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace<UserReportTemplate>();
            var template = LoadTemplate(objectSpace, templateId);
            if (template == null)
                return FailedValidate("Template not found.");

            if (template.Placeholders.Count == 0)
                return FailedValidate("No placeholders to validate.");

            var placeholderKeys = template.Placeholders.Select(p => p.PlaceholderKey).ToList();
            IList<PlaceholderValidationResult> validationResults;
            if (template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
            {
                validationResults = await _excelValidationService.ValidatePlaceholdersAsync(
                    placeholderKeys,
                    template.RootBoType,
                    template.ExcelMergeMode).ConfigureAwait(false);
            }
            else
            {
                validationResults = await _wordValidationService.ValidatePlaceholdersAsync(
                    placeholderKeys,
                    template.RootBoType).ConfigureAwait(false);
            }

            foreach (var placeholder in template.Placeholders)
            {
                var result = validationResults.FirstOrDefault(r => r.PlaceholderKey == placeholder.PlaceholderKey);
                if (result == null)
                    continue;

                placeholder.IsValid = result.IsValid;
                placeholder.ResolvedPropertyPath = result.ResolvedPath;
                placeholder.ExampleValue = result.ExampleValue;
                placeholder.ValidationError = result.ErrorMessage;
            }

            objectSpace.CommitChanges();

            var validCount = validationResults.Count(r => r.IsValid);
            var invalidCount = validationResults.Count(r => !r.IsValid);
            return new UserReportTemplateValidateResult
            {
                Success = true,
                ValidCount = validCount,
                InvalidCount = invalidCount,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validate placeholders failed for template {TemplateId}", templateId);
            return FailedValidate(ex.Message);
        }
    }

    public async Task<UserReportTemplateExtractValidateResult> ExtractAndValidatePlaceholdersAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var extract = await ExtractPlaceholdersAsync(templateId, cancellationToken).ConfigureAwait(false);
        if (!extract.Success || extract.PlaceholderCount == 0)
        {
            return new UserReportTemplateExtractValidateResult
            {
                Extract = extract,
                Validate = extract.PlaceholderCount == 0 && extract.Success
                    ? null
                    : FailedValidate(extract.ErrorMessage ?? "Extract failed."),
            };
        }

        var validate = await ValidatePlaceholdersAsync(templateId, cancellationToken).ConfigureAwait(false);
        return new UserReportTemplateExtractValidateResult
        {
            Extract = extract,
            Validate = validate,
        };
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

    private static UserReportTemplateExtractResult FailedExtract(string message) =>
        new() { Success = false, ErrorMessage = message };

    private static UserReportTemplateValidateResult FailedValidate(string message) =>
        new() { Success = false, ErrorMessage = message };
}
