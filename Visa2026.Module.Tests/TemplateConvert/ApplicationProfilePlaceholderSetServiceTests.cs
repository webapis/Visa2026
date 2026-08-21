using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class ApplicationProfilePlaceholderSetServiceTests
{
    private readonly IUserReportPlaceholderCatalogService _catalog = new UserReportPlaceholderCatalogService();
    private readonly IApplicationProfilePlaceholderSetService _service;

    public ApplicationProfilePlaceholderSetServiceTests()
    {
        _service = new ApplicationProfilePlaceholderSetService(_catalog);
    }

    /// <summary>Every person record collected, so only scope and kind can exclude anything.</summary>
    private static ApplicationProfile FullProfile() =>
        new()
        {
            RequirePersonPassport = true,
            RequirePersonVisa = true,
            RequirePersonEducation = true,
            RequirePersonAddressOfResidence = true,
            RequirePersonPosition = true,
            RequirePersonSalary = true,
            RequirePersonMedical = true,
            RequirePersonInvitationItem = true,
            RequirePersonWorkPermitItem = true,
            RequirePersonBorderZoneItem = true,
            RequirePersonRejectionItem = true,
            RequirePersonTravelHistory = true,
        };

    private ApplicationProfilePlaceholderSet GetSet(
        ApplicationProfile profile,
        ApplicationProfileTemplateDataScope dataScope = ApplicationProfileTemplateDataScope.Both,
        ApplicationProfileTemplateKind kind = ApplicationProfileTemplateKind.Word) =>
        _service.GetSet(new ApplicationProfilePlaceholderSetQuery
        {
            Profile = profile,
            DataScope = dataScope,
            TemplateKind = kind,
        });

    private static bool Allows(ApplicationProfilePlaceholderSet set, string shortCode) =>
        set.Allowed.Any(e => string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

    private static PlaceholderExclusionReason ReasonFor(ApplicationProfilePlaceholderSet set, string shortCode) =>
        set.Excluded.Single(e => string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase)).Reason;

    [Fact]
    public void Every_catalog_entry_declares_a_known_pack()
    {
        var unknown = _catalog.GetEntries()
            .Where(static e => e.Pack == UserReportPlaceholderPack.Unknown)
            .Select(static e => e.ShortCode)
            .ToList();

        Assert.Empty(unknown);
    }

    [Fact]
    public void A_profile_that_collects_everything_allows_the_whole_catalog()
    {
        var set = GetSet(FullProfile());

        Assert.Equal(_catalog.GetEntries().Count, set.Allowed.Count);
        Assert.Empty(set.Excluded);
    }

    [Fact]
    public void Disabling_the_visa_pack_excludes_visa_tokens()
    {
        var profile = FullProfile();
        profile.RequirePersonVisa = false;

        var set = GetSet(profile);

        Assert.False(Allows(set, "VNUM"));
        Assert.Equal(PlaceholderExclusionReason.PersonPackDisabled, ReasonFor(set, "VNUM"));
    }

    /// <summary>
    /// `Contract_StartDateText` and `Contract_ExpirationDateText` are computed from
    /// `CurrentVisa.ExpirationDate`, so they belong to the visa pack despite the `Contract_` prefix.
    /// </summary>
    [Fact]
    public void Contract_date_tokens_follow_the_visa_pack_not_their_prefix()
    {
        var profile = FullProfile();
        profile.RequirePersonVisa = false;

        var set = GetSet(profile);

        Assert.Equal(PlaceholderExclusionReason.PersonPackDisabled, ReasonFor(set, "CSDT"));
        Assert.Equal(PlaceholderExclusionReason.PersonPackDisabled, ReasonFor(set, "CEDT"));
    }

    /// <summary>Salary keeps its own pack, so turning off visa must not take it with it.</summary>
    [Fact]
    public void Contract_salary_token_follows_the_salary_pack()
    {
        var profile = FullProfile();
        profile.RequirePersonVisa = false;

        Assert.True(Allows(GetSet(profile), "CSAL"));

        profile.RequirePersonSalary = false;
        Assert.Equal(PlaceholderExclusionReason.PersonPackDisabled, ReasonFor(GetSet(profile), "CSAL"));
    }

    /// <summary>
    /// `Passport_PersonalNumber` falls back to `Person.PersonalNumber`, so it resolves without a
    /// passport record and must stay allowed when the passport pack is off.
    /// </summary>
    [Fact]
    public void Personal_number_survives_a_disabled_passport_pack()
    {
        var profile = FullProfile();
        profile.RequirePersonPassport = false;

        var set = GetSet(profile);

        Assert.True(Allows(set, "PPIN"));
        Assert.Equal(PlaceholderExclusionReason.PersonPackDisabled, ReasonFor(set, "PPN"));
    }

    [Fact]
    public void Registration_purpose_follows_the_position_pack()
    {
        var profile = FullProfile();
        profile.RequirePersonPosition = false;

        var set = GetSet(profile);

        Assert.Equal(PlaceholderExclusionReason.PersonPackDisabled, ReasonFor(set, "RGEL"));
        Assert.Equal(PlaceholderExclusionReason.PersonPackDisabled, ReasonFor(set, "POSN"));
    }

    [Fact]
    public void Header_data_scope_excludes_row_only_tokens()
    {
        var set = GetSet(FullProfile(), ApplicationProfileTemplateDataScope.ApplicationHeader);

        Assert.True(Allows(set, "AFNUM"));
        Assert.Equal(PlaceholderExclusionReason.OutOfDataScope, ReasonFor(set, "PFN"));
    }

    [Fact]
    public void People_data_scope_excludes_header_only_tokens()
    {
        var set = GetSet(FullProfile(), ApplicationProfileTemplateDataScope.PeopleM2M);

        Assert.True(Allows(set, "PFN"));
        Assert.Equal(PlaceholderExclusionReason.OutOfDataScope, ReasonFor(set, "AFNUM"));
    }

    [Fact]
    public void Both_data_scope_keeps_header_and_row_tokens()
    {
        var set = GetSet(FullProfile(), ApplicationProfileTemplateDataScope.Both);

        Assert.True(Allows(set, "AFNUM"));
        Assert.True(Allows(set, "PFN"));
    }

    [Fact]
    public void Excel_templates_exclude_image_placeholders()
    {
        var set = GetSet(FullProfile(), kind: ApplicationProfileTemplateKind.Excel);

        Assert.Equal(PlaceholderExclusionReason.StructuralUnsupportedForKind, ReasonFor(set, "PPH"));
        Assert.True(Allows(GetSet(FullProfile(), kind: ApplicationProfileTemplateKind.Word), "PPH"));
    }

    [Fact]
    public void Pdf_form_templates_allow_nothing()
    {
        var set = GetSet(FullProfile(), kind: ApplicationProfileTemplateKind.PdfForm);

        Assert.Empty(set.Allowed);
        Assert.All(set.Excluded, e => Assert.Equal(PlaceholderExclusionReason.StructuralUnsupportedForKind, e.Reason));
    }

    [Fact]
    public void A_narrower_profile_never_allows_more_than_a_broader_one()
    {
        var broad = GetSet(FullProfile());

        var narrow = FullProfile();
        narrow.RequirePersonVisa = false;
        narrow.RequirePersonEducation = false;

        var narrowSet = GetSet(narrow);
        var broadCodes = broad.Allowed.Select(static e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(narrowSet.Allowed, e => Assert.Contains(e.ShortCode, broadCodes));
        Assert.True(narrowSet.Allowed.Count < broad.Allowed.Count);
    }

    [Fact]
    public void Fingerprint_is_stable_for_the_same_allowed_set_and_differs_otherwise()
    {
        var first = GetSet(FullProfile()).Fingerprint;
        var second = GetSet(FullProfile()).Fingerprint;
        Assert.Equal(first, second);

        var narrow = FullProfile();
        narrow.RequirePersonMedical = false;
        narrow.RequirePersonVisa = false;

        Assert.NotEqual(first, GetSet(narrow).Fingerprint);
    }

    [Theory]
    [InlineData("{{ds.PFN}}")]
    [InlineData("{{.PFN}}")]
    [InlineData("PFN")]
    [InlineData("  {{ds.pfn}}  ")]
    public void Contains_accepts_short_codes_and_full_tokens(string token) =>
        Assert.True(GetSet(FullProfile()).Contains(token));

    [Fact]
    public void Contains_recognises_image_tokens() =>
        Assert.True(GetSet(FullProfile()).Contains("{{IMAGE:PPH}}"));

    [Theory]
    [InlineData("{{ds.NOPE}}")]
    [InlineData("")]
    [InlineData("{{ds.Person.Nested}}")]
    public void Contains_rejects_unknown_or_nested_tokens(string token) =>
        Assert.False(GetSet(FullProfile()).Contains(token));

    [Fact]
    public void Contains_rejects_a_token_whose_pack_the_profile_disabled()
    {
        var profile = FullProfile();
        profile.RequirePersonVisa = false;

        Assert.False(GetSet(profile).Contains("{{.VNUM}}"));
    }
}
