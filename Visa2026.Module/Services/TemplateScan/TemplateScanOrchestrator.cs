#nullable enable

using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

public interface ITemplateScanOrchestrator
{
    Task<TemplateScanOutcome> GenerateAsync(
        TemplateScanAnalysis analysis,
        CancellationToken cancellationToken = default);

    ApplicationProfileTemplate Save(TemplateScanSaveRequest request);
}

public sealed class TemplateScanOrchestrator : ITemplateScanOrchestrator
{
    private readonly IScanDocxLayoutService _layout;
    private readonly IScanDraftDocxBuilder _builder;
    private readonly IEphemeralTemplateValidationService _validation;
    private readonly ITemplateDocumentOutlineReader _outlines;
    private readonly ITemplateTokenWriter _tokenWriter;
    private readonly ITemplateConversionDiffGate _diffGate;

    public TemplateScanOrchestrator(
        IScanDocxLayoutService layout,
        IScanDraftDocxBuilder builder,
        IEphemeralTemplateValidationService validation,
        ITemplateDocumentOutlineReader outlines,
        ITemplateTokenWriter tokenWriter,
        ITemplateConversionDiffGate diffGate)
    {
        _layout = layout;
        _builder = builder;
        _validation = validation;
        _outlines = outlines;
        _tokenWriter = tokenWriter;
        _diffGate = diffGate;
    }

