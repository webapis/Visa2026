#nullable enable
namespace Visa2026.Blazor.Server.Components;

/// <summary>
/// Compose-slot field border cues: orange = empty required, blue = system default to review,
/// green = officer confirmed (blur / change), sourced = read-only from person or computed.
/// </summary>
internal static class IssueComposeFieldCue
{
    public const string Needs = "issue-issued-header-slot__field--needs";
    public const string Default = "issue-issued-header-slot__field--default";
    public const string Confirmed = "issue-issued-header-slot__field--confirmed";
    public const string Sourced = "issue-issued-header-slot__field--sourced";

    public static string FieldClass(
        bool required,
        bool hasValue,
        bool isDefault,
        bool reviewed,
        bool sourced = false)
    {
        if (sourced)
            return hasValue ? Sourced : (required ? Needs : string.Empty);
        if (!hasValue)
            return required ? Needs : string.Empty;
        if (reviewed || !isDefault)
            return Confirmed;
        return Default;
    }

    public static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

    public static bool HasId(Guid? id) => id is Guid g && g != Guid.Empty;

    public static bool HasDate(DateTime value) => value != default;

    public static bool HasDate(DateTime? value) => value is DateTime dt && dt != default;
}