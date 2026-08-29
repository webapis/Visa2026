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

    public TemplateScanOrchestrator(
        IScanDocxLayoutService layout,
        IScanDraftDocxBuilder builder,
        IEphemeralTemplateValidationService validation,
        ITemplateDocumentOutlineReader outlines)
    {
        _layout = layout;
        _builder = builder;
        _validation = validation;
        _outlines = outlines;
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
            TemplateKind = ApplicationProfileTemplateKind.Word,
            DataScope = request.DataScope,
            CatalogScope = request.CatalogScope,
            Content = request.Content,
            FileName = request.FileName,
        });
    }
}
