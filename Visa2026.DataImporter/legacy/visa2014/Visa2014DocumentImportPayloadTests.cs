using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014DocumentImportPayloadTests
{
    [Fact]
    public void WithNestedFile_BuildsParentAndFileDictionary()
    {
        var parentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bytes = new byte[] { 1, 2, 3 };

        var payload = Visa2014DocumentImportPayload.WithNestedFile("Passport", parentId, "scan.pdf", bytes);

        Assert.True(payload.ContainsKey("Passport"));
        Assert.True(payload.ContainsKey("File"));

        var parent = Assert.IsType<Dictionary<string, object?>>(payload["Passport"]);
        Assert.Equal(parentId, parent["ID"]);

        var file = Assert.IsType<Dictionary<string, object?>>(payload["File"]);
        Assert.Equal("scan.pdf", file["FileName"]);
        Assert.Same(bytes, file["Content"]);
    }
}
