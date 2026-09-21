namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Officer-facing business object the placeholder reads. Independent of
/// <see cref="UserReportPlaceholderPack"/> (profile gating). Used to group the placeholder
/// manual, Review picker, and AI payload.
/// </summary>
public enum UserReportPlaceholderRelatedBo
{
    Unknown = 0,
    /// <summary>Legacy flat Application group; prefer ApplicationGeneral / FamilyMember / Cancellation / BusinessTrip.</summary>
    Application = 1,
    CompanyProfile = 2,
    CompanySignatory = 3,
    AuthorizedRepresentative = 4,
    Person = 5,
    Passport = 6,
    /// <summary>Legacy flat Visa group; prefer VisaLinkedActive / VisaCancel.</summary>
    Visa = 7,
    Education = 8,
    AddressOfResidence = 9,
    Position = 10,
    Salary = 11,
    Invitation = 12,
    WorkPermit = 13,
    Travel = 14,
    FamilyMember = 15,
    BorderZone = 16,
    RosterRow = 17,
    TravelHistory = 18,
    BusinessTrip = 19,
    ApplicationGeneral = 20,
    ApplicationFamilyMember = 21,
    ApplicationCancellation = 22,
    ApplicationBusinessTrip = 23,
    VisaLinkedActive = 24,
    VisaCancel = 25,
    /// <summary>Labor-contract salary and period (Zähmet şertnamasy).</summary>
    Contract = 26,
}

public static class UserReportPlaceholderRelatedBoCatalog
{
    public static int SortOrder(UserReportPlaceholderRelatedBo relatedBo) => relatedBo switch
    {
        UserReportPlaceholderRelatedBo.Unknown => int.MaxValue,
        UserReportPlaceholderRelatedBo.Application => 1,
        UserReportPlaceholderRelatedBo.ApplicationGeneral => 2,
        UserReportPlaceholderRelatedBo.ApplicationFamilyMember => 3,
        UserReportPlaceholderRelatedBo.ApplicationCancellation => 4,
        UserReportPlaceholderRelatedBo.ApplicationBusinessTrip => 5,
        UserReportPlaceholderRelatedBo.BusinessTrip => 6,
        UserReportPlaceholderRelatedBo.VisaLinkedActive => 107,
        UserReportPlaceholderRelatedBo.VisaCancel => 108,
        UserReportPlaceholderRelatedBo.Visa => 109,
        UserReportPlaceholderRelatedBo.Position => 110,
        UserReportPlaceholderRelatedBo.Contract => 111,
        UserReportPlaceholderRelatedBo.Salary => 112,
        _ => 100 + (int)relatedBo,
    };

    public static string DisplayNameEn(UserReportPlaceholderRelatedBo relatedBo) => relatedBo switch
    {
        UserReportPlaceholderRelatedBo.Application => "Application",
        UserReportPlaceholderRelatedBo.ApplicationGeneral => "Application — general",
        UserReportPlaceholderRelatedBo.ApplicationFamilyMember => "Application — family member",
        UserReportPlaceholderRelatedBo.ApplicationCancellation => "Application — cancellation",
        UserReportPlaceholderRelatedBo.ApplicationBusinessTrip => "Application — business trip",
        UserReportPlaceholderRelatedBo.CompanyProfile => "Company",
        UserReportPlaceholderRelatedBo.CompanySignatory => "Authorized signatory",
        UserReportPlaceholderRelatedBo.AuthorizedRepresentative => "Authorized representative (wekil)",
        UserReportPlaceholderRelatedBo.Person => "Person",
        UserReportPlaceholderRelatedBo.Passport => "Passport",
        UserReportPlaceholderRelatedBo.Visa => "Visa",
        UserReportPlaceholderRelatedBo.VisaLinkedActive => "Visa — linked active",
        UserReportPlaceholderRelatedBo.VisaCancel => "Visa — cancel",
        UserReportPlaceholderRelatedBo.Education => "Education",
        UserReportPlaceholderRelatedBo.AddressOfResidence => "Address of residence",
        UserReportPlaceholderRelatedBo.Position => "Position",
        UserReportPlaceholderRelatedBo.Contract => "Contract",
        UserReportPlaceholderRelatedBo.Salary => "Salary",
        UserReportPlaceholderRelatedBo.Invitation => "Invitation",
        UserReportPlaceholderRelatedBo.WorkPermit => "Work permit",
        UserReportPlaceholderRelatedBo.Travel => "Travel",
        UserReportPlaceholderRelatedBo.FamilyMember => "Family member",
        UserReportPlaceholderRelatedBo.BorderZone => "Border zone",
        UserReportPlaceholderRelatedBo.RosterRow => "Roster row",
        UserReportPlaceholderRelatedBo.TravelHistory => "Travel history",
        UserReportPlaceholderRelatedBo.BusinessTrip => "Business trip",
        _ => relatedBo.ToString(),
    };

    public static string LocalizationKey(UserReportPlaceholderRelatedBo relatedBo) =>
        "PlaceholderManual.Group." + relatedBo;

    public static IReadOnlyList<UserReportPlaceholderCatalogGroup> Group(
        IEnumerable<UserReportPlaceholderCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries
            .GroupBy(static e => e.RelatedBo)
            .OrderBy(static g => SortOrder(g.Key))
            .Select(static g => new UserReportPlaceholderCatalogGroup
            {
                RelatedBo = g.Key,
                Entries = g
                    .OrderBy(static e => e.ShortCode, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            })
            .ToList();
    }
}