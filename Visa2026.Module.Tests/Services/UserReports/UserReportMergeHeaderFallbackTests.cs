#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class UserReportMergeHeaderFallbackTests
{
    [Fact]
    public void Item_root_reads_application_number_and_migration_service()
    {
        var application = new ApplicationProfileInstance
        {
            FullApplicationNumber = "10/-12521",
            MigrationService = new MigrationService { NameTm = "Dowlet migrasiya gullugy" },
        };
        var line = new ApplicationRosterMergeLine { ApplicationProfileInstance = application };

        Assert.Null(UserReportMergeDataHelper.GetPropertyValue(line, "AFNUM"));
        Assert.Equal("10/-12521", UserReportMergeDataHelper.GetPropertyValueFromItemOrApplication(line, "AFNUM"));
        Assert.Equal(
            "Dowlet migrasiya gullugy",
            UserReportMergeDataHelper.GetPropertyValueFromItemOrApplication(line, "MSRV"));
        Assert.Equal(0, Assert.IsType<int>(UserReportMergeDataHelper.GetPropertyValueFromItemOrApplication(line, "TPCNT")));
    }

    [Fact]
    public void Yuztutma_ds_tokens_are_application_header_only()
    {
        var template = WordTemplate(
            "Yuztutma-Hasapdan Cykarmak",
            "{{ds.AFNUM}}",
            "{{ds.ADAT}}",
            "{{ds.MSRV}}",
            "{{ds.TPCNT}}",
            "{{ds.ACPOS}}",
            "{{ds.CHFN}}",
            "IMAGE:PPH");

        Assert.True(UserReportMergeDataHelper.IsApplicationHeaderOnlyWordTemplate(template));
    }

    [Fact]
    public void Forma16_row_tokens_are_not_application_header_only()
    {
        var template = WordTemplate("FORMA 16", ".PFN", ".PNAT", "IMAGE:PPH");

        Assert.False(UserReportMergeDataHelper.IsApplicationHeaderOnlyWordTemplate(template));
    }

    [Fact]
    public void Sanaw_row_list_is_not_application_header_only()
    {
        var template = WordTemplate("SANAW-HASABA ALMAK", "#ds.rows", ".PFN");

        Assert.False(UserReportMergeDataHelper.IsApplicationHeaderOnlyWordTemplate(template));
    }

    private static UserReportTemplate WordTemplate(string name, params string[] keys)
    {
        var template = new UserReportTemplate
        {
            TemplateName = name,
            TemplateOutputFormat = TemplateOutputFormat.Word,
            RootBoType = UserReportBoType.ApplicationItem,
        };
        foreach (var key in keys)
        {
            template.Placeholders.Add(new UserReportPlaceholder { PlaceholderKey = key });
        }

        return template;
    }
}