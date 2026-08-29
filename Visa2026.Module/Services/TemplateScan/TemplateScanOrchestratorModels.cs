#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

public sealed class TemplateScanAnalysis
{
    public required ScanNormalizedInput NormalizedInput { get; init; }

    public required ScanSuitabilityReport Suitability { get; init; }

    public required ScanFieldPlan FieldPlan { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public required ScanAuthoringPlaybook Playbook { get; init; }

    public required string TemplateName { get; init; }

    public ApplicationProfileTemplateDataScope DataScope { get; init; } = ApplicationProfileTemplateDataScope.ApplicationHeader;

    public ScanKind ScanKind { get; init; } = ScanKind.BlankForm;

    public IReadOnlyList<ScanOcrLine> OcrLines { get; init; } = Array.Empty<ScanOcrLine>();

    public IReadOnlyList<ScanValueHint> ValueHints { get; init; } = Array.Empty<ScanValueHint>();

    public bool CanGenerate =>
        Suitability.CanContinue && FieldPlan.HasMappedFields;
}

public sealed class ScanDraftDocxRequest
{
    public required ScanDocxLayoutProposal Layout { get; init; }

    public required ScanFieldPlan FieldPlan { get; init; }
}

public sealed class ScanDraftDocxResult
{
    public required byte[] Content { get; init; }

    public required IReadOnlyList<string> EmittedTokens { get; init; }
}

public sealed class TemplateScanOutcome
{
    public required byte[] Content { get; init; }

    public required TemplateDocumentOutline Outline { get; init; }

    public required TemplateValidationReport Validation { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public required IReadOnlyList<ScanGap> Gaps { get; init; }

    public required IReadOnlyList<string> EmittedTokens { get; init; }

    public bool HasErrors => Errors.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;

    public bool CanApprove => Errors.Count == 0;
}

public sealed class TemplateScanSaveRequest
{
    public required DevExpress.ExpressApp.IObjectSpace ObjectSpace { get; init; }

    public required ApplicationProfile Profile { get; init; }

    public required string TemplateName { get; init; }

    public required ApplicationProfileTemplateDataScope DataScope { get; init; }

    public required ApplicationProfileTemplateCatalogScope CatalogScope { get; init; }

    public required byte[] Content { get; init; }

    public required string FileName { get; init; }
}
