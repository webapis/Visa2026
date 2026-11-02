using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// One later life of an issued invitation. A person shows at most one:
/// cancelled, otherwise changed, otherwise used, otherwise expired and never used.
/// The invitation circle keeps its letters and takes this state's color.
/// </summary>
public static class InvitationLaterLife
{
    public const string Used = "invitationUsed";
    public const string ExpiredUnused = "invitationExpiredUnused";
    public const string Cancelled = "invitationCancelled";
    public const string Changed = "invitationChanged";

    public const string ToneUsed = "used";
    public const string ToneExpired = "expired";
    public const string ToneCancelled = "cancelled";
    public const string ToneChanged = "changed";

    /// <summary>
    /// Cancelled wins, then changed, then used, then expired while unused.
    /// A used invitation is not marked expired.
    /// </summary>
    public static string? Resolve(bool cancelled, bool changed, bool used, bool expired)
    {
        if (cancelled)
            return Cancelled;
        if (changed)
            return Changed;
        if (used)
            return Used;
        if (expired)
            return ExpiredUnused;
        return null;
    }

    public static string? Strongest(IEnumerable<string?> states)
    {
        string? best = null;
        var rank = 0;
        foreach (var state in states)
        {
            var next = Rank(state);
            if (next <= rank)
                continue;
            rank = next;
            best = state;
        }

        return best;
    }

    public static string Tone(string key) => key switch
    {
        Used => ToneUsed,
        ExpiredUnused => ToneExpired,
        Cancelled => ToneCancelled,
        Changed => ToneChanged,
        _ => ToneExpired,
    };

    public static bool IsExpired(DateTime? expirationDate) =>
        ExpirationLogicHelper.IsExpired(expirationDate);

    private static int Rank(string? state) => state switch
    {
        Cancelled => 4,
        Changed => 3,
        Used => 2,
        ExpiredUnused => 1,
        _ => 0,
    };
}
