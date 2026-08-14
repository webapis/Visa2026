using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class Visa2026DbContextModelTests
{
    /// <summary>
    /// The Blazor host builds the model with proxies, so a plain POCO reached through a navigation
    /// property fails at startup ("… is not virtual") instead of at build time.
    /// </summary>
    [Fact]
    public void Model_BuildsWithProxies_WithoutRosterMergeLineEntity()
    {
        var options = new DbContextOptionsBuilder<Visa2026EFCoreDbContext>()
            .UseNpgsql("Host=localhost;Database=visa2026_model_check;Username=model;Password=model")
            .UseChangeTrackingProxies()
            .UseLazyLoadingProxies()
            .Options;

        using var context = new Visa2026EFCoreDbContext(options);

        Assert.Null(context.Model.FindEntityType(typeof(ApplicationRosterMergeLine)));
        Assert.DoesNotContain(
            context.Model.GetEntityTypes(),
            entity => entity.ClrType?.Name == "ApplicationProfileInstancePerson");

        var join = context.Model.FindEntityType("ApplicationProfileInstancePeople");
        Assert.NotNull(join);
        Assert.DoesNotContain(join!.GetProperties(), p => p.Name is "ID" or "LinkedAt");

        var resolved = context.Model.FindEntityType(typeof(ApplicationProfileInstancePersonResolvedLink));
        Assert.NotNull(resolved);
        Assert.NotNull(resolved!.FindProperty(nameof(ApplicationProfileInstancePersonResolvedLink.ApplicationProfileInstanceId)));
        Assert.NotNull(resolved.FindProperty(nameof(ApplicationProfileInstancePersonResolvedLink.PersonId)));
        Assert.Null(resolved.FindProperty("ApplicationProfileInstancePersonId"));

        foreach (var joinName in new[]
        {
            "ApplicationProfileInstancePassports",
            "ApplicationProfileInstanceVisas",
            "ApplicationProfileInstanceEducations",
            "ApplicationProfileInstanceAddressesOfResidence",
            "ApplicationProfileInstanceEmployeePositionHistories",
            "ApplicationProfileInstanceEmployeeSalaries",
            "ApplicationProfileInstanceMedicalRecords",
            "ApplicationProfileInstanceWorkDuties",
            "ApplicationProfileInstanceInvitationItems",
            "ApplicationProfileInstanceWorkPermitItems",
            "ApplicationProfileInstanceBorderZoneItems",
            "ApplicationProfileInstanceTravelHistories",
        })
        {
            var childJoin = context.Model.FindEntityType(joinName);
            Assert.NotNull(childJoin);
            Assert.DoesNotContain(childJoin!.GetProperties(), p => p.Name is "ID" or "LinkedAt");
        }

        foreach (var headerJoinName in new[]
        {
            "ApplicationProfileInstanceInvitations",
            "ApplicationProfileInstanceWorkPermits",
            "ApplicationProfileInstanceBorderZones",
        })
        {
            Assert.Null(context.Model.FindEntityType(headerJoinName));
        }

        AssertHeaderOneToMany(
            context,
            typeof(Invitation),
            nameof(ApplicationProfileInstance.Invitations),
            required: false);
        AssertHeaderOneToMany(
            context,
            typeof(WorkPermit),
            nameof(ApplicationProfileInstance.WorkPermits),
            required: false);
        AssertHeaderOneToMany(
            context,
            typeof(BorderZone),
            nameof(ApplicationProfileInstance.BorderZones),
            required: true);
        AssertHeaderOneToMany(
            context,
            typeof(Rejection),
            nameof(ApplicationProfileInstance.Rejections),
            required: true);

        var visa = context.Model.FindEntityType(typeof(Visa));
        Assert.NotNull(visa);
        var issuingFk = Assert.Single(
            visa!.GetForeignKeys(),
            f => f.DependentToPrincipal?.Name == nameof(Visa.IssuingApplicationProfileInstance));
        Assert.Equal(nameof(ApplicationProfileInstance.IssuedVisas), issuingFk.PrincipalToDependent?.Name);
        Assert.False(issuingFk.IsRequired);

        var borderZoneItem = context.Model.FindEntityType(typeof(BorderZoneItem));
        Assert.NotNull(borderZoneItem);
        Assert.Equal("BorderZoneItems", borderZoneItem!.GetTableName());
    }

    private static void AssertHeaderOneToMany(
        Visa2026EFCoreDbContext context,
        Type headerType,
        string collectionName,
        bool required)
    {
        var header = context.Model.FindEntityType(headerType);
        Assert.NotNull(header);
        var fk = Assert.Single(
            header!.GetForeignKeys(),
            f => f.PrincipalEntityType.ClrType == typeof(ApplicationProfileInstance));
        Assert.Equal(collectionName, fk.PrincipalToDependent?.Name);
        Assert.Equal(required, fk.IsRequired);
    }
}
