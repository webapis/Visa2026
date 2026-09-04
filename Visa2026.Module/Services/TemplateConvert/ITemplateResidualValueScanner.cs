#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Asserts that filled-sample values from the uploaded document did not survive into the saved
/// template. A leftover passport number in a committed master is a data-protection defect.
/// </summary>
public interface ITemplateResidualValueScanner
{
    ResidualValueScanResult Scan(byte[] content, TemplateSourceFormat format, IReadOnlyList<ResidualValueProbe> probes);
}
