using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Later life of an issued work-permit item. No invitation-style Used bucket.
/// Cancelled wins, then changed, then expired, else in force (Güýjünde).
/// </summary>
public static class WorkPermitItemLaterLife
{
    public const string InForce = "workPermitInForce";
    public const string Expired = "workPermitExpired";
    public const string Cancelled = "workPermitCancelled";
    public const string Changed = "workPermitChanged";

    public static string? Resolve(bool cancelled, bool changed, bool expired)
    {
        if (cancelled)
            return Cancelled;
        if (changed)
            return Changed;
        if (expired)
            return Expired;
        return null;
    }

    public static bool IsExpired(DateTime? expirationDate) =>
        ExpirationLogicHelper.IsExpired(expirationDate);

    public static bool IsExpired(DateTime expirationDate) =>
        ExpirationLogicHelper.IsExpired((DateTime?)expirationDate);
}