namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Maps legacy <c>PersonInApplication.Cancelled</c> to Visa2026 type-specific cancel flags.
/// </summary>
internal static class Visa2014ApplicationItemCancelledFlagsMapper
{
    private const string ShowInvitationItemIsCancelled = "ShowInvitationItemIsCancelled";
    private const string ShowWorkPermitItemIsCancelled = "ShowWorkPermitItemIsCancelled";
    private const string ShowVisaIsCancelled = "ShowVisaIsCancelled";

    internal const int LegacySubtypeCancelVisaAndWorkPermit = 12;
    internal const int LegacySubtypeCancelVisa = 21;
    internal const int LegacySubtypeCancelWorkPermit = 22;

    internal readonly record struct LegacyDocumentCancellationFlags(
        bool InvitationItem,
        bool WorkPermitItem,
        bool Visa);

    public static void ApplyLegacyCancelledFlags(
        IDictionary<string, object?> row,
        string? applicationTypeName,
        ApplicationTypeVisibilityCatalog visibility,
        bool legacyCancelled)
    {
        row["InvitationItemIsCancelled"] = false;
        row["IsCancelled"] = false;
        row["VisaIsCancelled"] = false;

        if (!legacyCancelled)
            return;

        var flags = ResolveDocumentCancellation(applicationTypeName, visibility, legacyCancelled: true);
        row["InvitationItemIsCancelled"] = flags.InvitationItem;
        row["IsCancelled"] = flags.WorkPermitItem;
        row["VisaIsCancelled"] = flags.Visa;
    }

    public static LegacyDocumentCancellationFlags ResolveDocumentCancellation(
        string? applicationTypeName,
        ApplicationTypeVisibilityCatalog visibility,
        bool legacyCancelled)
    {
        if (!legacyCancelled)
            return default;

        bool invitationItem = false;
        bool workPermitItem = false;
        bool visa = false;

        ApplyCancelProcedureNameHeuristics(
            applicationTypeName,
            ref invitationItem,
            ref workPermitItem,
            ref visa);
        ApplyVisibilityCatalogFlags(
            applicationTypeName,
            visibility,
            ref invitationItem,
            ref workPermitItem,
            ref visa);

        if (!invitationItem && !workPermitItem && !visa)
            workPermitItem = true;

        return new LegacyDocumentCancellationFlags(invitationItem, workPermitItem, visa);
    }

    public static LegacyDocumentCancellationFlags ResolveFromCompletedCancelSubtype(int? subtypeId) =>
        subtypeId switch
        {
            LegacySubtypeCancelVisaAndWorkPermit => new LegacyDocumentCancellationFlags(false, true, true),
            LegacySubtypeCancelVisa => new LegacyDocumentCancellationFlags(false, false, true),
            LegacySubtypeCancelWorkPermit => new LegacyDocumentCancellationFlags(false, true, false),
            _ => default,
        };

    public static LegacyDocumentCancellationFlags Merge(
        LegacyDocumentCancellationFlags left,
        LegacyDocumentCancellationFlags right) =>
        new(
            left.InvitationItem || right.InvitationItem,
            left.WorkPermitItem || right.WorkPermitItem,
            left.Visa || right.Visa);

    private static void ApplyVisibilityCatalogFlags(
        string? applicationTypeName,
        ApplicationTypeVisibilityCatalog visibility,
        ref bool invitationItem,
        ref bool workPermitItem,
        ref bool visa)
    {
        if (string.IsNullOrWhiteSpace(applicationTypeName) ||
            !visibility.TryGetFlags(applicationTypeName, out var flags))
        {
            return;
        }

        if (GetShowFlag(flags, ShowInvitationItemIsCancelled))
            invitationItem = true;

        if (GetShowFlag(flags, ShowWorkPermitItemIsCancelled))
            workPermitItem = true;

        if (GetShowFlag(flags, ShowVisaIsCancelled))
            visa = true;
    }

    private static void ApplyCancelProcedureNameHeuristics(
        string? applicationTypeName,
        ref bool invitationItem,
        ref bool workPermitItem,
        ref bool visa)
    {
        if (string.IsNullOrWhiteSpace(applicationTypeName) ||
            !applicationTypeName.StartsWith("App_Cancel", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (ContainsCancelToken(applicationTypeName, "Inv"))
            invitationItem = true;

        if (ContainsCancelToken(applicationTypeName, "Visa"))
            visa = true;

        if (ContainsCancelToken(applicationTypeName, "WP") ||
            applicationTypeName.Equals("App_Cancell_WP", StringComparison.OrdinalIgnoreCase))
        {
            workPermitItem = true;
        }
    }

    private static bool ContainsCancelToken(string applicationTypeName, string token) =>
        applicationTypeName.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static bool GetShowFlag(IReadOnlyDictionary<string, bool> flags, string flagName) =>
        flags.TryGetValue(flagName, out var show) && show;
}