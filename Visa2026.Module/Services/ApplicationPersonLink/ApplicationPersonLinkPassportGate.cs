using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonLink;

/// <summary>
/// Officer Link existing person: require at least one usable passport.
/// Expired previous booklets stay auto-linkable after the person is on the case (Last-N).
/// </summary>
public static class ApplicationProfileInstancePersonLinkPassportGate
{
    public const string NoPassport = "No passport";
    public const string PassportCancelled = "Passport is cancelled";
    public const string PassportExpired = "Passport is expired";
    public const string PassportInvalid = "No valid passport";

    public static bool TryGetBlockReason(Person? person, out string reason)
    {
        var passports = person?.Passports;
        if (passports == null || passports.Count == 0)
        {
            reason = NoPassport;
            return true;
        }

        var sawCancelled = false;
        var sawExpired = false;
        var sawIncomplete = false;

        foreach (var passport in passports)
        {
            if (passport == null)
                continue;

            if (passport.IsCancelled)
            {
                sawCancelled = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(passport.PassportNumber))
            {
                sawIncomplete = true;
                continue;
            }

            if (ExpirationLogicHelper.IsExpired(passport))
            {
                sawExpired = true;
                continue;
            }

            reason = string.Empty;
            return false;
        }

        if (sawExpired)
            reason = PassportExpired;
        else if (sawCancelled)
            reason = PassportCancelled;
        else
            reason = sawIncomplete ? PassportInvalid : NoPassport;

        return true;
    }
}