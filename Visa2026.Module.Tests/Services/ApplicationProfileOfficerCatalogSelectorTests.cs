using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileCatalog;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileOfficerCatalogSelectorTests
{
    [Fact]
    public void SelectDistinctTemplates_CollapsesContractClones_PrefersTypeOnly()
    {
        var typeOnly = NewProfile("get_invitation", "201", "Gulluk Pasporty Üçin Çakylyk Almak", contract: false);
        var clones = new[]
        {
            typeOnly,
            NewProfile("get_invitation", "201", "Gulluk Pasporty Üçin Çakylyk Almak (TAP)", contract: true),
            NewProfile("get_invitation", "201", "Gulluk Pasporty Üçin Çakylyk Almak (14306 Mary)", contract: true),
        };

        var selected = ApplicationProfileOfficerCatalogSelector.SelectDistinctTemplates(clones).ToList();

        Assert.Single(selected);
        Assert.Same(typeOnly, selected[0]);
    }

    [Fact]
    public void SelectDistinctTemplates_KeepsDifferentSelectionCodes()
    {
        var invitation = NewProfile("get_invitation", "101", "Çakylyk", contract: true);
        var workPermit = NewProfile("get_invitation", "102", "Çakylyk + WP", contract: true);
        var official = NewProfile("get_invitation", "201", "Gulluk Pasporty", contract: false);

        var selected = ApplicationProfileOfficerCatalogSelector
            .SelectDistinctTemplates([invitation, workPermit, official])
            .OrderBy(p => p.SelectionCode)
            .ToList();

        Assert.Equal(3, selected.Count);
        Assert.Equal(new[] { "101", "102", "201" }, selected.Select(p => p.SelectionCode).ToArray());
    }

    [Fact]
    public void SelectDistinctTemplates_WhenOnlyContractClones_PicksShortestName()
    {
        var longer = NewProfile("get_invitation", "101", "Çakylyk (VERY-LONG-CONTRACT)", contract: true);
        var shorter = NewProfile("get_invitation", "101", "Çakylyk (TAP)", contract: true);

        var selected = ApplicationProfileOfficerCatalogSelector.SelectDistinctTemplates([longer, shorter]).Single();

        Assert.Same(shorter, selected);
    }

    private static ApplicationProfile NewProfile(string code, string selectionCode, string name, bool contract)
    {
        var profile = new ApplicationProfile
        {
            Code = code,
            SelectionCode = selectionCode,
            Name = name,
            IsActive = true,
        };
        if (contract)
            profile.DefaultProjectContractId = Guid.NewGuid();
        return profile;
    }
}
