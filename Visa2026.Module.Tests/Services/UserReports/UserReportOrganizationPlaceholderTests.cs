using System.Reflection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class UserReportOrganizationPlaceholderTests
{
    private static readonly string[] OrganizationCanonicalPaths =
    [
        "Company_Code",
        "Application_Company_Name",
        "Application_Company_Address",
        "Application_Company_PhoneNumber",
        "Application_Company_Email",
        "Application_Company_TaxInformation",
        "Application_Company_RegistrationDateText",
        "Application_CompanyRegistryAddressLine",
        "Application_CompanyHead_FullName",
        "Application_CompanyHead_PositionTm",
        "CompanyHead_FullName",
        "CompanyHead_PassportLine",
        "CompanyHead_PassportNumber",
        "CompanyHead_PassportAuthority",
        "CompanyHead_PassportIssueDateText",
        "Representative_FullName",
        "Representative_PositionTm",
        "Representative_Phone",
        "Representative_PassportLine",
        "Representative_PassportNumber",
        "Representative_PassportAuthority",
        "Representative_PassportIssueDateText",
        "Representative_PassportPhoneLine",
    ];

    [Fact]
    public void Catalog_includes_company_signatory_and_representative_short_codes()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var codes = catalog.GetEntries().Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("ACPHN", codes);
        Assert.Contains("ACEML", codes);
        Assert.Contains("ACTAX", codes);
        Assert.Contains("ACRDT", codes);
        Assert.Contains("CHPN", codes);
        Assert.Contains("CHPL", codes);
        Assert.Contains("RPFN", codes);
        Assert.Contains("RPPH", codes);
        Assert.Contains("RPPL", codes);
        Assert.Contains("RPCL", codes);
        Assert.Contains("PPTP", codes);
        Assert.Contains("PPAT", codes);
        Assert.Contains("PPCC", codes);
        Assert.Contains("PPCT", codes);
    }

    [Fact]
    public void Organization_canonical_paths_exist_on_case_and_roster_roots()
    {
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        foreach (var path in OrganizationCanonicalPaths)
        {
            Assert.True(
                typeof(ApplicationProfileInstance).GetProperty(path, flags) != null,
                path + " missing on ApplicationProfileInstance");
            Assert.True(
                typeof(ApplicationRosterMergeLine).GetProperty(path, flags) != null,
                path + " missing on ApplicationRosterMergeLine");
        }
    }
}