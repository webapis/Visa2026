#nullable enable

using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// WDUT / Gelmeginiň maksady search aliases added with the WorkDuty placeholder.
/// </summary>
public class ScanPlaceholderChoiceListWorkDutySearchTests
{
    [Theory]
    [InlineData("WDUT")]
    [InlineData("work duty")]
    [InlineData("gelmegin")]
    [InlineData("purpose of arrival")]
    [InlineData("WorkDuty_Description")]
    public void RemainingGroups_work_duty_aliases_surface_WDUT(string search)
    {
        var allowed = WorkDutySet().Allowed;

        var codes = ScanPlaceholderChoiceList.RemainingGroups(
                allowed,
                hideShortCodes: Array.Empty<string>(),
                search: search)
            .SelectMany(static g => g.Entries)
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("WDUT", codes);
    }

    [Fact]
    public void MatchesSearch_WDUT_entry_matches_purpose_of_arrival_alias()
    {
        var entry = WorkDutySet().Allowed.Single(e =>
            string.Equals(e.ShortCode, "WDUT", StringComparison.OrdinalIgnoreCase));

        Assert.True(ScanPlaceholderChoiceList.MatchesSearch(entry, "purpose of arrival"));
        Assert.True(ScanPlaceholderChoiceList.MatchesSearch(entry, "WDUT"));
        Assert.False(ScanPlaceholderChoiceList.MatchesSearch(entry, "passport number"));
    }

    private static ApplicationProfilePlaceholderSet WorkDutySet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
                {
                    RequirePersonPosition = true,
                    ProduceWorkPermit = true,
                    Code = "get_invitation_wp",
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });
}
