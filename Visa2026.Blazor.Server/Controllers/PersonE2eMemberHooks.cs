using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Required / always-visible scalar <see cref="Person"/> detail members for UI test hooks.
/// Excludes collections, computed/hidden members, optional gear-hidden scalars, and gear toggle.
/// See docs/OPTIONAL_DETAIL_FIELDS.md and visa2026-ui-test-hooks skill.
/// </summary>
internal static class PersonE2eMemberHooks
{
    private static readonly string[] SharedScalarMembers =
    [
        nameof(Person.FirstName),
        nameof(Person.LastName),
        nameof(Person.PersonalNumber),
        nameof(Person.DateOfBirth),
        nameof(Person.BirthPlace),
        nameof(Person.CountryOfBirth),
        nameof(Person.Gender),
        nameof(Person.Nationality),
        nameof(Person.ForeignAddress),
        nameof(Person.ForeignAddressCountry),
        nameof(Person.ProjectContract),
        nameof(Person.Subcontractor),
    ];

    private static readonly string[] EmployeeOnlyScalarMembers =
    [
        nameof(Person.MaritalStatus),
        nameof(Person.VisaApplicationFamilyMembersText),
    ];

    private static readonly string[] FamilyMemberOnlyScalarMembers =
    [
        nameof(Person.Relationship),
    ];

    public static readonly IReadOnlyList<string> ScalarDetailMembers =
        SharedScalarMembers
            .Concat(EmployeeOnlyScalarMembers)
            .Concat(FamilyMemberOnlyScalarMembers)
            .ToArray();

    private static readonly HashSet<string> ScalarDetailMemberSet =
        new(ScalarDetailMembers, StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ScalarMembersByDetailViewId =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [PersonDetailViewIds.Employee] = ScalarDetailMembers,
            [PersonDetailViewIds.FamilyMember] = SharedScalarMembers
                .Concat(FamilyMemberOnlyScalarMembers)
                .ToArray(),
            [PersonDetailViewIds.TemporaryVisitor] = SharedScalarMembers,
        };

    public static readonly IReadOnlyList<string> TypedDetailViewIds =
    [
        PersonDetailViewIds.Employee,
        PersonDetailViewIds.FamilyMember,
        PersonDetailViewIds.TemporaryVisitor,
    ];

    public static bool IsTypedDetailView(string? detailViewId) =>
        !string.IsNullOrEmpty(detailViewId) && ScalarMembersByDetailViewId.ContainsKey(detailViewId);

    public static IReadOnlyList<string> GetScalarMembersForDetailView(string? detailViewId)
    {
        if (!string.IsNullOrEmpty(detailViewId)
            && ScalarMembersByDetailViewId.TryGetValue(detailViewId, out IReadOnlyList<string>? members))
        {
            return members;
        }

        return ScalarDetailMembers;
    }

    public static bool IsScalarDetailMember(string? propertyName) =>
        propertyName != null && ScalarDetailMemberSet.Contains(propertyName);

    public static string TestId(string memberName) =>
        $"person-{ToKebabCase(memberName)}";

    public static string CssClass(string memberName) =>
        $"e2e-{TestId(memberName)}";

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var buffer = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0)
            {
                buffer.Append('-');
            }

            buffer.Append(char.ToLowerInvariant(c));
        }

        return buffer.ToString();
    }
}
