using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanAuthoringPlaybookServiceTests
{
    [Fact]
    public void GetPlaybook_ReturnsStableFingerprint()
    {
        var service = new ScanAuthoringPlaybookService();
        var first = service.GetPlaybook();
        var second = service.GetPlaybook();

        Assert.False(string.IsNullOrWhiteSpace(first.Markdown));
        Assert.Equal(64, first.Fingerprint.Length);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Contains("ds.", first.Markdown, StringComparison.Ordinal);
    }
}
