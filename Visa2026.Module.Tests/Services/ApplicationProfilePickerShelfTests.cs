using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationProfilePickerShelfTests
{
    [Theory]
    [InlineData(true, false, false, null, ApplicationProfileActionFamily.Issuance, false, false, false, false, false, ApplicationProfilePickerShelfCodes.FamilyMemberOnly)]
    [InlineData(true, true, false, null, ApplicationProfileActionFamily.Registration, false, false, false, false, false, ApplicationProfilePickerShelfCodes.Registration)]
    [InlineData(false, true, false, "get_invitation", ApplicationProfileActionFamily.Issuance, true, false, false, false, false, ApplicationProfilePickerShelfCodes.Invitations)]
    [InlineData(false, true, false, "extend_visa_wp", ApplicationProfileActionFamily.Issuance, false, true, false, false, false, ApplicationProfilePickerShelfCodes.WorkPermitOnly)]
    [InlineData(false, true, false, "visa_ext", ApplicationProfileActionFamily.Issuance, false, false, false, false, false, null)]
    public void Heuristic_resolves_expected_shelf(
        bool forFamily,
        bool forEmployee,
        bool forVisitor,
        string? code,
        ApplicationProfileActionFamily family,
        bool produceInvitation,
        bool produceWp,
        bool cancelInvitation,
        bool cancelWp,
        bool changeInvitation,
        string? expected)
    {
        var profile = new ApplicationProfile
        {
            Code = code ?? "x",
            ForFamilyMember = forFamily,
            ForEmployee = forEmployee,
            ForTemporaryVisitor = forVisitor,
            ActionFamily = family,
            ProduceInvitation = produceInvitation,
            ProduceWorkPermit = produceWp,
            CancelInvitations = cancelInvitation,
            CancelWorkPermits = cancelWp,
            ChangeInvitations = changeInvitation,
        };

        Assert.Equal(expected, ApplicationProfilePickerShelfHeuristic.ResolveShelfCode(profile));
    }

    [Fact]
    public void Filter_All_returns_every_row()
    {
        var rows = SampleRows();
        var filtered = ApplicationProfilePickerShelfFilter.Filter(
            new ApplicationProfilePickerTabKey(ApplicationProfilePickerTabKind.All),
            rows);
        Assert.Equal(3, filtered.Count);
    }

    [Fact]
    public void Filter_Recent_orders_and_caps()
    {
        var shelf = Guid.NewGuid();
        var rows = new List<ApplicationProfilePickerRow>
        {
            new() { ProfileId = Guid.NewGuid(), Name = "a", LastUsedAt = new DateTime(2026, 1, 1), ShelfId = shelf },
            new() { ProfileId = Guid.NewGuid(), Name = "b", LastUsedAt = new DateTime(2026, 3, 1), ShelfId = null },
            new() { ProfileId = Guid.NewGuid(), Name = "c", LastUsedAt = null, ShelfId = shelf },
        };

        var filtered = ApplicationProfilePickerShelfFilter.Filter(
            new ApplicationProfilePickerTabKey(ApplicationProfilePickerTabKind.Recent),
            rows);

        Assert.Equal(2, filtered.Count);
        Assert.Equal("b", filtered[0].Name);
        Assert.Equal("a", filtered[1].Name);
    }

    [Fact]
    public void Filter_Shelf_is_exclusive()
    {
        var shelfA = Guid.NewGuid();
        var shelfB = Guid.NewGuid();
        var rows = new List<ApplicationProfilePickerRow>
        {
            new() { ProfileId = Guid.NewGuid(), Name = "a", ShelfId = shelfA },
            new() { ProfileId = Guid.NewGuid(), Name = "b", ShelfId = shelfB },
            new() { ProfileId = Guid.NewGuid(), Name = "c", ShelfId = null },
        };

        var filtered = ApplicationProfilePickerShelfFilter.Filter(
            new ApplicationProfilePickerTabKey(ApplicationProfilePickerTabKind.Shelf, shelfA),
            rows);

        Assert.Single(filtered);
        Assert.Equal("a", filtered[0].Name);
    }

    [Fact]
    public void DefaultTab_prefers_Recent_when_any_LastUsed()
    {
        var rows = SampleRows();
        var tab = ApplicationProfilePickerShelfFilter.DefaultTab(rows);
        Assert.Equal(ApplicationProfilePickerTabKind.Recent, tab.Kind);
    }

    private static List<ApplicationProfilePickerRow> SampleRows() =>
    [
        new() { ProfileId = Guid.NewGuid(), Name = "one", LastUsedAt = DateTime.UtcNow, ShelfId = Guid.NewGuid() },
        new() { ProfileId = Guid.NewGuid(), Name = "two", LastUsedAt = null, ShelfId = null },
        new() { ProfileId = Guid.NewGuid(), Name = "three", LastUsedAt = DateTime.UtcNow.AddDays(-2), ShelfId = Guid.NewGuid() },
    ];
}