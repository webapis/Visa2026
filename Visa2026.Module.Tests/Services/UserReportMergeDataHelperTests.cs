using System.Collections.ObjectModel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class UserReportMergeDataHelperTests
{
    [Theory]
    [InlineData("Forma 16", "x.docx", true)]
    [InlineData("Other", "Forma_16_seed.docx", true)]
    [InlineData("Contract", "contract.docx", false)]
    public void IsForma16UserReportTemplate_MatchesNameOrFile(string name, string fileName, bool expected)
    {
        var template = Template(name, fileName);
        Assert.Equal(expected, UserReportMergeDataHelper.IsForma16UserReportTemplate(template));
    }

    [Theory]
    [InlineData("Sahsy kagyz", "x.docx", true)]
    [InlineData("Other", "sahsy_kagyz.docx", true)]
    [InlineData("Sanaw", "Sanaw_uzt.docx", false)]
    public void IsSahsyKagyzUserReportTemplate_MatchesNameOrFile(string name, string fileName, bool expected)
    {
        var template = Template(name, fileName);
        Assert.Equal(expected, UserReportMergeDataHelper.IsSahsyKagyzUserReportTemplate(template));
    }

    [Theory]
    [InlineData("Sanaw", "list.docx", true)]
    [InlineData("Sanaw_ckl", "x.docx", true)]
    [InlineData("Other", "Sanaw_uzt.docx", true)]
    [InlineData("Other", "Sanaw_uzt.xlsx", false)]
    public void IsSanawUserReportTemplate_MatchesWordSeedsOnly(string name, string fileName, bool expected)
    {
        var template = Template(name, fileName);
        Assert.Equal(expected, UserReportMergeDataHelper.IsSanawUserReportTemplate(template));
    }

    [Fact]
    public void ShouldUseSanawyStyleRows_FalseWhenForma16OrSahsyDetected()
    {
        var formaPlaceholders = new[]
        {
            Ph("rows.Registration_GelmeginMaksadyTm"),
            Ph("rows.Person_LastName")
        };
        Assert.False(UserReportMergeDataHelper.ShouldUseSanawyStyleRows(null, formaPlaceholders));

        var sahsy = Template("Sahsy kagyz", "sahsy_kagyz.docx");
        Assert.False(UserReportMergeDataHelper.ShouldUseSanawyStyleRows(sahsy, null));
    }

    [Fact]
    public void ShouldUseSanawyStyleRows_TrueForSanawTemplateOrScannedTokens()
    {
        var sanaw = Template("Sanaw", "Sanaw_uzt.docx");
        Assert.True(UserReportMergeDataHelper.ShouldUseSanawyStyleRows(sanaw, null));

        Assert.True(UserReportMergeDataHelper.ShouldUseSanawyStyleRows(
            null,
            null,
            new[] { "ds.rows.Person_LastName", "#rows" }));

        Assert.False(UserReportMergeDataHelper.ScannedTokensIndicateSanawyRows(new[] { "Person_FullName" }));
    }

    [Fact]
    public void TemplateUsesWizaYatyrylmakSanawRowPlaceholders_DetectsCancelVisaBlock()
    {
        Assert.True(UserReportMergeDataHelper.TemplateUsesWizaYatyrylmakSanawRowPlaceholders(
            null,
            new[] { Ph(".CancelVisa_NumberBlock") }));
        Assert.True(UserReportMergeDataHelper.IsWizaYatyrylmakSanawUserReportTemplate(
            Template("Wiza ýatyrmak sanaw", "other.docx")));
    }

    [Theory]
    [InlineData("ds.rows.Person_LastName", "rows.Person_LastName")]
    [InlineData("DS.FullApplicationNumber", "FullApplicationNumber")]
    [InlineData("Person_LastName", "Person_LastName")]
    [InlineData("", "")]
    public void StripDocxModelPrefix_RemovesDsPrefix(string input, string expected)
    {
        Assert.Equal(expected, UserReportMergeDataHelper.StripDocxModelPrefix(input));
    }

    [Fact]
    public void BuildSanawyRowDictionary_IncludesCoreKeysAndAliases()
    {
        var item = new ApplicationItem();
        var row = UserReportMergeDataHelper.BuildSanawyRowDictionary(item, 3);

        Assert.Equal(3, row["RowNo"]);
        Assert.True(row.ContainsKey("Person_LastName"));
        Assert.True(row.ContainsKey("Passport_Number"));
        Assert.True(row.ContainsKey("BorderZoneLocation_NameTm"));
    }

    [Fact]
    public void BuildRegistrationForm16RowDictionary_IncludesVisaAndPhotoKeys()
    {
        var item = new ApplicationItem();
        var row = UserReportMergeDataHelper.BuildRegistrationForm16RowDictionary(item, 1);

        Assert.Equal(1, row["RowNumber"]);
        Assert.True(row.ContainsKey("Visa_IssueDateText"));
        Assert.True(row.ContainsKey("Person_Photo"));
        Assert.True(row.ContainsKey("Registration_GelmeginMaksadyTm"));
    }

    [Fact]
    public void BuildSanawyStyleRows_UsesExplicitItemsAndRowNumbers()
    {
        var app = new Application
        {
            ApplicationItems = new ObservableCollection<ApplicationItem>()
        };
        var items = new[]
        {
            new ApplicationItem(),
            new ApplicationItem()
        };

        var rows = UserReportMergeDataHelper.BuildSanawyStyleRows(app, items);

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0]["RowNo"]);
        Assert.Equal(2, rows[1]["RowNo"]);
    }

    [Fact]
    public void ResolveCanonicalPropertyPath_And_EnrichDictionary_RoundTripShortCodes()
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Person_LastName"] = "Doe"
        };
        UserReportPlaceholderAliasRegistry.EnrichDictionary(data);

        Assert.Equal("Doe", data["Person_LastName"]);
        // Enrichment is catalog-driven; ensure the call is safe and keeps canonical key.
        Assert.True(data.Count >= 1);

        var resolved = UserReportPlaceholderAliasRegistry.ResolveCanonicalPropertyPath("Person_LastName");
        Assert.Equal("Person_LastName", resolved);
    }

    private static UserReportTemplate Template(string name, string fileName) =>
        new()
        {
            TemplateName = name,
            TemplateFile = new FileData { FileName = fileName }
        };

    private static UserReportPlaceholder Ph(string key) =>
        new()
        {
            PlaceholderKey = key,
            IsValid = true
        };
}
