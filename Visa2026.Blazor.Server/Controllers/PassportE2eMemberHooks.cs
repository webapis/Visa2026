using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Required / always-visible scalar <see cref="Passport"/> detail members for UI test hooks.
/// Excludes optional gear-hidden scalars, collections, computed members, and <see cref="Passport.Person"/>
/// when pre-filled from nested collection New.
/// </summary>
internal static class PassportE2eMemberHooks
{
    public const string DetailViewId = "Passport_DetailView";

    private static readonly string[] ScalarDetailMembers =
    [
        nameof(Passport.PassportNumber),
        nameof(Passport.PassportType),
        nameof(Passport.IssueDate),
        nameof(Passport.ExpirationDate),
        nameof(Passport.Authority),
        nameof(Passport.IssuedCountry),
    ];

    private static readonly HashSet<string> ScalarDetailMemberSet =
        new(ScalarDetailMembers, StringComparer.Ordinal);

    public static IReadOnlyList<string> GetScalarMembers() => ScalarDetailMembers;

    public static bool IsScalarDetailMember(string? propertyName) =>
        propertyName != null && ScalarDetailMemberSet.Contains(propertyName);

    public static string TestId(string memberName) =>
        $"passport-{ToKebabCase(memberName)}";

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
