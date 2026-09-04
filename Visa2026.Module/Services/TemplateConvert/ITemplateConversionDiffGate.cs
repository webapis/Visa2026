#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Fails a conversion when anything other than the approved placeholder substitutions changed.
/// Runs on every convert, including the deterministic path with no AI provider.
/// </summary>
public interface ITemplateConversionDiffGate
{
    DiffGateResult Verify(TemplateDiffGateRequest request);
}
