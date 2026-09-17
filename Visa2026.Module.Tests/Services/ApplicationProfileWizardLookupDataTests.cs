#nullable enable

using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileWizardLookupDataTests
{
    [Fact]
    public void LoadApplicabilityItemsForInstance_ViaMinistry_ReturnsOnlyCaseContract()
    {
        var contract = new ProjectContract { ID = Guid.NewGuid(), NameTm = "1574 -KIYANLI" };
        var instance = new ApplicationProfileInstance { ProjectContract = contract };

        var items = ApplicationProfileWizardLookupData.LoadApplicabilityItemsForInstance(
            objectSpace: null,
            instance,
            viaMinistry: true);

        var item = Assert.Single(items);
        Assert.Equal(contract.ID, item.Id);
        Assert.Contains("KIYANLI", item.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadApplicabilityItemsForInstance_Direct_ReturnsOnlyCaseMigrationService()
    {
        var service = new MigrationService { ID = Guid.NewGuid(), NameTm = "Ashgabat" };
        var instance = new ApplicationProfileInstance { MigrationService = service };

        var items = ApplicationProfileWizardLookupData.LoadApplicabilityItemsForInstance(
            objectSpace: null,
            instance,
            viaMinistry: false);

        var item = Assert.Single(items);
        Assert.Equal(service.ID, item.Id);
    }

    [Fact]
    public void LoadApplicabilityItemsForInstance_MissingLookup_IsEmpty()
    {
        var instance = new ApplicationProfileInstance();

        Assert.Empty(ApplicationProfileWizardLookupData.LoadApplicabilityItemsForInstance(
            objectSpace: null,
            instance,
            viaMinistry: true));
        Assert.Empty(ApplicationProfileWizardLookupData.LoadApplicabilityItemsForInstance(
            objectSpace: null,
            instance: null,
            viaMinistry: true));
    }
}