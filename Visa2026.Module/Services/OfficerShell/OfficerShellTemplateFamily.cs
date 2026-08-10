using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>Template family keys aligned with HTML prototype <c>TPL_KEYS</c> (reg / inv / ext / wp).</summary>
public static class OfficerShellTemplateFamily
{
    public const string All = "all";
    public const string Registration = "reg";
    public const string Invitation = "inv";
    public const string Extension = "ext";
    public const string WorkPermit = "wp";

    public static readonly string[] DisplayOrder = { Registration, Invitation, Extension, WorkPermit };

    public static string ResolveKey(Application? application)
    {
        var profile = application?.ApplicationProfile;
        if (profile != null)
            return ResolveKey(profile);

        var typeCode = application?.ApplicationType?.Code;
        return FromCode(typeCode) ?? Invitation;
    }

    public static string ResolveKey(ApplicationProfile? profile)
    {
        if (profile == null)
            return Invitation;

        var fromCode = FromCode(profile.Code) ?? FromCode(profile.SelectionCode);
        if (fromCode != null)
            return fromCode;

        return profile.ActionFamily switch
        {
            ApplicationProfileActionFamily.Registration => Registration,
            ApplicationProfileActionFamily.BusinessTrip => WorkPermit,
            ApplicationProfileActionFamily.Cancellation => Invitation,
            _ => Invitation,
        };
    }

    public static string GetLabel(string key) => key switch
    {
        Registration => "Registration",
        Invitation => "Invitation",
        Extension => "Visa extension",
        WorkPermit => "Work permit",
        _ => "Other",
    };

    public static string GetColor(string key) => key switch
    {
        Registration => "#3b82f6",
        Invitation => "#22c55e",
        Extension => "#f59e0b",
        WorkPermit => "#a855f7",
        _ => "#94a3b8",
    };

    private static string? FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var c = code.Trim().ToLowerInvariant();
        if (c.Contains("reg", StringComparison.Ordinal))
            return Registration;
        if (c.Contains("ext", StringComparison.Ordinal) || c.Contains("extension", StringComparison.Ordinal))
            return Extension;
        if (c.Contains("wp", StringComparison.Ordinal) || c.Contains("work", StringComparison.Ordinal))
            return WorkPermit;
        if (c.Contains("inv", StringComparison.Ordinal) || c.Contains("invite", StringComparison.Ordinal))
            return Invitation;
        return null;
    }
}
