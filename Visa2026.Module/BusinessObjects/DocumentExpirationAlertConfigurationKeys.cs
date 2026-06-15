using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Document families editable under Configuration → document expiration alerts.
/// Other <see cref="ExpirationAlertRule"/> rows (e.g. BorderZone) remain seeded for runtime only.
/// </summary>
public static class DocumentExpirationAlertConfigurationKeys
{
    public const string Passport = ExpirationAlertBusinessObjectKeys.Passport;
    public const string Visa = ExpirationAlertBusinessObjectKeys.Visa;
    public const string WorkPermitItem = ExpirationAlertBusinessObjectKeys.WorkPermitItem;
    public const string AddressOfResidence = ExpirationAlertBusinessObjectKeys.AddressOfResidence;
    public const string MedicalRecord = ExpirationAlertBusinessObjectKeys.MedicalRecord;
    public const string Invitation = ExpirationAlertBusinessObjectKeys.Invitation;

    public static IReadOnlyList<string> All { get; } =
    [
        Passport,
        Visa,
        WorkPermitItem,
        AddressOfResidence,
        MedicalRecord,
        Invitation
    ];

    public static bool SupportsExtensionApplicationRequiredDays(string? businessObjectKey) =>
        businessObjectKey == Visa || businessObjectKey == WorkPermitItem;

    public static string ListViewCriteria =>
        string.Join(" Or ", All.Select(key => $"{nameof(ExpirationAlertRule.BusinessObjectKey)} = '{key}'"));
}
