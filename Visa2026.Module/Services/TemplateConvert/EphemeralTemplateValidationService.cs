using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Extract + validate for bytes that are not (yet) a <see cref="UserReportTemplate"/> row.
/// <see cref="IUserReportTemplateMaintenanceService"/> needs a persisted template id, but conversion
/// must gate Approve on a draft that only exists in memory (Q3).
/// </summary>
public interface IEphemeralTemplateValidationService
{
    Task<TemplateValidationReport> ExtractAndValidateAsync(
        byte[] content,
        TemplateSourceFormat format,
        ApplicationProfilePlaceholderSet allowedSet,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IEphemeralTemplateValidationService"/>
public sealed class EphemeralTemplateValidationService : IEphemeralTemplateValidationService
{
    private readonly IUserReportPlaceholderExtractor _wordExtractor;
    private readonly IExcelTemplatePlaceholderExtractor _excelExtractor;
    private readonly IUserReportValidationService _wordValidator;
    private readonly IExcelReportValidationService _excelValidator;

    public EphemeralTemplateValidationService(
        IUserReportPlaceholderExtractor wordExtractor,
        IExcelTemplatePlaceholderExtractor excelExtractor,
        IUserReportValidationService wordValidator,
        IExcelReportValidationService excelValidator)
    {
        _wordExtractor = wordExtractor ?? throw new ArgumentNullException(nameof(wordExtractor));
        _excelExtractor = excelExtractor ?? throw new ArgumentNullException(nameof(excelExtractor));
        _wordValidator = wordValidator ?? throw new ArgumentNullException(nameof(wordValidator));
        _excelValidator = excelValidator ?? throw new ArgumentNullException(nameof(excelValidator));
    }

    public async Task<TemplateValidationReport> ExtractAndValidateAsync(
        byte[] content,
        TemplateSourceFormat format,
        ApplicationProfilePlaceholderSet allowedSet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(allowedSet);
        cancellationToken.ThrowIfCancellationRequested();

        if (content == null || content.Length == 0)
            return Unreadable("The uploaded file is empty.");

        IList<string> extracted;
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            extracted = format == TemplateSourceFormat.Xlsx
                ? await _excelExtractor.ExtractPlaceholdersAsync(stream).ConfigureAwait(false)
                : await _wordExtractor.ExtractPlaceholdersAsync(stream).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Unreadable($"The file could not be read as {(format == TemplateSourceFormat.Xlsx ? "Excel" : "Word")}: {ex.Message}");
        }

        var tokens = extracted
            .Where(static t => !string.IsNullOrWhiteSpace(t))
            .Select(static t => t.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static t => t, StringComparer.Ordinal)
            .ToList();

        if (tokens.Count == 0)
        {
            return new TemplateValidationReport(
                tokens,
                [],
                [new TemplateValidationIssue(
                    "No placeholders were found in the document.",
                    TemplateValidationSeverity.Error,
                    TemplateValidationIssueCode.NoTokensFound,
                    null)],
                HasHardFailure: true);
        }

        var issues = new List<TemplateValidationIssue>();
        InspectLoops(tokens, issues);

        var bindable = new List<string>();
        foreach (var token in tokens)
        {
            if (IsLoopMarker(token))
                continue;

            if (InspectToken(token, format, allowedSet, issues))
                bindable.Add(token);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var results = bindable.Count == 0
            ? new List<PlaceholderValidationResult>()
            : (await ValidateAsync(bindable, allowedSet).ConfigureAwait(false)).ToList();

        foreach (var result in results.Where(static r => !r.IsValid))
        {
            issues.Add(new TemplateValidationIssue(
                string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? $"{{{{{result.PlaceholderKey}}}}} does not resolve on the merge root."
                    : $"{{{{{result.PlaceholderKey}}}}}: {result.ErrorMessage}",
                TemplateValidationSeverity.Error,
                TemplateValidationIssueCode.UnresolvedOnBoType,
                result.PlaceholderKey));
        }

        return new TemplateValidationReport(
            tokens,
            results,
            issues,
            issues.Any(static i => i.Severity == TemplateValidationSeverity.Error));
    }

    private Task<IList<PlaceholderValidationResult>> ValidateAsync(
        IList<string> tokens,
        ApplicationProfilePlaceholderSet allowedSet)
    {
        var boType = ResolveBoType(allowedSet.DataScope);

        return allowedSet.TemplateKind == ApplicationProfileTemplateKind.Excel
            ? _excelValidator.ValidatePlaceholdersAsync(tokens, boType, ExcelMergeMode.ItemList)
            : _wordValidator.ValidatePlaceholdersAsync(tokens, boType);
    }

    /// <summary>
    /// <see cref="ExcelMergeMode.SingleItem"/> (one workbook per person) is a seed-time authoring
    /// choice with no equivalent in the convert flow, so conversion always validates as a header +
    /// rows workbook.
    /// </summary>
    private static UserReportBoType ResolveBoType(ApplicationProfileTemplateDataScope dataScope) =>
        dataScope == ApplicationProfileTemplateDataScope.PeopleM2M
            ? UserReportBoType.ApplicationItem
            : UserReportBoType.ApplicationProfileInstance;

    /// <summary>
    /// Both extractors de-duplicate, so document order is unavailable and nesting cannot be checked.
    /// What remains checkable — and what actually breaks the generators — is an open with no close.
    /// </summary>
    private static void InspectLoops(IEnumerable<string> tokens, List<TemplateValidationIssue> issues)
    {
        var opens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var closes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            if (token.StartsWith('#'))
                opens.Add(token[1..].Trim());
            else if (token.StartsWith('/'))
                closes.Add(token[1..].Trim());
        }

        foreach (var name in opens.Except(closes, StringComparer.OrdinalIgnoreCase).OrderBy(static n => n, StringComparer.Ordinal))
        {
            issues.Add(new TemplateValidationIssue(
                $"{{{{#{name}}}}} is opened but never closed.",
                TemplateValidationSeverity.Error,
                TemplateValidationIssueCode.BrokenLoop,
                $"#{name}"));
        }

        foreach (var name in closes.Except(opens, StringComparer.OrdinalIgnoreCase).OrderBy(static n => n, StringComparer.Ordinal))
        {
            issues.Add(new TemplateValidationIssue(
                $"{{{{/{name}}}}} is closed but never opened.",
                TemplateValidationSeverity.Error,
                TemplateValidationIssueCode.BrokenLoop,
                $"/{name}"));
        }
    }

    /// <summary>Returns whether the token should also go to the property-resolution validator.</summary>
    private static bool InspectToken(
        string token,
        TemplateSourceFormat format,
        ApplicationProfilePlaceholderSet allowedSet,
        List<TemplateValidationIssue> issues)
    {
        if (allowedSet.Contains(token))
            return true;

        if (!TemplateTokenSyntax.TryGetShortCode(token, out var shortCode))
        {
            issues.Add(Unknown(token));
            return false;
        }

        var exclusion = allowedSet.Excluded
            .FirstOrDefault(e => string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        if (exclusion == null)
        {
            issues.Add(Unknown(token));
            return false;
        }

        switch (exclusion.Reason)
        {
            // Resolves on the merge root, but the profile does not collect the record behind it, so
            // it merges blank. Product spec §6.1 lets the officer acknowledge that and continue.
            case PlaceholderExclusionReason.PersonPackDisabled:
                issues.Add(new TemplateValidationIssue(
                    $"{{{{{token}}}}} needs a person record this profile does not collect. It will merge as empty text.",
                    TemplateValidationSeverity.Warning,
                    TemplateValidationIssueCode.PackDisabledToken,
                    token));
                return true;

            case PlaceholderExclusionReason.StructuralUnsupportedForKind:
                issues.Add(new TemplateValidationIssue(
                    format == TemplateSourceFormat.Xlsx
                        ? $"{{{{{token}}}}} is an image placeholder, which Excel templates cannot fill."
                        : $"{{{{{token}}}}} is not supported for this template kind.",
                    TemplateValidationSeverity.Error,
                    TemplateValidationIssueCode.UnsupportedImageToken,
                    token));
                return false;

            case PlaceholderExclusionReason.OutOfDataScope:
                issues.Add(new TemplateValidationIssue(
                    $"{{{{{token}}}}} belongs to a different data scope than this template.",
                    TemplateValidationSeverity.Error,
                    TemplateValidationIssueCode.OutOfDataScopeToken,
                    token));
                return false;

            default:
                issues.Add(Unknown(token));
                return false;
        }
    }

    private static TemplateValidationIssue Unknown(string token) =>
        new($"{{{{{token}}}}} is not a placeholder this profile can fill.",
            TemplateValidationSeverity.Error,
            TemplateValidationIssueCode.UnknownToken,
            token);

    private static TemplateValidationReport Unreadable(string message) =>
        new(
            [],
            [],
            [new TemplateValidationIssue(message, TemplateValidationSeverity.Error, TemplateValidationIssueCode.UnreadableDocument, null)],
            HasHardFailure: true);

    private static bool IsLoopMarker(string token) => token.StartsWith('#') || token.StartsWith('/');
}
