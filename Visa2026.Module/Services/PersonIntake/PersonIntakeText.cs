using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>Person intake wizard UI strings (<c>PersonIntake.*</c> keys in <c>UiStrings.person-intake.json</c>).</summary>
public static class PersonIntakeText
{
    public const string Prefix = "PersonIntake.";

    public static string Get(string key) => VisaUiMessages.Get(Prefix + key);

    public static string Format(string key, params object[] args) => VisaUiMessages.Format(Prefix + key, args);
}
