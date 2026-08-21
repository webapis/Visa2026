#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Never trust provider output. Runs before the writer on mapping and chat paths (Q11–Q13).
/// </summary>
public interface ITemplateMappingPlanSanitizer
{
    TemplateMappingPlan Sanitize(
        TemplateMappingPlan proposed,
        ApplicationProfilePlaceholderSet allowedSet,
        IReadOnlyList<DocumentExtractRegion> knownRegions,
        out IReadOnlyList<string> dropped);
}