    public async Task<TemplateScanOutcome> GenerateAsync(
        TemplateScanAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(analysis.FieldPlan);
        ArgumentNullException.ThrowIfNull(analysis.PlaceholderSet);

        if (!analysis.CanGenerate)
            throw new InvalidOperationException("This scan cannot generate a draft template yet.");

        if (!analysis.NormalizedInput.IsOfficeSource)
        {
            throw new InvalidOperationException(
                "Create from yellow marks accepts only Word (.docx) or Excel (.xlsx).");
        }

        return await GenerateFromOfficeAsync(analysis, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TemplateScanOutcome> GenerateFromOfficeAsync(
        TemplateScanAnalysis analysis,
        CancellationToken cancellationToken)
    {
        var package = analysis.NormalizedInput.OfficePackageBytes
            ?? throw new InvalidOperationException("Office package bytes are missing.");

        var format = analysis.NormalizedInput.SourceKind == ScanSourceKind.Excel
            ? TemplateSourceFormat.Xlsx
            : TemplateSourceFormat.Docx;

        var substitutions = new List<TokenSubstitution>();
        foreach (var field in analysis.FieldPlan.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.ProposedToken) || field.SourceRegion is null)
                continue;
            if (format == TemplateSourceFormat.Xlsx
                && field.SourceRegion is DocumentRegion.ExcelCell excelCell
                && !ScanExcelWorkbookPolicy.IsOnFirstWorksheet(package, excelCell.SheetName))
                continue;
            var trimmed = field.ProposedToken.Trim();
            if (!trimmed.Contains("{{", StringComparison.Ordinal)
                && !TemplateTokenSyntax.TryGetShortCode(trimmed, out _))
                continue;
            substitutions.Add(new TokenSubstitution(field.SourceRegion, trimmed));
        }

        if (substitutions.Count == 0)
        {
            return new TemplateScanOutcome
            {
                Content = package,
                Outline = ScanExcelWorkbookPolicy.LimitOutlineToFirstSheet(_outlines.Read(package, format)),
                Validation = await _validation
                    .ExtractAndValidateAsync(package, format, analysis.PlaceholderSet, cancellationToken)
                    .ConfigureAwait(false),
                Errors = ["No yellow-marked spans could be written as placeholders. Re-check yellow highlights in Word/Excel."],
                Warnings = Array.Empty<string>(),
                Gaps = analysis.FieldPlan.Gaps,
                EmittedTokens = Array.Empty<string>(),
                TemplateKind = format == TemplateSourceFormat.Xlsx
                    ? ApplicationProfileTemplateKind.Excel
                    : ApplicationProfileTemplateKind.Word,
                SourceFormat = format,
            };
        }

        // Word writer expects bare short token path for Wrap — Convert uses Token that may already be wrapped.
        // Scan fields store BuildWordToken which is already {{ds.X}}. WordTemplateTokenWriter wraps again!
        // Check WordTemplateTokenWriter: TemplateTokenSyntax.Wrap(substitution.Token)
        // And Convert stores what?
        var bareSubs = substitutions.Select(s =>
        {
            var token = s.Token;
            if (TemplateTokenSyntax.TryGetShortCode(token, out var code))
            {
                // Writer wraps — pass bare catalog token form used by Convert.
                // Convert Highlight.Token is typically without braces? Check TemplateTokenSyntax.Wrap
                return new TokenSubstitution(s.Region, UnwrapOrBare(token));
            }
            return s;
        }).ToList();

        var loops = format == TemplateSourceFormat.Xlsx
            ? TemplateRosterLoopPlanner.PlanExcelLoopsFromSubstitutions(bareSubs, package)
            : Array.Empty<LoopMarker>();

        var write = _tokenWriter.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = package,
            Format = format,
            Substitutions = bareSubs,
            Loops = loops,
        });

        // Yellow is scan markup only — strip every remaining mark so unmapped leftovers
        // (e.g. "6 (alty)" when only VCAT was written) do not survive into catalog Preview.
        var cleanedContent = format == TemplateSourceFormat.Xlsx
            ? ExcelTemplateTokenWriter.StripAllYellowFills(write.Content)
            : WordTemplateTokenWriter.StripAllYellowMarkup(write.Content);

        var diff = _diffGate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = package,
            ConvertedContent = cleanedContent,
            Format = format,
            Substitutions = write.AppliedSubstitutions,
            Loops = write.AppliedLoops,
        });

        var validation = await _validation
            .ExtractAndValidateAsync(cleanedContent, format, analysis.PlaceholderSet, cancellationToken)
            .ConfigureAwait(false);

        var errors = new List<string>();
        var warnings = new List<string>();

        if (!diff.Passed)
            errors.AddRange(diff.Violations.Select(static v => "Diff gate: " + v));

        foreach (var skip in write.Skipped)
            warnings.Add($"Skipped {skip.Token}: {skip.Reason}");

        foreach (var issue in validation.Issues)
        {
            if (issue.Severity == TemplateValidationSeverity.Error)
                errors.Add(issue.Message);
            else
                warnings.Add(issue.Message);
        }

        foreach (var gap in analysis.FieldPlan.Gaps)
            warnings.Add($"Unmapped yellow: {gap.LabelText}");

        var emitted = write.AppliedSubstitutions
            .Select(static s => TemplateTokenSyntax.Wrap(s.Token))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (emitted.Count == 0 && !validation.HasHardFailure)
            errors.Add("The draft contains no merge placeholders.");

        return new TemplateScanOutcome
        {
            Content = cleanedContent,
            Outline = ScanExcelWorkbookPolicy.LimitOutlineToFirstSheet(_outlines.Read(cleanedContent, format)),
            Validation = validation,
            Errors = errors,
            Warnings = warnings,
            Gaps = analysis.FieldPlan.Gaps,
            EmittedTokens = emitted,
            TemplateKind = format == TemplateSourceFormat.Xlsx
                ? ApplicationProfileTemplateKind.Excel
                : ApplicationProfileTemplateKind.Word,
            SourceFormat = format,
        };
    }

    private static string UnwrapOrBare(string token)
    {
        if (token.Contains("{{", StringComparison.Ordinal))
            return token.Trim();

        if (TemplateTokenSyntax.TryGetShortCode(token, out _))
            return token.Trim().Trim('{', '}').Trim();

        return token;
    }

    private async Task<TemplateScanOutcome> GenerateFromVisionLayoutAsync(
        TemplateScanAnalysis analysis,
        CancellationToken cancellationToken)
    {
        var layout = await _layout.ProposeLayoutAsync(
            new ScanDocxLayoutRequest
            {
                FieldPlan = analysis.FieldPlan,
                Playbook = analysis.Playbook,
                Pages = analysis.NormalizedInput.Pages,
                OcrLines = analysis.OcrLines,
                ValueHints = analysis.ValueHints,
            },
            cancellationToken).ConfigureAwait(false);

        var draft = _builder.Build(new ScanDraftDocxRequest
        {
            Layout = layout,
            FieldPlan = analysis.FieldPlan,
        });

        var validation = await _validation
            .ExtractAndValidateAsync(
                draft.Content,
                TemplateSourceFormat.Docx,
                analysis.PlaceholderSet,
                cancellationToken)
            .ConfigureAwait(false);

        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (var issue in validation.Issues)
        {
            if (issue.Severity == TemplateValidationSeverity.Error)
                errors.Add(issue.Message);
            else
                warnings.Add(issue.Message);
        }

        foreach (var gap in analysis.FieldPlan.Gaps)
            warnings.Add($"Unmapped on scan: {gap.LabelText}");

        var emitted = new HashSet<string>(draft.EmittedTokens, StringComparer.Ordinal);
        foreach (var field in analysis.FieldPlan.Fields)
        {
            var token = field.ProposedToken?.Trim();
            if (string.IsNullOrWhiteSpace(token) || emitted.Contains(token))
                continue;

            warnings.Add(
                $"Placeholder {token} was mapped on Review but not placed in the letter layout — refine in Word or Regenerate.");
        }

        if (draft.EmittedTokens.Count == 0 && !validation.HasHardFailure)
            errors.Add("The draft contains no merge placeholders.");

        return new TemplateScanOutcome
        {
            Content = draft.Content,
            Outline = _outlines.Read(draft.Content, TemplateSourceFormat.Docx),
            Validation = validation,
            Errors = errors,
            Warnings = warnings,
            Gaps = analysis.FieldPlan.Gaps,
            EmittedTokens = draft.EmittedTokens,
            TemplateKind = ApplicationProfileTemplateKind.Word,
            SourceFormat = TemplateSourceFormat.Docx,
        };
    }

    public ApplicationProfileTemplate Save(TemplateScanSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ApplicationProfileTemplateSaveHelper.Save(new ApplicationProfileTemplateSaveRequest
        {
            ObjectSpace = request.ObjectSpace,
            Profile = request.Profile,
            TemplateName = request.TemplateName,
            TemplateKind = request.TemplateKind,
            DataScope = request.DataScope,
            CatalogScope = request.CatalogScope,
            Content = request.Content,
            FileName = request.FileName,
        });
    }
}