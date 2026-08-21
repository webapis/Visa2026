#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <inheritdoc cref="ITemplateConversionDiffGate"/>
public sealed class TemplateConversionDiffGate : ITemplateConversionDiffGate
{
    public DiffGateResult Verify(TemplateDiffGateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.OriginalContent);
        ArgumentNullException.ThrowIfNull(request.ConvertedContent);

        var violations = new List<string>();

        try
        {
            switch (request.Format)
            {
                case TemplateSourceFormat.Docx:
                    WordConversionDiffInspector.Inspect(request, violations);
                    break;
                case TemplateSourceFormat.Xlsx:
                    ExcelConversionDiffInspector.Inspect(request, violations);
                    break;
                default:
                    violations.Add($"Unsupported template format '{request.Format}'.");
                    break;
            }
        }
        catch (Exception exception)
        {
            violations.Add($"Could not compare documents: {exception.Message}");
        }

        return violations.Count == 0 ? DiffGateResult.Pass() : DiffGateResult.Fail(violations);
    }
}
