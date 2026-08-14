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
        Assert.Contains(snapshot.LiveConfigurationLines, l => l.Contains("Ministry SLA: 14"));
        Assert.Contains(snapshot.LiveConfigurationLines, l => l.Contains("invitation"));
        Assert.Contains(snapshot.LiveConfigurationLines, l => l.Contains("border zone permits"));
        Assert.Empty(snapshot.ApprovalLegs);
        Assert.Empty(snapshot.NestedTemplates);
        Assert.Empty(snapshot.PersonDataToggles);
        Assert.Empty(snapshot.PerApplicationDefaults);
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
}
