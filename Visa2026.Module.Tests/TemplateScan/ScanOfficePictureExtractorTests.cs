#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Tests.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanOfficePictureExtractorTests
{
    private static readonly long PortraitWidthEmu = 35L * WordInlinePictureLocator.EmuPerMillimetre;
    private static readonly long PortraitHeightEmu = 45L * WordInlinePictureLocator.EmuPerMillimetre;
    private static readonly long IconEmu = 4L * WordInlinePictureLocator.EmuPerMillimetre;

    [Fact]
    public void Extract_Word_FindsPortraitSizedBodyPicture()
    {
        var bytes = TemplateConvertFixtures.CreateWordWithInlinePicture("Suraty: ", PortraitWidthEmu, PortraitHeightEmu);
        var slots = ScanOfficePictureExtractor.Extract(bytes);

        var slot = Assert.Single(slots);
        Assert.Equal("body/0", slot.ParagraphAddress);
        Assert.Equal(0, slot.DrawingIndex);
        Assert.Equal("Suraty: ".Length, slot.TextInsertOffset);
    }

    [Fact]
    public void Extract_Word_SkipsTinyIcons()
    {
        var bytes = TemplateConvertFixtures.CreateWordWithInlinePicture(null, IconEmu, IconEmu);
        Assert.Empty(ScanOfficePictureExtractor.Extract(bytes));
    }

    [Fact]
    public void Build_YellowWordWithPortrait_AddsPersonPhotoToken()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var bytes = TemplateConvertFixtures.AppendInlinePicture(
            ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434"),
            PortraitWidthEmu,
            PortraitHeightEmu);

        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);

        Assert.Contains(proposal.Fields, f =>
            f.SourceRegion is DocumentRegion.WordDrawing
            && f.ProposedToken == "{{IMAGE:PPH}}");
        Assert.Contains(proposal.Fields, f => f.ProposedToken != null
            && TemplateTokenSyntax.TryGetShortCode(f.ProposedToken, out var code)
            && code.Equals("AFNUM", StringComparison.OrdinalIgnoreCase));
        Assert.True(set.Contains("{{IMAGE:Person_Photo}}"));
        Assert.True(set.Contains("{{IMAGE:PPH}}"));
    }
}
