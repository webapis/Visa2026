#nullable enable

using System.Reflection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class UserReportPlaceholderRelatedBoTests
{
    [Theory]
    [InlineData("CVCNT", "CancelVisaCount")]
    [InlineData("CVCTX", "CancelVisaCountText")]
    [InlineData("CWCNT", "CancelWPCount")]
    [InlineData("CWCTX", "CancelWPCountText")]
    [InlineData("CICNT", "CancelInvCount")]
    [InlineData("CICTX", "CancelInvCountText")]
    public void Cancel_document_count_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.Core, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.ApplicationCancellation, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Header, entry.Scope);
        Assert.Equal("{{ds." + shortCode + "}}", entry.BuildWordToken(UserReportPlaceholderScope.Header));
    }

    [Fact]
    public void Catalog_assigns_a_known_related_bo_to_every_entry()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var unknown = catalog.GetEntries()
            .Where(e => e.RelatedBo == UserReportPlaceholderRelatedBo.Unknown)
            .Select(e => e.ShortCode)
            .ToList();

        Assert.Empty(unknown);
    }

    [Theory]
    [InlineData("PPTP", "Passport_TypeTm")]
    [InlineData("PPAT", "Passport_Authority")]
    [InlineData("PPCC", "Passport_CountryCode")]
    [InlineData("PPCT", "Passport_CountryTm")]
    [InlineData("PRPN", "PreviousPassport_Number")]
    [InlineData("PRIS", "PreviousPassport_IssueDateText")]
    [InlineData("PRED", "PreviousPassport_ExpirationDateText")]
    [InlineData("PRTP", "PreviousPassport_TypeTm")]
    [InlineData("PRAT", "PreviousPassport_Authority")]
    [InlineData("PRCC", "PreviousPassport_CountryCode")]
    [InlineData("PRCT", "PreviousPassport_CountryTm")]
    public void Passport_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonPassport, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Passport, entry.RelatedBo);
    }

    [Theory]
    [InlineData("PMNM", "Person_MiddleName")]
    [InlineData("PMST", "Person_MaritalStatusTm")]
    [InlineData("PNTM", "Person_NationalityTm")]
    [InlineData("PCBT", "Person_CountryOfBirthTm")]
    [InlineData("PSEF", "Person_SponsoringEmployeeFullName")]
    [InlineData("PSEP", "Person_SponsoringEmployeePositionTm")]
    [InlineData("PVFM", "Person_VisaApplicationFamilyMembersText")]
    public void Person_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.Core, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Person, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Row, entry.Scope);
        Assert.Equal("{{." + shortCode + "}}", entry.BuildWordToken(UserReportPlaceholderScope.Header));
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void Previous_workplaces_token_is_catalogued_on_person()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, "PWTM", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Person_PreviousWorkplacesInTurkmenistan", entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.Core, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Person, entry.RelatedBo);
        Assert.Contains(UserReportBoType.ApplicationItem, entry.RootBoTypes);
        Assert.Contains(UserReportBoType.ApplicationProfileInstance, entry.RootBoTypes);

        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            "Person_PreviousWorkplacesInTurkmenistan", flags));
    }

    [Theory]
    [InlineData("EGLV", "Education_LevelTm")]
    [InlineData("EGIN", "Education_InstitutionName")]
    [InlineData("EGCC", "Education_CountryCode")]
    [InlineData("EGYR", "Education_GraduationYear")]
    [InlineData("EGSP", "Education_SpecialtyTm")]
    [InlineData("EGIY", "Education_LevelAndInstitutionTm")]
    public void Education_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonEducation, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Education, entry.RelatedBo);
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void Grouped_manual_puts_education_codes_together()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var groups = catalog.GetGroupedEntries();
        var education = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.Education);
        var codes = education.Entries.Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("EGLV", codes);
        Assert.Contains("EGIN", codes);
        Assert.Contains("EGCC", codes);
        Assert.Contains("EGYR", codes);
        Assert.Contains("EGSP", codes);
        Assert.DoesNotContain("EGIY", codes);
        Assert.DoesNotContain(education.Entries, e => e.RelatedBo != UserReportPlaceholderRelatedBo.Education);
    }

    [Fact]
    public void Passport_type_property_exists_on_roster_merge_line()
    {
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty("Passport_TypeTm", flags));
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty("Passport_Authority", flags));
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty("Passport_CountryCode", flags));
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty("Passport_CountryTm", flags));
    }

    [Fact]
    public void Grouped_manual_puts_passport_codes_together()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var groups = catalog.GetGroupedEntries();
        var passport = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.Passport);
        var codes = passport.Entries.Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("PPN", codes);
        Assert.Contains("PRPN", codes);
        Assert.Contains("PRIS", codes);
        Assert.Contains("PRED", codes);
        Assert.Contains("PPTP", codes);
        Assert.Contains("PPAT", codes);
        Assert.Contains("PPCC", codes);
        Assert.Contains("PPCT", codes);
        Assert.DoesNotContain(passport.Entries, e => e.RelatedBo != UserReportPlaceholderRelatedBo.Passport);
    }

    [Fact]
    public void Grouped_manual_puts_authorized_signatory_codes_together()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var groups = catalog.GetGroupedEntries();
        var signatory = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.CompanySignatory);
        var codes = signatory.Entries.Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal("Authorized signatory", UserReportPlaceholderRelatedBoCatalog.DisplayNameEn(signatory.RelatedBo));
        Assert.Contains("ACFNM", codes);
        Assert.Contains("ACPOS", codes);
        Assert.Contains("CHFN", codes);
        Assert.Contains("CHPL", codes);
        Assert.Contains("CHPN", codes);
        Assert.Contains("CHPA", codes);
        Assert.Contains("CHPD", codes);
        Assert.Contains("CHPE", codes);
        Assert.DoesNotContain(signatory.Entries, e => e.RelatedBo != UserReportPlaceholderRelatedBo.CompanySignatory);
    }

    [Theory]
    [InlineData("CWNB", "CancelWorkPermit_NumberBlock")]
    [InlineData("CWAB", "CancelWorkPermit_ASNumberBlock")]
    [InlineData("CWSB", "CancelWorkPermit_StartDateBlock")]
    [InlineData("CWEB", "CancelWorkPermit_ExpirationDateBlock")]
    [InlineData("CWLB", "CancelWorkPermit_LocationsBlock")]
    [InlineData("WPLC", "WorkPermit_WorkPermittedLocations")]
    [InlineData("WPNM", "WorkPermit_Number")]
    [InlineData("WPAS", "WorkPermit_ASNumber")]
    [InlineData("WPST", "WorkPermit_StartDateText")]
    [InlineData("WPED", "WorkPermit_ExpirationDateText")]
    public void Cancel_work_permit_block_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonWorkPermitItem, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.WorkPermit, entry.RelatedBo);
        Assert.Contains(UserReportBoType.ApplicationProfileInstance, entry.RootBoTypes);
        Assert.Contains(UserReportBoType.ApplicationItem, entry.RootBoTypes);
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void Case_work_permit_location_roster_token_is_catalogued()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, "AWPLC", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Application_WorkPermitLocation_NameTm", entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.Core, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.ApplicationGeneral, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Row, entry.Scope);
        Assert.Equal("{{.AWPLC}}", entry.BuildWordToken(UserReportPlaceholderScope.Row));
        Assert.Contains(UserReportBoType.ApplicationProfileInstance, entry.RootBoTypes);
        Assert.Contains(UserReportBoType.ApplicationItem, entry.RootBoTypes);
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            "Application_WorkPermitLocation_NameTm",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void Excel_row_repeats_case_work_permit_location()
    {
        var application = new ApplicationProfileInstance
        {
            MovementPermitLocation = "Aşgabat şäheri, Mary welaýaty",
        };
        var line = new ApplicationRosterMergeLine { ApplicationProfileInstance = application };
        var row = UserReportMergeDataHelper.BuildExcelItemListRowDictionary(line, 1);

        Assert.Equal("Aşgabat şäheri, Mary welaýaty", row["Application_WorkPermitLocation_NameTm"]);
        Assert.Equal("Aşgabat şäheri, Mary welaýaty", row["AWPLC"]);
        Assert.NotEqual(
            application.MovementPermitLocation,
            row.TryGetValue("WPLC", out var wplc) ? wplc : null);
    }

    [Fact]
    public void Excel_row_fills_linked_work_permit_item_fields()
    {
        var item = new WorkPermitItem
        {
            WorkPermitNumber = "1430/7",
            ASNumber = "COO01884433",
            StartDate = new DateTime(2026, 1, 1),
            ExpirationDate = new DateTime(2027, 1, 1),
            WorkPermittedLocations = "Aşgabat şäheri",
        };
        var line = new ApplicationRosterMergeLine { CurrentWorkPermitItem = item };
        var row = UserReportMergeDataHelper.BuildExcelItemListRowDictionary(line, 1);

        Assert.Equal("1430/7", row["WPNM"]);
        Assert.Equal("COO01884433", row["WPAS"]);
        Assert.Equal("01.01.2026", row["WPST"]);
        Assert.Equal("01.01.2027", row["WPED"]);
        Assert.Equal("Aşgabat şäheri", row["WPLC"]);
    }

    [Theory]
    [InlineData("BTSD", "BusinessTripStartDateText")]
    [InlineData("BTED", "BusinessTripEndDateText")]
    [InlineData("BTDCNT", "BusinessTripDurationDays")]
    [InlineData("BTDCTX", "BusinessTripDurationDaysText")]
    public void Business_trip_header_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.Core, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.BusinessTrip, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Header, entry.Scope);
        Assert.Equal("{{ds." + shortCode + "}}", entry.BuildWordToken(UserReportPlaceholderScope.Header));
        Assert.NotNull(typeof(ApplicationProfileInstance).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Theory]
    [InlineData("BTFRG", "FromRegionName_Genitive", "From Region")]
    [InlineData("BTFCT", "FromCityName_Ablative", "From City")]
    [InlineData("BTTRG", "ToRegionName_Genitive", "To Region")]
    [InlineData("BTTCT", "ToCityName_Dative", "To City")]
    [InlineData("BTPRP", "Purpose", "Purpose")]
    public void From_to_region_city_tokens_are_catalogued_as_application(string shortCode, string canonical, string labelEn)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.Core, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.ApplicationBusinessTrip, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Header, entry.Scope);
        Assert.Equal(labelEn, entry.LabelEn);
        Assert.Equal("{{ds." + shortCode + "}}", entry.BuildWordToken(UserReportPlaceholderScope.Header));
        Assert.NotNull(typeof(ApplicationProfileInstance).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void Business_trip_destination_address_is_catalogued()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, "BTAD", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("BusinessTripAddress_FullAddress", entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderRelatedBo.BusinessTrip, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Row, entry.Scope);
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            "BusinessTripAddress_FullAddress", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Theory]
    [InlineData("CINB", "CancelInvitation_NumberBlock")]
    [InlineData("CISB", "CancelInvitation_IssuedDateBlock")]
    [InlineData("CIEB", "CancelInvitation_ExpirationDateBlock")]
    public void Cancel_invitation_block_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonInvitationItem, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Invitation, entry.RelatedBo);
        Assert.Contains(UserReportBoType.ApplicationProfileInstance, entry.RootBoTypes);
        Assert.Contains(UserReportBoType.ApplicationItem, entry.RootBoTypes);
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Theory]
    [InlineData("INVN", "Invitation_Number")]
    [InlineData("INVS", "Invitation_StartDateText")]
    [InlineData("INVE", "Invitation_ExpirationDateText")]
    public void Invitation_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonInvitationItem, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Invitation, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Both, entry.Scope);
        Assert.Equal("{{ds." + shortCode + "}}", entry.BuildWordToken(UserReportPlaceholderScope.Header));
        Assert.Equal("{{." + shortCode + "}}", entry.BuildWordToken(UserReportPlaceholderScope.Row));
        Assert.Contains(UserReportBoType.ApplicationProfileInstance, entry.RootBoTypes);
        Assert.Contains(UserReportBoType.ApplicationItem, entry.RootBoTypes);

        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(canonical, flags));
        Assert.NotNull(typeof(ApplicationProfileInstance).GetProperty(canonical, flags));
    }

    [Theory]
    [InlineData("VNUM", "Visa_Number")]
    [InlineData("VTYP", "Visa_TypeTm")]
    [InlineData("VCTM", "Visa_CategoryTm")]
    [InlineData("VNAT", "Visa_NumberAndType")]
    [InlineData("VPLC", "Visa_IssuedPlaceTm")]
    [InlineData("VISD", "Visa_IssueDateText")]
    [InlineData("VSTD", "Visa_StartDateText")]
    [InlineData("VEDT", "Visa_ExpirationDateText")]
    [InlineData("VBLK", "Visa_DurationFrequencyBlock")]
    public void Linked_active_visa_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonVisa, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.VisaLinkedActive, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Row, entry.Scope);
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Theory]
    [InlineData("CVNB", "CancelVisa_NumberBlock")]
    [InlineData("CVSB", "CancelVisa_StartDateBlock")]
    [InlineData("CVEB", "CancelVisa_ExpirationDateBlock")]
    public void Cancel_visa_block_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonVisa, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.VisaCancel, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Row, entry.Scope);
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void Grouped_manual_splits_linked_active_and_cancel_visa()
    {
        var groups = new UserReportPlaceholderCatalogService().GetGroupedEntries();
        var linked = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.VisaLinkedActive);
        var cancel = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.VisaCancel);

        Assert.Equal("Visa — linked active", UserReportPlaceholderRelatedBoCatalog.DisplayNameEn(linked.RelatedBo));
        Assert.Equal("Visa — cancel", UserReportPlaceholderRelatedBoCatalog.DisplayNameEn(cancel.RelatedBo));
        Assert.Contains(linked.Entries, e => e.ShortCode == "VNUM");
        Assert.Contains(linked.Entries, e => e.ShortCode == "VTYP");
        Assert.Contains(linked.Entries, e => e.ShortCode == "VSTD");
        Assert.Contains(linked.Entries, e => e.ShortCode == "VEDT");
        Assert.DoesNotContain(linked.Entries, e => e.ShortCode is "CVNB" or "CVSB" or "CVEB");
        Assert.Contains(cancel.Entries, e => e.ShortCode == "CVNB");
        Assert.Contains(cancel.Entries, e => e.ShortCode == "CVSB");
        Assert.Contains(cancel.Entries, e => e.ShortCode == "CVEB");
        Assert.DoesNotContain(cancel.Entries, e => e.ShortCode == "VNUM");
        Assert.DoesNotContain(groups, g =>
            g.RelatedBo == UserReportPlaceholderRelatedBo.Visa
            && g.Entries.Any(e => e.ShortCode is "VNUM" or "CVNB"));
        Assert.DoesNotContain(linked.Entries, e => e.ShortCode == "VNAT");
    }

    [Fact]
    public void Grouped_manual_matches_review_add_hide_joined_and_search()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var officer = catalog.GetGroupedEntries();
        var review = ScanPlaceholderChoiceList.RemainingGroups(
            catalog.GetEntries(),
            Array.Empty<string>());

        Assert.Equal(
            review.Select(g => g.RelatedBo),
            officer.Select(g => g.RelatedBo));
        foreach (var (addGroup, manualGroup) in review.Zip(officer))
        {
            Assert.Equal(
                addGroup.Entries.Select(e => e.ShortCode),
                manualGroup.Entries.Select(e => e.ShortCode));
        }

        Assert.DoesNotContain(officer.SelectMany(g => g.Entries), e => e.ShortCode == "EGIY");
        Assert.DoesNotContain(officer.SelectMany(g => g.Entries), e => e.ShortCode == "VNAT");

        var visaStart = catalog.GetGroupedEntries(new UserReportPlaceholderManualQuery
        {
            Search = "visa start",
        });
        Assert.Contains(visaStart.SelectMany(g => g.Entries), e => e.ShortCode == "VSTD");

        var joined = catalog.GetGroupedEntries(new UserReportPlaceholderManualQuery
        {
            Search = "VNAT",
        });
        Assert.Contains(joined.SelectMany(g => g.Entries), e => e.ShortCode == "VNAT");
    }

    [Fact]
    public void Grouped_manual_puts_invitation_codes_together()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var groups = catalog.GetGroupedEntries();
        var invitation = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.Invitation);
        var codes = invitation.Entries.Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal("Invitation", UserReportPlaceholderRelatedBoCatalog.DisplayNameEn(invitation.RelatedBo));
        Assert.Contains("INVN", codes);
        Assert.Contains("INVS", codes);
        Assert.Contains("INVE", codes);
        Assert.Contains("CINB", codes);
        Assert.Contains("CISB", codes);
        Assert.Contains("CIEB", codes);
        Assert.DoesNotContain(invitation.Entries, e => e.RelatedBo != UserReportPlaceholderRelatedBo.Invitation);
    }

    [Fact]
    public void Related_bo_filter_returns_only_that_group()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var groups = catalog.GetGroupedEntries(new UserReportPlaceholderManualQuery
        {
            RelatedBo = UserReportPlaceholderRelatedBo.AuthorizedRepresentative,
        });

        Assert.Single(groups);
        Assert.Equal(UserReportPlaceholderRelatedBo.AuthorizedRepresentative, groups[0].RelatedBo);
        Assert.Contains(groups[0].Entries, e => e.ShortCode == "RPFN");
    }

    [Fact]
    public void FMRLH_is_relationship_only_under_Application_family_member()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, "FMRLH", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("FamilyMember_Relationship_NameTm", entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderRelatedBo.ApplicationFamilyMember, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Header, entry.Scope);
        Assert.Equal("FM relationship only (genitive)", entry.LabelEn);
        Assert.Equal("{{ds.FMRLH}}", entry.BuildWordToken(UserReportPlaceholderScope.Header));
    }

    [Fact]
    public void Application_family_member_group_contains_FM_header_tokens()
    {
        var groups = new UserReportPlaceholderCatalogService().GetGroupedEntries(
            new UserReportPlaceholderManualQuery());
        var family = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.ApplicationFamilyMember);
        Assert.Equal("Application — family member", UserReportPlaceholderRelatedBoCatalog.DisplayNameEn(family.RelatedBo));
        Assert.Contains(family.Entries, e => e.ShortCode == "FMRLH");
        Assert.Contains(family.Entries, e => e.ShortCode == "FMSPH");
        Assert.Contains(family.Entries, e => e.ShortCode == "FMREL");
    }

    [Fact]
    public void Header_enrich_adds_both_FMREL_and_FMRLH()
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["FamilyMember_Relationship_NameTm"] = "adamsynyň",
        };
        UserReportPlaceholderAliasRegistry.EnrichDictionary(data);
        Assert.Equal("adamsynyň", data["FMREL"]);
        Assert.Equal("adamsynyň", data["FMRLH"]);
    }
}
