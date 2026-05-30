namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Stable identities for EasyTest / Visa2026EasyTest database runs.</summary>
public static class E2ETestDataSeed
{
    public const string PersonFirstName = "E2E";
    public const string PersonLastName = "Applicant";
    public const string PersonPersonalNumber = "E2E-TEST-001";
    public const string PassportNumber = "E2E-PASS-001";

    public static string PersonFullName => $"{PersonFirstName} {PersonLastName}";
}
