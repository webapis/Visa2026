using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanIngestServiceTests
{
    [Fact]
    public void Ingest_YellowWord_PassesSuitabilityWithPlaybook()
    {
        var (_, _, ingest, _) = ScanTestServiceFactory.Create();
        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434");

        var result = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "marked.docx",
        });

        Assert.True(result.Suitability.CanContinue);
        Assert.Equal(ScanSourceKind.Word, result.Input.SourceKind);
        Assert.False(string.IsNullOrWhiteSpace(result.Playbook.Fingerprint));
    }
}