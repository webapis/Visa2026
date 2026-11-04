using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationProfilePickerShelfTests
{
    [Fact]
    public void Heuristic_is_name_only_ignores_flags_and_code()
    {
        var byFlags = new ApplicationProfile
        {
            Code = "extend_visa_wp",
            Name = "Something else",
            ProduceVisa = true,
            ProduceWorkPermit = true,
            ForFamilyMember = true,
            ActionFamily = ApplicationProfileActionFamily.Registration,
        };
        Assert.Empty(ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(byFlags));
    }

    [Fact]
    public void Heuristic_family_from_name()
    {
        var profile = new ApplicationProfile { Code = "x", Name = "Maşgala agzasy üçin" };
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.FamilyMemberOnly,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile));
    }

    [Fact]
    public void Heuristic_registration_from_hasaba_in_name()
    {
        var profile = new ApplicationProfile { Code = "x", Name = "Hasaba Almak" };
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.Registration,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile));
    }

    [Fact]
    public void Heuristic_cakylyk_goes_to_invitations_not_visa()
    {
        var invitationOnly = new ApplicationProfile { Code = "get_invitation", Name = "Çakylyk Almak" };
        var codes = ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(invitationOnly);
        Assert.Contains(ApplicationProfilePickerShelfCodes.Invitations, codes);
        Assert.DoesNotContain(ApplicationProfilePickerShelfCodes.Visa, codes);

        var invitationAndWp = new ApplicationProfile
        {
            Code = "get_invitation_wp",
            Name = "Çakylyk we Iş Rugsatnamasyny Almak",
            ProduceVisa = true,
        };
        codes = ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(invitationAndWp);
        Assert.Contains(ApplicationProfilePickerShelfCodes.Invitations, codes);
        Assert.Contains(ApplicationProfilePickerShelfCodes.WorkPermitOnly, codes);
        Assert.DoesNotContain(ApplicationProfilePickerShelfCodes.Visa, codes);
    }

    [Fact]
    public void Heuristic_cakylygy_also_goes_to_invitations()
    {
        var change = new ApplicationProfile { Code = "change_invitation", Name = "Çakylygy üýtgetmek" };
        var cancel = new ApplicationProfile { Code = "cancel_invitation", Name = "Çakylygy ýatyrmak" };

        Assert.Contains(
            ApplicationProfilePickerShelfCodes.Invitations,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(change));
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.Invitations,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(cancel));
        Assert.DoesNotContain(
            ApplicationProfilePickerShelfCodes.Visa,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(change));
    }

    [Fact]
    public void Heuristic_extend_visa_wp_name_is_on_visa_and_work_permit()
    {
        var profile = new ApplicationProfile
        {
            Code = "ignored_code",
            Name = "Wiza we Iş Rugsatnamasyny Uzaltmak",
        };

        var codes = ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile);
        Assert.Contains(ApplicationProfilePickerShelfCodes.Visa, codes);
        Assert.Contains(ApplicationProfilePickerShelfCodes.WorkPermitOnly, codes);
        Assert.DoesNotContain(ApplicationProfilePickerShelfCodes.Invitations, codes);
    }

    [Fact]
    public void Filter_Shelf_includes_multi_membership_rows()
    {
        var shelfA = Guid.NewGuid();
        var shelfB = Guid.NewGuid();
        var rows = new List<ApplicationProfilePickerRow>
        {
            new() { ProfileId = Guid.NewGuid(), Name = "both", ShelfIds = new[] { shelfA, shelfB } },
            new() { ProfileId = Guid.NewGuid(), Name = "a-only", ShelfIds = new[] { shelfA } },
            new() { ProfileId = Guid.NewGuid(), Name = "none", ShelfIds = Array.Empty<Guid>() },
        };

        var filtered = ApplicationProfilePickerShelfFilter.Filter(
            new ApplicationProfilePickerTabKey(ApplicationProfilePickerTabKind.Shelf, shelfA),
            rows);

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, r => r.Name == "both");
        Assert.Contains(filtered, r => r.Name == "a-only");
    }

    [Fact]
    public void Filter_Recent_orders_and_caps()
    {
        var shelf = Guid.NewGuid();
        var rows = new List<ApplicationProfilePickerRow>
        {
            new() { ProfileId = Guid.NewGuid(), Name = "a", LastUsedAt = new DateTime(2026, 1, 1), ShelfIds = new[] { shelf } },
            new() { ProfileId = Guid.NewGuid(), Name = "b", LastUsedAt = new DateTime(2026, 3, 1), ShelfIds = Array.Empty<Guid>() },
            new() { ProfileId = Guid.NewGuid(), Name = "c", LastUsedAt = null, ShelfIds = new[] { shelf } },
        };

        var filtered = ApplicationProfilePickerShelfFilter.Filter(
            new ApplicationProfilePickerTabKey(ApplicationProfilePickerTabKind.Recent),
            rows);

        Assert.Equal(2, filtered.Count);
        Assert.Equal("b", filtered[0].Name);
        Assert.Equal("a", filtered[1].Name);
    }

    [Fact]
    public void DefaultTab_prefers_Recent_when_any_LastUsed()
    {
        var rows = new List<ApplicationProfilePickerRow>
        {
            new() { ProfileId = Guid.NewGuid(), Name = "one", LastUsedAt = DateTime.UtcNow, ShelfIds = new[] { Guid.NewGuid() } },
            new() { ProfileId = Guid.NewGuid(), Name = "two", LastUsedAt = null },
        };
        var tab = ApplicationProfilePickerShelfFilter.DefaultTab(rows);
        Assert.Equal(ApplicationProfilePickerTabKind.Recent, tab.Kind);
    }

    [Fact]
    public void Heuristic_trailing_FM_counts_as_family()
    {
        var profile = new ApplicationProfile { Code = "visa_ext_fm", Name = "Wiza Möhletini Uzaltmak FM" };
        var codes = ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile);
        Assert.Contains(ApplicationProfilePickerShelfCodes.FamilyMemberOnly, codes);
        Assert.Contains(ApplicationProfilePickerShelfCodes.Visa, codes);
    }

    [Fact]
    public void Heuristic_masgala_agzasy_in_name_is_family()
    {
        var profile = new ApplicationProfile { Code = "visa_ext_fm", Name = "Wiza Möhletini Uzaltmak Maşgala agzasy" };
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.FamilyMemberOnly,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile));
    }

    [Fact]
    public void Heuristic_business_trip_from_is_sapary_name()
    {
        var arrival = new ApplicationProfile { Code = "business_trip_arrival", Name = "Iş Saparyna Gelmek" };
        var departure = new ApplicationProfile { Code = "business_trip_departure", Name = "Iş Saparyna Gitmek" };
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.BusinessTrip,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(arrival));
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.BusinessTrip,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(departure));
    }

    [Fact]
    public void SystemShelves_include_business_trip()
    {
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.BusinessTrip,
            ApplicationProfilePickerShelfCodes.SystemShelves.Select(s => s.Code));
    }

    [Fact]
    public void Heuristic_additional_wp_location_goes_to_work_permit()
    {
        var profile = new ApplicationProfile
        {
            Code = "change_workpermit",
            Name = "Iş Rugsatnama goşmaça  barjak ýeri",
        };
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.WorkPermitOnly,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile));
    }

    [Fact]
    public void Heuristic_work_permit_matches_turkish_capital_I()
    {
        var profile = new ApplicationProfile
        {
            Code = "get_invitation_according_to_wp",
            Name = "İş Rugsatnama görä Çakylyk Almak",
        };
        var codes = ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile);
        Assert.Contains(ApplicationProfilePickerShelfCodes.WorkPermitOnly, codes);
        Assert.Contains(ApplicationProfilePickerShelfCodes.Invitations, codes);
    }

    [Fact]
    public void Heuristic_serhet_yaka_goes_to_border_zone()
    {
        var obtain = new ApplicationProfile { Code = "get_borderzone", Name = "Serhet Ýaka Üçin Rugsatnama Almak" };
        var cancel = new ApplicationProfile { Code = "cancel_borderzone", Name = "Serhet Ýaka Rugsady Ýatyrmak" };
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.BorderZone,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(obtain));
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.BorderZone,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(cancel));
        Assert.DoesNotContain(
            ApplicationProfilePickerShelfCodes.WorkPermitOnly,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(obtain));
    }

    [Fact]
    public void SystemShelves_include_border_zone()
    {
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.BorderZone,
            ApplicationProfilePickerShelfCodes.SystemShelves.Select(s => s.Code));
    }

    [Fact]
    public void SystemShelves_include_seretmezlik()
    {
        Assert.Contains(
            ApplicationProfilePickerShelfCodes.Seretmezlik,
            ApplicationProfilePickerShelfCodes.SystemShelves.Select(s => s.Code));
    }

    [Fact]
    public void ResolveShelfCodes_Seretmezlik_ByRequireTargetFlag()
    {
        var profile = new ApplicationProfile
        {
            Name = "Exclusion letter",
            RequireTargetApplicationProfileInstance = true,
        };

        Assert.Equal(
            ApplicationProfilePickerShelfCodes.Seretmezlik,
            Assert.Single(ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile)));
    }

    [Fact]
    public void ResolveShelfCodes_Seretmezlik_ByName()
    {
        var profile = new ApplicationProfile { Name = "Seretmezlik" };

        Assert.Contains(
            ApplicationProfilePickerShelfCodes.Seretmezlik,
            ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile));
    }

    [Fact]
    public void SystemShelves_include_visa_and_keep_registration()
    {
        var codes = ApplicationProfilePickerShelfCodes.SystemShelves.Select(s => s.Code).ToArray();
        Assert.Contains(ApplicationProfilePickerShelfCodes.Visa, codes);
        Assert.Contains(ApplicationProfilePickerShelfCodes.Registration, codes);
        Assert.Contains(ApplicationProfilePickerShelfCodes.Invitations, codes);
    }
}