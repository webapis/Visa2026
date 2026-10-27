using Visa2026.Module.Services.EmailDocumentCopies;
using Xunit;

namespace Visa2026.Module.Tests.Services.EmailDocumentCopies;

public class EmailDocumentCopiesAttachmentFileNameTests
{
    [Theory]
    [InlineData("Ada Lovelace", 1, 1, "Ada_Lovelace_documents.pdf")]
    [InlineData("Ada/Lovelace", 1, 1, "Ada_Lovelace_documents.pdf")]
    [InlineData("  ", 1, 1, "person_documents.pdf")]
    [InlineData("Ada", 2, 3, "Ada_documents_part2.pdf")]
    public void BuildFileName_SanitizesAndAddsPartSuffix(string name, int part, int count, string expected)
    {
        Assert.Equal(expected, EmailDocumentCopiesAttachmentBuilder.BuildFileName(name, part, count));
    }
}
