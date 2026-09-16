#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class UserReportPlaceholderCatalogRootBoTypeTests
{
    [Fact]
    public void Application_alias_includes_case_header_tokens_for_profile_instance_filter()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var forCase = catalog.GetEntries(new UserReportPlaceholderManualQuery
        {
            RootBoType = UserReportBoType.ApplicationProfileInstance,
        });

        Assert.Contains(forCase, e => e.ShortCode == "AFNUM");
        Assert.Contains(forCase, e => e.ShortCode == "ADAT");
        Assert.Contains(forCase, e => e.ShortCode == "ACNAM");
        Assert.Contains(forCase, e => e.ShortCode == "ACRDT");
        Assert.Contains(forCase, e => e.ShortCode == "TPCTX");
        Assert.Contains(forCase, e => e.ShortCode == "CVCNT");
        Assert.Contains(forCase, e => e.ShortCode == "CVCTX");
        Assert.Contains(forCase, e => e.ShortCode == "CWCNT");
        Assert.Contains(forCase, e => e.ShortCode == "CWCTX");
        Assert.Contains(forCase, e => e.ShortCode == "CICNT");
        Assert.Contains(forCase, e => e.ShortCode == "CICTX");
        Assert.Contains(forCase, e => e.ShortCode == "BTSD");
        Assert.Contains(forCase, e => e.ShortCode == "BTDCNT");
        Assert.Contains(forCase, e => e.ShortCode == "BTPRP");
        Assert.Contains(forCase, e => e.ShortCode == "BTAD");
        Assert.Contains(forCase, e => e.ShortCode == "BTFRG");
        Assert.Contains(forCase, e => e.ShortCode == "BTFCT");
        Assert.Contains(forCase, e => e.ShortCode == "BTTRG");
        Assert.Contains(forCase, e => e.ShortCode == "BTTCT");
        Assert.Contains(forCase, e => e.ShortCode == "Urgency_NameTm");
        // Dual-listed Application + ApplicationItem must still appear on case filter.
        Assert.Contains(forCase, e => e.ShortCode == "ACADR");
        Assert.Contains(forCase, e => e.ShortCode == "PFN");
    }
}