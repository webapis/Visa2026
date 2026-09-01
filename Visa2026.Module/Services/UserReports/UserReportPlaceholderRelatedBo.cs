namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Officer-facing business object the placeholder reads. Independent of
/// <see cref="UserReportPlaceholderPack"/> (profile gating). Used to group the placeholder
/// manual, Review picker, and AI payload.
/// </summary>
public enum UserReportPlaceholderRelatedBo
{
    Unknown = 0,
    Application = 1,
    CompanyProfile = 2,
    CompanySignatory = 3,
    AuthorizedRepresentative = 4,
    Person = 5,
    Passport = 6,
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
}

public static class UserReportPlaceholderRelatedBoCatalog
{
    public static int SortOrder(UserReportPlaceholderRelatedBo relatedBo) =>
        relatedBo == UserReportPlaceholderRelatedBo.Unknown
            ? int.MaxValue
            : (int)relatedBo;

    public static string DisplayNameEn(UserReportPlaceholderRelatedBo relatedBo) => relatedBo switch
    {
        UserReportPlaceholderRelatedBo.Application => "Application",
        UserReportPlaceholderRelatedBo.CompanyProfile => "Company",
        UserReportPlaceholderRelatedBo.CompanySignatory => "Signatory",
        UserReportPlaceholderRelatedBo.AuthorizedRepresentative => "Authorized representative (wekil)",
        UserReportPlaceholderRelatedBo.Person => "Person",
        UserReportPlaceholderRelatedBo.Passport => "Passport",
        UserReportPlaceholderRelatedBo.Visa => "Visa",
        UserReportPlaceholderRelatedBo.Education => "Education",
        UserReportPlaceholderRelatedBo.AddressOfResidence => "Address of residence",
        UserReportPlaceholderRelatedBo.Position => "Position",
        UserReportPlaceholderRelatedBo.Salary => "Salary",
        UserReportPlaceholderRelatedBo.Invitation => "Invitation",
        UserReportPlaceholderRelatedBo.WorkPermit => "Work permit",
        UserReportPlaceholderRelatedBo.Travel => "Travel",
        UserReportPlaceholderRelatedBo.FamilyMember => "Family member",
        UserReportPlaceholderRelatedBo.BorderZone => "Border zone",
        UserReportPlaceholderRelatedBo.RosterRow => "Roster row",
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