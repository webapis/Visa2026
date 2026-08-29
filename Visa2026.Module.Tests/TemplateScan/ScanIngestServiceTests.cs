using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanIngestServiceTests
{
    [Fact]
    public void Ingest_LowResolutionPng_FailsSuitability()
    {
        var (_, _, ingest, _) = ScanTestServiceFactory.Create();
        var png = ScanTestImageFactory.CreatePngWithDimensions(50, 50);

        var result = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = png,
            FileName = "small.png",
        });

        Assert.Equal(ScanSuitabilityVerdict.Fail, result.Suitability.Verdict);
        Assert.False(string.IsNullOrWhiteSpace(result.Playbook.Fingerprint));
    }
}
