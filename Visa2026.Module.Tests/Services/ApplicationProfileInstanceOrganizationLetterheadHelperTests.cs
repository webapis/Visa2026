using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstanceOrganizationLetterheadHelperTests
{
    [Fact]
    public void Resolve_uses_instance_relations_when_set()
    {
        var application = new ApplicationProfileInstance
        {
            OrganizationCompany = new CompanyProfile { Name = "Case Co", Code = "CC" },
            OrganizationSignatory = new AuthorizedSignatory { FullName = "Mehmet" },
        };

        var resolved = ApplicationProfileInstanceOrganizationLetterheadHelper.Resolve(application);

        Assert.Equal("Case Co", resolved.CompanyName);
        Assert.Equal("CC", resolved.CompanyCode);
        Assert.Equal("Mehmet", resolved.SignatoryFullName);
        Assert.True(resolved.Copied);
    }

    [Fact]
    public void AssignDefaultsIfEmpty_skips_when_company_already_set()
    {
        var kept = new CompanyProfile { Name = "Kept" };
        var application = new ApplicationProfileInstance { OrganizationCompany = kept };

        OrganizationCatalogHelper.AssignDefaultsIfEmpty(application, objectSpace: null);

        Assert.Same(kept, application.OrganizationCompany);
    }

    [Fact]
    public void DisplayCompany_includes_code()
    {
        var line = OrganizationCatalogHelper.DisplayCompany(new CompanyProfile
        {
            Name = "Calik",
            Code = "CLK",
        });

        Assert.Equal("Calik (CLK)", line);
    }

    [Fact]
    public void DisplayPerson_joins_name_and_title()
    {
        Assert.Equal("Mehmet — Director", OrganizationCatalogHelper.DisplayPerson("Mehmet", "Director"));
        Assert.Equal("Mehmet", OrganizationCatalogHelper.DisplayPerson("Mehmet", "  "));
    }

    [Fact]
    public void FilterRows_matches_name_code_and_title()
    {
        var rows = new[]
        {
            new OrganizationCatalogRow
            {
                Id = Guid.NewGuid(),
                Kind = OrganizationCatalogHelper.Company,
                Name = "Çalık Enerji",
                Code = "CLK",
                IsDefault = true,
            },
            new OrganizationCatalogRow
            {
                Id = Guid.NewGuid(),
                Kind = OrganizationCatalogHelper.Signatory,
                Name = "Ali Demir",
                Title = "Mali işler",
            },
        };

        Assert.Single(OrganizationCatalogHelper.FilterRows(rows, "clk"));
        Assert.Single(OrganizationCatalogHelper.FilterRows(rows, "mali"));
        Assert.Empty(OrganizationCatalogHelper.FilterRows(rows, "zzz"));
    }
}