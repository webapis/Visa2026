#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Substitutes approved spans with placeholder tokens and changes nothing else.
/// Pair every write with <see cref="ITemplateConversionDiffGate"/> before persisting the result.
/// </summary>
public interface ITemplateTokenWriter
{
    TokenWriteResult Apply(TemplateTokenWriteRequest request);
}
