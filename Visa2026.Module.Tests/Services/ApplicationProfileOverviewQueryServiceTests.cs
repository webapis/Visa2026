using System;
using System.Collections.ObjectModel;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileOverview;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileOverviewQueryServiceTests
{
    [Fact]
    public void Load_UnresolvedProfile_IsPrototypeMock()
    {
        var snapshot = new ApplicationProfileOverviewQueryService().Load(Guid.Empty, objectSpace: null);

        Assert.True(snapshot.IsPrototypeMock);
        Assert.Equal("INV_WP_EMP", snapshot.Code);
    }

    [Fact]
    public void MapFromProfile_UsesLiveData_WithoutMockFillers()
    {
        var profile = new ApplicationProfile
        {
            Name = "Invitation employee",
            Code = "INV_EMP",
            MinistrySlaDays = 14,
            MigrationSlaDays = 7,
            RequirePersonPassport = false,
            ProduceInvitation = true,
            CancelBorderZonePermits = true,
        };

        var snapshot = ApplicationProfileOverviewQueryService.MapFromProfile(profile, objectSpace: null);

        Assert.False(snapshot.IsPrototypeMock);
        Assert.Equal("Invitation employee", snapshot.Name);
        Assert.Equal(14, snapshot.MinistrySlaDays);
        Assert.Equal(7, snapshot.MigrationSlaDays);
        Assert.Contains(snapshot.LiveConfigurationLines, l => l.Contains("invitation"));
        Assert.Contains(snapshot.LiveConfigurationLines, l => l.Contains("border zone permits"));
        Assert.DoesNotContain(snapshot.LiveConfigurationLines, l => l.StartsWith("Project contract:", StringComparison.Ordinal));
        Assert.Empty(snapshot.NestedTemplates);
        Assert.Empty(snapshot.PersonDataToggles);
        Assert.Contains(snapshot.PerApplicationDefaults, r => r.FieldLabel == "Border Zone" && r.Required);
        Assert.Contains(snapshot.PerApplicationDefaults, r => r.FieldLabel == "Process number" && r.Required);
        Assert.Empty(snapshot.LinkedApplications);
        Assert.Equal(0, snapshot.LinkedApplicationCount);
    }

    [Fact]
    public void MapFromProfile_IncludesDefaultPassportToggle()
    {
        var profile = new ApplicationProfile { Name = "P", Code = "P" };

        var snapshot = ApplicationProfileOverviewQueryService.MapFromProfile(profile, objectSpace: null);

        Assert.Equal(new[] { "Passport" }, snapshot.PersonDataToggles);
    }

    [Fact]
    public void MapLinkedRow_UsesCaptionDateAndProgressDisplay()
    {
        var instance = new ApplicationProfileInstance
        {
            FullApplicationNumber = "12/-7010",
            ProcessNumber = "PN-1",
            ApplicationDate = new DateTime(2026, 8, 1),
            LatestProgressDisplay = "Office preparation",
        };

        var row = ApplicationProfileOverviewQueryService.MapLinkedRow(instance);

        Assert.Equal(ApplicationProcessNumberHelper.FormatDisplayCaption(instance), row.FullNumber);
        Assert.Equal("01.08.2026", row.ApplicationDate);
        Assert.Equal("Office preparation", row.Status);
    }

    [Fact]
    public void MapFromProfile_CapsLinkedApplications_AndKeepsTotalCount()
    {
        var profile = new ApplicationProfile
        {
            Name = "Busy",
            Code = "BUSY",
            RequirePersonPassport = false,
            Instances = new ObservableCollection<ApplicationProfileInstance>(),
        };

        for (var i = 0; i < ApplicationProfileOverviewQueryService.LinkedApplicationsDisplayCap + 1; i++)
        {
            profile.Instances.Add(new ApplicationProfileInstance
            {
                FullApplicationNumber = $"N-{i:00}",
                ApplicationDate = new DateTime(2026, 1, 1).AddDays(i),
                LatestProgressDisplay = "In process",
            });
        }

        var snapshot = ApplicationProfileOverviewQueryService.MapFromProfile(profile, objectSpace: null);

        Assert.Equal(ApplicationProfileOverviewQueryService.LinkedApplicationsDisplayCap + 1, snapshot.LinkedApplicationCount);
        Assert.Equal(ApplicationProfileOverviewQueryService.LinkedApplicationsDisplayCap, snapshot.LinkedApplications.Count);
        Assert.Equal("N-25", snapshot.LinkedApplications[0].FullNumber);
    }

    [Fact]
    public void MapFromProfile_IncludesRequiredFieldsWithoutDefaults()
    {
        var profile = new ApplicationProfile
        {
            Name = "Dates",
            Code = "DATES",
            SelectionCode = "101",
            ApplicabilityCriteria = "ProjectContract.Code = 'P1'",
            RequireStartDate = true,
            RequireEndDate = true,
            RequireRegion = true,
            RequireCity = true,
        };

        var snapshot = ApplicationProfileOverviewQueryService.MapFromProfile(profile, objectSpace: null);

        Assert.Equal("101", snapshot.SelectionCode);
        Assert.False(snapshot.IsAlwaysAvailable);
        Assert.Equal("ProjectContract.Code = 'P1'", snapshot.ApplicabilityCriteria);
        Assert.Contains(snapshot.PerApplicationDefaults, r => r.FieldLabel == "Start date" && r.Required);
        Assert.Contains(snapshot.PerApplicationDefaults, r => r.FieldLabel == "End date" && r.Required);
        Assert.Contains(snapshot.PerApplicationDefaults, r => r.FieldLabel == "Region" && r.Required);
        Assert.Contains(snapshot.PerApplicationDefaults, r => r.FieldLabel == "City" && r.Required);
    }

    [Fact]
    public void MapFromProfile_MapsTemplateScope()
    {
        var profile = new ApplicationProfile
        {
            Name = "Full",
            Code = "FULL",
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            NestedTemplates = new ObservableCollection<ApplicationProfileTemplate>
            {
                new()
                {
                    TemplateName = "Sanaw",
                    TemplateKind = ApplicationProfileTemplateKind.Excel,
                    CatalogScope = ApplicationProfileTemplateCatalogScope.Category,
                    DataScope = ApplicationProfileTemplateDataScope.Both,
                    CategoryKey = "WorkPermit",
                    SortOrder = 1,
                },
            },
        };

        var snapshot = ApplicationProfileOverviewQueryService.MapFromProfile(profile, objectSpace: null);

        Assert.Empty(snapshot.ProgressStates);
        Assert.Single(snapshot.NestedTemplates);
        Assert.Equal("Sanaw", snapshot.NestedTemplates[0].Name);
        Assert.Equal("Excel", snapshot.NestedTemplates[0].Kind);
        Assert.Equal("Shared", snapshot.NestedTemplates[0].Scope);
        Assert.Equal("Header + M2M", snapshot.NestedTemplates[0].DataScope);
        Assert.Equal("Work permit", snapshot.NestedTemplates[0].Category);
    }

}
