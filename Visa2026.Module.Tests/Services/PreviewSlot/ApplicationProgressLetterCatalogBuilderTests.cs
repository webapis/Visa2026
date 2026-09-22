using System;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services.PreviewSlot;

public class ApplicationProgressLetterCatalogBuilderTests
{
    [Fact]
    public void Build_NullObjectSpace_ReturnsEmpty()
    {
        var entries = ApplicationProgressLetterCatalogBuilder.Build(
            objectSpace: null!,
            applicationId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        Assert.Empty(entries);
    }

    [Fact]
    public void Build_EmptyApplicationId_ReturnsEmpty()
    {
        // Never-called proxy would still be unused because empty Guid short-circuits first.
        var entries = ApplicationProgressLetterCatalogBuilder.Build(
            objectSpace: null!,
            applicationId: Guid.Empty);

        Assert.Empty(entries);
    }
}
