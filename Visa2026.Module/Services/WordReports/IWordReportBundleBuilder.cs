using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

public sealed class WordReportBundleResult
{
    public int ReportCount { get; init; }
    public string ZipFileName { get; init; } = "Resminamalar.zip";
}

/// <summary>Builds the Resminamalar zip for one <see cref="Application"/>.</summary>
public interface IWordReportBundleBuilder
{
    Task<WordReportBundleResult> BuildZipAsync(
        Application application,
        IObjectSpace objectSpace,
        Stream zipStream,
        IReadOnlySet<string>? selectedEntryKeys = null,
        WordReportGenerationContext? context = null,
        CancellationToken cancellationToken = default);
}
